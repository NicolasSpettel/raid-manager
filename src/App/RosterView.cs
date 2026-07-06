using System;
using Content;
using Engine;
using Game;
using Godot;

namespace App;

/// <summary>
/// The guild roster screen: guild name/gold/reputation, a last-raid banner, the raiders (name, class,
/// level, gear power), and the actions — raid a chosen boss, or save. Framed in the carved-stone theme;
/// reads the save aggregate, never writes it (that goes through the coordinator).
/// </summary>
public partial class RosterView : Control
{
    public void Load(
        GuildSave guild,
        Difficulty difficulty,
        Action<EncounterDef> onStartRaid,
        Action onCycleDifficulty,
        Action onRest,
        Action onRecruit,
        Action onSave)
    {
        ArgumentNullException.ThrowIfNull(guild);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var panel = new PanelContainer();
        margin.AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);

        var title = new Label
        {
            Text = $"{guild.Guild.Name}       Gold {guild.Economy.Gold}       Reputation {guild.Guild.Reputation}",
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", AppTheme.Gold);
        root.AddChild(title);

        if (guild.History.Count > 0)
        {
            root.AddChild(LastRaidBanner(guild.History[^1]));
        }

        root.AddChild(new Label { Text = $"Roster ({guild.Roster.Count})        Raids fought: {guild.History.Count}" });

        var listPanel = new PanelContainer();
        listPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(listPanel);

        var scroll = new ScrollContainer();
        listPanel.AddChild(scroll);

        var list = new VBoxContainer();
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(list);
        foreach (RaiderRecord raider in guild.Roster)
        {
            ClassDef cls = Classes.Registry.Get(raider.ClassId);
            string injured = raider.InjuryRaidsLeft > 0 ? "       [injured]" : string.Empty;
            list.AddChild(new Label
            {
                Text = $"    {raider.Name}       {cls.Name} ({cls.Role})       Gear {Warband.GearPower(raider)}{injured}",
            });
        }

        var raidRow = new HBoxContainer();
        raidRow.AddThemeConstantOverride("separation", 10);
        foreach (EncounterDef encounter in Encounters.All)
        {
            var raidButton = new Button { Text = $"Raid: {encounter.Name}" };
            raidButton.Pressed += () => onStartRaid(encounter);
            raidRow.AddChild(raidButton);
        }

        root.AddChild(raidRow);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 10);

        var difficultyButton = new Button { Text = $"Difficulty: {difficulty}" };
        difficultyButton.Pressed += () => onCycleDifficulty();
        actionRow.AddChild(difficultyButton);

        var restButton = new Button { Text = "Rest" };
        restButton.Pressed += () => onRest();
        actionRow.AddChild(restButton);

        var recruitButton = new Button { Text = $"Recruit ({Recruitment.Cost}g)" };
        recruitButton.Pressed += () => onRecruit();
        actionRow.AddChild(recruitButton);

        var saveButton = new Button { Text = "Save Guild" };
        saveButton.Pressed += () => onSave();
        actionRow.AddChild(saveButton);

        root.AddChild(actionRow);
    }

    private static Label LastRaidBanner(RaidSummary last)
    {
        string loot = last.LootDropped is null
            ? "no drop"
            : Items.Registry.Contains(last.LootDropped) ? Items.Registry.Get(last.LootDropped).Name : last.LootDropped;

        var banner = new Label
        {
            Text = $"Last raid: {last.Outcome} vs {last.EncounterId}   —   +{last.GoldAwarded} gold, loot: {loot}",
        };
        banner.AddThemeColorOverride("font_color", AppTheme.Gold);
        return banner;
    }
}
