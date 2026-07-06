using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Game;

/// <summary>One save-format migration: transforms the untyped JSON from <see cref="From"/> to From+1.</summary>
public sealed record Migration(int From, string Note, Func<JsonNode, JsonNode> Migrate);

/// <summary>
/// The migration registry — the antidote to DM1's ad-hoc <c>normalizeSavedRunState</c> (six inlined
/// migrations with the version stuck at 1). Every breaking save change bumps
/// <see cref="CurrentVersion"/> and appends one ordered, documented migration; a frozen fixture of the
/// old version round-trips in CI (save-format.md §Migration registry).
/// </summary>
public static class SaveMigrations
{
    /// <summary>The current save format version. Bumped by every breaking change.</summary>
    public const int CurrentVersion = 2;

    /// <summary>Ordered {from → from+1} migrations, applied in sequence at load.</summary>
    public static IReadOnlyList<Migration> All { get; } = new List<Migration>
    {
        new(1, "v2: add per-raider level/xp and a raid-history log", MigrateV1ToV2),
    };

    // v1 saves have no progression or history; add sensible defaults so nothing is lost.
    private static JsonNode MigrateV1ToV2(JsonNode node)
    {
        JsonObject obj = node.AsObject();
        obj["version"] = 2;
        obj["history"] ??= new JsonArray();

        if (obj["roster"] is JsonArray roster)
        {
            foreach (JsonNode? entry in roster)
            {
                if (entry is JsonObject raider)
                {
                    raider["level"] ??= 1;
                    raider["xp"] ??= 0;
                }
            }
        }

        return node;
    }
}
