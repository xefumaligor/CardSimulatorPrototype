using Godot;
using System.Collections.Generic;

public partial class CardEditor : Control
{
    private LineEdit           _nameInput;
    private TextEdit           _descInput;
    private SpinBox            _useTimeInput;
    private ColorPickerButton  _colorPicker;
    private List<CheckBox>     _tagCheckboxes = new();
    private CardData           _card;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadTags();

        _card = DeckStore.AllCards.Find(c => c.Id == DeckStore.EditingCardId);
        if (_card == null)
        {
            GetTree().ChangeSceneToFile("res://scenes/CardListScreen.tscn");
            return;
        }

        BuildUI();
    }

    private void BuildUI()
    {
        int x = 50;
        int y = 28;

        var title = new Label();
        title.Text     = "Edit Card";
        title.Position = new Vector2(x, y);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var backBtn = new Button();
        backBtn.Text     = "Back to Cards";
        backBtn.Size     = new Vector2(160, 36);
        backBtn.Position = new Vector2(900 - 50 - 160, 26);
        backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/CardListScreen.tscn");
        AddChild(backBtn);

        y += 60;

        // Card Name
        var nameLabel = new Label();
        nameLabel.Text     = "Name:";
        nameLabel.Position = new Vector2(x, y + 6);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(nameLabel);

        _nameInput          = new LineEdit();
        _nameInput.Position = new Vector2(x + 80, y);
        _nameInput.Size     = new Vector2(300, 32);
        _nameInput.Text     = _card.Name;
        AddChild(_nameInput);

        y += 50;

        // Color
        var colorLabel = new Label();
        colorLabel.Text     = "Color:";
        colorLabel.Position = new Vector2(x, y + 6);
        colorLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(colorLabel);

        _colorPicker          = new ColorPickerButton();
        _colorPicker.Position = new Vector2(x + 80, y);
        _colorPicker.Size     = new Vector2(160, 32);
        _colorPicker.Color    = _card.Color;
        AddChild(_colorPicker);

        y += 50;

        // Use Time
        var useTimeLabel = new Label();
        useTimeLabel.Text     = "Use Time:";
        useTimeLabel.Position = new Vector2(x, y + 6);
        useTimeLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(useTimeLabel);

        _useTimeInput              = new SpinBox();
        _useTimeInput.Position     = new Vector2(x + 100, y);
        _useTimeInput.Size         = new Vector2(160, 32);
        _useTimeInput.MinValue     = 0.1;
        _useTimeInput.MaxValue     = 300.0;
        _useTimeInput.Step         = 0.1;
        _useTimeInput.Value        = _card.UseTime;
        _useTimeInput.CustomMinimumSize = new Vector2(160, 32);
        AddChild(_useTimeInput);

        y += 50;

        // Description
        var descLabel = new Label();
        descLabel.Text     = "Description:";
        descLabel.Position = new Vector2(x, y);
        descLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(descLabel);

        y += 24;

        _descInput          = new TextEdit();
        _descInput.Position = new Vector2(x, y);
        _descInput.Size     = new Vector2(800, 120);
        _descInput.Text     = _card.Text;
        _descInput.WrapMode = TextEdit.LineWrappingMode.Boundary;
        AddChild(_descInput);

        y += 140;

        // Tags
        var tagsLabel = new Label();
        tagsLabel.Text     = "Tags:";
        tagsLabel.Position = new Vector2(x, y);
        tagsLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(tagsLabel);

        y += 28;

        var tagsPanel = new Panel();
        tagsPanel.Position = new Vector2(x, y);
        tagsPanel.Size     = new Vector2(800, 185);
        var tagsPanelStyle = new StyleBoxFlat();
        tagsPanelStyle.BgColor     = new Color(0.12f, 0.12f, 0.18f);
        tagsPanelStyle.BorderColor = new Color(0.30f, 0.30f, 0.40f);
        tagsPanelStyle.SetBorderWidthAll(1);
        tagsPanel.AddThemeStyleboxOverride("panel", tagsPanelStyle);
        AddChild(tagsPanel);

        var tagsGrid = new GridContainer();
        tagsGrid.Position  = new Vector2(8, 8);
        tagsGrid.Columns   = 4;
        tagsGrid.AddThemeConstantOverride("h_separation", 0);
        tagsGrid.AddThemeConstantOverride("v_separation", 0);
        tagsPanel.AddChild(tagsGrid);

        _tagCheckboxes.Clear();
        foreach (var tag in DeckStore.AllTags)
        {
            var cb = new CheckBox();
            cb.Text                = tag;
            cb.ButtonPressed       = _card.Tags.Contains(tag);
            cb.CustomMinimumSize   = new Vector2(190, 40);
            cb.AddThemeColorOverride("font_color", Colors.White);
            tagsGrid.AddChild(cb);
            _tagCheckboxes.Add(cb);
        }

        y += 205;

        var saveBtn = new Button();
        saveBtn.Text              = "Save";
        saveBtn.Position          = new Vector2(x, y);
        saveBtn.CustomMinimumSize = new Vector2(120, 36);
        saveBtn.Pressed           += OnSavePressed;
        AddChild(saveBtn);

        var cancelBtn = new Button();
        cancelBtn.Text              = "Cancel";
        cancelBtn.Position          = new Vector2(x + 130, y);
        cancelBtn.CustomMinimumSize = new Vector2(120, 36);
        cancelBtn.Pressed           += () => GetTree().ChangeSceneToFile("res://scenes/CardListScreen.tscn");
        AddChild(cancelBtn);
    }

    private void OnSavePressed()
    {
        var name = _nameInput.Text.Trim();
        if (name.Length > 0)
            _card.Name = name;

        _card.Text    = _descInput.Text;
        _card.UseTime = (float)_useTimeInput.Value;
        _card.Color   = _colorPicker.Color;

        _card.Tags.Clear();
        for (int i = 0; i < _tagCheckboxes.Count && i < DeckStore.AllTags.Count; i++)
            if (_tagCheckboxes[i].ButtonPressed)
                _card.Tags.Add(DeckStore.AllTags[i]);

        DeckStore.SaveCards();
        GetTree().ChangeSceneToFile("res://scenes/CardListScreen.tscn");
    }
}
