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
    private Difficulty _difficulty = Difficulty.Normal;
    private SimInput? _lastInput;
    private SimResult? _lastResult;

    public override void _Ready()
    {
        Theme = AppTheme.Build();
        AddChild(AppTheme.CreateBackdrop());

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
        view.Load(_guild, _difficulty,
            onStartRaid: StartRaid, onCycleDifficulty: CycleDifficulty, onRest: Rest, onRecruit: Recruit, onSave: SaveGuild);
        Swap(view);
    }

    private void Rest()
    {
        _guild = Downtime.Rest(_guild);
        _saves.Save(_guild);
        GD.Print("the guild rested; injuries recovered a step");
        ShowRoster();
    }

    private void Recruit()
    {
        if (Recruitment.TryHire(_guild, (ulong)DateTime.UtcNow.Ticks, out GuildSave updated))
        {
            _guild = updated;
            _saves.Save(_guild);
            GD.Print($"recruited a raider (-{Recruitment.Cost} gold); roster now {_guild.Roster.Count}");
        }
        else
        {
            GD.Print("not enough gold to recruit");
        }

        ShowRoster();
    }

    private void CycleDifficulty()
    {
        _difficulty = _difficulty switch
        {
            Difficulty.Normal => Difficulty.Heroic,
            Difficulty.Heroic => Difficulty.Mythic,
            _ => Difficulty.Normal,
        };
        ShowRoster();
    }

    private void StartRaid(EncounterDef encounter)
    {
        EncounterDef scaled = Difficulties.Scale(encounter, _difficulty);
        var raid = new RaidSetup(_guild.Roster.Select(ToCombatant).ToList());
        ulong seed = (ulong)DateTime.UtcNow.Ticks; // a fresh fight each time
        var input = new SimInput(new SeededRng(seed), SimConfig.Default, raid, scaled);
        SimResult result = Simulator.SimulateEncounter(input);

        (GuildSave updated, RaidSummary summary) = RaidResolver.Resolve(_guild, result, input.Encounter, seed);
        _guild = updated;
        _saves.Save(_guild); // auto-save the outcome
        GD.Print($"raid {result.Outcome} vs {scaled.Name}: +{summary.GoldAwarded} gold (now {_guild.Economy.Gold}); saved");

        _lastInput = input;
        _lastResult = result;
        ShowStage();
    }

    // Two renderers, one precomputed stream — you toggle between them (the M2 floor).
    private void ShowStage()
    {
        if (_lastInput is null || _lastResult is null)
        {
            ShowRoster();
            return;
        }

        var view = new StageView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(_lastInput, _lastResult, onBack: ShowRoster, onSwitch: ShowLog);
        Swap(view);
    }

    private void ShowLog()
    {
        if (_lastInput is null || _lastResult is null)
        {
            ShowRoster();
            return;
        }

        var view = new CombatView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(_lastInput, _lastResult, onBack: ShowRoster, onSwitch: ShowStage);
        Swap(view);
    }

    private void SaveGuild()
    {
        _saves.Save(_guild);
        GD.Print("guild saved");
    }

    // Project a persistent roster member into a combat combatant (class kit + base stats + equipped gear).
    private static CombatantSpec ToCombatant(RaiderRecord raider) => Warband.ToCombatant(raider);

    private void Swap(Control view)
    {
        _current?.QueueFree();
        _current = view;
        AddChild(view);
    }
}
