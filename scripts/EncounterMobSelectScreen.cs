using Godot;

public partial class EncounterMobSelectScreen : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        MobStore.LoadMobs();

        BuildUI();
    }

    private void BuildUI()
    {
        var title = new Label();
        title.Text     = "Select a Mob";
        title.Position = new Vector2(50, 28);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var backBtn = new Button();
        backBtn.Text     = "Cancel";
        backBtn.Size     = new Vector2(160, 36);
        backBtn.Position = new Vector2(900 - 50 - 160, 26);
        backBtn.Pressed += OnCancelPressed;
        AddChild(backBtn);

        var listPanel = new Panel();
        listPanel.Position = new Vector2(50, 80);
        listPanel.Size     = new Vector2(800, 740);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor     = new Color(0.12f, 0.12f, 0.18f);
        panelStyle.BorderColor = new Color(0.30f, 0.30f, 0.40f);
        panelStyle.SetBorderWidthAll(1);
        listPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(listPanel);

        var scroll = new ScrollContainer();
        scroll.Position = new Vector2(10, 10);
        scroll.Size     = new Vector2(780, 720);
        listPanel.AddChild(scroll);

        var mobList = new VBoxContainer();
        mobList.CustomMinimumSize = new Vector2(760, 0);
        mobList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(mobList);

        if (MobStore.Mobs.Count == 0)
        {
            var empty = new Label();
            empty.Text = "No mobs defined. Create mobs in Mob Management first.";
            empty.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
            empty.CustomMinimumSize = new Vector2(0, 44);
            mobList.AddChild(empty);
            return;
        }

        for (int i = 0; i < MobStore.Mobs.Count; i++)
        {
            string mobName = MobStore.Mobs[i].Name;

            var btn = new Button();
            btn.Text                = mobName;
            btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            btn.CustomMinimumSize   = new Vector2(0, 44);
            btn.Pressed            += () => OnMobSelected(mobName);
            mobList.AddChild(btn);
        }
    }

    private void OnMobSelected(string mobName)
    {
        EncounterStore.PendingEntry.Mobs.Add(mobName);
        GetTree().ChangeSceneToFile("res://scenes/EncounterManagement.tscn");
    }

    private void OnCancelPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/EncounterManagement.tscn");
    }
}
