using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Game;

/// <summary>Thrown when a save blob is missing, corrupt, or fails validation — never crashes the caller blind.</summary>
public sealed class SaveException : Exception
{
    public SaveException()
    {
    }

    public SaveException(string message)
        : base(message)
    {
    }

    public SaveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// (De)serializes the save aggregate as JSON, and runs the load pipeline: parse → read version → fold
/// migrations → validate against the current schema (validation only at this boundary, never in hot
/// paths). See save-format.md §Migration registry.
/// </summary>
public static class SaveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(GuildSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        return JsonSerializer.Serialize(save, Options);
    }

    public static GuildSave Load(string json)
    {
        JsonNode root;
        try
        {
            root = JsonNode.Parse(json) ?? throw new SaveException("save is empty");
        }
        catch (JsonException ex)
        {
            throw new SaveException("save is not valid JSON", ex);
        }

        int version = root["version"]?.GetValue<int>() ?? throw new SaveException("save has no version");
        root = Fold(root, version);

        GuildSave save = root.Deserialize<GuildSave>(Options) ?? throw new SaveException("save could not be read");
        Validate(save);
        return save;
    }

    private static JsonNode Fold(JsonNode root, int fromVersion)
    {
        JsonNode current = root;
        foreach (Migration migration in SaveMigrations.All.Where(m => m.From >= fromVersion).OrderBy(m => m.From))
        {
            current = migration.Migrate(current);
        }

        return current;
    }

    private static void Validate(GuildSave save)
    {
        if (save.Version != SaveMigrations.CurrentVersion)
        {
            throw new SaveException($"save version {save.Version} was not migrated to {SaveMigrations.CurrentVersion}");
        }

        if (save.Roster is null)
        {
            throw new SaveException("save has no roster");
        }
    }
}
