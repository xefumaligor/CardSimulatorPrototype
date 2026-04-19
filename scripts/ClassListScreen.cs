using Godot;

public partial class ClassListScreen : Control
{
    private VBoxContainer      _classList;
    private ConfirmationDialog _confirmDialog;
    private int                _pendingDeleteIndex = -1;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        ClassStore.EnsureSkillsLoaded();
        ClassStore.LoadClasses();
        DeckStore.LoadDecks();

        BuildUI();

        _confirmDialog = new ConfirmationDialog();
        _confirmDialog.Confirmed += OnDeleteConfirmed;
        AddChild(_confirmDialog);
    }

    private void BuildUI()
    {
        var title = new Label();
        title.Text     = "Classes";
        title.Position = new Vector2(50, 28);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var backBtn = new Button();
        backBtn.Text     = "Back to Menu";
        backBtn.Size     = new Vector2(160, 36);
        backBtn.Position = new Vector2(900 - 50 - 160, 26);
        backBtn.Pressed  += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
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
        scroll.Size     = new Vector2(780, 660);
        listPanel.AddChild(scroll);

        _classList = new VBoxContainer();
        _classList.CustomMinimumSize = new Vector2(760, 0);
        _classList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_classList);

        var newClassBtn = new Button();
        newClassBtn.Text     = "+ New Class";
        newClassBtn.Position = new Vector2(10, 685);
        newClassBtn.Size     = new Vector2(200, 40);
        newClassBtn.Pressed  += OnNewClassPressed;
        listPanel.AddChild(newClassBtn);

        RebuildList();
    }

    private void RebuildList()
    {
        foreach (Node child in _classList.GetChildren())
            child.QueueFree();

        if (ClassStore.Classes.Count == 0)
        {
            var empty = new Label();
            empty.Text = "No classes saved yet.";
            empty.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
            empty.CustomMinimumSize = new Vector2(0, 44);
            _classList.AddChild(empty);
            return;
        }

        for (int i = 0; i < ClassStore.Classes.Count; i++)
        {
            int capturedIndex = i;

            var row = new HBoxContainer();
            row.CustomMinimumSize = new Vector2(0, 48);
            row.AddThemeConstantOverride("separation", 10);
            _classList.AddChild(row);

            var classBtn = new Button();
            classBtn.Text               = ClassStore.Classes[i].Name;
            classBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            classBtn.CustomMinimumSize   = new Vector2(0, 44);
            classBtn.Pressed += () => OnClassSelected(capturedIndex);
            row.AddChild(classBtn);

            var delBtn = new Button();
            delBtn.Text              = "✕";
            delBtn.CustomMinimumSize = new Vector2(44, 44);
            delBtn.Pressed += () => ShowDeleteConfirm(capturedIndex);
            row.AddChild(delBtn);
        }
    }

    private void ShowDeleteConfirm(int index)
    {
        _pendingDeleteIndex       = index;
        _confirmDialog.DialogText = $"Delete \"{ClassStore.Classes[index].Name}\"?";
        _confirmDialog.PopupCentered();
    }

    private void OnDeleteConfirmed()
    {
        if (_pendingDeleteIndex < 0) return;
        ClassStore.DeleteClass(_pendingDeleteIndex);
        _pendingDeleteIndex = -1;
        RebuildList();
    }

    private void OnNewClassPressed()
    {
        ClassStore.EditingIndex = -1;
        GetTree().ChangeSceneToFile("res://scenes/ClassManagement.tscn");
    }

    private void OnClassSelected(int index)
    {
        ClassStore.EditingIndex = index;
        GetTree().ChangeSceneToFile("res://scenes/ClassManagement.tscn");
    }
}
