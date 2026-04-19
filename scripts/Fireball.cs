using Godot;
using System.Collections.Generic;

public partial class Fireball : Area2D
{
    private const float MaxDist = 850f;

    public bool           IsPlayerOwned    { get; set; } = true;
    public int            AreaDamage       { get; set; } = 5;
    public float          ProjectileRadius { get; set; } = 8f;
    public float          ProjectileSpeed  { get; set; } = 400f;
    public float          BlastRadius      { get; set; } = 80f;
    public float          BurnDuration     { get; set; } = 5f;
    public List<MobActor> Mobs             { get; set; } = new();
    public Node2D         PlayerRef        { get; set; }

    private Vector2 _direction;
    private Vector2 _origin;
    private bool    _exploded;

    public void Init(Vector2 direction, Vector2 origin)
    {
        _direction     = direction;
        _origin        = origin;
        GlobalPosition = origin;
    }

    public override void _Ready()
    {
        var collision   = new CollisionShape2D();
        collision.Shape = new CircleShape2D { Radius = ProjectileRadius };
        AddChild(collision);

        var poly     = new Polygon2D();
        poly.Color   = new Color(1f, 0.45f, 0.05f);
        poly.Polygon = BuildCirclePoly(ProjectileRadius * 1.25f, 12);
        AddChild(poly);

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_exploded) return;
        if (IsPlayerOwned  && body is Player)   return;
        if (!IsPlayerOwned && body is MobActor) return;
        Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        var pos = GlobalPosition;

        if (IsPlayerOwned)
        {
            var toHit = new List<MobActor>();
            foreach (var mob in Mobs)
                if (IsInstanceValid(mob) && mob.GlobalPosition.DistanceTo(pos) <= BlastRadius)
                    toHit.Add(mob);
            foreach (var mob in toHit)
                if (IsInstanceValid(mob))
                    mob.TakeDamage(AreaDamage);
        }
        else
        {
            if (PlayerRef != null && PlayerRef.GlobalPosition.DistanceTo(pos) <= BlastRadius)
                (GetParent() as BaseEncounter)?.OnPlayerHit(AreaDamage);
        }

        var ground = new BurningGroundEffect
        {
            IsPlayerOwned = IsPlayerOwned,
            Mobs          = Mobs,
            PlayerRef     = PlayerRef,
            Radius        = BlastRadius,
            Lifetime      = BurnDuration,
        };
        GetParent().AddChild(ground);
        ground.GlobalPosition = pos;

        QueueFree();
    }

    public override void _Process(double delta)
    {
        if (_exploded) return;
        GlobalPosition += _direction * ProjectileSpeed * (float)delta;
        if (GlobalPosition.DistanceTo(_origin) > MaxDist)
            Explode();
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
