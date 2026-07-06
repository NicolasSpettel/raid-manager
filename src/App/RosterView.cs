using System;
using Content;
using Game;
using Godot;

namespace App;

/// <summary>
/// The guild roster screen: the guild's name/gold/reputation, its raiders (name + class + role), and the
/// actions — Start Raid and Save. A plain functional layout; the RPG-textured Theme + design-system
/// primitives are step 8. Reads the save aggregate; never writes it (that goes through the coordinator).
/// </summary>
public partial class RosterView : Control
{
    public void Load(GuildSave guild, Action onStartRaid, Action onSave)
    {
        ArgumentNullException.ThrowIfNull(guild);

        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        var title = new Label
        {
            Text = $"{guild.Guild.Name}      Gold: {guild.Economy.Gold}      Reputation: {guild.Guild.Reputation}",
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        root.AddChild(title);

        root.AddChild(new Label { Text = $"Roster ({guild.Roster.Count})" });

        var list = new VBoxContainer();
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        foreach (RaiderRecord raider in guild.Roster)
        {
            ClassDef cls = Classes.Registry.Get(raider.ClassId);
            list.AddChild(new Label { Text = $"    {raider.Name}   —   {cls.Name} ({cls.Role})" });
        }

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(list);
        root.AddChild(scroll);

        var buttons = new HBoxContainer();
        var raidButton = new Button { Text = "Start Raid  —  The Warden" };
        raidButton.Pressed += () => onStartRaid();
        var saveButton = new Button { Text = "Save Guild" };
        saveButton.Pressed += () => onSave();
        buttons.AddChild(raidButton);
        buttons.AddChild(saveButton);
        root.AddChild(buttons);
    }
}
