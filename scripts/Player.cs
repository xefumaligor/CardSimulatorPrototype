using Godot;

public partial class Player : CharacterBody2D
{
    private const float Speed = 200f;

    private bool _isBurning;

    public bool IsBurning => _isBurning;

    public void SetBurning(bool burning) => _isBurning = burning;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 dir = Vector2.Zero;

        if (Input.IsKeyPressed(Key.W)) dir.Y -= 1;
        if (Input.IsKeyPressed(Key.S)) dir.Y += 1;
        if (Input.IsKeyPressed(Key.A)) dir.X -= 1;
        if (Input.IsKeyPressed(Key.D)) dir.X += 1;

        Velocity = dir.Normalized() * Speed;
        MoveAndSlide();
    }
}
