using Godot;

public partial class EncounterManagement : Control
{
    private LineEdit     _nameInput;
    private SpinBox      _heightInput;
    private SpinBox      _widthInput;
    private OptionButton _typeDropdown;
    private VBoxContainer _mobList;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        EncounterStore.LoadEncounters();
        MobStore.LoadMobs();

        if (!EncounterStore.ComingFromMobSelect)
            EncounterStore.InitPending();
        EncounterStore.ComingFromMobSelect = false;

        BuildUI();
    }

    private void BuildUI()
    {
        var pending = EncounterStore.PendingEntry;
        int x = 50;
        int y = 40;

        // Name
        var nameLabel = new Label();
        nameLabel.Text     = "Encounter Name:";
        nameLabel.Position = new Vector2(x, y + 6);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(nameLabel);

        _nameInput          = new LineEdit();
        _nameInput.Position = new Vector2(x + 150, y);
        _nameInput.Size     = new Vector2(260, 30);
        _nameInput.Text     = pending.Name;
        AddChild(_nameInput);
        y += 50;

        // Height
        var heightLabel = new Label();
        heightLabel.Text     = "Height:";
        heightLabel.Position = new Vector2(x, y + 6);
        heightLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(heightLabel);

        _heightInput          = new SpinBox();
        _heightInput.Position = new Vector2(x + 150, y);
        _heightInput.Size     = new Vector2(120, 30);
        _heightInput.MinValue = 1;
        _heightInput.MaxValue = 9999;
        _heightInput.Step     = 1;
        _heightInput.Value    = pending.Height;
        AddChild(_heightInput);
        y += 50;

        // Width
        var widthLabel = new Label();
        widthLabel.Text     = "Width:";
        widthLabel.Position = new Vector2(x, y + 6);
        widthLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(widthLabel);

        _widthInput          = new SpinBox();
        _widthInput.Position = new Vector2(x + 150, y);
        _widthInput.Size     = new Vector2(120, 30);
        _widthInput.MinValue = 1;
        _widthInput.MaxValue = 9999;
        _widthInput.Step     = 1;
        _widthInput.Value    = pending.Width;
        AddChild(_widthInput);
        y += 50;

        // Type dropdown
        var typeLabel = new Label();
        typeLabel.Text     = "Type:";
        typeLabel.Position = new Vector2(x, y + 6);
        typeLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(typeLabel);

        _typeDropdown          = new OptionButton();
        _typeDropdown.Position = new Vector2(x + 150, y);
        _typeDropdown.Size     = new Vector2(200, 30);
        _typeDropdown.AddItem("Select a Type");
        _typeDropdown.AddItem("Standard");
        _typeDropdown.AddItem("Waves");
        if (pending.Type == "Standard")      _typeDropdown.Select(1);
        else if (pending.Type == "Waves")    _typeDropdown.Select(2);
        else                                 _typeDropdown.Select(0);
        AddChild(_typeDropdown);
        y += 60;

        // Mob list panel
        var mobsLabel = new Label();
        mobsLabel.Text     = "Mobs:";
        mobsLabel.Position = new Vector2(x, y);
        mobsLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(mobsLabel);
        y += 28;

        var mobPanel = new Panel();
        mobPanel.Position = new Vector2(x, y);
        mobPanel.Size     = new Vector2(560, 280);
        var mobPanelStyle = new StyleBoxFlat();
        mobPanelStyle.BgColor     = new Color(0.12f, 0.12f, 0.18f);
        mobPanelStyle.BorderColor = new Color(0.30f, 0.30f, 0.40f);
        mobPanelStyle.SetBorderWidthAll(1);
        mobPanel.AddThemeStyleboxOverride("panel", mobPanelStyle);
        AddChild(mobPanel);

        var scroll = new ScrollContainer();
        scroll.Position = new Vector2(6, 6);
        scroll.Size     = new Vector2(548, 268);
        mobPanel.AddChild(scroll);

        _mobList = new VBoxContainer();
        _mobList.CustomMinimumSize = new Vector2(528, 0);
        _mobList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_mobList);

        y += 290;

        var addMobBtn = new Button();
        addMobBtn.Text     = "+ Add Mob";
        addMobBtn.Position = new Vector2(x, y);
        addMobBtn.Size     = new Vector2(160, 36);
        addMobBtn.Pressed += OnAddMobPressed;
        AddChild(addMobBtn);
        y += 56;

        var saveBtn = new Button();
        saveBtn.Text     = "Save";
        saveBtn.Position = new Vector2(x, y);
        saveBtn.Size     = new Vector2(120, 36);
        saveBtn.Pressed += OnSavePressed;
        AddChild(saveBtn);

        var cancelBtn = new Button();
        cancelBtn.Text     = "Cancel";
        cancelBtn.Position = new Vector2(x + 130, y);
        cancelBtn.Size     = new Vector2(120, 36);
        cancelBtn.Pressed += OnCancelPressed;
        AddChild(cancelBtn);

        var saveTestBtn = new Button();
        saveTestBtn.Text     = "Save and Test";
        saveTestBtn.Position = new Vector2(x + 260, y);
        saveTestBtn.Size     = new Vector2(160, 36);
        saveTestBtn.Pressed += OnSaveAndTestPressed;
        AddChild(saveTestBtn);

        RebuildMobList();
    }

    private void RebuildMobList()
    {
        foreach (Node child in _mobList.GetChildren())
            child.QueueFree();

        var mobs = EncounterStore.PendingEntry.Mobs;

        if (mobs.Count == 0)
        {
            var empty = new Label();
            empty.Text = "No mobs added yet.";
            empty.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
            empty.CustomMinimumSize = new Vector2(0, 36);
            _mobList.AddChild(empty);
            return;
        }

        for (int i = 0; i < mobs.Count; i++)
        {
            int capturedIndex = i;

            var row = new HBoxContainer();
            row.CustomMinimumSize = new Vector2(0, 36);
            row.AddThemeConstantOverride("separation", 8);
            _mobList.AddChild(row);

            var nameLabel = new Label();
            nameLabel.Text                = mobs[i];
            nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            nameLabel.AddThemeColorOverride("font_color", Colors.White);
            nameLabel.VerticalAlignment   = VerticalAlignment.Center;
            nameLabel.CustomMinimumSize   = new Vector2(0, 36);
            row.AddChild(nameLabel);

            var delBtn = new Button();
            delBtn.Text              = "✕";
            delBtn.CustomMinimumSize = new Vector2(36, 36);
            delBtn.Pressed          += () =>
            {
                EncounterStore.PendingEntry.Mobs.RemoveAt(capturedIndex);
                RebuildMobList();
            };
            row.AddChild(delBtn);
        }
    }

    private void SaveFormToPending()
    {
        EncounterStore.PendingEntry.Name   = _nameInput.Text.Trim();
        EncounterStore.PendingEntry.Height = (int)_heightInput.Value;
        EncounterStore.PendingEntry.Width  = (int)_widthInput.Value;
        EncounterStore.PendingEntry.Type   = _typeDropdown.Selected > 0
            ? _typeDropdown.GetItemText(_typeDropdown.Selected)
            : "";
    }

    private void OnAddMobPressed()
    {
        SaveFormToPending();
        EncounterStore.ComingFromMobSelect = true;
        GetTree().ChangeSceneToFile("res://scenes/EncounterMobSelectScreen.tscn");
    }

    private void OnSavePressed()
    {
        if (_typeDropdown.Selected == 0) return;

        SaveFormToPending();

        var pending = EncounterStore.PendingEntry;
        string name = pending.Name.Length == 0 ? EncounterStore.NextEncounterName() : pending.Name;

        var entry = new EncounterEntry
        {
            Name   = name,
            Type   = pending.Type,
            Height = pending.Height,
            Width  = pending.Width,
            Mobs   = new System.Collections.Generic.List<string>(pending.Mobs),
        };

        if (EncounterStore.EditingIndex < 0)
            EncounterStore.Encounters.Add(entry);
        else
            EncounterStore.Encounters[EncounterStore.EditingIndex] = entry;

        EncounterStore.SaveEncounters();
        GetTree().ChangeSceneToFile("res://scenes/EncounterListScreen.tscn");
    }

    private void OnCancelPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/EncounterListScreen.tscn");
    }

    private void OnSaveAndTestPressed()
    {
        if (_typeDropdown.Selected == 0) return;

        SaveFormToPending();

        var pending = EncounterStore.PendingEntry;
        string name = pending.Name.Length == 0 ? EncounterStore.NextEncounterName() : pending.Name;

        var entry = new EncounterEntry
        {
            Name   = name,
            Type   = pending.Type,
            Height = pending.Height,
            Width  = pending.Width,
            Mobs   = new System.Collections.Generic.List<string>(pending.Mobs),
        };

        if (EncounterStore.EditingIndex < 0)
        {
            EncounterStore.Encounters.Add(entry);
            EncounterStore.EditingIndex = EncounterStore.Encounters.Count - 1;
        }
        else
        {
            EncounterStore.Encounters[EncounterStore.EditingIndex] = entry;
        }
        EncounterStore.SaveEncounters();

        RunState.CurrentEncounter = entry;
        RunState.IsTestMode       = true;
        RunState.TestReturnScene  = "res://scenes/EncounterManagement.tscn";
        GetTree().ChangeSceneToFile("res://scenes/ClassSelectScreen.tscn");
    }
}
