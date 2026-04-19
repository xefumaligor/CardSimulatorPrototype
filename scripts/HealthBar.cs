using Godot;

public partial class HealthBar : Node2D
{
    private int _current;
    private int _max;

    private const float Width  = 40f;
    private const float Height = 5f;

    public void Init(int current, int max)
    {
        _current = current;
        _max     = max;
    }

    public void Update(int current, int max)
    {
        _current = current;
        _max     = max;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-Width / 2f, -Height, Width, Height), new Color(0.25f, 0.08f, 0.08f));
        float fill = _max > 0 ? Mathf.Clamp((float)_current / _max, 0f, 1f) : 0f;
        if (fill > 0f)
            DrawRect(new Rect2(-Width / 2f, -Height, Width * fill, Height), new Color(0.18f, 0.75f, 0.18f));
    }
}
