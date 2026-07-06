using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Godot;

namespace App;

/// <summary>
/// Combat-log playback (BLUEPRINT §8): replays a precomputed event stream with a playback clock. A pure
/// consumer — it never drives the sim, only folds a stream it already has. HP bars, a scrolling log, and
/// play/pause/speed/seek controls, plus a Back button to return to the roster.
/// </summary>
public partial class CombatView : Control
{
    private const int SeekBarHeight = 22;

    private SimResult? _result;
    private string[] _lines = Array.Empty<string>();
    private int _maxTick;
    private double _currentTick;
    private double _speed = 1.0;
    private bool _playing;
    private bool _syncingSeek;
    private Action? _onBack;

    private Button _playButton = null!;
    private Button _speedButton = null!;
    private Label _tickLabel = null!;
    private HSlider _seek = null!;
    private RichTextLabel _log = null!;
    private readonly List<CombatantSpec> _combatants = new();
    private readonly Dictionary<string, ProgressBar> _hpBars = new();
    private readonly Dictionary<string, int> _maxHp = new();

    /// <summary>Show the given fight and start playing it.</summary>
    public void Load(SimInput input, SimResult result, Action onBack)
    {
        _result = result;
        _onBack = onBack;
        _lines = EventStream.Serialize(result.Events).TrimEnd('\n').Split('\n');
        _maxTick = result.Events.Count == 0 ? 0 : result.Events[^1].Tick.Value;

        foreach (CombatantSpec spec in input.Raid.Raiders.Concat(input.Encounter.Enemies))
        {
            _combatants.Add(spec);
            _maxHp[spec.Id.Value] = spec.Stats.MaxHp;
        }

        _currentTick = 0;
        _playing = true;
        BuildUi();
        Render();
    }

    public override void _Process(double delta)
    {
        if (_result is null || !_playing)
        {
            return;
        }

        _currentTick += delta * TimeModel.TicksPerSecond * _speed;
        if (_currentTick >= _maxTick)
        {
            _currentTick = _maxTick;
            SetPlaying(false);
        }

        Render();
    }

    private void BuildUi()
    {
        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 6);
        AddChild(root);

        var controls = new HBoxContainer();
        var back = new Button { Text = "< Roster" };
        back.Pressed += () => _onBack?.Invoke();
        _playButton = new Button { Text = "Pause" };
        _playButton.Pressed += () => SetPlaying(!_playing);
        _speedButton = new Button { Text = "1x" };
        _speedButton.Pressed += CycleSpeed;
        var restart = new Button { Text = "Restart" };
        restart.Pressed += () =>
        {
            _currentTick = 0;
            SetPlaying(true);
            Render();
        };
        _tickLabel = new Label { Text = string.Empty };
        controls.AddChild(back);
        controls.AddChild(_playButton);
        controls.AddChild(_speedButton);
        controls.AddChild(restart);
        controls.AddChild(_tickLabel);
        root.AddChild(controls);

        _seek = new HSlider { MinValue = 0, MaxValue = _maxTick, Step = 1 };
        _seek.CustomMinimumSize = new Vector2(0, SeekBarHeight);
        _seek.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _seek.ValueChanged += OnSeek;
        root.AddChild(_seek);

        var main = new HBoxContainer();
        main.SizeFlagsVertical = SizeFlags.ExpandFill;
        main.AddThemeConstantOverride("separation", 12);

        var rosterPanel = new PanelContainer();
        rosterPanel.CustomMinimumSize = new Vector2(320, 0);
        var roster = new VBoxContainer();
        roster.AddThemeConstantOverride("separation", 4);
        foreach (CombatantSpec spec in _combatants)
        {
            roster.AddChild(new Label { Text = $"{spec.Name}  ({spec.Role})" });
            var bar = new ProgressBar
            {
                MinValue = 0,
                MaxValue = spec.Stats.MaxHp,
                Value = spec.Stats.MaxHp,
                ShowPercentage = false,
            };
            bar.CustomMinimumSize = new Vector2(0, 18);
            _hpBars[spec.Id.Value] = bar;
            roster.AddChild(bar);
        }

        rosterPanel.AddChild(roster);
        main.AddChild(rosterPanel);

        var logPanel = new PanelContainer();
        logPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        logPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        _log = new RichTextLabel { ScrollActive = true, ScrollFollowing = true };
        logPanel.AddChild(_log);
        main.AddChild(logPanel);

        root.AddChild(main);
    }

    private void Render()
    {
        if (_result is null)
        {
            return;
        }

        int tick = (int)_currentTick;
        var hp = new Dictionary<string, int>(_maxHp);
        int visible = 0;

        foreach (CombatEvent e in _result.Events)
        {
            if (e.Tick.Value > tick)
            {
                break; // events are emitted in non-decreasing tick order
            }

            ApplyToHp(hp, e);
            visible++;
        }

        foreach ((string id, ProgressBar bar) in _hpBars)
        {
            bar.Value = System.Math.Max(0, hp.TryGetValue(id, out int v) ? v : 0);
        }

        _log.Text = string.Join('\n', _lines.Take(visible));
        _tickLabel.Text = $"   tick {tick} / {_maxTick}    outcome: {_result.Outcome}";

        _syncingSeek = true;
        _seek.Value = tick;
        _syncingSeek = false;
    }

    private void ApplyToHp(Dictionary<string, int> hp, CombatEvent e)
    {
        switch (e)
        {
            case Damage d when hp.ContainsKey(d.Target.Value):
                hp[d.Target.Value] -= d.Amount;
                break;
            case Heal h when hp.ContainsKey(h.Target.Value):
                hp[h.Target.Value] = System.Math.Min(_maxHp[h.Target.Value], hp[h.Target.Value] + h.Amount);
                break;
            case Death dth when hp.ContainsKey(dth.Victim.Value):
                hp[dth.Victim.Value] = 0;
                break;
        }
    }

    private void OnSeek(double value)
    {
        if (_syncingSeek)
        {
            return;
        }

        _currentTick = value;
        SetPlaying(false);
        Render();
    }

    private void CycleSpeed()
    {
        _speed = _speed switch { 1.0 => 2.0, 2.0 => 4.0, _ => 1.0 };
        _speedButton.Text = $"{_speed:0}x";
    }

    private void SetPlaying(bool playing)
    {
        _playing = playing;
        _playButton.Text = playing ? "Pause" : "Play";
    }
}
