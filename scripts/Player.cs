using Godot;

public partial class Player : CharacterBody2D
{
    private const float Speed      = 200f;
    private const float DashSpeed  = 1200f;

    private bool    _isBurning;
    private float   _dashTimer    = 0f;
    private Vector2 _dashVelocity = Vector2.Zero;

    public bool    IsBurning         => _isBurning;
    public Vector2 LastMoveDirection { get; private set; } = Vector2.Right;

    public void SetBurning(bool burning) => _isBurning = burning;

    public void StartDash(Vector2 direction, float distance)
    {
        _dashVelocity = direction * DashSpeed;
        _dashTimer    = distance / DashSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_dashTimer > 0f)
        {
            _dashTimer -= (float)delta;
            Velocity    = _dashVelocity;
            MoveAndSlide();
            return;
        }

        Vector2 dir = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W)) dir.Y -= 1;
        if (Input.IsKeyPressed(Key.S)) dir.Y += 1;
        if (Input.IsKeyPressed(Key.A)) dir.X -= 1;
        if (Input.IsKeyPressed(Key.D)) dir.X += 1;

        if (dir != Vector2.Zero)
            LastMoveDirection = dir.Normalized();

        Velocity = dir.Normalized() * Speed;
        MoveAndSlide();
    }
}
