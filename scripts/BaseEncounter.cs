using Godot;
using System.Collections.Generic;

public partial class BaseEncounter : Node2D
{
    private const int RoomHalf    = 350;
    private const int RoomOffsetY = -90;
    private const int TileSize    = 70;
    private const int CardW       = 75;
    private const int CardH       = 80;
    private const int Gap         = 5;
    private const int HudSlots    = 4;

    private static readonly Color TileA = new(0.15f, 0.15f, 0.20f);
    private static readonly Color TileB = new(0.12f, 0.12f, 0.17f);

    // ── Deck / queue ─────────────────────────────────────────────────────────

    private List<CardData>  _deckCards  = new();
    private Queue<CardData> _cardQueue  = new();

    // ── HUD state ─────────────────────────────────────────────────────────────

    private CardData[]  _slotCards     = new CardData[HudSlots];
    private Panel[]     _hudPanels     = new Panel[HudSlots];
    private ColorRect   _progressFill;
    private double      _elapsed       = 0.0;

    // ─────────────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        DeckStore.EnsureCardsLoaded();
        LoadDeckCards();

        var hud = new CanvasLayer();
        AddChild(hud);
        BuildCardHud(hud);
    }

    public override void _Draw()
    {
        int startX = -RoomHalf;
        int startY = RoomOffsetY - RoomHalf;
        int endX   =  RoomHalf;
        int endY   = RoomOffsetY + RoomHalf;

        for (int x = startX; x < endX; x += TileSize)
        for (int y = startY; y < endY; y += TileSize)
        {
            bool even = (((x - startX) / TileSize) + ((y - startY) / TileSize)) % 2 == 0;
            DrawRect(new Rect2(x, y, TileSize, TileSize), even ? TileA : TileB);
        }
    }

    // ── Deck loading ──────────────────────────────────────────────────────────

    private void LoadDeckCards()
    {
        if (DeckStore.ActiveDeck == null) return;

        var byId = new Dictionary<string, CardData>();
        foreach (var c in DeckStore.AllCards)
            byId[c.Id] = c;

        var entries = new List<SlotEntry>(DeckStore.ActiveDeck.Slots);
        entries.Sort((a, b) => a.Slot.CompareTo(b.Slot));

        foreach (var entry in entries)
        {
            CardData card;
            if (byId.TryGetValue(entry.CardId, out card))
                _deckCards.Add(card);
        }

        RefillQueue();

        for (int i = 0; i < HudSlots; i++)
            _slotCards[i] = DequeueNext();
    }

    private void RefillQueue()
    {
        foreach (var card in _deckCards)
            _cardQueue.Enqueue(card);
    }

    private CardData DequeueNext()
    {
        if (_cardQueue.Count == 0)
        {
            if (_deckCards.Count == 0) return null;
            RefillQueue();
        }
        return _cardQueue.Count > 0 ? _cardQueue.Dequeue() : null;
    }

    // ── Per-frame update ──────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_slotCards[0] == null)
        {
            _progressFill.Visible = false;
            return;
        }

        _progressFill.Visible = true;

        float useTime = Mathf.Max(_slotCards[0].UseTime, 0.01f);
        _elapsed += delta;

        float fraction = Mathf.Clamp((float)(_elapsed / useTime), 0f, 1f);
        _progressFill.Size = new Vector2(CardW * fraction, _progressFill.Size.Y);

        if (_elapsed >= useTime)
            PlayCurrentCard();
    }

    private void PlayCurrentCard()
    {
        // Card effect placeholder — currently no gameplay action.

        // Shift slots 1-3 into slots 0-2.
        for (int i = 0; i < HudSlots - 1; i++)
        {
            _slotCards[i] = _slotCards[i + 1];
            UpdateSlotVisual(_hudPanels[i], _slotCards[i]);
        }

        // Fill the last slot from the queue (wraps to deck start when empty).
        _slotCards[HudSlots - 1] = DequeueNext();
        UpdateSlotVisual(_hudPanels[HudSlots - 1], _slotCards[HudSlots - 1]);

        _elapsed = 0.0;
        _progressFill.Size = new Vector2(0f, _progressFill.Size.Y);
    }

    // ── HUD construction ──────────────────────────────────────────────────────

    private void BuildCardHud(CanvasLayer hud)
    {
        int totalW = HudSlots * CardW + (HudSlots - 1) * Gap;
        int startX = (900 - totalW) / 2;
        int y      = 722;

        for (int i = 0; i < HudSlots; i++)
        {
            var panel = CreateSlotPanel(startX + i * (CardW + Gap), y);
            UpdateSlotVisual(panel, _slotCards[i]);
            _hudPanels[i] = panel;
            hud.AddChild(panel);
        }

        const int BarH   = 6;
        const int BarGap = 3;
        int barY = y + CardH + BarGap;

        // Progress bar background (full width under slot 0).
        var progressBg = new ColorRect();
        progressBg.Position    = new Vector2(startX, barY);
        progressBg.Size        = new Vector2(CardW, BarH);
        progressBg.Color       = new Color(0.15f, 0.15f, 0.20f);
        progressBg.MouseFilter = Control.MouseFilterEnum.Ignore;
        hud.AddChild(progressBg);

        // Progress bar fill (grows right as card is used).
        _progressFill              = new ColorRect();
        _progressFill.Position     = new Vector2(startX, barY);
        _progressFill.Size         = new Vector2(0f, BarH);
        _progressFill.Color        = new Color(0.30f, 0.65f, 1.00f);
        _progressFill.MouseFilter  = Control.MouseFilterEnum.Ignore;
        hud.AddChild(_progressFill);
    }

    private Panel CreateSlotPanel(int x, int y)
    {
        var panel      = new Panel();
        panel.Position = new Vector2(x, y);
        panel.Size     = new Vector2(CardW, CardH);

        var style = new StyleBoxFlat();
        style.BgColor     = new Color(0.18f, 0.18f, 0.22f);
        style.BorderColor = new Color(0.38f, 0.38f, 0.48f);
        style.SetBorderWidthAll(1);
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
        style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 3;
        panel.AddThemeStyleboxOverride("panel", style);

        var cardBg = new ColorRect();
        cardBg.Name        = "CardBg";
        cardBg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        cardBg.OffsetLeft  = 2;  cardBg.OffsetTop    = 2;
        cardBg.OffsetRight = -2; cardBg.OffsetBottom = -2;
        cardBg.Visible     = false;
        cardBg.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.AddChild(cardBg);

        var lbl = new Label();
        lbl.Name                = "CardName";
        lbl.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.VerticalAlignment   = VerticalAlignment.Center;
        lbl.AutowrapMode        = TextServer.AutowrapMode.Word;
        lbl.AddThemeColorOverride("font_color", Colors.White);
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.Visible     = false;
        lbl.MouseFilter = Control.MouseFilterEnum.Ignore;
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
}
