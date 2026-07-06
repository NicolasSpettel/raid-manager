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
    private World? _pendingWorld; // this career's living world, generated at New Career, used for job offers
    private string? _lastWeekSummary; // the just-advanced week's result, shown on the Calendar tab

    public override void _Ready()
    {
        Theme = AppTheme.Build();
        AddChild(AppTheme.CreateBackdrop());

        string path = ProjectSettings.GlobalizePath("user://saves/guild.json");
        _saves = new SaveService(new FileStorageAdapter(path));

        ShowWelcome();
    }

    // The opening screen (GDD §1): Continue loads the saved career; New Career walks manager creation.
    private void ShowWelcome()
    {
        GuildSave? existing = TryLoad();
        var view = new WelcomeView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(
            hasSave: existing is not null,
            onContinue: () =>
            {
                if (existing is not null)
                {
                    _guild = existing;
                    if (existing.WorldSeed != 0) // regenerate this career's living world from its pinned seed
                    {
                        _pendingWorld = WorldGen.Generate(existing.WorldSeed);
                    }

                    ShowHome();
                }
            },
            onNewCareer: StartNewCareer,
            onQuit: () => GetTree().Quit());
        Swap(view);
    }

    // New Career: generate this career's living world once, then walk manager creation.
    private void StartNewCareer()
    {
        _pendingWorld = WorldGen.Generate((ulong)DateTime.UtcNow.Ticks);
        ShowManagerCreation();
    }

    private void ShowManagerCreation()
    {
        var view = new ManagerCreationView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(onCreate: ShowJobOffers, onBack: ShowWelcome);
        Swap(view);
    }

    // Getting the job (GDD §4): a fresh manager is offered struggling low-prestige guilds from the world.
    private void ShowJobOffers(Manager manager)
    {
        World world = _pendingWorld ?? WorldGen.Generate((ulong)DateTime.UtcNow.Ticks);
        _pendingWorld = world;
        var view = new JobOffersView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(
            JobMarket.OffersFor(world, count: 8),
            onSelect: offer => ShowNegotiation(manager, offer),
            onBack: ShowManagerCreation);
        Swap(view);
    }

    // Contract talks (GDD §4): push for terms, then sign or walk.
    private void ShowNegotiation(Manager manager, JobOffer offer)
    {
        var view = new NegotiationView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(
            offer, manager, seed: (ulong)DateTime.UtcNow.Ticks,
            onSign: state => ShowGuildIntro(manager, offer, state),
            onWalkAway: () => ShowJobOffers(manager));
        Swap(view);
    }

    // Meet the guild you just signed with (GDD §3): its past and your brief, then take the reins.
    private void ShowGuildIntro(Manager manager, JobOffer offer, NegotiationState terms)
    {
        World world = _pendingWorld!;
        Guild guild = world.Guilds.First(g => g.Id == offer.GuildId);
        var view = new GuildIntroView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(
            guild.Name, guild.Region, GuildLore.For(world, guild), terms.ExpectationText,
            onEnter: () => EnterGuild(manager, offer, terms));
        Swap(view);
    }

    private void EnterGuild(Manager manager, JobOffer offer, NegotiationState terms)
    {
        string createdAtIso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        GuildSave guild = JobMarket.Take(_pendingWorld!, offer.GuildId, manager, createdAtIso);
        guild = guild with
        {
            Guild = guild.Guild with { BoardExpectation = terms.ExpectationText },
            WorldSeed = _pendingWorld!.Seed,                 // persist the living world (ADR-0007) …
            GeneratorVersion = WorldGen.GeneratorVersion,    // … pinned to the generator that made it …
            ManagerGuildId = offer.GuildId.Value,            // … and which guild in it you manage.
            SeasonWeek = 1,
        };

        if (manager.BackgroundId == "rich_sponsor") // the sponsor's perk: extra starting funds
        {
            guild = guild with { Economy = new Economy(guild.Economy.Gold + 3000) };
        }

        _guild = guild;
        _saves.Save(_guild);
        ShowHome();
    }

    private GuildSave? TryLoad()
    {
        try
        {
            return _saves.Load();
        }
        catch (SaveException ex)
        {
            GD.PushWarning($"save unreadable: {ex.Message}");
            return null;
        }
    }

    // The home hub with tab navigation.
    private void ShowHome(string tab = "Home")
    {
        var view = new HomeShell();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(_guild, _difficulty,
            onStartRaid: StartRaid, onCycleDifficulty: CycleDifficulty, onRest: Rest, onRecruit: Recruit, onSave: SaveGuild,
            onRaider: ShowRaider, onMenu: ShowWelcome, onAdvanceWeek: AdvanceWeek, lastWeekSummary: _lastWeekSummary, initialTab: tab);
        Swap(view);
    }

    // Run one planned week through the real season loop (GDD §5/§6): raids + activities + condition/morale/injury.
    private void AdvanceWeek(WeekSchedule schedule)
    {
        int week = _guild.SeasonWeek;
        SeasonSchedule calendar = SeasonSchedule.Build(12);
        ulong seed = unchecked(_guild.WorldSeed + ((ulong)week * 0x9E3779B1UL));
        bool holidayGranted = calendar.HolidayIn(week) is not null; // grant any holiday (deny = a morale hit, later)

        WeekResult result = WeekExecutor.Run(
            _guild, schedule, Encounters.All, week, Lockout.Empty, _difficulty, seed, calendar, holidayGranted);

        _guild = result.Guild with { SeasonWeek = week + 1 };
        _saves.Save(_guild);

        string frontier = result.FurthestBossIndex >= 0 ? Encounters.All[result.FurthestBossIndex].Name : "no boss";
        _lastWeekSummary =
            $"Week {week}: {result.Kills} kills (reached {frontier}), +{result.GearDrops} gear, {result.TrainingSessions} trained, {result.Injured} hurt.";
        ShowHome("Calendar");
    }

    private void ShowRaider(RaiderRecord raider)
    {
        var view = new RaiderView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(raider, onBack: () => ShowHome("Squad"));
        Swap(view);
    }

    private void Rest()
    {
        _guild = Downtime.Rest(_guild);
        _saves.Save(_guild);
        GD.Print("the guild rested; injuries recovered a step");
        ShowHome("Squad");
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

        ShowHome("Squad");
    }

    private void CycleDifficulty()
    {
        _difficulty = _difficulty switch
        {
            Difficulty.Normal => Difficulty.Heroic,
            Difficulty.Heroic => Difficulty.Mythic,
            _ => Difficulty.Normal,
        };
        ShowHome("Squad");
    }

    private void StartRaid(EncounterDef encounter)
    {
        EncounterDef scaled = Difficulties.Scale(encounter, _difficulty);
        var raid = new RaidSetup(Formation.Place(_guild.Roster.Select(ToCombatant).ToList()));
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
            ShowHome("Squad");
            return;
        }

        var view = new StageView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(_lastInput, _lastResult, onBack: () => ShowHome("Squad"), onSwitch: ShowLog);
        Swap(view);
    }

    private void ShowLog()
    {
        if (_lastInput is null || _lastResult is null)
        {
            ShowHome("Squad");
            return;
        }

        var view = new CombatView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(_lastInput, _lastResult, onBack: () => ShowHome("Squad"), onSwitch: ShowStage);
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
