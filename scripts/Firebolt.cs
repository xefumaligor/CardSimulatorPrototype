using Godot;

public partial class Firebolt : Area2D
{
    private const float DefaultSpeed  = 450f;
    private const float DefaultRadius = 6f;
    private const float MaxDist       = 850f;

    public bool  IsPlayerOwned    { get; set; } = true;
    public int   Damage           { get; set; } = 10;
    public float ProjectileRadius { get; set; } = 0f; // 0 = use default
    public float ProjectileSpeed  { get; set; } = 0f; // 0 = use default

    private float   _speed;
    private Vector2 _direction;
    private Vector2 _origin;

    public void Init(Vector2 direction, Vector2 origin)
    {
        _speed = ProjectileSpeed > 0f ? ProjectileSpeed : DefaultSpeed;

        float radius = ProjectileRadius > 0f ? ProjectileRadius : DefaultRadius;
        if (GetNode<CollisionShape2D>("CollisionShape2D").Shape is CircleShape2D circle)
            circle.Radius = radius;
        float scale = radius / DefaultRadius;
        GetNode<Polygon2D>("Visual").Scale = new Vector2(scale, scale);

        _direction     = direction;
        _origin        = origin;
        GlobalPosition = origin;
    }

    public override void _Ready()
    {
        if (IsPlayerOwned)
            BodyEntered += OnBodyEnteredPlayerOwned;
        else
            BodyEntered += OnBodyEnteredMobOwned;
    }

    private void OnBodyEnteredPlayerOwned(Node2D body)
    {
        if (body is MobActor mob)
        {
            mob.TakeDamage(Damage);
            QueueFree();
        }
        else if (body is not Player)
        {
            QueueFree();
        }
    }

    private void OnBodyEnteredMobOwned(Node2D body)
    {
        if (body is Player)
        {
            (GetParent() as BaseEncounter)?.OnPlayerHit(Damage);
            QueueFree();
        }
        else if (body is not MobActor)
        {
            QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        GlobalPosition += _direction * _speed * (float)delta;
        if (GlobalPosition.DistanceTo(_origin) > MaxDist)
            QueueFree();
    }
}
