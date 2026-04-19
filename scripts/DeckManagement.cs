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
	private Panel[]    _inventoryPanels = new Panel[Cols * Rows];

	private List<CardData> _deckSlots  = new();
	private List<Panel>    _deckPanels = new();
	private Control        _deckContainer;

	private CardData  _heldCard;
	private Control   _heldCardDisplay;
	private Label     _heldCardLabel;
	private ColorRect _heldCardBg;
	private Button    _saveButton;
	private Button    _cancelButton;
	private LineEdit  _deckNameInput;

	private int     _marginX;
	private int     _deckSectionY;
	private Tooltip _tooltip;

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
		_deckSlots.Clear();

		if (DeckStore.EditingIndex >= 0 && DeckStore.EditingIndex < DeckStore.Decks.Count)
		{
			var sorted = new List<SlotEntry>(DeckStore.Decks[DeckStore.EditingIndex].Slots);
			sorted.Sort((a, b) => a.Slot.CompareTo(b.Slot));
			foreach (var entry in sorted)
			{
				var card = DeckStore.AllCards.Find(c => c.Id == entry.CardId);
				if (card != null) _deckSlots.Add(card);
			}
		}

		int invIdx = 0;
		foreach (var card in DeckStore.AllCards)
			if (invIdx < _inventorySlots.Length)
				_inventorySlots[invIdx++] = card;
	}

	// ── UI construction ───────────────────────────────────────────────────────

	private void BuildUI()
	{
		int gridW = Cols * CardW + (Cols - 1) * Gap;
		_marginX  = (900 - gridW) / 2;
		int y     = 10;

		var nameLabel = new Label();
		nameLabel.Text     = "Deck Name:";
		nameLabel.Position = new Vector2(_marginX, y + 6);
		nameLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
		AddChild(nameLabel);

		_deckNameInput          = new LineEdit();
		_deckNameInput.Position = new Vector2(_marginX + 105, y);
		_deckNameInput.Size     = new Vector2(220, 30);
		_deckNameInput.Text     = DeckStore.EditingIndex >= 0
			? DeckStore.Decks[DeckStore.EditingIndex].Name
			: DeckStore.NextDeckName();
		AddChild(_deckNameInput);
		y += 42;

		y = AddSectionLabel("Cards", _marginX, y);
		y = AddInventoryGrid(_marginX, y);
		y += 12;

		y = AddSectionLabel("Deck", _marginX, y);
		_deckSectionY = y;

		_deckContainer             = new Control();
		_deckContainer.Position    = new Vector2(_marginX, y);
		_deckContainer.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(_deckContainer);

		RebuildDeckGrid();
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

	private int AddInventoryGrid(int marginX, int y)
	{
		for (int i = 0; i < Cols * Rows; i++)
		{
			int col   = i % Cols;
			int row   = i / Cols;
			var panel = CreateSlotPanel(
				marginX + col * (CardW + Gap),
				y       + row * (CardH + Gap),
				CardW, CardH);

			int captured = i;
			panel.GuiInput   += (InputEvent e) => OnSlotInput(e, false, captured);
			panel.MouseEntered += () => { var c = _inventorySlots[captured]; if (c != null) _tooltip?.Show(c.Name, c.Text, string.Join(", ", c.Tags)); };
			panel.MouseExited  += () => _tooltip?.Hide();
			_inventoryPanels[i] = panel;
			AddChild(panel);
			UpdateSlotVisual(panel, _inventorySlots[i]);
		}
		return y + Rows * (CardH + Gap) - Gap;
	}

	// ── Dynamic deck grid ─────────────────────────────────────────────────────

	private void RebuildDeckGrid()
	{
		foreach (var p in _deckPanels)
		{
			_deckContainer.RemoveChild(p);
			p.QueueFree();
		}
		_deckPanels.Clear();

		int count = _deckSlots.Count + 1; // filled slots + one empty trailing slot
		for (int i = 0; i < count; i++)
		{
			int col   = i % Cols;
			int row   = i / Cols;
			var panel = CreateSlotPanel(col * (CardW + Gap), row * (CardH + Gap), CardW, CardH);

			int captured = i;
			panel.GuiInput   += (InputEvent e) => OnSlotInput(e, true, captured);
			if (captured < _deckSlots.Count)
			{
				var card = _deckSlots[captured];
				panel.MouseEntered += () => _tooltip?.Show(card.Name, card.Text, string.Join(", ", card.Tags));
				panel.MouseExited  += () => _tooltip?.Hide();
			}
			_deckPanels.Add(panel);
			_deckContainer.AddChild(panel);
			UpdateSlotVisual(panel, i < _deckSlots.Count ? _deckSlots[i] : null);
		}

		RepositionButtons();
	}

	private int DeckGridHeight()
	{
		int count = _deckSlots.Count + 1;
		int rows  = Mathf.Max(1, (int)Math.Ceiling(count / (float)Cols));
		return rows * (CardH + Gap) - Gap;
	}

	private void RepositionButtons()
	{
		if (_saveButton == null) return;
		int y = _deckSectionY + DeckGridHeight() + 15;
		_saveButton.Position   = new Vector2(_marginX, y);
		_cancelButton.Position = new Vector2(_marginX + 130, y);
	}

	private void AddButtons()
	{
		int y = _deckSectionY + DeckGridHeight() + 15;

		_saveButton                  = new Button();
		_saveButton.Text             = "Save";
		_saveButton.Position         = new Vector2(_marginX, y);
		_saveButton.CustomMinimumSize = new Vector2(120, 36);
		_saveButton.Pressed          += OnSavePressed;
		AddChild(_saveButton);

		_cancelButton                  = new Button();
		_cancelButton.Text             = "Cancel";
		_cancelButton.Position         = new Vector2(_marginX + 130, y);
		_cancelButton.CustomMinimumSize = new Vector2(120, 36);
		_cancelButton.Pressed          += OnCancelPressed;
		AddChild(_cancelButton);
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

		_heldCardLabel                     = new Label();
		_heldCardLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_heldCardLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_heldCardLabel.VerticalAlignment   = VerticalAlignment.Center;
		_heldCardLabel.AutowrapMode        = TextServer.AutowrapMode.Word;
		_heldCardLabel.AddThemeColorOverride("font_color", Colors.White);
		_heldCardLabel.AddThemeFontSizeOverride("font_size", 11);
		_heldCardLabel.MouseFilter = MouseFilterEnum.Ignore;
		_heldCardDisplay.AddChild(_heldCardLabel);

		// Buttons are added last so they render on top of the deck grid.
		AddButtons();

		_tooltip = new Tooltip();
		AddChild(_tooltip);
	}

	// ── Input handling ────────────────────────────────────────────────────────

	public override void _Process(double delta)
	{
		if (_heldCard != null)
			_heldCardDisplay.Position =
				GetLocalMousePosition() - new Vector2(CardW / 2f, CardH / 2f);
		_tooltip?.UpdatePosition(GetViewport().GetMousePosition());
	}

	private void OnSlotInput(InputEvent e, bool isDeck, int index)
	{
		if (e is not InputEventMouseButton mb || !mb.Pressed) return;

		if (!isDeck)
		{
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

		bool isEmptySlot = index >= _deckSlots.Count;

		if (mb.ButtonIndex == MouseButton.Right)
		{
			if (isEmptySlot) return;
			_deckSlots.RemoveAt(index);
			RebuildDeckGrid();
			UpdateSaveButton();
			return;
		}

		if (mb.ButtonIndex != MouseButton.Left) return;

		if (_heldCard == null)
		{
			if (isEmptySlot) return;
			_heldCard = _deckSlots[index];
			_deckSlots.RemoveAt(index);
			RebuildDeckGrid();

			_heldCardBg.Color        = _heldCard.Color;
			_heldCardLabel.Text      = _heldCard.Name;
			_heldCardDisplay.Visible = true;
			UpdateSaveButton();
		}
		else
		{
			if (isEmptySlot)
				_deckSlots.Add(_heldCard);
			else
				_deckSlots.Insert(index, _heldCard);

			_heldCard                = null;
			_heldCardDisplay.Visible = false;
			RebuildDeckGrid();
			UpdateSaveButton();
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

		for (int i = 0; i < _deckSlots.Count; i++)
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
