using Godot;
using System.Collections.Generic;

public partial class CleaveAttack : Node2D
{
    public const float Range    = 150f;
    private const float Duration = 0.30f;
    private const int   Segments = 16;

    public int Damage { get; set; } = 15;

    private Polygon2D _visual;
    private float     _elapsed;

    public override void _Ready()
    {
        _visual         = BuildFanPolygon();
        _visual.Scale   = new Vector2(0.1f, 0.1f);
        AddChild(_visual);
    }

    public void Init(Vector2 origin, Vector2 direction, List<MobActor> mobs)
    {
        GlobalPosition = origin;
        Rotation       = direction.Angle();

        foreach (var mob in mobs)
        {
            if (!IsInstanceValid(mob)) continue;
            var  toMob = mob.GlobalPosition - origin;
            float dist = toMob.Length();
            float dot  = dist > 0f ? toMob.Normalized().Dot(direction) : 0f;
            if (dist <= Range && dot >= 0f)
                mob.TakeDamage(Damage);
        }
    }

    private static Polygon2D BuildFanPolygon()
    {
        // Fan from -90° to +90° (local space faces right = forward).
        const float halfArc = Mathf.Pi / 2f;
        var pts = new Vector2[Segments + 2];
        pts[0]  = Vector2.Zero;
        for (int i = 0; i <= Segments; i++)
        {
            float a = -halfArc + i * (Mathf.Pi / Segments);
            pts[i + 1] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Range;
        }

        var poly   = new Polygon2D();
        poly.Polygon = pts;
        poly.Color   = new Color(0.95f, 0.90f, 0.35f, 0.55f);
        return poly;
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float t = Mathf.Clamp(_elapsed / Duration, 0f, 1f);
        _visual.Scale    = new Vector2(t, t);
        _visual.Modulate = new Color(1f, 1f, 1f, 1f - t);
        if (_elapsed >= Duration)
            QueueFree();
    }
}
