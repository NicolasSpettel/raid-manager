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
    private string? _lastWeekSummary; // the just-advanced day/week's result, shown on the Calendar tab
    private const int SeasonWeeks = 12;

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
            YouthProspects = YouthProgram.Intake(_pendingWorld!.Seed, seasonNumber: 1, offer.GuildId.Value), // your first academy intake
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
            onRaider: ShowRaider, onMenu: ShowWelcome, onSimulateDay: SimulateDay, onSimulateWeek: SimulateWeek,
            onSignFreeAgent: SignFreeAgent, onPromoteYouth: PromoteYouth, world: _pendingWorld,
            lastWeekSummary: _lastWeekSummary, initialTab: tab);
        Swap(view);
    }

    // Simulate the current day of the 4-slot calendar (GDD §6), advancing the clock.
    private void SimulateDay(IReadOnlyList<IReadOnlyList<ActivityType>> weekPlan)
    {
        int ranDay = _guild.SeasonDay;
        (_guild, DayResult result, IReadOnlyList<string> seasonEvents) = StepDay(_guild, weekPlan);
        _saves.Save(_guild);
        _lastWeekSummary =
            $"{(Weekday)ranDay}: {result.Kills} kills, +{result.GearDrops} gear, {result.TrainingSessions} trained, {result.Injured} hurt."
            + SeasonNote(seasonEvents);
        ShowHome("Calendar");
    }

    private static string SeasonNote(IReadOnlyList<string> seasonEvents) =>
        seasonEvents.Count == 0 ? string.Empty : "   ★ New season! " + string.Join("  ", seasonEvents.Take(3));

    // Fast-forward through non-raid days, stopping right before the next raid day (rolling into the next
    // week if this one has none) — FM's "continue until something matters", so you never auto-play a raid.
    private void SimulateWeek(IReadOnlyList<IReadOnlyList<ActivityType>> weekPlan)
    {
        int kills = 0, gear = 0, trained = 0, hurt = 0, days = 0;
        var seasonEvents = new System.Collections.Generic.List<string>();
        while (!weekPlan[_guild.SeasonDay].Contains(ActivityType.Raid) && days < 366) // stop at the next raid day (bounded)
        {
            (_guild, DayResult result, IReadOnlyList<string> rolled) = StepDay(_guild, weekPlan);
            kills += result.Kills;
            gear += result.GearDrops;
            trained += result.TrainingSessions;
            hurt += result.Injured;
            seasonEvents.AddRange(rolled);
            days++;
        }

        _saves.Save(_guild);
        _lastWeekSummary = (days == 0
            ? "A raid day — set the day's plan, then Simulate day to run it."
            : $"Advanced {days} day(s): {kills} kills, +{gear} gear, {trained} trained, {hurt} hurt.")
            + SeasonNote(seasonEvents);
        ShowHome("Calendar");
    }

    // Run the current day and advance the clock. The weekly reset clears the lockout; the season boundary
    // (past week 12) rolls to a new season and ages the roster (GDD §8).
    private (GuildSave Guild, DayResult Result, IReadOnlyList<string> SeasonEvents) StepDay(
        GuildSave guild, IReadOnlyList<IReadOnlyList<ActivityType>> weekPlan)
    {
        int week = guild.SeasonWeek;
        int day = guild.SeasonDay;
        IReadOnlyList<string> downed = guild.DownedThisWeek ?? System.Array.Empty<string>();
        ulong seed = unchecked(guild.WorldSeed + ((ulong)(((guild.SeasonNumber * 10000) + (week * 100)) + day) * 0x9E3779B1UL));

        DayResult result = DayExecutor.RunDay(guild, weekPlan[day], Encounters.All, week, day, downed, _difficulty, seed);
        guild = result.Guild with { DownedThisWeek = result.DownedThisWeek };

        IReadOnlyList<string> seasonEvents = System.Array.Empty<string>();
        if (day < 6)
        {
            guild = guild with { SeasonDay = day + 1 };
        }
        else if (week < SeasonWeeks)
        {
            guild = guild with { SeasonWeek = week + 1, SeasonDay = 0, DownedThisWeek = System.Array.Empty<string>() };
        }
        else
        {
            int nextSeason = guild.SeasonNumber + 1; // season boundary → age the roster, start season N+1
            AgingResult aged = Aging.AdvanceSeason(guild, nextSeason);
            IReadOnlyList<RaiderRecord> intake = guild.ManagerGuildId is { } gid
                ? YouthProgram.Intake(guild.WorldSeed, nextSeason, gid) // a fresh academy intake each season (GDD §10)
                : System.Array.Empty<RaiderRecord>();
            guild = aged.Guild with
            {
                SeasonNumber = nextSeason, SeasonWeek = 1, SeasonDay = 0,
                DownedThisWeek = System.Array.Empty<string>(), YouthProspects = intake,
            };
            seasonEvents = aged.Events;
        }

        return (guild, result, seasonEvents);
    }

    private void ShowRaider(RaiderRecord raider)
    {
        // On your roster → back to the squad; a scouted prospect/free agent → back to the transfer market.
        string origin = _guild.Roster.Any(r => r.Id == raider.Id) ? "Squad" : "Transfers";
        var view = new RaiderView();
        view.SetAnchorsPreset(LayoutPreset.FullRect);
        view.Load(raider, _guild.SeasonNumber, onBack: () => ShowHome(origin), onSetTraining: target => SetTraining(raider.Id, target));
        Swap(view);
    }

    // Set a raider's training focus (which attribute they develop). Persisted; the calendar's training uses it.
    private void SetTraining(string raiderId, string? attributeId)
    {
        var roster = _guild.Roster.Select(r => r.Id == raiderId ? r with { TrainingTarget = attributeId } : r).ToList();
        _guild = _guild with { Roster = roster };
        _saves.Save(_guild);
    }

    private void Rest()
    {
        _guild = Downtime.Rest(_guild);
        _saves.Save(_guild);
        GD.Print("the guild rested; injuries recovered a step");
        ShowHome("Squad");
    }

    // Recruitment now runs through the Transfers tab: your youth intake + the free-agent market (GDD §8b/§10).
    private void Recruit() => ShowHome("Transfers");

    // Sign a free agent from the living world (GDD §8b) — pay the fee, they join your roster.
    private void SignFreeAgent(RaiderRecord agent)
    {
        if (TransferMarket.TrySign(_guild, agent, out GuildSave updated))
        {
            _guild = updated;
            _saves.Save(_guild);
            GD.Print($"signed {agent.Name} for {TransferMarket.Fee(agent)}g; roster now {_guild.Roster.Count}");
        }
        else
        {
            GD.Print($"can't sign {agent.Name} — not enough gold");
        }

        ShowHome("Transfers");
    }

    // Promote a youth prospect (GDD §10) from your academy into the senior roster.
    private void PromoteYouth(string prospectId)
    {
        if (YouthProgram.TryPromote(_guild, prospectId, out GuildSave updated))
        {
            _guild = updated;
            _saves.Save(_guild);
            GD.Print($"promoted a prospect (-{YouthProgram.PromoteCost}g); roster now {_guild.Roster.Count}");
        }
        else
        {
            GD.Print("can't promote — not enough gold");
        }

        ShowHome("Transfers");
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
