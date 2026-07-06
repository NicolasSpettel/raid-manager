using System;
using System.Collections.Generic;
using Content;
using Game;
using Godot;

namespace App;

/// <summary>
/// A raider's page — the base stats at a glance: class/role, the eleven attributes as bars, and the two
/// condition axes plus morale. (Gear and per-role stars come once those systems are wired to the roster.)
/// </summary>
public partial class RaiderView : Control
{
    public void Load(RaiderRecord raider, int seasonNumber, Action onBack, Action<string?> onSetTraining)
    {
        ArgumentNullException.ThrowIfNull(raider);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 20);
        }

        AddChild(margin);

        var panel = new PanelContainer();
        margin.AddChild(panel);

        var scroll = new ScrollContainer();
        panel.AddChild(scroll);

        var root = new VBoxContainer();
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(root);

        var back = new Button { Text = "< Roster", CustomMinimumSize = new Vector2(120, 36) };
        back.Pressed += onBack;
        root.AddChild(back);

        var title = new Label { Text = raider.Name };
        title.AddThemeFontSizeOverride("font_size", 30);
        title.AddThemeColorOverride("font_color", AppTheme.Gold);
        root.AddChild(title);

        ClassDef cls = Classes.Registry.Get(raider.ClassId);
        string injury = raider.InjuryRaidsLeft > 0 ? $"   —   injured ({raider.InjuryRaidsLeft} raids out)" : string.Empty;
        var subtitle = new Label { Text = $"{cls.Name}  ({cls.Role})   ·   age {Aging.AgeOf(raider, seasonNumber)}{injury}" };
        subtitle.AddThemeColorOverride("font_color", new Color("#9a9486"));
        root.AddChild(subtitle);

        root.AddChild(Header("Training focus"));
        var focus = new OptionButton { CustomMinimumSize = new Vector2(240, 0) };
        var focusIds = new List<string?> { null };
        focus.AddItem("Auto (train weakest)");
        int selected = 0;
        foreach (AttributeDef attr in Attributes.Registry.All)
        {
            focus.AddItem(attr.Name);
            focusIds.Add(attr.Id);
            if (attr.Id == raider.TrainingTarget)
            {
                selected = focusIds.Count - 1;
            }
        }

        focus.Selected = selected;
        focus.ItemSelected += index => onSetTraining(focusIds[(int)index]);
        root.AddChild(focus);

        Condition condition = raider.Condition ?? ConditionModel.Fresh;
        root.AddChild(Header("Condition"));
        root.AddChild(StatBar("Freshness", condition.Freshness, 100));
        root.AddChild(StatBar("Sharpness", condition.Sharpness, 100));
        root.AddChild(StatBar("Morale", condition.Morale, 100));

        root.AddChild(Header("Attributes"));
        foreach (AttributeDef attr in Attributes.Registry.All)
        {
            int value = raider.Attributes?.Of(attr.Id) ?? 10;
            root.AddChild(StatBar(attr.Name, value, 20));
        }
    }

    private static HBoxContainer StatBar(string name, int value, int max)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        var label = new Label { Text = name, CustomMinimumSize = new Vector2(150, 0) };
        row.AddChild(label);

        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = max,
            Value = value,
            CustomMinimumSize = new Vector2(240, 18),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddChild(bar);

        var number = new Label { Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture), CustomMinimumSize = new Vector2(32, 0) };
        number.AddThemeColorOverride("font_color", AppTheme.Gold);
        row.AddChild(number);

        return row;
    }

    private static Label Header(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", new Color("#d8c99a"));
        return label;
    }
}
