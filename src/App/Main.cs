using System;
using System.Globalization;
using System.Linq;
using Content;
using Engine;
using Game;
using Godot;

namespace App;

/// <summary>
/// The app coordinator (m1-build-plan step 10, first cut). Loads or creates the guild save, then walks
/// the raid-night loop: roster → Start Raid (project the roster into combatants, run the real engine) →
/// watch the playback → back → Save. The App owns the wall-clock and file paths; the deterministic
/// libraries never do.
/// </summary>
public partial class Main : Control
{
    private SaveService _saves = null!;
    private GuildSave _guild = null!;
    private Control? _current;

    public override void _Ready()
    {
        string path = ProjectSettings.GlobalizePath("user://saves/guild.json");
        _saves = new SaveService(new FileStorageAdapter(path));
        _guild = LoadOrCreateGuild();

        GD.Print($"guild '{_guild.Guild.Name}' roster={_guild.Roster.Count} gold={_guild.Economy.Gold}");
        ShowRoster();
    }

    private GuildSave LoadOrCreateGuild()
    {
        try
        {
            GuildSave? loaded = _saves.Load();
            if (loaded is not null)
            {
                return loaded;
            }
        }
        catch (SaveException ex)
        {
            GD.PushWarning($"save unreadable, starting a fresh guild: {ex.Message}");
        }

        // The app supplies the seed + timestamp (Game never reads wall-clock — the determinism guard).
        ulong seed = (ulong)DateTime.UtcNow.Ticks;
        string createdAtIso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        return Guilds.CreateStarter("The Founders", seed, createdAtIso);
    }

    private void ShowRoster()
    {
        var view = new RosterView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(_guild, onStartRaid: StartRaid, onSave: SaveGuild);
        Swap(view);
    }

    private void StartRaid()
    {
        var raid = new RaidSetup(_guild.Roster.Select(ToCombatant).ToList());
        var input = new SimInput(new SeededRng(1), SimConfig.Default, raid, Encounters.Warden);
        SimResult result = Simulator.SimulateEncounter(input);
        GD.Print($"raid vs {input.Encounter.Name}: {result.Outcome} (hash {result.Hash()})");

        var view = new CombatView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(input, result, onBack: ShowRoster);
        Swap(view);
    }

    private void SaveGuild()
    {
        _saves.Save(_guild);
        GD.Print("guild saved");
    }

    // Project a persistent roster member into a combat combatant via the class factory.
    private static CombatantSpec ToCombatant(RaiderRecord raider) =>
        Roster.CreateRaider(Classes.Registry.Get(raider.ClassId), raider.Id, raider.Name);

    private void Swap(Control view)
    {
        _current?.QueueFree();
        _current = view;
        AddChild(view);
    }
}
