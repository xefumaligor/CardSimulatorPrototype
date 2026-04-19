using Godot;

public partial class Firebolt : Area2D
{
    private const float Speed   = 450f;
    private const float MaxDist = 850f;

    public bool IsPlayerOwned { get; set; } = true;
    public int  Damage        { get; set; } = 10;

    private Vector2 _direction;
    private Vector2 _origin;

    public void Init(Vector2 direction, Vector2 origin)
    {
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
        GlobalPosition += _direction * Speed * (float)delta;
        if (GlobalPosition.DistanceTo(_origin) > MaxDist)
            QueueFree();
    }
}
