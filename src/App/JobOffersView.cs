using System;
using System.Collections.Generic;
using Game;
using Godot;

namespace App;

/// <summary>
/// The job market (GDD §4): as an unknown manager, only struggling low-prestige guilds will take you.
/// Each offer is a real guild from the living world, shown with what you inspect before signing — roster
/// quality, finances, the board's expectation, and a regional rival — and a button to take the job.
/// </summary>
public partial class JobOffersView : Control
{
    public void Load(IReadOnlyList<JobOffer> offers, Action<JobOffer> onSelect, Action onBack)
    {
        ArgumentNullException.ThrowIfNull(offers);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 20);
        }

        AddChild(margin);

        var panel = new PanelContainer();
        margin.AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);

        var title = new Label { Text = "Job Offers" };
        title.AddThemeFontSizeOverride("font_size", 30);
        title.AddThemeColorOverride("font_color", AppTheme.Gold);
        root.AddChild(title);
        root.AddChild(new Label
        {
            Text = "You're an unknown — only struggling guilds will take a chance on you. Pick one to manage.",
        });

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        var listPanel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        listPanel.AddChild(scroll);
        root.AddChild(listPanel);

        var list = new VBoxContainer();
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        list.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(list);

        foreach (JobOffer offer in offers)
        {
            list.AddChild(OfferCard(offer, onSelect));
        }

        var back = new Button { Text = "< Back", CustomMinimumSize = new Vector2(120, 38) };
        back.Pressed += onBack;
        root.AddChild(back);
    }

    private static PanelContainer OfferCard(JobOffer offer, Action<JobOffer> onSelect)
    {
        var card = new PanelContainer();
        var inner = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            inner.AddThemeConstantOverride(side, 12);
        }

        card.AddChild(inner);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        inner.AddChild(row);

        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(info);

        var name = new Label { Text = $"{offer.GuildName}    ({offer.Region}, {offer.Tier})" };
        name.AddThemeFontSizeOverride("font_size", 20);
        name.AddThemeColorOverride("font_color", AppTheme.Gold);
        info.AddChild(name);

        info.AddChild(Dim($"Roster: {offer.RosterSize} raiders, avg {Ratings.Format(offer.AvgHalfStars)}    |    "
            + $"Bank: {offer.Bank}g,  wages {offer.WeeklyWages}g/wk"));
        info.AddChild(Dim($"Board expects: {offer.Expectation}"));
        info.AddChild(Dim($"Regional rival: {offer.Rival}"));

        var take = new Button { Text = "Negotiate", CustomMinimumSize = new Vector2(150, 40) };
        take.Pressed += () => onSelect(offer);
        var takeWrap = new CenterContainer();
        takeWrap.AddChild(take);
        row.AddChild(takeWrap);

        return card;
    }

    private static Label Dim(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", new Color("#9a9486"));
        return label;
    }
}
