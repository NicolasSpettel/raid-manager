using System;
using Godot;

namespace App;

/// <summary>
/// The opening screen (GDD §1): minimal and atmospheric — get into a career fast, with Continue as the
/// one-click default. Load Career / Hall of Fame / Settings are placeholders until those systems exist.
/// </summary>
public partial class WelcomeView : Control
{
    public void Load(bool hasSave, Action onContinue, Action onNewCareer, Action onQuit)
    {
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        center.AddChild(panel);

        var margin = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 44);
        }

        panel.AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        margin.AddChild(col);

        var title = new Label { Text = "RAID MANAGER", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 46);
        title.AddThemeColorOverride("font_color", AppTheme.Gold);
        col.AddChild(title);

        var subtitle = new Label
        {
            Text = "Football Manager, in an RPG world",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        subtitle.AddThemeColorOverride("font_color", new Color("#8f8a7d"));
        col.AddChild(subtitle);

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 18) }); // spacer

        Button continueBtn = MenuButton("Continue", enabled: hasSave);
        continueBtn.Pressed += onContinue;
        col.AddChild(continueBtn);

        Button newBtn = MenuButton("New Career", enabled: true);
        newBtn.Pressed += onNewCareer;
        col.AddChild(newBtn);

        col.AddChild(MenuButton("Load Career", enabled: false));
        col.AddChild(MenuButton("Hall of Fame", enabled: false));
        col.AddChild(MenuButton("Settings", enabled: false));

        Button quitBtn = MenuButton("Quit", enabled: true);
        quitBtn.Pressed += onQuit;
        col.AddChild(quitBtn);
    }

    private static Button MenuButton(string text, bool enabled)
    {
        var button = new Button
        {
            Text = text,
            Disabled = !enabled,
            CustomMinimumSize = new Vector2(300, 44),
        };
        return button;
    }
}
