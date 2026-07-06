using System;
using Engine;
using Game;
using Godot;

namespace App;

/// <summary>
/// Contract talks (GDD §4), kept simple: the board's current terms and their last line, with two ways to
/// press — gently or hard. Pushing may lower the expectation and bump your pay; pushing too hard can make
/// them pull the offer. Your manager's Negotiation attribute tilts the odds. You can sign the terms on the
/// table at any time, or walk away.
/// </summary>
public partial class NegotiationView : Control
{
    private Manager _manager = null!;
    private SeededRng _rng = null!;
    private NegotiationState _state = null!;
    private Action<NegotiationState> _onSign = null!;

    private Label _terms = null!;
    private Label _response = null!;
    private Button _gentle = null!;
    private Button _hard = null!;
    private Button _sign = null!;

    public void Load(JobOffer offer, Manager manager, ulong seed, Action<NegotiationState> onSign, Action onWalkAway)
    {
        ArgumentNullException.ThrowIfNull(offer);
        _manager = manager;
        _rng = new SeededRng(seed);
        _state = Negotiations.Start(offer);
        _onSign = onSign;

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        center.AddChild(panel);

        var margin = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 28);
        }

        panel.AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        col.CustomMinimumSize = new Vector2(520, 0);
        margin.AddChild(col);

        var title = new Label { Text = $"Contract talks — {offer.GuildName}" };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", AppTheme.Gold);
        col.AddChild(title);

        _terms = new Label();
        col.AddChild(_terms);

        _response = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _response.AddThemeColorOverride("font_color", new Color("#c7b98c"));
        col.AddChild(_response);

        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });

        var pushRow = new HBoxContainer();
        pushRow.AddThemeConstantOverride("separation", 10);
        _gentle = new Button { Text = "Push gently", CustomMinimumSize = new Vector2(160, 40) };
        _gentle.Pressed += () => Push(PushIntensity.Gentle);
        _hard = new Button { Text = "Push hard", CustomMinimumSize = new Vector2(160, 40) };
        _hard.Pressed += () => Push(PushIntensity.Hard);
        pushRow.AddChild(_gentle);
        pushRow.AddChild(_hard);
        col.AddChild(pushRow);

        var signRow = new HBoxContainer();
        signRow.AddThemeConstantOverride("separation", 10);
        _sign = new Button { Text = "Sign the contract", CustomMinimumSize = new Vector2(200, 42) };
        _sign.Pressed += () => _onSign(_state);
        var walk = new Button { Text = "Walk away", CustomMinimumSize = new Vector2(140, 42) };
        walk.Pressed += onWalkAway;
        signRow.AddChild(_sign);
        signRow.AddChild(walk);
        col.AddChild(signRow);

        Refresh();
    }

    private void Push(PushIntensity intensity)
    {
        _state = Negotiations.Push(_state, intensity, _manager, _rng);
        Refresh();
    }

    private void Refresh()
    {
        _terms.Text = _state.Withdrawn
            ? "The offer is off the table."
            : $"Salary: {_state.Salary}g/wk       Board expects: {_state.ExpectationText}       (patience: {_state.Patience})";
        _response.Text = _state.Response;

        _gentle.Disabled = !_state.CanPush;
        _hard.Disabled = !_state.CanPush;
        _sign.Disabled = _state.Withdrawn;
    }
}
