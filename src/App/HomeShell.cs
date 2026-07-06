using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;
using Game;
using Godot;

namespace App;

/// <summary>
/// The home hub: a persistent nav bar over a content area, the way an FM save reads. Tabs — <b>Home</b>
/// (dashboard), <b>Squad</b> (the roster, click a raider for their page), <b>Calendar</b> (the season's
/// upcoming events), <b>Guild</b> (finances, brief, history), <b>Manager</b> (you). Actions that leave the
/// hub (watch a raid, open a raider) are full-screen and return here.
/// </summary>
public partial class HomeShell : Control
{
    // The full FM-style tab set. Built: Home/Squad/Calendar/Guild/Manager. The rest are labelled placeholders.
    private static readonly string[] Tabs =
    {
        "Home", "Squad", "Tactics", "Calendar", "Transfers", "Staff", "Inbox", "World", "Guild", "Manager",
    };

    private readonly Dictionary<string, Button> _tabButtons = new(StringComparer.Ordinal);

    private GuildSave _guild = null!;
    private Difficulty _difficulty;
    private Action<EncounterDef> _onStartRaid = null!;
    private Action<RaiderRecord> _onRaider = null!;
    private Action _onCycleDifficulty = null!;
    private Action _onRest = null!;
    private Action _onRecruit = null!;
    private Action _onSave = null!;
    private Action _onMenu = null!;
    private Action<IReadOnlyList<IReadOnlyList<ActivityType>>> _onSimulateDay = null!;
    private Action<IReadOnlyList<IReadOnlyList<ActivityType>>> _onSimulateWeek = null!;
    private Action<RaiderRecord> _onSignFreeAgent = null!;
    private Action<string> _onPromoteYouth = null!;
    private World? _world; // this career's living world — source of the free-agent market (may be null on very old saves)
    private string? _lastWeekSummary;

    private PanelContainer _contentPanel = null!;
    private Control? _currentContent;
    private ActivityType[][]? _slots; // this week's plan: 7 days × 4 slots

    public void Load(
        GuildSave guild,
        Difficulty difficulty,
        Action<EncounterDef> onStartRaid,
        Action onCycleDifficulty,
        Action onRest,
        Action onRecruit,
        Action onSave,
        Action<RaiderRecord> onRaider,
        Action onMenu,
        Action<IReadOnlyList<IReadOnlyList<ActivityType>>> onSimulateDay,
        Action<IReadOnlyList<IReadOnlyList<ActivityType>>> onSimulateWeek,
        Action<RaiderRecord> onSignFreeAgent,
        Action<string> onPromoteYouth,
        World? world,
        string? lastWeekSummary,
        string initialTab)
    {
        ArgumentNullException.ThrowIfNull(guild);
        _guild = guild;
        _difficulty = difficulty;
        _onStartRaid = onStartRaid;
        _onCycleDifficulty = onCycleDifficulty;
        _onRest = onRest;
        _onRecruit = onRecruit;
        _onSave = onSave;
        _onRaider = onRaider;
        _onMenu = onMenu;
        _onSimulateDay = onSimulateDay;
        _onSimulateWeek = onSimulateWeek;
        _onSignFreeAgent = onSignFreeAgent;
        _onPromoteYouth = onPromoteYouth;
        _world = world;
        _lastWeekSummary = lastWeekSummary;

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 14);
        }

        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var header = new Label
        {
            Text = $"{_guild.Guild.Name}       Gold {_guild.Economy.Gold}       Reputation {_guild.Guild.Reputation}",
        };
        header.AddThemeFontSizeOverride("font_size", 22);
        header.AddThemeColorOverride("font_color", AppTheme.Gold);
        root.AddChild(header);

        root.AddChild(BuildNavBar());

        _contentPanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(_contentPanel);

        ShowTab(Array.IndexOf(Tabs, initialTab) >= 0 ? initialTab : "Home");
    }

    private HFlowContainer BuildNavBar()
    {
        var nav = new HFlowContainer(); // wraps to a second row if the window is narrow
        nav.AddThemeConstantOverride("h_separation", 4);
        nav.AddThemeConstantOverride("v_separation", 4);

        foreach (string tab in Tabs)
        {
            var button = new Button { Text = tab, ToggleMode = true, CustomMinimumSize = new Vector2(92, 36) };
            string captured = tab;
            button.Pressed += () => ShowTab(captured);
            _tabButtons[tab] = button;
            nav.AddChild(button);
        }

        var save = new Button { Text = "Save", CustomMinimumSize = new Vector2(78, 36) };
        save.Pressed += () => _onSave();
        nav.AddChild(save);

        var menu = new Button { Text = "Menu", CustomMinimumSize = new Vector2(78, 36) };
        menu.Pressed += () => _onMenu();
        nav.AddChild(menu);

        return nav;
    }

    private void ShowTab(string tab)
    {
        foreach ((string name, Button button) in _tabButtons)
        {
            button.ButtonPressed = name == tab;
        }

        if (_currentContent is not null)
        {
            _contentPanel.RemoveChild(_currentContent);
            _currentContent.QueueFree();
        }

        _currentContent = tab switch
        {
            "Squad" => BuildSquad(),
            "Calendar" => BuildCalendar(),
            "Transfers" => BuildTransfers(),
            "Guild" => BuildGuild(),
            "Manager" => BuildManager(),
            "Home" => BuildHome(),
            _ => BuildPlaceholder(tab),
        };

        _contentPanel.AddChild(_currentContent);
    }

    private RosterView BuildSquad()
    {
        var roster = new RosterView();
        roster.Load(_guild, _difficulty, _onStartRaid, _onCycleDifficulty, _onRest, _onRecruit, _onSave, _onRaider);
        return roster;
    }

    private ScrollContainer BuildHome()
    {
        VBoxContainer col = Padded();

        col.AddChild(Header("Dashboard"));
        col.AddChild(new Label { Text = $"Squad: {_guild.Roster.Count} raiders    ·    Raids fought: {_guild.History.Count}" });
        col.AddChild(new Label { Text = $"Board expects: {_guild.Guild.BoardExpectation ?? "settle in and see what you've got."}" });
        col.AddChild(Dim($"Season {_guild.SeasonNumber}, week {_guild.SeasonWeek} of 12   ·   in a living world of ~200 guilds"));

        SeasonSchedule calendar = SeasonSchedule.Build(12);
        CalendarEvent? next = calendar.Upcoming(_guild.SeasonWeek, 1).FirstOrDefault();
        col.AddChild(Dim(next is not null ? $"Next up: week {next.Week} — {next.Name}" : "The season awaits."));

        if (_guild.History.Count > 0)
        {
            RaidSummary last = _guild.History[^1];
            col.AddChild(Dim($"Last raid: {last.Outcome} vs {last.EncounterId} (+{last.GoldAwarded}g)"));
        }

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        var toSquad = new Button { Text = "Go to the squad", CustomMinimumSize = new Vector2(220, 42) };
        toSquad.Pressed += () => ShowTab("Squad");
        col.AddChild(toSquad);
        return Wrap(col);
    }

    private ScrollContainer BuildCalendar()
    {
        const int seasonWeeks = 12;
        VBoxContainer col = Padded();
        col.AddChild(Header("Calendar"));

        int week = _guild.SeasonWeek;
        int today = _guild.SeasonDay;
        SeasonSchedule calendar = SeasonSchedule.Build(seasonWeeks);
        col.AddChild(new Label { Text = $"Season {_guild.SeasonNumber}   ·   Week {week} of {seasonWeeks}   ·   {(Weekday)today}" });
        CalendarEvent? holiday = calendar.HolidayIn(week);
        if (holiday is not null)
        {
            col.AddChild(Dim($"Holiday this week: {holiday.Name} — your raiders expect the time off."));
        }

        col.AddChild(Dim("Upcoming: " + string.Join("   |   ", calendar.Upcoming(week, 3).Select(e => $"wk{e.Week} {e.Name}"))));

        if (_lastWeekSummary is not null)
        {
            var summary = new Label { Text = _lastWeekSummary, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            summary.AddThemeColorOverride("font_color", AppTheme.Gold);
            col.AddChild(summary);
        }

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
        col.AddChild(Dim("Each day has four 2-hour slots — click a slot to set the guild's activity, then simulate."));

        _slots = DefaultWeekPlan();
        string[] dayNames = System.Enum.GetNames<Weekday>();
        for (int d = 0; d < dayNames.Length; d++)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 5);

            var dayLabel = new Label { Text = dayNames[d][..3], CustomMinimumSize = new Vector2(60, 0) };
            dayLabel.AddThemeColorOverride("font_color", d == today ? AppTheme.Gold : (d < today ? new Color("#6f6a60") : new Color("#cdc8bb")));
            row.AddChild(dayLabel);

            for (int s = 0; s < 4; s++)
            {
                int day = d;
                int slot = s;
                var slotButton = new Button { Text = Abbrev(_slots[d][s]), CustomMinimumSize = new Vector2(94, 34), Disabled = d < today };
                slotButton.Pressed += () =>
                {
                    _slots![day][slot] = Cycle(_slots[day][slot]);
                    slotButton.Text = Abbrev(_slots[day][slot]);
                };
                row.AddChild(slotButton);
            }

            col.AddChild(row);
        }

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 10);
        var simDay = new Button { Text = "Simulate day", CustomMinimumSize = new Vector2(150, 42) };
        simDay.Pressed += () => _onSimulateDay(_slots!);
        var simWeek = new Button { Text = "Continue to next raid", CustomMinimumSize = new Vector2(190, 42) };
        simWeek.Pressed += () => _onSimulateWeek(_slots!);
        actions.AddChild(simDay);
        actions.AddChild(simWeek);
        col.AddChild(actions);

        return Wrap(col);
    }

    private static string Abbrev(ActivityType activity) => activity switch
    {
        ActivityType.Raid => "Raid",
        ActivityType.Dungeon => "Dungeon",
        ActivityType.Training => "Train",
        _ => "Rest",
    };

    private static ActivityType Cycle(ActivityType activity) => activity switch
    {
        ActivityType.Rest => ActivityType.Raid,
        ActivityType.Raid => ActivityType.Dungeon,
        ActivityType.Dungeon => ActivityType.Training,
        _ => ActivityType.Rest,
    };

    private static ActivityType[][] DefaultWeekPlan()
    {
        const ActivityType r = ActivityType.Raid, d = ActivityType.Dungeon, t = ActivityType.Training, x = ActivityType.Rest;
        return new[]
        {
            new[] { r, r, r, x }, // Mon — raid night
            new[] { d, t, x, x }, // Tue — dungeon + training
            new[] { r, r, r, x }, // Wed — raid night
            new[] { d, t, x, x }, // Thu — dungeon + training
            new[] { r, r, r, x }, // Fri — raid night
            new[] { d, x, x, x }, // Sat — a dungeon
            new[] { x, x, x, x }, // Sun — rest
        };
    }

    // The Transfers tab (GDD §8b/§10): your youth academy's intake + the living world's free-agent market.
    private ScrollContainer BuildTransfers()
    {
        VBoxContainer col = Padded();
        col.AddChild(Header("Transfers"));
        col.AddChild(Dim($"Bank: {_guild.Economy.Gold}g   ·   sign youth prospects or free agents to fill out the roster."));

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
        col.AddChild(Header("Youth intake"));
        IReadOnlyList<RaiderRecord> prospects = _guild.YouthProspects ?? Array.Empty<RaiderRecord>();
        if (prospects.Count == 0)
        {
            col.AddChild(Dim("No prospects right now — a fresh intake arrives with each new season."));
        }
        else
        {
            col.AddChild(Dim("Your academy's promising youngsters — modest now, room to grow. Promote one into the senior roster."));
            foreach (RaiderRecord p in prospects)
            {
                col.AddChild(RecruitRow(p, YouthProgram.PromoteCost, "Promote", () => _onPromoteYouth(p.Id)));
            }
        }

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
        col.AddChild(Header("Free agents"));
        if (_world is null)
        {
            col.AddChild(Dim("The market is unavailable on this save."));
            return Wrap(col);
        }

        IReadOnlyList<TransferListing> agents = TransferMarket.FreeAgents(_world, _guild);
        col.AddChild(Dim($"{agents.Count} guildless raiders looking for a home — best first. Click a name to inspect them."));
        foreach (TransferListing listing in agents)
        {
            col.AddChild(RecruitRow(listing.Raider, listing.Fee, "Sign", () => _onSignFreeAgent(listing.Raider)));
        }

        return Wrap(col);
    }

    // One market row: an inspectable name, role/stars/age at a glance, and an affordable-gated sign/promote button.
    private HBoxContainer RecruitRow(RaiderRecord raider, int cost, string verb, Action onBuy)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        var name = new Button { Text = raider.Name, CustomMinimumSize = new Vector2(190, 32) };
        name.Pressed += () => _onRaider(raider);
        row.AddChild(name);

        (CombatantRole role, int half) = Ratings.Best(raider);
        ClassDef cls = Classes.Registry.Get(raider.ClassId);
        var info = new Label
        {
            Text = $"{cls.Name}  ·  {role} {Ratings.Format(half)}  ·  age {Aging.AgeOf(raider, _guild.SeasonNumber)}",
            CustomMinimumSize = new Vector2(300, 0),
        };
        info.AddThemeColorOverride("font_color", new Color("#c8c2b4"));
        row.AddChild(info);

        bool canAfford = _guild.Economy.Gold >= cost;
        var buy = new Button { Text = $"{verb} ({cost}g)", CustomMinimumSize = new Vector2(130, 32), Disabled = !canAfford };
        buy.Pressed += onBuy;
        row.AddChild(buy);

        return row;
    }

    private ScrollContainer BuildGuild()
    {
        VBoxContainer col = Padded();
        col.AddChild(Header(_guild.Guild.Name));
        col.AddChild(new Label { Text = $"Reputation: {_guild.Guild.Reputation}    ·    Bank: {_guild.Economy.Gold}g" });
        col.AddChild(new Label { Text = $"The board's brief: {_guild.Guild.BoardExpectation ?? "—"}", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        col.AddChild(Header("Recent raids"));
        if (_guild.History.Count == 0)
        {
            col.AddChild(Dim("No raids yet — the first pull is ahead of you."));
        }
        else
        {
            foreach (RaidSummary raid in _guild.History.Reverse().Take(8))
            {
                col.AddChild(new Label { Text = $"  {raid.Outcome,-7} vs {raid.EncounterId}   (+{raid.GoldAwarded}g{(raid.LootDropped is { } l ? $", {l}" : string.Empty)})" });
            }
        }

        return Wrap(col);
    }

    private ScrollContainer BuildManager()
    {
        VBoxContainer col = Padded();
        Manager? manager = _guild.Manager;
        if (manager is null)
        {
            col.AddChild(new Label { Text = "No manager on this save." });
            return Wrap(col);
        }

        col.AddChild(Header(manager.Name));
        string background = manager.BackgroundId;
        foreach (BackgroundDef b in ManagerProfile.Backgrounds)
        {
            if (b.Id == manager.BackgroundId)
            {
                background = b.Name;
                break;
            }
        }

        col.AddChild(new Label { Text = $"Age {manager.Age}   ·   {manager.Region}   ·   {background}" });
        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
        col.AddChild(Header("Attributes"));
        foreach (ManagerAttributeDef attr in ManagerProfile.Attributes)
        {
            col.AddChild(StatBar(attr.Name, manager.Of(attr.Id), ManagerProfile.MaxValue));
        }

        return Wrap(col);
    }

    // Not-yet-built tabs: a labelled placeholder saying what the section will be (grounded in the GDD).
    private static ScrollContainer BuildPlaceholder(string tab)
    {
        string description = tab switch
        {
            "Tactics" => "Tactics & raid prep (GDD §7): set the raid group, assignments (who taunts, who interrupts, defensive-CD plans), and standing orders before a pull.",
            "Transfers" => "The transfer market (GDD §8b): scout, value, buy, sell, and poach raiders from other guilds and the ~400 free agents — performance-driven prices, never wallet-scaling.",
            "Staff" => "Staff & delegation (GDD §6b/§12): hire a co-leader, analyst, or trainer and delegate the parts of the job you'd rather not micromanage — quality scales with their ability.",
            "Inbox" => "Your inbox (GDD §11): the connective tissue — injuries, offers, board messages, world-first news, and choices to act on all arrive here.",
            "World" => "The living world (GDD §5): the season leaderboard, your rivals, the world-first race, and news from the other ~200 guilds racing the same raid.",
            _ => "Coming soon.",
        };

        VBoxContainer col = Padded();
        col.AddChild(Header(tab));
        var soon = new Label { Text = "— not built yet —" };
        soon.AddThemeColorOverride("font_color", new Color("#7a756a"));
        col.AddChild(soon);
        col.AddChild(Dim(description));
        return Wrap(col);
    }

    // ── small layout helpers ─────────────────────────────────────────────────────────────────────
    private static VBoxContainer Padded()
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 6);
        return col;
    }

    private static ScrollContainer Wrap(Control content)
    {
        var scroll = new ScrollContainer();
        var margin = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 16);
        }

        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        margin.AddChild(content);
        scroll.AddChild(margin);
        return scroll;
    }

    private static Label Header(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 22);
        label.AddThemeColorOverride("font_color", new Color("#d8c99a"));
        return label;
    }

    private static Label Dim(string text)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeColorOverride("font_color", new Color("#9a9486"));
        return label;
    }

    private static HBoxContainer StatBar(string name, int value, int max)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.AddChild(new Label { Text = name, CustomMinimumSize = new Vector2(150, 0) });
        row.AddChild(new ProgressBar
        {
            MinValue = 0,
            MaxValue = max,
            Value = value,
            CustomMinimumSize = new Vector2(220, 18),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        });
        var number = new Label { Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture), CustomMinimumSize = new Vector2(30, 0) };
        number.AddThemeColorOverride("font_color", AppTheme.Gold);
        row.AddChild(number);
        return row;
    }
}
