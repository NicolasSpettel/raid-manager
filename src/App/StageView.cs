using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Godot;

namespace App;

/// <summary>
/// The 2D tactical stage (M2, BLUEPRINT §8 / ADR-0005): draws the fight as tokens on a board — side
/// colour, size by kind, filled by live HP, dark when dead, with a telegraph flash when a mechanic
/// fires. A PURE consumer of the same precomputed event stream as the log view, on the same playback
/// clock — it never drives the sim. Non-spatial fights use a role-based layout; spatial fights (M2
/// step 2) fold real engine positions, animate the dodge (`MoveEvent`), and draw the void zone (`HazardEvent`).
/// </summary>
public partial class StageView : Control
{
    private const float BoardTop = 96f; // leave room for the controls + seek bar
    private const int SeekBarHeight = 22;

    private SimResult? _result;
    private int _maxTick;
    private double _currentTick;
    private double _speed = 1.0;
    private bool _playing;
    private bool _syncingSeek;
    private Action? _onBack;
    private Action? _onSwitch;

    private Button _playButton = null!;
    private Button _speedButton = null!;
    private Label _tickLabel = null!;
    private HSlider _seek = null!;

    private readonly List<CombatantSpec> _combatants = new();
    private readonly Dictionary<string, Vector2> _layout = new();
    private readonly Dictionary<string, int> _maxHp = new();
    private readonly Dictionary<string, int> _hp = new();
    private string? _mechanicNote;
    private int _mechanicTick = int.MinValue;

    // Spatial mode (M2 step 2): real engine positions folded from the stream, plus live hazards.
    private bool _spatial;
    private readonly Dictionary<string, Vector2> _spawnPos = new();
    private readonly Dictionary<string, Vector2> _pos = new();
    private readonly List<(Vector2 Center, float Radius, string Id)> _hazards = new();
    private Vector2 _worldCenter;
    private Vector2 _worldHalf = Vector2.One;

    public void Load(SimInput input, SimResult result, Action onBack, Action onSwitch)
    {
        _result = result;
        _onBack = onBack;
        _onSwitch = onSwitch;
        _maxTick = result.Events.Count == 0 ? 0 : result.Events[^1].Tick.Value;

        foreach (CombatantSpec spec in input.Raid.Raiders.Concat(input.Encounter.Enemies))
        {
            _combatants.Add(spec);
            _maxHp[spec.Id.Value] = spec.Stats.MaxHp;
            _spawnPos[spec.Id.Value] = new Vector2(spec.SpawnPosition.X, spec.SpawnPosition.Y);
        }

        _spatial = input.Raid.Raiders.Any(r => r.SpawnPosition != Engine.Position.Origin);
        if (_spatial)
        {
            ComputeWorldBounds(result);
        }

        ComputeLayout();
        _currentTick = 0;
        _playing = true;
        BuildControls();
        Fold();
        QueueRedraw();
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

        Fold();
        SyncSeek();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_result is null)
        {
            return;
        }

        var board = new Rect2(0, BoardTop, Size.X, Math.Max(1f, Size.Y - BoardTop));
        DrawRect(board, new Color("#141016"));

        if (_mechanicNote is not null && (int)_currentTick - _mechanicTick <= 12)
        {
            DrawRect(board, new Color(0.78f, 0.20f, 0.15f, 0.16f)); // telegraph flash
        }

        if (_spatial)
        {
            foreach ((Vector2 center, float radius, string _) in _hazards)
            {
                Vector2 sc = WorldToScreen(center, board);
                float sr = radius * WorldScale(board);
                DrawCircle(sc, sr, new Color(0.85f, 0.35f, 0.10f, 0.22f));            // the void zone fill
                DrawArc(sc, sr, 0f, Mathf.Tau, 48, new Color(0.95f, 0.45f, 0.15f, 0.9f), 2.5f, true); // its edge
            }
        }

        foreach (CombatantSpec c in _combatants)
        {
            Vector2 pos;
            if (_spatial && _pos.TryGetValue(c.Id.Value, out Vector2 world))
            {
                pos = WorldToScreen(world, board);
            }
            else if (_layout.TryGetValue(c.Id.Value, out Vector2 frac))
            {
                pos = new Vector2(board.Position.X + (frac.X * board.Size.X), board.Position.Y + (frac.Y * board.Size.Y));
            }
            else
            {
                continue;
            }

            float radius = c.Kind == CombatantKind.Boss ? 46f : 22f;
            int hp = _hp.GetValueOrDefault(c.Id.Value);
            int max = _maxHp.GetValueOrDefault(c.Id.Value, 1);
            float hpFrac = max > 0 ? Math.Clamp((float)hp / max, 0f, 1f) : 0f;
            bool dead = hp <= 0;

            Color baseCol = c.Side == Engine.Side.Enemy ? new Color("#3a1414") : new Color("#16303a");
            Color fill = dead ? new Color("#2a2a2e") : (c.Side == Engine.Side.Enemy ? new Color("#b23b30") : new Color("#4a9bbf"));

            DrawCircle(pos, radius, baseCol);
            if (!dead && hpFrac > 0f)
            {
                DrawCircle(pos, radius * hpFrac, fill);
            }

            DrawArc(pos, radius, 0f, Mathf.Tau, 40, new Color(0f, 0f, 0f, 0.6f), 2f, true);
        }
    }

    private void ComputeLayout()
    {
        List<CombatantSpec> enemies = _combatants.Where(c => c.Side == Engine.Side.Enemy).ToList();
        List<CombatantSpec> front = _combatants
            .Where(c => c.Side == Engine.Side.Raid && (c.Role == CombatantRole.Tank || c.Role == CombatantRole.Melee))
            .ToList();
        List<CombatantSpec> back = _combatants
            .Where(c => c.Side == Engine.Side.Raid && (c.Role == CombatantRole.Healer || c.Role == CombatantRole.Ranged))
            .ToList();

        PlaceRow(enemies, 0.28f);
        PlaceRow(front, 0.55f);
        PlaceRow(back, 0.80f);
    }

    private void PlaceRow(List<CombatantSpec> row, float y)
    {
        for (int i = 0; i < row.Count; i++)
        {
            _layout[row[i].Id.Value] = new Vector2((i + 1f) / (row.Count + 1f), y);
        }
    }

    private void Fold()
    {
        int tick = (int)_currentTick;
        foreach ((string id, int max) in _maxHp)
        {
            _hp[id] = max;
        }

        foreach ((string id, Vector2 sp) in _spawnPos)
        {
            _pos[id] = sp;
        }

        _hazards.Clear();
        _mechanicNote = null;
        _mechanicTick = int.MinValue;

        foreach (CombatEvent e in _result!.Events)
        {
            if (e.Tick.Value > tick)
            {
                break;
            }

            switch (e)
            {
                case Damage d when _hp.ContainsKey(d.Target.Value):
                    _hp[d.Target.Value] -= d.Amount;
                    break;
                case Heal h when _hp.ContainsKey(h.Target.Value):
                    _hp[h.Target.Value] = Math.Min(_maxHp[h.Target.Value], _hp[h.Target.Value] + h.Amount);
                    break;
                case Death dth when _hp.ContainsKey(dth.Victim.Value):
                    _hp[dth.Victim.Value] = 0;
                    break;
                case MoveEvent mv when _pos.ContainsKey(mv.Who.Value):
                    _pos[mv.Who.Value] = new Vector2(mv.To.X, mv.To.Y);
                    break;
                case HazardEvent hz when hz.State == HazardState.Spawn:
                    _hazards.Add((new Vector2(hz.At.X, hz.At.Y), hz.Radius, hz.Id));
                    break;
                case HazardEvent hz when hz.State == HazardState.Expire:
                    Vector2 gone = new(hz.At.X, hz.At.Y);
                    _hazards.RemoveAll(h => h.Id == hz.Id && h.Center == gone);
                    break;
                case MechanicEvent m:
                    _mechanicNote = m.Note;
                    _mechanicTick = e.Tick.Value;
                    break;
            }
        }
    }

    // Fit all spawn positions, hazard footprints, and move targets into a stable world box (computed once,
    // so the view doesn't jitter as tokens move). Rendering uses float; only the sim is integer/deterministic.
    private void ComputeWorldBounds(SimResult result)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

        void Include(float x, float y)
        {
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        foreach (Vector2 p in _spawnPos.Values)
        {
            Include(p.X, p.Y);
        }

        foreach (CombatEvent e in result.Events)
        {
            switch (e)
            {
                case HazardEvent h:
                    Include(h.At.X - h.Radius, h.At.Y - h.Radius);
                    Include(h.At.X + h.Radius, h.At.Y + h.Radius);
                    break;
                case MoveEvent m:
                    Include(m.To.X, m.To.Y);
                    break;
            }
        }

        if (minX > maxX)
        {
            (minX, minY, maxX, maxY) = (-1000f, -1000f, 1000f, 1000f); // no spatial data — a safe box
        }

        const float pad = 1200f; // a token's worth of breathing room
        _worldCenter = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
        _worldHalf = new Vector2(Math.Max(1f, ((maxX - minX) / 2f) + pad), Math.Max(1f, ((maxY - minY) / 2f) + pad));
    }

    private float WorldScale(Rect2 board) =>
        Math.Min(board.Size.X / (_worldHalf.X * 2f), board.Size.Y / (_worldHalf.Y * 2f));

    private Vector2 WorldToScreen(Vector2 world, Rect2 board) =>
        board.Position + (board.Size / 2f) + ((world - _worldCenter) * WorldScale(board));

    private void BuildControls()
    {
        var bar = new VBoxContainer();
        bar.SetAnchorsPreset(LayoutPreset.TopWide);
        AddChild(bar);

        var controls = new HBoxContainer();
        var back = new Button { Text = "< Roster" };
        back.Pressed += () => _onBack?.Invoke();
        var switchButton = new Button { Text = "Log view" };
        switchButton.Pressed += () => _onSwitch?.Invoke();
        _playButton = new Button { Text = "Pause" };
        _playButton.Pressed += () => SetPlaying(!_playing);
        _speedButton = new Button { Text = "1x" };
        _speedButton.Pressed += CycleSpeed;
        var restart = new Button { Text = "Restart" };
        restart.Pressed += () =>
        {
            _currentTick = 0;
            SetPlaying(true);
        };
        _tickLabel = new Label { Text = string.Empty };
        controls.AddChild(back);
        controls.AddChild(switchButton);
        controls.AddChild(_playButton);
        controls.AddChild(_speedButton);
        controls.AddChild(restart);
        controls.AddChild(_tickLabel);
        bar.AddChild(controls);

        _seek = new HSlider { MinValue = 0, MaxValue = _maxTick, Step = 1 };
        _seek.CustomMinimumSize = new Vector2(0, SeekBarHeight);
        _seek.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _seek.ValueChanged += OnSeek;
        bar.AddChild(_seek);
    }

    private void SyncSeek()
    {
        _syncingSeek = true;
        _seek.Value = (int)_currentTick;
        _syncingSeek = false;
        _tickLabel.Text = $"   tick {(int)_currentTick} / {_maxTick}    outcome: {_result!.Outcome}";
    }

    private void OnSeek(double value)
    {
        if (_syncingSeek)
        {
            return;
        }

        _currentTick = value;
        SetPlaying(false);
        Fold();
        QueueRedraw();
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
