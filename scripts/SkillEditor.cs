using Godot;
using System.Collections.Generic;

public partial class SkillEditor : Control
{
    private LineEdit          _nameInput;
    private TextEdit          _descInput;
    private SpinBox           _cooldownInput;
    private ColorPickerButton _colorPicker;
    private Dictionary<int, SpinBox> _valueInputs = new();
    private SkillData         _skill;

    private static readonly Dictionary<string, (int idx, string label)[]> ValueDefs = new()
    {
        // Add skill-specific value labels here as new skills are implemented.
        // ["duplicate"] = new[] { (1, "Number of Copies") },
    };

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        ClassStore.EnsureSkillsLoaded();

        _skill = ClassStore.AllSkills.Find(s => s.Id == ClassStore.EditingSkillId);
        if (_skill == null)
        {
            GetTree().ChangeSceneToFile("res://scenes/SkillListScreen.tscn");
            return;
        }

        BuildUI();
    }

    private void BuildUI()
    {
        int x = 50;
        int y = 28;

        var title = new Label();
        title.Text     = "Edit Skill";
        title.Position = new Vector2(x, y);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var backBtn = new Button();
        backBtn.Text     = "Back to Skills";
        backBtn.Size     = new Vector2(160, 36);
        backBtn.Position = new Vector2(900 - 50 - 160, 26);
        backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/SkillListScreen.tscn");
        AddChild(backBtn);

        y += 60;

        // Name
        var nameLabel = new Label();
        nameLabel.Text     = "Name:";
        nameLabel.Position = new Vector2(x, y + 6);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(nameLabel);

        _nameInput          = new LineEdit();
        _nameInput.Position = new Vector2(x + 80, y);
        _nameInput.Size     = new Vector2(300, 32);
        _nameInput.Text     = _skill.Name;
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
        _colorPicker.Color    = _skill.Color;
        AddChild(_colorPicker);

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
        _descInput.Text     = _skill.Description;
        _descInput.WrapMode = TextEdit.LineWrappingMode.Boundary;
        AddChild(_descInput);

        y += 140;

        // Cooldown
        var cooldownLabel = new Label();
        cooldownLabel.Text     = "Cooldown:";
        cooldownLabel.Position = new Vector2(x, y + 6);
        cooldownLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(cooldownLabel);

        _cooldownInput              = new SpinBox();
        _cooldownInput.Position     = new Vector2(x + 100, y);
        _cooldownInput.Size         = new Vector2(160, 32);
        _cooldownInput.MinValue     = 0.0;
        _cooldownInput.MaxValue     = 300.0;
        _cooldownInput.Step         = 0.1;
        _cooldownInput.Value        = _skill.Cooldown;
        _cooldownInput.CustomMinimumSize = new Vector2(160, 32);
        AddChild(_cooldownInput);

        y += 55;

        // Balance Values
        if (ValueDefs.TryGetValue(_skill.Id, out var defs))
        {
            var valLabel = new Label();
            valLabel.Text     = "Balance Values:";
            valLabel.Position = new Vector2(x, y);
            valLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
            AddChild(valLabel);
            y += 30;

            int col   = 0;
            int row   = 0;
            int colW  = 400;
            int rowH  = 44;
            int labelW = 200;
            int spinW  = 160;

            foreach (var (idx, lbl) in defs)
            {
                int cx = x + col * colW;
                int cy = y + row * rowH;

                var entryLabel = new Label();
                entryLabel.Text     = lbl + ":";
                entryLabel.Position = new Vector2(cx, cy + 6);
                entryLabel.Size     = new Vector2(labelW, 24);
                entryLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
                AddChild(entryLabel);

                var spin = new SpinBox();
                spin.Position  = new Vector2(cx + labelW, cy);
                spin.Size      = new Vector2(spinW, 32);
                spin.MinValue  = 0;
                spin.MaxValue  = 10000;
                spin.Step      = 0.1;
                spin.Value     = _skill.Values[idx - 1];
                spin.CustomMinimumSize = new Vector2(spinW, 32);
                AddChild(spin);

                _valueInputs[idx] = spin;

                col = 1 - col;
                if (col == 0) row++;
            }

            int usedRows = (defs.Length + 1) / 2;
            y += usedRows * rowH + 20;
        }

        // Save / Cancel
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
        cancelBtn.Pressed           += () => GetTree().ChangeSceneToFile("res://scenes/SkillListScreen.tscn");
        AddChild(cancelBtn);
    }

    private void OnSavePressed()
    {
        var name = _nameInput.Text.Trim();
        if (name.Length > 0)
            _skill.Name = name;

        _skill.Description = _descInput.Text;
        _skill.Cooldown    = (float)_cooldownInput.Value;
        _skill.Color       = _colorPicker.Color;

        foreach (var (idx, spin) in _valueInputs)
            _skill.Values[idx - 1] = (float)spin.Value;

        ClassStore.SaveSkills();
        GetTree().ChangeSceneToFile("res://scenes/SkillListScreen.tscn");
    }
}
