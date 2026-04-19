using Godot;

public partial class SelectEncounterScreen : Control
{
    private static readonly string[] EncounterNames = { "Corridor", "Small Monster", "Swarm", "Waves", "Boss" };

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var title = new Label();
        title.Text     = "Select Encounter";
        title.Position = new Vector2(50, 28);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var backBtn = new Button();
        backBtn.Text     = "Back to Menu";
        backBtn.Size     = new Vector2(140, 36);
        backBtn.Position = new Vector2(900 - 50 - 140, 26);
        backBtn.Pressed  += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        AddChild(backBtn);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2((900 - 260) / 2f, 180);
        vbox.AddThemeConstantOverride("separation", 14);
        AddChild(vbox);

        foreach (var name in EncounterNames)
        {
            var btn = new Button();
            btn.Text              = name;
            btn.CustomMinimumSize = new Vector2(260, 52);
            vbox.AddChild(btn);
        }
    }
}
