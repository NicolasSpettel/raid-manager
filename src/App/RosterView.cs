using System;
using Content;
using Game;
using Godot;

namespace App;

/// <summary>
/// The guild roster screen: the guild's name/gold/reputation, its raiders (name + class + role), and the
/// actions — Start Raid and Save. Framed in the app's carved-stone theme; reads the save aggregate,
/// never writes it (that goes through the coordinator).
/// </summary>
public partial class RosterView : Control
{
    public void Load(GuildSave guild, Action onStartRaid, Action onSave)
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
        root.AddChild(title);

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
            list.AddChild(new Label { Text = $"    {raider.Name}       {cls.Name} ({cls.Role})       Lv {raider.Level}" });
        }

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 10);
        var raidButton = new Button { Text = "Start Raid  —  The Warden" };
        raidButton.Pressed += () => onStartRaid();
        var saveButton = new Button { Text = "Save Guild" };
        saveButton.Pressed += () => onSave();
        buttons.AddChild(raidButton);
        buttons.AddChild(saveButton);
        root.AddChild(buttons);
    }
}
