using Godot;

public partial class Tooltip : Control
{
    private const int W = 220;
    private const int H = 95;

    private Label _name;
    private Label _tags;
    private Label _desc;

    public override void _Ready()
    {
        Size        = new Vector2(W, H);
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex      = 200;
        Visible     = false;

        var style = new StyleBoxFlat();
        style.BgColor     = new Color(0.06f, 0.06f, 0.12f, 0.20f);
        style.BorderColor = new Color(0.55f, 0.55f, 0.70f, 0.35f);
        style.SetBorderWidthAll(1);
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
        style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 4;

        var panel = new Panel();
        panel.Size        = new Vector2(W, H);
        panel.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        _name = new Label();
        _name.Position = new Vector2(8, 7);
        _name.Size     = new Vector2(W - 16, 20);
        _name.AddThemeColorOverride("font_color",   Colors.White);
        _name.AddThemeFontSizeOverride("font_size", 13);
        _name.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(_name);

        _tags = new Label();
        _tags.Position    = new Vector2(8, 27);
        _tags.Size        = new Vector2(W - 16, 14);
        _tags.AddThemeColorOverride("font_color",   new Color(0.55f, 0.75f, 0.90f));
        _tags.AddThemeFontSizeOverride("font_size", 10);
        _tags.MouseFilter = MouseFilterEnum.Ignore;
        _tags.Visible     = false;
        panel.AddChild(_tags);

        _desc = new Label();
        _desc.Position    = new Vector2(8, 44);
        _desc.Size        = new Vector2(W - 16, H - 50);
        _desc.AutowrapMode = TextServer.AutowrapMode.Word;
        _desc.AddThemeColorOverride("font_color",   new Color(0.80f, 0.80f, 0.80f));
        _desc.AddThemeFontSizeOverride("font_size", 11);
        _desc.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(_desc);
    }

    public void Show(string name, string desc, string tags = "")
    {
        _name.Text    = name;
        _tags.Text    = tags ?? "";
        _tags.Visible = !string.IsNullOrEmpty(tags);
        _desc.Text    = desc ?? "";
        Visible       = true;
    }

    public new void Hide() => Visible = false;

    public void UpdatePosition(Vector2 mouse)
    {
        if (!Visible) return;
        var viewport = GetViewport().GetVisibleRect().Size;
        Position = new Vector2(
            Mathf.Clamp(mouse.X + 14, 0, viewport.X - W),
            Mathf.Clamp(mouse.Y - H - 10, 0, viewport.Y - H));
    }
}
