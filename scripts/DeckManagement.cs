using Godot;
using System;
using System.Collections.Generic;

public partial class DeckManagement : Control
{
	private const int Cols  = 10;
	private const int Rows  = 4;
	private const int CardW = 75;
	private const int CardH = 80;
	private const int Gap   = 5;

	private CardData[] _inventorySlots  = new CardData[Cols * Rows];
	private CardData[] _deckSlots       = new CardData[Cols * Rows];
	private Panel[]    _inventoryPanels = new Panel[Cols * Rows];
	private Panel[]    _deckPanels      = new Panel[Cols * Rows];

	private CardData  _heldCard;
	private Control   _heldCardDisplay;
	private Label     _heldCardLabel;
	private ColorRect _heldCardBg;
	private Button    _saveButton;
	private LineEdit  _deckNameInput;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var bg = new ColorRect();
		bg.Color = new Color(0.08f, 0.08f, 0.12f);
		bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		DeckStore.EnsureCardsLoaded();
		InitState();
		BuildUI();
		CreateHeldCardDisplay();
	}

	// ── State initialisation ──────────────────────────────────────────────────

	private void InitState()
	{
		Array.Clear(_inventorySlots, 0, _inventorySlots.Length);
		Array.Clear(_deckSlots,      0, _deckSlots.Length);

		// Populate deck slots from the saved deck (if editing an existing one).
		if (DeckStore.EditingIndex >= 0 && DeckStore.EditingIndex < DeckStore.Decks.Count)
		{
			foreach (var entry in DeckStore.Decks[DeckStore.EditingIndex].Slots)
			{
				var card = DeckStore.AllCards.Find(c => c.Id == entry.CardId);
				if (card != null && entry.Slot < _deckSlots.Length)
					_deckSlots[entry.Slot] = card;
			}
		}

		// Cards section always shows every available card.
		int invIdx = 0;
		foreach (var card in DeckStore.AllCards)
			if (invIdx < _inventorySlots.Length)
				_inventorySlots[invIdx++] = card;
	}

	// ── UI construction ───────────────────────────────────────────────────────

	private void BuildUI()
	{
		int gridW   = Cols * CardW + (Cols - 1) * Gap;
		int marginX = (900 - gridW) / 2;
		int y       = 10;

		// Deck name row
		var nameLabel = new Label();
		nameLabel.Text     = "Deck Name:";
		nameLabel.Position = new Vector2(marginX, y + 6);
		nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
		AddChild(nameLabel);

		_deckNameInput          = new LineEdit();
		_deckNameInput.Position = new Vector2(marginX + 105, y);
		_deckNameInput.Size     = new Vector2(220, 30);
		_deckNameInput.Text     = DeckStore.EditingIndex >= 0
			? DeckStore.Decks[DeckStore.EditingIndex].Name
			: DeckStore.NextDeckName();
		AddChild(_deckNameInput);
		y += 42;

		y = AddSectionLabel("Cards", marginX, y);
		y = AddGrid(_inventorySlots, _inventoryPanels, false, marginX, y);
		y += 12;

		y = AddSectionLabel("Deck", marginX, y);
		y = AddGrid(_deckSlots, _deckPanels, true, marginX, y);
		y += 15;

		AddButtons(marginX, y);
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

	private int AddGrid(CardData[] slots, Panel[] panels, bool isDeck, int marginX, int y)
	{
		for (int i = 0; i < Cols * Rows; i++)
		{
			int col   = i % Cols;
			int row   = i / Cols;
			var panel = CreateSlotPanel(
				marginX + col * (CardW + Gap),
				y       + row * (CardH + Gap),
				CardW, CardH);

			int capturedIndex = i;
			panel.GuiInput += (InputEvent e) => OnSlotInput(e, isDeck, capturedIndex);
			panels[i] = panel;
			AddChild(panel);
			UpdateSlotVisual(panel, slots[i]);
		}
		return y + Rows * (CardH + Gap) - Gap;
	}

	private void AddButtons(int x, int y)
	{
		_saveButton = new Button();
		_saveButton.Text             = "Save";
		_saveButton.Position         = new Vector2(x, y);
		_saveButton.CustomMinimumSize = new Vector2(120, 36);
		_saveButton.Pressed          += OnSavePressed;
		AddChild(_saveButton);

		var cancelBtn = new Button();
		cancelBtn.Text              = "Cancel";
		cancelBtn.Position          = new Vector2(x + 130, y);
		cancelBtn.CustomMinimumSize = new Vector2(120, 36);
		cancelBtn.Pressed           += OnCancelPressed;
		AddChild(cancelBtn);
	}

	private Panel CreateSlotPanel(int x, int y, int w, int h)
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

		var cardBg = new ColorRect();
		cardBg.Name        = "CardBg";
		cardBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		cardBg.OffsetLeft  = 2;  cardBg.OffsetTop    = 2;
		cardBg.OffsetRight = -2; cardBg.OffsetBottom = -2;
		cardBg.Visible     = false;
		cardBg.MouseFilter = MouseFilterEnum.Ignore;
		panel.AddChild(cardBg);

		var lbl = new Label();
		lbl.Name                = "CardName";
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

	private void UpdateSlotVisual(Panel panel, CardData card)
	{
		var cardBg = panel.GetNode<ColorRect>("CardBg");
		var lbl    = panel.GetNode<Label>("CardName");

		if (card == null)
		{
			cardBg.Visible = false;
			lbl.Visible    = false;
		}
		else
		{
			cardBg.Color   = card.Color;
			cardBg.Visible = true;
			lbl.Text       = card.Name;
			lbl.Visible    = true;
		}
	}

	private void CreateHeldCardDisplay()
	{
		_heldCardDisplay             = new Control();
		_heldCardDisplay.Size        = new Vector2(CardW, CardH);
		_heldCardDisplay.MouseFilter = MouseFilterEnum.Ignore;
		_heldCardDisplay.ZIndex      = 10;
		_heldCardDisplay.Visible     = false;
		AddChild(_heldCardDisplay);

		_heldCardBg             = new ColorRect();
		_heldCardBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_heldCardBg.MouseFilter = MouseFilterEnum.Ignore;
		_heldCardDisplay.AddChild(_heldCardBg);

		_heldCardLabel                    = new Label();
		_heldCardLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_heldCardLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_heldCardLabel.VerticalAlignment   = VerticalAlignment.Center;
		_heldCardLabel.AutowrapMode        = TextServer.AutowrapMode.Word;
		_heldCardLabel.AddThemeColorOverride("font_color", Colors.White);
		_heldCardLabel.AddThemeFontSizeOverride("font_size", 11);
		_heldCardLabel.MouseFilter = MouseFilterEnum.Ignore;
		_heldCardDisplay.AddChild(_heldCardLabel);
	}

	// ── Input handling ────────────────────────────────────────────────────────

	public override void _Process(double delta)
	{
		if (_heldCard != null)
			_heldCardDisplay.Position =
				GetLocalMousePosition() - new Vector2(CardW / 2f, CardH / 2f);
	}

	private void OnSlotInput(InputEvent e, bool isDeck, int index)
	{
		if (e is not InputEventMouseButton mb || !mb.Pressed) return;

		if (!isDeck)
		{
			// Cards section: left-click copies card to hand; right-click does nothing.
			if (mb.ButtonIndex != MouseButton.Left) return;
			if (_heldCard != null) return;
			if (_inventorySlots[index] == null) return;

			_heldCard                = _inventorySlots[index];
			_heldCardBg.Color        = _heldCard.Color;
			_heldCardLabel.Text      = _heldCard.Name;
			_heldCardDisplay.Visible = true;
			UpdateSaveButton();
			return;
		}

		// ── Deck section ──────────────────────────────────────────────────────

		if (mb.ButtonIndex == MouseButton.Right)
		{
			// Right-click: remove card and close the gap.
			if (_deckSlots[index] == null) return;
			_deckSlots[index] = null;
			UpdateSlotVisual(_deckPanels[index], null);
			ShiftDeckDown(index);
			return;
		}

		if (mb.ButtonIndex != MouseButton.Left) return;

		if (_heldCard == null)
		{
			// Pick up card from deck and close the gap.
			if (_deckSlots[index] == null) return;
			_heldCard         = _deckSlots[index];
			_deckSlots[index] = null;
			UpdateSlotVisual(_deckPanels[index], null);
			ShiftDeckDown(index);

			_heldCardBg.Color        = _heldCard.Color;
			_heldCardLabel.Text      = _heldCard.Name;
			_heldCardDisplay.Visible = true;
			UpdateSaveButton();
		}
		else
		{
			if (_deckSlots[index] == null)
			{
				// Empty slot: place directly.
				_deckSlots[index] = _heldCard;
				UpdateSlotVisual(_deckPanels[index], _heldCard);
			}
			else
			{
				// Occupied slot: shift existing cards up to make room, then insert.
				ShiftDeckUp(index);
				_deckSlots[index] = _heldCard;
				UpdateSlotVisual(_deckPanels[index], _heldCard);
			}

			_heldCard                = null;
			_heldCardDisplay.Visible = false;
			UpdateSaveButton();
		}
	}

	// Closes the gap at fromIndex by pulling subsequent cards one slot back.
	private void ShiftDeckDown(int fromIndex)
	{
		for (int i = fromIndex; i < _deckSlots.Length - 1; i++)
		{
			_deckSlots[i] = _deckSlots[i + 1];
			UpdateSlotVisual(_deckPanels[i], _deckSlots[i]);
		}
		int last = _deckSlots.Length - 1;
		_deckSlots[last] = null;
		UpdateSlotVisual(_deckPanels[last], null);
	}

	// Makes room at fromIndex by pushing all subsequent cards one slot forward.
	private void ShiftDeckUp(int fromIndex)
	{
		for (int i = _deckSlots.Length - 1; i > fromIndex; i--)
		{
			_deckSlots[i] = _deckSlots[i - 1];
			UpdateSlotVisual(_deckPanels[i], _deckSlots[i]);
		}
	}

	private void UpdateSaveButton()
	{
		if (_saveButton != null)
			_saveButton.Disabled = _heldCard != null;
	}

	// ── Save / Cancel ─────────────────────────────────────────────────────────

	private void OnSavePressed()
	{
		if (_heldCard != null) return;

		var name  = _deckNameInput.Text.Trim();
		var entry = new DeckEntry
		{
			Name  = name.Length > 0 ? name : DeckStore.NextDeckName(),
			Slots = new List<SlotEntry>()
		};

		for (int i = 0; i < _deckSlots.Length; i++)
			if (_deckSlots[i] != null)
				entry.Slots.Add(new SlotEntry { Slot = i, CardId = _deckSlots[i].Id });

		if (DeckStore.EditingIndex < 0)
			DeckStore.Decks.Add(entry);
		else
			DeckStore.Decks[DeckStore.EditingIndex] = entry;

		DeckStore.SaveDecks();
		GetTree().ChangeSceneToFile("res://scenes/DeckListScreen.tscn");
	}

	private void OnCancelPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/DeckListScreen.tscn");
	}
}
