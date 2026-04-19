using Godot;
using System.Collections.Generic;

public partial class BurningGroundEffect : Area2D
{
    private const float PlayerTickRate   = 0.5f;
    private const int   PlayerTickDamage = 1;   // 2 dmg/sec

    public bool           IsPlayerOwned { get; set; } = true;
    public float          Radius        { get; set; } = 80f;
    public float          Lifetime      { get; set; } = 5f;
    public List<MobActor> Mobs          { get; set; } = new();
    public Node2D         PlayerRef     { get; set; }

    private readonly HashSet<MobActor> _mobsInside = new();
    private bool      _playerInside;
    private float     _elapsed;
    private float     _playerTickElapsed;
    private Polygon2D _visual;

    public override void _Ready()
    {
        var collision   = new CollisionShape2D();
        collision.Shape = new CircleShape2D { Radius = Radius };
        AddChild(collision);

        Monitoring  = true;
        Monitorable = false;

        _visual         = new Polygon2D();
        _visual.Color   = new Color(1f, 0.35f, 0.0f, 0.40f);
        _visual.Polygon = BuildCirclePoly(Radius, 32);
        AddChild(_visual);

        BodyEntered += OnBodyEntered;
        BodyExited  += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (IsPlayerOwned)
        {
            if (body is MobActor mob) { _mobsInside.Add(mob); mob.SetBurning(true); }
        }
        else
        {
            if (body is Player) _playerInside = true;
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (IsPlayerOwned)
        {
            if (body is not MobActor mob) return;
            _mobsInside.Remove(mob);
            if (!IsInstanceValid(mob)) return;
            mob.SetBurning(IsStillBurning(mob));
        }
        else
        {
            if (body is Player) _playerInside = false;
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;

        float lifeRatio = 1f - Mathf.Clamp(_elapsed / Lifetime, 0f, 1f);
        _visual.Color   = new Color(1f, 0.35f, 0.0f, 0.40f * lifeRatio);

        if (!IsPlayerOwned && _playerInside)
        {
            _playerTickElapsed += (float)delta;
            if (_playerTickElapsed >= PlayerTickRate)
            {
                _playerTickElapsed -= PlayerTickRate;
                (GetParent() as BaseEncounter)?.OnPlayerHit(PlayerTickDamage);
            }
        }

        if (_elapsed >= Lifetime)
        {
            ClearAllBurning();
            QueueFree();
        }
    }

    private void ClearAllBurning()
    {
        foreach (var mob in _mobsInside)
            if (IsInstanceValid(mob))
                mob.SetBurning(IsStillBurning(mob));
        _mobsInside.Clear();
    }

    private bool IsStillBurning(MobActor mob)
    {
        foreach (var child in GetParent().GetChildren())
        {
            if (child == this) continue;
            if (child is BurningGroundEffect other && other._mobsInside.Contains(mob))
                return true;
        }
        return false;
    }

    private static Vector2[] BuildCirclePoly(float r, int segments)
    {
        var pts = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.Tau / segments;
            pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        return pts;
    }
}
