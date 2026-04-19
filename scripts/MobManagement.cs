using Godot;

public partial class MobManagement : Control
{
    private LineEdit          _nameInput;
    private ColorPickerButton _colorPicker;
    private SpinBox           _sizeInput;
    private SpinBox           _healthInput;
    private OptionButton      _behaviorDropdown;
    private OptionButton      _deckDropdown;
    private Button            _saveButton;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        MobStore.LoadMobs();
        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadDecks();

        BuildUI();
    }

    private void BuildUI()
    {
        bool editing = MobStore.EditingIndex >= 0 && MobStore.EditingIndex < MobStore.Mobs.Count;
        MobEntry existing = editing ? MobStore.Mobs[MobStore.EditingIndex] : null;

        int x = 50;
        int y = 40;

        // Name row
        var nameLabel = new Label();
        nameLabel.Text     = "Mob Name:";
        nameLabel.Position = new Vector2(x, y + 6);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(nameLabel);

        _nameInput          = new LineEdit();
        _nameInput.Position = new Vector2(x + 105, y);
        _nameInput.Size     = new Vector2(220, 30);
        _nameInput.Text     = existing != null ? existing.Name : MobStore.NextMobName();
        AddChild(_nameInput);
        y += 50;

        // Color row
        var colorLabel = new Label();
        colorLabel.Text     = "Color:";
        colorLabel.Position = new Vector2(x, y + 6);
        colorLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(colorLabel);

        _colorPicker          = new ColorPickerButton();
        _colorPicker.Position = new Vector2(x + 105, y);
        _colorPicker.Size     = new Vector2(120, 30);
        _colorPicker.Color    = existing != null ? existing.Color : Colors.White;
        AddChild(_colorPicker);
        y += 50;

        // Size row
        var sizeLabel = new Label();
        sizeLabel.Text     = "Size:";
        sizeLabel.Position = new Vector2(x, y + 6);
        sizeLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(sizeLabel);

        _sizeInput             = new SpinBox();
        _sizeInput.Position    = new Vector2(x + 105, y);
        _sizeInput.Size        = new Vector2(120, 30);
        _sizeInput.MinValue    = 1;
        _sizeInput.MaxValue    = 9999;
        _sizeInput.Step        = 1;
        _sizeInput.Value       = existing != null ? existing.Size : 1;
        AddChild(_sizeInput);
        y += 50;

        // Health row
        var healthLabel = new Label();
        healthLabel.Text     = "Health:";
        healthLabel.Position = new Vector2(x, y + 6);
        healthLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(healthLabel);

        _healthInput          = new SpinBox();
        _healthInput.Position = new Vector2(x + 105, y);
        _healthInput.Size     = new Vector2(120, 30);
        _healthInput.MinValue = 1;
        _healthInput.MaxValue = 9999;
        _healthInput.Step     = 1;
        _healthInput.Value    = existing != null ? existing.Health : 50;
        AddChild(_healthInput);
        y += 50;

        // Behavior dropdown
        var behaviorLabel = new Label();
        behaviorLabel.Text     = "Behavior:";
        behaviorLabel.Position = new Vector2(x, y + 6);
        behaviorLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(behaviorLabel);

        _behaviorDropdown          = new OptionButton();
        _behaviorDropdown.Position = new Vector2(x + 105, y);
        _behaviorDropdown.Size     = new Vector2(200, 30);
        _behaviorDropdown.AddItem("Select a behavior");
        _behaviorDropdown.AddItem("Aggressive");
        _behaviorDropdown.AddItem("Kiting");
        if (existing != null)
        {
            if (existing.BehaviorName == "Aggressive") _behaviorDropdown.Select(1);
            else if (existing.BehaviorName == "Kiting") _behaviorDropdown.Select(2);
        }
        AddChild(_behaviorDropdown);
        y += 50;

        // Deck dropdown
        var deckLabel = new Label();
        deckLabel.Text     = "Deck:";
        deckLabel.Position = new Vector2(x, y + 6);
        deckLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(deckLabel);

        _deckDropdown          = new OptionButton();
        _deckDropdown.Position = new Vector2(x + 105, y);
        _deckDropdown.Size     = new Vector2(200, 30);
        _deckDropdown.AddItem("Select a deck");
        int selectedDeckIdx = 0;
        for (int i = 0; i < DeckStore.Decks.Count; i++)
        {
            _deckDropdown.AddItem(DeckStore.Decks[i].Name);
            if (existing != null && DeckStore.Decks[i].Name == existing.DeckName)
                selectedDeckIdx = i + 1;
        }
        _deckDropdown.Select(selectedDeckIdx);
        AddChild(_deckDropdown);
        y += 60;

        // Save / Cancel buttons
        _saveButton          = new Button();
        _saveButton.Text     = "Save";
        _saveButton.Position = new Vector2(x, y);
        _saveButton.Size     = new Vector2(120, 36);
        _saveButton.Pressed += OnSavePressed;
        AddChild(_saveButton);

        var cancelBtn = new Button();
        cancelBtn.Text     = "Cancel";
        cancelBtn.Position = new Vector2(x + 130, y);
        cancelBtn.Size     = new Vector2(120, 36);
        cancelBtn.Pressed += OnCancelPressed;
        AddChild(cancelBtn);
    }

    private void OnSavePressed()
    {
        if (_behaviorDropdown.Selected == 0 || _deckDropdown.Selected == 0) return;

        string name = _nameInput.Text.Trim();
        if (name.Length == 0) name = MobStore.NextMobName();

        var color = _colorPicker.Color;
        var entry = new MobEntry
        {
            Name         = name,
            R            = color.R,
            G            = color.G,
            B            = color.B,
            Size         = (int)_sizeInput.Value,
            Health       = (int)_healthInput.Value,
            BehaviorName = _behaviorDropdown.GetItemText(_behaviorDropdown.Selected),
            DeckName     = _deckDropdown.GetItemText(_deckDropdown.Selected),
        };

        if (MobStore.EditingIndex < 0)
            MobStore.Mobs.Add(entry);
        else
            MobStore.Mobs[MobStore.EditingIndex] = entry;

        MobStore.SaveMobs();
        GetTree().ChangeSceneToFile("res://scenes/MobListScreen.tscn");
    }

    private void OnCancelPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MobListScreen.tscn");
    }
}
