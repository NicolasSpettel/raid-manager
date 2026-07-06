using System;
using Game;
using Godot;

namespace App;

/// <summary>
/// The guild intro (GDD §3/§4): after signing, meet your new guild — its founding, motto, past endeavours,
/// and the season goal you agreed. A beat of world texture before you take the reins.
/// </summary>
public partial class GuildIntroView : Control
{
    public void Load(string guildName, string region, GuildLore lore, string boardExpectation, Action onEnter)
    {
        ArgumentNullException.ThrowIfNull(lore);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        center.AddChild(panel);

        var margin = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 32);
        }

        panel.AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        col.CustomMinimumSize = new Vector2(560, 0);
        margin.AddChild(col);

        var welcome = new Label { Text = "Welcome to your new guild" };
        welcome.AddThemeColorOverride("font_color", new Color("#8f8a7d"));
        col.AddChild(welcome);

        var title = new Label { Text = guildName };
        title.AddThemeFontSizeOverride("font_size", 32);
        title.AddThemeColorOverride("font_color", AppTheme.Gold);
        col.AddChild(title);

        var subtitle = new Label { Text = $"{region}   ·   founded {lore.FoundedSeasonsAgo} seasons ago   ·   \"{lore.Motto}\"" };
        subtitle.AddThemeColorOverride("font_color", new Color("#9a9486"));
        col.AddChild(subtitle);

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        col.AddChild(Header("Past endeavours"));
        foreach (string line in lore.PastEndeavours)
        {
            var label = new Label { Text = $"  •  {line}", AutowrapMode = TextServer.AutowrapMode.WordSmart };
            col.AddChild(label);
        }

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        col.AddChild(Header("Your brief"));
        var brief = new Label { Text = boardExpectation, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        brief.AddThemeColorOverride("font_color", new Color("#c7b98c"));
        col.AddChild(brief);

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 12) });
        var enter = new Button { Text = "Take the reins", CustomMinimumSize = new Vector2(220, 44) };
        enter.Pressed += onEnter;
        col.AddChild(enter);
    }

    private static Label Header(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", new Color("#d8c99a"));
        return label;
    }
}
