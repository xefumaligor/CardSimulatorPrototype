using Godot;

public partial class SkillListScreen : Control
{
    private VBoxContainer _skillList;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        ClassStore.EnsureSkillsLoaded();

        BuildUI();
    }

    private void BuildUI()
    {
        var title = new Label();
        title.Text     = "Skills";
        title.Position = new Vector2(50, 28);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var backBtn = new Button();
        backBtn.Text     = "Back to Menu";
        backBtn.Size     = new Vector2(160, 36);
        backBtn.Position = new Vector2(900 - 50 - 160, 26);
        backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
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

        _skillList = new VBoxContainer();
        _skillList.CustomMinimumSize = new Vector2(760, 0);
        _skillList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_skillList);

        BuildList();
    }

    private void BuildList()
    {
        foreach (Node child in _skillList.GetChildren())
            child.QueueFree();

        if (ClassStore.AllSkills.Count == 0)
        {
            var empty = new Label();
            empty.Text = "No skills available.";
            empty.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
            empty.CustomMinimumSize = new Vector2(0, 44);
            _skillList.AddChild(empty);
            return;
        }

        foreach (var skill in ClassStore.AllSkills)
        {
            string skillId = skill.Id;

            var skillBtn = new Button();
            skillBtn.Text                = skill.Name;
            skillBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            skillBtn.CustomMinimumSize   = new Vector2(0, 44);
            skillBtn.Pressed += () => OnSkillSelected(skillId);
            _skillList.AddChild(skillBtn);
        }
    }

    private void OnSkillSelected(string skillId)
    {
        ClassStore.EditingSkillId = skillId;
        GetTree().ChangeSceneToFile("res://scenes/SkillEditor.tscn");
    }
}
