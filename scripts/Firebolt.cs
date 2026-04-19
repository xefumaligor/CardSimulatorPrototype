using Godot;

public partial class Firebolt : Area2D
{
    private const float Speed   = 450f;
    private const float MaxDist = 850f;

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
        BodyEntered += body => { if (body is not Player) QueueFree(); };
    }

    public override void _Process(double delta)
    {
        GlobalPosition += _direction * Speed * (float)delta;
        if (GlobalPosition.DistanceTo(_origin) > MaxDist)
            QueueFree();
    }
}
