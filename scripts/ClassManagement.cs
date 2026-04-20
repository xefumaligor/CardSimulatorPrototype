using Godot;
using System;
using System.Collections.Generic;

public partial class ClassManagement : Control
{
    private const int Cols      = 10;
    private const int Rows      = 4;
    private const int CardW     = 75;
    private const int CardH     = 80;
    private const int Gap       = 5;
    private const int MaxSkills = 5;

    private SkillData[] _catalogueSlots  = new SkillData[Cols * Rows];
    private Panel[]     _cataloguePanels = new Panel[Cols * Rows];
    private SkillData[] _selectedSlots   = new SkillData[MaxSkills];
    private Panel[]     _selectedPanels  = new Panel[MaxSkills];

    private Tooltip      _tooltip;
    private SkillData    _heldSkill;
    private Control      _heldSkillDisplay;
    private Label        _heldSkillLabel;
    private ColorRect    _heldSkillBg;
    private Button       _saveButton;
    private LineEdit     _classNameInput;
    private OptionButton _deckDropdown;
    private SpinBox      _healthInput;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        ClassStore.EnsureSkillsLoaded();
        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadDecks();

        InitState();
        BuildUI();
        CreateHeldSkillDisplay();
    }

    // ── State initialisation ──────────────────────────────────────────────────

    private void InitState()
    {
        Array.Clear(_catalogueSlots, 0, _catalogueSlots.Length);
        Array.Clear(_selectedSlots,  0, _selectedSlots.Length);

        int invIdx = 0;
        foreach (var skill in ClassStore.AllSkills)
            if (invIdx < _catalogueSlots.Length)
                _catalogueSlots[invIdx++] = skill;

        if (ClassStore.EditingIndex >= 0 && ClassStore.EditingIndex < ClassStore.Classes.Count)
        {
            foreach (var s in ClassStore.Classes[ClassStore.EditingIndex].Skills)
            {
                var skill = ClassStore.AllSkills.Find(x => x.Id == s.SkillId);
                if (skill != null && s.Slot < MaxSkills)
                    _selectedSlots[s.Slot] = skill;
            }
        }
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        int gridW   = Cols * CardW + (Cols - 1) * Gap;
        int marginX = (900 - gridW) / 2;
        int y       = 10;

        // Class name row
        var nameLabel = new Label();
        nameLabel.Text     = "Class Name:";
        nameLabel.Position = new Vector2(marginX, y + 6);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(nameLabel);

        _classNameInput          = new LineEdit();
        _classNameInput.Position = new Vector2(marginX + 105, y);
        _classNameInput.Size     = new Vector2(220, 30);
        _classNameInput.Text     = ClassStore.EditingIndex >= 0
            ? ClassStore.Classes[ClassStore.EditingIndex].Name
            : ClassStore.NextClassName();
        AddChild(_classNameInput);
        y += 42;

        // Skills catalogue (40 slots)
        y = AddSectionLabel("Skills", marginX, y);
        y = AddCatalogueGrid(marginX, y);
        y += 12;

        // Selected Skills (4 slots)
        y = AddSectionLabel("Selected Skills", marginX, y);
        y = AddSelectedGrid(marginX, y);
        y += 12;

        // Deck dropdown
        var deckLabel = new Label();
        deckLabel.Text     = "Deck";
        deckLabel.Position = new Vector2(marginX, y);
        deckLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        deckLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(deckLabel);
        y += 22;

        _deckDropdown          = new OptionButton();
        _deckDropdown.Position = new Vector2(marginX, y);
        _deckDropdown.Size     = new Vector2(gridW, 30);
        _deckDropdown.AddItem("Select a deck");
        for (int i = 0; i < DeckStore.Decks.Count; i++)
            _deckDropdown.AddItem(DeckStore.Decks[i].Name);

        if (ClassStore.EditingIndex >= 0)
        {
            string savedDeck = ClassStore.Classes[ClassStore.EditingIndex].DeckName;
            for (int i = 0; i < DeckStore.Decks.Count; i++)
            {
                if (DeckStore.Decks[i].Name == savedDeck)
                {
                    _deckDropdown.Selected = i + 1;
                    break;
                }
            }
        }
        AddChild(_deckDropdown);
        y += 42;

        // Health row
        var healthLabel = new Label();
        healthLabel.Text     = "Health:";
        healthLabel.Position = new Vector2(marginX, y + 6);
        healthLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        AddChild(healthLabel);

        _healthInput          = new SpinBox();
        _healthInput.Position = new Vector2(marginX + 105, y);
        _healthInput.Size     = new Vector2(120, 30);
        _healthInput.MinValue = 1;
        _healthInput.MaxValue = 9999;
        _healthInput.Step     = 1;
        _healthInput.Value    = ClassStore.EditingIndex >= 0
            ? ClassStore.Classes[ClassStore.EditingIndex].Health
            : 100;
        AddChild(_healthInput);
        y += 50;

        // Save / Cancel
        _saveButton = new Button();
        _saveButton.Text              = "Save";
        _saveButton.Position          = new Vector2(marginX, y);
        _saveButton.CustomMinimumSize = new Vector2(120, 36);
        _saveButton.Pressed           += OnSavePressed;
        AddChild(_saveButton);

        var cancelBtn = new Button();
        cancelBtn.Text              = "Cancel";
        cancelBtn.Position          = new Vector2(marginX + 130, y);
        cancelBtn.CustomMinimumSize = new Vector2(120, 36);
        cancelBtn.Pressed           += OnCancelPressed;
        AddChild(cancelBtn);
    }

    private int AddSectionLabel(string text, int x, int y)
    {
        var label = new Label();
        label.Text     = text;
        label.Position = new Vector2(x, y);
        label.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        label.AddThemeFontSizeOverride("font_size", 14);
        AddChild(label);
        return y + 22;
    }

    private int AddCatalogueGrid(int marginX, int y)
    {
        for (int i = 0; i < Cols * Rows; i++)
        {
            int col   = i % Cols;
            int row   = i / Cols;
            var panel = CreateSkillPanel(
                marginX + col * (CardW + Gap),
                y       + row * (CardH + Gap),
                CardW, CardH);

            int idx = i;
            panel.GuiInput    += (InputEvent e) => OnCatalogueSlotInput(e, idx);
            panel.MouseEntered += () => { var s = _catalogueSlots[idx]; if (s != null) _tooltip?.Show(s.Name, s.Description); };
            panel.MouseExited  += () => _tooltip?.Hide();
            _cataloguePanels[i] = panel;
            AddChild(panel);
            UpdateSkillVisual(panel, _catalogueSlots[i]);
        }
        return y + Rows * (CardH + Gap) - Gap;
    }

    private int AddSelectedGrid(int marginX, int y)
    {
        for (int i = 0; i < MaxSkills; i++)
        {
            var panel = CreateSkillPanel(
                marginX + i * (CardW + Gap),
                y,
                CardW, CardH);

            int idx = i;
            panel.GuiInput    += (InputEvent e) => OnSelectedSlotInput(e, idx);
            panel.MouseEntered += () => { var s = _selectedSlots[idx]; if (s != null) _tooltip?.Show(s.Name, s.Description); };
            panel.MouseExited  += () => _tooltip?.Hide();
            _selectedPanels[i] = panel;
            AddChild(panel);
            UpdateSkillVisual(panel, _selectedSlots[i]);
        }
        return y + CardH;
    }

    private Panel CreateSkillPanel(int x, int y, int w, int h)
    {
        var panel      = new Panel();
        panel.Position = new Vector2(x, y);
        panel.Size     = new Vector2(w, h);

        var style = new StyleBoxFlat();
        style.BgColor     = new Color(0.18f, 0.18f, 0.22f);
        style.BorderColor = new Color(0.38f, 0.38f, 0.48f);
        style.SetBorderWidthAll(1);
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
        style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 3;
        panel.AddThemeStyleboxOverride("panel", style);

        var bg = new ColorRect();
        bg.Name        = "SkillBg";
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.OffsetLeft  = 2;  bg.OffsetTop    = 2;
        bg.OffsetRight = -2; bg.OffsetBottom = -2;
        bg.Visible     = false;
        bg.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(bg);

        var lbl = new Label();
        lbl.Name                = "SkillName";
        lbl.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.VerticalAlignment   = VerticalAlignment.Center;
        lbl.AutowrapMode        = TextServer.AutowrapMode.Word;
        lbl.AddThemeColorOverride("font_color", Colors.White);
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.Visible     = false;
        lbl.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(lbl);

        return panel;
    }

    private void UpdateSkillVisual(Panel panel, SkillData skill)
    {
        var bg  = panel.GetNode<ColorRect>("SkillBg");
        var lbl = panel.GetNode<Label>("SkillName");

        if (skill == null)
        {
            bg.Visible  = false;
            lbl.Visible = false;
        }
        else
        {
            bg.Color    = skill.Color;
            bg.Visible  = true;
            lbl.Text    = skill.Name;
            lbl.Visible = true;
        }
    }

    private void CreateHeldSkillDisplay()
    {
        _heldSkillDisplay             = new Control();
        _heldSkillDisplay.Size        = new Vector2(CardW, CardH);
        _heldSkillDisplay.MouseFilter = MouseFilterEnum.Ignore;
        _heldSkillDisplay.ZIndex      = 10;
        _heldSkillDisplay.Visible     = false;
        AddChild(_heldSkillDisplay);

        _heldSkillBg             = new ColorRect();
        _heldSkillBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _heldSkillBg.MouseFilter = MouseFilterEnum.Ignore;
        _heldSkillDisplay.AddChild(_heldSkillBg);

        _heldSkillLabel                     = new Label();
        _heldSkillLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _heldSkillLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _heldSkillLabel.VerticalAlignment   = VerticalAlignment.Center;
        _heldSkillLabel.AutowrapMode        = TextServer.AutowrapMode.Word;
        _heldSkillLabel.AddThemeColorOverride("font_color", Colors.White);
        _heldSkillLabel.AddThemeFontSizeOverride("font_size", 11);
        _heldSkillLabel.MouseFilter = MouseFilterEnum.Ignore;
        _heldSkillDisplay.AddChild(_heldSkillLabel);

        _tooltip = new Tooltip();
        AddChild(_tooltip);
    }

    // ── Input handling ────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_heldSkill != null)
            _heldSkillDisplay.Position =
                GetLocalMousePosition() - new Vector2(CardW / 2f, CardH / 2f);
        _tooltip?.UpdatePosition(GetViewport().GetMousePosition());
    }

    private void OnCatalogueSlotInput(InputEvent e, int index)
    {
        if (e is not InputEventMouseButton mb || !mb.Pressed) return;
        if (mb.ButtonIndex != MouseButton.Left) return;
        if (_heldSkill != null) return;
        if (_catalogueSlots[index] == null) return;

        _heldSkill                = _catalogueSlots[index];
        _heldSkillBg.Color        = _heldSkill.Color;
        _heldSkillLabel.Text      = _heldSkill.Name;
        _heldSkillDisplay.Visible = true;
        UpdateSaveButton();
    }

    private void OnSelectedSlotInput(InputEvent e, int index)
    {
        if (e is not InputEventMouseButton mb || !mb.Pressed) return;

        if (mb.ButtonIndex == MouseButton.Right)
        {
            if (_selectedSlots[index] == null) return;
            _selectedSlots[index] = null;
            UpdateSkillVisual(_selectedPanels[index], null);
            ShiftSlotsDown(index);
            return;
        }

        if (mb.ButtonIndex != MouseButton.Left) return;

        if (_heldSkill == null)
        {
            if (_selectedSlots[index] == null) return;
            _heldSkill            = _selectedSlots[index];
            _selectedSlots[index] = null;
            UpdateSkillVisual(_selectedPanels[index], null);
            ShiftSlotsDown(index);

            _heldSkillBg.Color        = _heldSkill.Color;
            _heldSkillLabel.Text      = _heldSkill.Name;
            _heldSkillDisplay.Visible = true;
            UpdateSaveButton();
        }
        else
        {
            if (_selectedSlots[index] == null)
            {
                _selectedSlots[index] = _heldSkill;
                UpdateSkillVisual(_selectedPanels[index], _heldSkill);
            }
            else
            {
                ShiftSlotsUp(index);
                _selectedSlots[index] = _heldSkill;
                UpdateSkillVisual(_selectedPanels[index], _heldSkill);
            }

            _heldSkill                = null;
            _heldSkillDisplay.Visible = false;
            UpdateSaveButton();
        }
    }

    private void ShiftSlotsDown(int fromIndex)
    {
        for (int i = fromIndex; i < MaxSkills - 1; i++)
        {
            _selectedSlots[i] = _selectedSlots[i + 1];
            UpdateSkillVisual(_selectedPanels[i], _selectedSlots[i]);
        }
        _selectedSlots[MaxSkills - 1] = null;
        UpdateSkillVisual(_selectedPanels[MaxSkills - 1], null);
    }

    private void ShiftSlotsUp(int fromIndex)
    {
        for (int i = MaxSkills - 1; i > fromIndex; i--)
        {
            _selectedSlots[i] = _selectedSlots[i - 1];
            UpdateSkillVisual(_selectedPanels[i], _selectedSlots[i]);
        }
    }

    private void UpdateSaveButton()
    {
        if (_saveButton != null)
            _saveButton.Disabled = _heldSkill != null;
    }

    // ── Save / Cancel ─────────────────────────────────────────────────────────

    private void OnSavePressed()
    {
        if (_heldSkill != null) return;

        string deckName = "";
        int sel = _deckDropdown.Selected;
        if (sel > 0 && sel - 1 < DeckStore.Decks.Count)
            deckName = DeckStore.Decks[sel - 1].Name;

        var name  = _classNameInput.Text.Trim();
        var entry = new ClassEntry
        {
            Name     = name.Length > 0 ? name : ClassStore.NextClassName(),
            DeckName = deckName,
            Health   = (int)_healthInput.Value,
            Skills   = new List<ClassSkillEntry>()
        };

        for (int i = 0; i < MaxSkills; i++)
            if (_selectedSlots[i] != null)
                entry.Skills.Add(new ClassSkillEntry { Slot = i, SkillId = _selectedSlots[i].Id });

        if (ClassStore.EditingIndex < 0)
            ClassStore.Classes.Add(entry);
        else
            ClassStore.Classes[ClassStore.EditingIndex] = entry;

        ClassStore.SaveClasses();
        GetTree().ChangeSceneToFile("res://scenes/ClassListScreen.tscn");
    }

    private void OnCancelPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/ClassListScreen.tscn");
    }
}
