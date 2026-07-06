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
    private Action<WeekSchedule> _onAdvanceWeek = null!;
    private string? _lastWeekSummary;

    private PanelContainer _contentPanel = null!;
    private Control? _currentContent;
    private readonly List<OptionButton> _dayPickers = new();

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
        Action<WeekSchedule> onAdvanceWeek,
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
        _onAdvanceWeek = onAdvanceWeek;
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
        col.AddChild(Dim($"Season: week {_guild.SeasonWeek} of 12   ·   in a living world of ~200 guilds"));

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
        if (week > seasonWeeks)
        {
            col.AddChild(new Label { Text = $"The season is over — {_guild.History.Count} raids fought." });
            col.AddChild(Dim("New seasons + aging are the next step."));
            return Wrap(col);
        }

        SeasonSchedule calendar = SeasonSchedule.Build(seasonWeeks);
        col.AddChild(new Label { Text = $"Week {week} of {seasonWeeks}" });
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

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        col.AddChild(Header("Plan the week"));
        col.AddChild(Dim("Set each day; dungeons run a 5-man group, training develops your weakest. Then advance."));

        _dayPickers.Clear();
        ActivityType[] defaults = { ActivityType.Raid, ActivityType.Dungeon, ActivityType.Raid, ActivityType.Training, ActivityType.Raid, ActivityType.Dungeon, ActivityType.Rest };
        string[] dayNames = System.Enum.GetNames<Weekday>();
        for (int d = 0; d < dayNames.Length; d++)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            row.AddChild(new Label { Text = dayNames[d], CustomMinimumSize = new Vector2(110, 0) });

            var picker = new OptionButton { CustomMinimumSize = new Vector2(160, 0) };
            foreach (ActivityType activity in System.Enum.GetValues<ActivityType>())
            {
                picker.AddItem(activity.ToString());
            }

            picker.Selected = (int)defaults[d];
            _dayPickers.Add(picker);
            row.AddChild(picker);
            col.AddChild(row);
        }

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
        var advance = new Button { Text = "Advance the week", CustomMinimumSize = new Vector2(220, 44) };
        advance.Pressed += () =>
        {
            var perDay = _dayPickers.Select(p => (ActivityType)p.Selected).ToList();
            _onAdvanceWeek(WeekPlanner.FromDays(_guild, perDay));
        };
        col.AddChild(advance);

        return Wrap(col);
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
