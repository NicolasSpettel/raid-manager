using System;
using System.IO;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// Save/load is the career's memory: deterministic creation, a lossless round-trip, atomic writes, and
/// a historical fixture that round-trips through the migrate+validate pipeline (save-format.md).
/// </summary>
public class SaveTests
{
    private const string Iso = "2026-01-01T00:00:00Z";

    [Fact]
    public void CreateStarter_IsDeterministic()
    {
        GuildSave a = Guilds.CreateStarter("Guild", 1, Iso);
        GuildSave b = Guilds.CreateStarter("Guild", 1, Iso);

        Assert.Equal(SaveSerializer.Serialize(a), SaveSerializer.Serialize(b));
    }

    [Fact]
    public void CreateStarter_ProducesARosterFromTheClasses()
    {
        GuildSave save = Guilds.CreateStarter("Ironforge", 1, Iso, rosterSize: 8);

        Assert.Equal(8, save.Roster.Count);
        Assert.All(save.Roster, r => Assert.False(string.IsNullOrWhiteSpace(r.ClassId)));
        Assert.Equal(1000, save.Economy.Gold);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        GuildSave original = Guilds.CreateStarter("Ironforge", 7, Iso);

        string json = SaveSerializer.Serialize(original);
        GuildSave loaded = SaveSerializer.Load(json);

        // Records with list members don't ==; compare via canonical serialization + key fields.
        Assert.Equal(json, SaveSerializer.Serialize(loaded));
        Assert.Equal(original.Guild.Name, loaded.Guild.Name);
        Assert.Equal(original.Roster.Count, loaded.Roster.Count);
    }

    [Fact]
    public void FileStorage_WritesAtomically_AndRoundTrips()
    {
        string dir = Path.Combine(Path.GetTempPath(), "rm-savetest-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(dir, "guild.json");
        try
        {
            var service = new SaveService(new FileStorageAdapter(path));
            Assert.Null(service.Load()); // nothing saved yet

            GuildSave save = Guilds.CreateStarter("Ironforge", 3, Iso);
            service.Save(save);
            service.Save(save); // second write exercises the temp → replace → .bak path

            GuildSave? loaded = service.Load();
            Assert.NotNull(loaded);
            Assert.Equal(save.Guild.Name, loaded!.Guild.Name);
            Assert.True(File.Exists(path + ".bak"));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void LoadingCorruptOrVersionlessJson_ThrowsSaveException()
    {
        Assert.Throws<SaveException>(() => SaveSerializer.Load("{ not json"));
        Assert.Throws<SaveException>(() => SaveSerializer.Load("{}")); // no version field
    }

    [Fact]
    public void HistoricalFixture_v1_RoundTripsThroughThePipeline()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "fixtures", "save-v1.json");
        GuildSave save = SaveSerializer.Load(File.ReadAllText(path));

        Assert.Equal(1, save.Version);
        Assert.Equal("The Founders", save.Guild.Name);
        Assert.Equal(2, save.Roster.Count);
        Assert.Equal("guardian", save.Roster[0].ClassId);
    }
}
