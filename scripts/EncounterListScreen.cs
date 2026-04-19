using Godot;

public partial class EncounterListScreen : Control
{
    private VBoxContainer      _encounterList;
    private ConfirmationDialog _confirmDialog;
    private int                _pendingDeleteIndex = -1;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        EncounterStore.LoadEncounters();

        BuildUI();

        _confirmDialog = new ConfirmationDialog();
        _confirmDialog.Confirmed += OnDeleteConfirmed;
        AddChild(_confirmDialog);
    }

    private void BuildUI()
    {
        var title = new Label();
        title.Text     = "Encounters";
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
        scroll.Size     = new Vector2(780, 660);
        listPanel.AddChild(scroll);

        _encounterList = new VBoxContainer();
        _encounterList.CustomMinimumSize = new Vector2(760, 0);
        _encounterList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_encounterList);

        var newBtn = new Button();
        newBtn.Text     = "+ New Encounter";
        newBtn.Position = new Vector2(10, 685);
        newBtn.Size     = new Vector2(200, 40);
        newBtn.Pressed += OnNewEncounterPressed;
        listPanel.AddChild(newBtn);

        RebuildList();
    }

    private void RebuildList()
    {
        foreach (Node child in _encounterList.GetChildren())
            child.QueueFree();

        if (EncounterStore.Encounters.Count == 0)
        {
            var empty = new Label();
            empty.Text = "No encounters saved yet.";
            empty.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
            empty.CustomMinimumSize = new Vector2(0, 44);
            _encounterList.AddChild(empty);
            return;
        }

        for (int i = 0; i < EncounterStore.Encounters.Count; i++)
        {
            int capturedIndex = i;

            var row = new HBoxContainer();
            row.CustomMinimumSize = new Vector2(0, 48);
            row.AddThemeConstantOverride("separation", 10);
            _encounterList.AddChild(row);

            var btn = new Button();
            btn.Text                = EncounterStore.Encounters[i].Name;
            btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            btn.CustomMinimumSize   = new Vector2(0, 44);
            btn.Pressed            += () => OnEncounterSelected(capturedIndex);
            row.AddChild(btn);

            var testBtn = new Button();
            testBtn.Text              = "Test";
            testBtn.CustomMinimumSize = new Vector2(80, 44);
            testBtn.Pressed          += () => OnTestEncounter(capturedIndex);
            row.AddChild(testBtn);

            var delBtn = new Button();
            delBtn.Text              = "✕";
            delBtn.CustomMinimumSize = new Vector2(44, 44);
            delBtn.Pressed          += () => ShowDeleteConfirm(capturedIndex);
            row.AddChild(delBtn);
        }
    }

    private void ShowDeleteConfirm(int index)
    {
        _pendingDeleteIndex       = index;
        _confirmDialog.DialogText = $"Delete \"{EncounterStore.Encounters[index].Name}\"?";
        _confirmDialog.PopupCentered();
    }

    private void OnDeleteConfirmed()
    {
        if (_pendingDeleteIndex < 0) return;
        EncounterStore.DeleteEncounter(_pendingDeleteIndex);
        _pendingDeleteIndex = -1;
        RebuildList();
    }

    private void OnNewEncounterPressed()
    {
        EncounterStore.EditingIndex = -1;
        GetTree().ChangeSceneToFile("res://scenes/EncounterManagement.tscn");
    }

    private void OnEncounterSelected(int index)
    {
        EncounterStore.EditingIndex = index;
        GetTree().ChangeSceneToFile("res://scenes/EncounterManagement.tscn");
    }

    private void OnTestEncounter(int index)
    {
        RunState.CurrentEncounter = EncounterStore.Encounters[index];
        RunState.IsTestMode       = true;
        RunState.TestReturnScene  = "res://scenes/EncounterListScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/ClassSelectScreen.tscn");
    }
}
