using Godot;
using System.Collections.Generic;

public partial class WhirlwindEffect : Area2D
{
    public bool   IsPlayerOwned { get; set; } = true;
    public int    Damage        { get; set; } = 10;
    public float  Radius        { get; set; } = 100f;
    public float  Duration      { get; set; } = 3f;
    public Node2D OwnerRef      { get; set; }  // node this whirlwind follows

    private float _elapsed;
    private float _tickElapsed;

    public override void _Ready()
    {
        var collision   = new CollisionShape2D();
        collision.Shape = new CircleShape2D { Radius = Radius };
        AddChild(collision);

        Monitoring  = true;
        Monitorable = false;

        // Outer filled circle
        var outer   = new Polygon2D();
        outer.Color   = new Color(0.55f, 0.85f, 1.0f, 0.20f);
        outer.Polygon = BuildCirclePoly(Radius, 48);
        AddChild(outer);

        // Spinning blade segments
        for (int i = 0; i < 6; i++)
        {
            var blade   = new Polygon2D();
            blade.Color = new Color(0.75f, 0.95f, 1.0f, 0.45f);
            blade.Polygon = BuildBlade(Radius * 0.85f, Radius * 0.30f, Mathf.DegToRad(22f));
            blade.Rotation = i * Mathf.Tau / 6f;
            AddChild(blade);
        }
    }

    public override void _Process(double delta)
    {
        if (OwnerRef != null && IsInstanceValid(OwnerRef))
            GlobalPosition = OwnerRef.GlobalPosition;

        Rotation += 2.5f * (float)delta;   // ~143 deg/sec spin

        _elapsed     += (float)delta;
        _tickElapsed += (float)delta;

        // Fade out in the last 0.5s
        float lifeRatio = 1f - Mathf.Clamp((_elapsed - (Duration - 0.5f)) / 0.5f, 0f, 1f);
        Modulate = new Color(1f, 1f, 1f, lifeRatio);

        if (_tickElapsed >= 1f)
        {
            _tickElapsed -= 1f;
            DamageTargetsInside();
        }

        if (_elapsed >= Duration)
            QueueFree();
    }

    private void DamageTargetsInside()
    {
        foreach (var body in GetOverlappingBodies())
        {
            if (IsPlayerOwned && body is MobActor mob)
                mob.TakeDamage(Damage);
            else if (!IsPlayerOwned && body is Player)
                (GetParent() as BaseEncounter)?.OnPlayerHit(Damage);
        }
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

    // A curved arc blade: sweeps from innerR to outerR over halfAngle
    private static Vector2[] BuildBlade(float outerR, float innerR, float halfAngle)
    {
        const int steps = 8;
        var pts = new Vector2[steps * 2];
        for (int i = 0; i < steps; i++)
        {
            float a = -halfAngle + i * (2f * halfAngle / (steps - 1));
            pts[i]              = new Vector2(Mathf.Cos(a) * outerR, Mathf.Sin(a) * outerR);
            pts[steps * 2 - 1 - i] = new Vector2(Mathf.Cos(a) * innerR, Mathf.Sin(a) * innerR);
        }
        return pts;
    }
}
