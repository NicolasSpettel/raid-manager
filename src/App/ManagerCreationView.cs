using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Game;
using Godot;

namespace App;

/// <summary>
/// Manager creation (GDD §2): you are a person, not a hero. Pick a name/age/region, choose a background
/// (a story that seeds an attribute spread + perk), then spend a small point-buy across the seven manager
/// attributes. Live totals; the background's bonus and your spend both show. Produces a <see cref="Manager"/>.
/// </summary>
public partial class ManagerCreationView : Control
{
    private readonly Dictionary<string, int> _pointBuy = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Label> _valueLabels = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Button> _backgroundButtons = new(StringComparer.Ordinal);

    private LineEdit _nameEdit = null!;
    private SpinBox _ageSpin = null!;
    private OptionButton _regionOption = null!;
    private Label _pointsLabel = null!;
    private Label _backgroundBlurb = null!;
    private Button _createButton = null!;

    private string _backgroundId = ManagerProfile.Backgrounds[0].Id;
    private int _pointsRemaining = ManagerProfile.PointBuy;

    public void Load(Action<Manager> onCreate, Action onBack)
    {
        ArgumentNullException.ThrowIfNull(onCreate);

        foreach (ManagerAttributeDef a in ManagerProfile.Attributes)
        {
            _pointBuy[a.Id] = 0;
        }

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 22);
        }

        AddChild(margin);

        var panel = new PanelContainer();
        margin.AddChild(panel);

        var scroll = new ScrollContainer();
        panel.AddChild(scroll);

        var root = new VBoxContainer();
        root.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(root);

        var title = new Label { Text = "Create Your Manager" };
        title.AddThemeFontSizeOverride("font_size", 30);
        title.AddThemeColorOverride("font_color", AppTheme.Gold);
        root.AddChild(title);
        root.AddChild(new Label { Text = "You manage the guild — a person, not a hero. Your attributes shape the quality of what you see and can do." });

        BuildIdentity(root);
        BuildBackground(root);
        BuildAttributes(root);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 12);
        var back = new Button { Text = "< Back", CustomMinimumSize = new Vector2(120, 40) };
        back.Pressed += onBack;
        _createButton = new Button { Text = "Create Manager", CustomMinimumSize = new Vector2(200, 40) };
        _createButton.Pressed += () => onCreate(BuildManager());
        buttons.AddChild(back);
        buttons.AddChild(_createButton);
        root.AddChild(buttons);

        Refresh();
    }

    private void BuildIdentity(VBoxContainer root)
    {
        root.AddChild(SectionHeader("Identity"));

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 8);
        root.AddChild(grid);

        grid.AddChild(new Label { Text = "Name" });
        _nameEdit = new LineEdit { PlaceholderText = "e.g. Aldric Vance", CustomMinimumSize = new Vector2(280, 0) };
        _nameEdit.TextChanged += _ => Refresh();
        grid.AddChild(_nameEdit);

        grid.AddChild(new Label { Text = "Age" });
        _ageSpin = new SpinBox { MinValue = 25, MaxValue = 60, Value = 35, CustomMinimumSize = new Vector2(120, 0) };
        grid.AddChild(_ageSpin);

        grid.AddChild(new Label { Text = "Region" });
        _regionOption = new OptionButton { CustomMinimumSize = new Vector2(200, 0) };
        foreach (NamePool pool in NamePools.All)
        {
            _regionOption.AddItem(pool.Region);
        }

        grid.AddChild(_regionOption);
    }

    private void BuildBackground(VBoxContainer root)
    {
        root.AddChild(SectionHeader("Background"));

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        root.AddChild(row);

        foreach (BackgroundDef bg in ManagerProfile.Backgrounds)
        {
            var button = new Button
            {
                Text = bg.Name,
                ToggleMode = true,
                CustomMinimumSize = new Vector2(0, 40),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ButtonPressed = bg.Id == _backgroundId,
            };
            string id = bg.Id;
            button.Pressed += () => SelectBackground(id);
            _backgroundButtons[bg.Id] = button;
            row.AddChild(button);
        }

        _backgroundBlurb = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _backgroundBlurb.AddThemeColorOverride("font_color", new Color("#9a9486"));
        root.AddChild(_backgroundBlurb);
    }

    private void BuildAttributes(VBoxContainer root)
    {
        _pointsLabel = SectionHeader("Attributes");
        root.AddChild(_pointsLabel);

        foreach (ManagerAttributeDef attr in ManagerProfile.Attributes)
        {
            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 8);

            var name = new Label { Text = attr.Name, CustomMinimumSize = new Vector2(120, 0) };
            hbox.AddChild(name);

            var minus = new Button { Text = "-", CustomMinimumSize = new Vector2(36, 0) };
            minus.Pressed += () => Spend(attr.Id, -1);
            hbox.AddChild(minus);

            var value = new Label { Text = "8", CustomMinimumSize = new Vector2(28, 0), HorizontalAlignment = HorizontalAlignment.Center };
            value.AddThemeColorOverride("font_color", AppTheme.Gold);
            _valueLabels[attr.Id] = value;
            hbox.AddChild(value);

            var plus = new Button { Text = "+", CustomMinimumSize = new Vector2(36, 0) };
            plus.Pressed += () => Spend(attr.Id, +1);
            hbox.AddChild(plus);

            var blurb = new Label { Text = attr.Blurb };
            blurb.AddThemeColorOverride("font_color", new Color("#8f8a7d"));
            hbox.AddChild(blurb);

            root.AddChild(hbox);
        }
    }

    private void SelectBackground(string id)
    {
        _backgroundId = id;
        foreach ((string bgId, Button button) in _backgroundButtons)
        {
            button.ButtonPressed = bgId == id;
        }

        Refresh();
    }

    private void Spend(string attributeId, int delta)
    {
        if (delta > 0 && (_pointsRemaining <= 0 || Effective(attributeId) >= ManagerProfile.MaxValue))
        {
            return;
        }

        if (delta < 0 && _pointBuy[attributeId] <= 0)
        {
            return;
        }

        _pointBuy[attributeId] += delta;
        _pointsRemaining -= delta;
        Refresh();
    }

    private int Effective(string attributeId)
    {
        BackgroundDef bg = ManagerProfile.Backgrounds.First(b => b.Id == _backgroundId);
        int value = ManagerProfile.BaseValue + (bg.Bonuses.TryGetValue(attributeId, out int bonus) ? bonus : 0) + _pointBuy[attributeId];
        return Math.Clamp(value, 1, ManagerProfile.MaxValue);
    }

    private void Refresh()
    {
        BackgroundDef bg = ManagerProfile.Backgrounds.First(b => b.Id == _backgroundId);
        _backgroundBlurb.Text = $"{bg.Blurb}   —   Perk: {bg.Perk}";
        _pointsLabel.Text = $"Attributes   (points to spend: {_pointsRemaining})";

        foreach (ManagerAttributeDef attr in ManagerProfile.Attributes)
        {
            _valueLabels[attr.Id].Text = Effective(attr.Id).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        _createButton.Disabled = string.IsNullOrWhiteSpace(_nameEdit.Text);
    }

    private Manager BuildManager()
    {
        string name = string.IsNullOrWhiteSpace(_nameEdit.Text) ? "The Manager" : _nameEdit.Text.Trim();
        string region = _regionOption.Selected >= 0 ? _regionOption.GetItemText(_regionOption.Selected) : NamePools.All[0].Region;
        return Managers.Create(name, (int)_ageSpin.Value, region, _backgroundId, _pointBuy);
    }

    private static Label SectionHeader(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", new Color("#d8c99a"));
        return label;
    }
}
