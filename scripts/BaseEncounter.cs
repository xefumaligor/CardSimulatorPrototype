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

    private PackedScene _fireboltScene;

    // ── Player health ─────────────────────────────────────────────────────────

    private HealthBar _playerHealthBar;

    // ── Mob ───────────────────────────────────────────────────────────────────

    private MobActor _mob;

    // ── Deck / queue ─────────────────────────────────────────────────────────

    private List<CardData>  _deckCards  = new();
    private Queue<CardData> _cardQueue  = new();

    // ── HUD state ─────────────────────────────────────────────────────────────

    private CardData[]  _slotCards     = new CardData[HudSlots];
    private Panel[]     _hudPanels     = new Panel[HudSlots];
    private Button[]    _actionButtons      = new Button[4];
    private StyleBoxFlat _actionStyleNormal;
    private StyleBoxFlat _actionStyleHover;
    private StyleBoxFlat _actionStylePressed;
    private ColorRect   _progressFill;
    private double      _elapsed       = 0.0;
    private bool        _duplicateNextCard = false;

    private SkillData[]  _skills             = new SkillData[4];
    private double[]     _skillCooldowns     = new double[4];
    private ColorRect[]  _skillCooldownFills = new ColorRect[4];

    // ─────────────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _fireboltScene = GD.Load<PackedScene>("res://scenes/Firebolt.tscn");

        ClassStore.EnsureSkillsLoaded();
        LoadSkillsFromClass();

        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadDecks();
        LoadDeckCards();

        // Ensure player HP is initialised (fallback for testing without class select)
        if (RunState.PlayerMaxHp <= 0)
            RunState.PlayerMaxHp = RunState.PlayerCurrentHp = 100;

        var player = GetNode<Node2D>("Player");

        // Player health bar
        _playerHealthBar          = new HealthBar();
        _playerHealthBar.Position = new Vector2(0, -28);
        _playerHealthBar.Init(RunState.PlayerCurrentHp, RunState.PlayerMaxHp);
        player.AddChild(_playerHealthBar);

        // Mob spawning
        SpawnMob(player);

        var hud = new CanvasLayer();
        AddChild(hud);
        BuildCardHud(hud);
    }

    // ── Mob spawning ──────────────────────────────────────────────────────────

    private void SpawnMob(Node2D player)
    {
        MobStore.LoadMobs();
        var entry = MobStore.Mobs.Find(m => m.Name == "Goblin Mage");
        if (entry == null) return;

        var deck = BuildDeckFromName(entry.DeckName);
        _mob = new MobActor();
        _mob.InitData(entry, deck, player, _fireboltScene);
        AddChild(_mob);
        _mob.GlobalPosition = new Vector2(0, -240);
    }

    private List<CardData> BuildDeckFromName(string deckName)
    {
        var result    = new List<CardData>();
        var deckEntry = DeckStore.Decks.Find(d => d.Name == deckName);
        if (deckEntry == null) return result;

        var byId = new Dictionary<string, CardData>();
        foreach (var c in DeckStore.AllCards) byId[c.Id] = c;

        var sorted = new List<SlotEntry>(deckEntry.Slots);
        sorted.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        foreach (var s in sorted)
            if (byId.TryGetValue(s.CardId, out var card))
                result.Add(card.Clone());

        return result;
    }

    // ── Player hit callback (called by mob firebolt) ──────────────────────────

    public void OnPlayerHit(int damage)
    {
        RunState.PlayerCurrentHp = Mathf.Max(0, RunState.PlayerCurrentHp - damage);
        _playerHealthBar?.Update(RunState.PlayerCurrentHp, RunState.PlayerMaxHp);
    }

    // ── Draw ──────────────────────────────────────────────────────────────────

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

    // ── Deck / skill loading ──────────────────────────────────────────────────

    private void LoadSkillsFromClass()
    {
        for (int i = 0; i < HudSlots; i++)
            _skills[i] = RunState.Skills[i];
    }

    private void LoadDeckCards()
    {
        if (RunState.Deck.Count == 0) return;
        foreach (var card in RunState.Deck)
            _deckCards.Add(card.Clone());

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

    // ── Input ─────────────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey key || !key.Pressed || key.Echo) return;

        int idx = key.Keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            _        => -1,
        };

        if (key.Keycode == Key.Escape)
        {
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            return;
        }

        if (idx < 0) return;

        if (_actionButtons[idx].Disabled) return;

        var btn = _actionButtons[idx];
        btn.AddThemeStyleboxOverride("normal", _actionStylePressed);
        btn.AddThemeStyleboxOverride("hover",  _actionStylePressed);
        GetTree().CreateTimer(0.12).Timeout += () =>
        {
            btn.AddThemeStyleboxOverride("normal", _actionStyleNormal);
            btn.AddThemeStyleboxOverride("hover",  _actionStyleHover);
        };
        btn.EmitSignal(BaseButton.SignalName.Pressed);
    }

    // ── Per-frame update ──────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        if (_slotCards[0] == null)
            _progressFill.Visible = false;
        else
        {
            _progressFill.Visible = true;
            float useTime = Mathf.Max(_slotCards[0].UseTime, 0.01f);
            _elapsed += delta;
            float fraction = Mathf.Clamp((float)(_elapsed / useTime), 0f, 1f);
            _progressFill.Size = new Vector2(CardW * fraction, _progressFill.Size.Y);
            if (_elapsed >= useTime)
                PlayCurrentCard();
        }

        for (int i = 0; i < 4; i++)
        {
            if (_skillCooldowns[i] <= 0.0) continue;
            _skillCooldowns[i] -= delta;
            if (_skillCooldowns[i] <= 0.0)
            {
                _skillCooldowns[i]             = 0.0;
                _actionButtons[i].Disabled     = false;
                _skillCooldownFills[i].Visible = false;
                _skillCooldownFills[i].Size    = new Vector2(0f, _skillCooldownFills[i].Size.Y);
            }
            else
            {
                float elapsed  = (float)(_skills[i].Cooldown - _skillCooldowns[i]);
                float fraction = Mathf.Clamp(elapsed / _skills[i].Cooldown, 0f, 1f);
                _skillCooldownFills[i].Size = new Vector2(CardW * fraction, _skillCooldownFills[i].Size.Y);
            }
        }
    }

    private void OnSkillActivated(int idx)
    {
        if (_skills[idx] == null) return;
        if (_skillCooldowns[idx] > 0.0) return;
        if (_skills[idx].Cooldown > 0f)
        {
            _skillCooldowns[idx]             = _skills[idx].Cooldown;
            _actionButtons[idx].Disabled     = true;
            _skillCooldownFills[idx].Size    = new Vector2(0f, _skillCooldownFills[idx].Size.Y);
            _skillCooldownFills[idx].Visible = true;
        }
        ApplySkillEffect(_skills[idx]);
    }

    private void ApplySkillEffect(SkillData skill)
    {
        if (skill.Id == "duplicate")
            _duplicateNextCard = true;
    }

    private void PlayCurrentCard()
    {
        var played = _slotCards[0];

        if (_duplicateNextCard && played != null)
        {
            _duplicateNextCard = false;
            var clone = played.Clone();
            _deckCards.Add(clone);
            _slotCards[0] = clone;
            UpdateSlotVisual(_hudPanels[0], clone);
            _elapsed = 0.0;
            _progressFill.Size = new Vector2(0f, _progressFill.Size.Y);
            SpawnCardEffect(played);
            return;
        }

        for (int i = 0; i < HudSlots - 1; i++)
        {
            _slotCards[i] = _slotCards[i + 1];
            UpdateSlotVisual(_hudPanels[i], _slotCards[i]);
        }

        _slotCards[HudSlots - 1] = DequeueNext();
        UpdateSlotVisual(_hudPanels[HudSlots - 1], _slotCards[HudSlots - 1]);

        _elapsed = 0.0;
        _progressFill.Size = new Vector2(0f, _progressFill.Size.Y);

        SpawnCardEffect(played);
    }

    private void SpawnCardEffect(CardData card)
    {
        if (card == null) return;
        if (card.Id == "firebolt") SpawnPlayerFirebolt();
    }

    private void SpawnPlayerFirebolt()
    {
        var player   = GetNode<Node2D>("Player");
        var origin   = player.GlobalPosition;
        var mousePos = GetGlobalMousePosition();
        var dir      = (mousePos - origin).Normalized();

        var bolt = _fireboltScene.Instantiate<Firebolt>();
        bolt.IsPlayerOwned = true;
        bolt.Damage        = 10;
        AddChild(bolt);
        bolt.Init(dir, origin);
    }

    // ── HUD construction ──────────────────────────────────────────────────────

    private void BuildCardHud(CanvasLayer hud)
    {
        int totalW = HudSlots * CardW + (HudSlots - 1) * Gap;
        int startX = (900 - totalW) / 2 + 200;
        int y      = 780;

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

        var progressBg = new ColorRect();
        progressBg.Position    = new Vector2(startX, barY);
        progressBg.Size        = new Vector2(CardW, BarH);
        progressBg.Color       = new Color(0.15f, 0.15f, 0.20f);
        progressBg.MouseFilter = Control.MouseFilterEnum.Ignore;
        hud.AddChild(progressBg);

        _progressFill              = new ColorRect();
        _progressFill.Position     = new Vector2(startX, barY);
        _progressFill.Size         = new Vector2(0f, BarH);
        _progressFill.Color        = new Color(0.30f, 0.65f, 1.00f);
        _progressFill.MouseFilter  = Control.MouseFilterEnum.Ignore;
        hud.AddChild(_progressFill);

        BuildActionButtons(hud, y);
    }

    private void BuildActionButtons(CanvasLayer hud, int y)
    {
        const int StartX = 80;

        _actionStyleNormal = new StyleBoxFlat();
        _actionStyleNormal.BgColor     = new Color(0.18f, 0.18f, 0.22f);
        _actionStyleNormal.BorderColor = new Color(0.38f, 0.38f, 0.48f);
        _actionStyleNormal.SetBorderWidthAll(1);
        _actionStyleNormal.CornerRadiusTopLeft = _actionStyleNormal.CornerRadiusTopRight =
        _actionStyleNormal.CornerRadiusBottomLeft = _actionStyleNormal.CornerRadiusBottomRight = 3;

        _actionStyleHover = new StyleBoxFlat();
        _actionStyleHover.BgColor     = new Color(0.26f, 0.26f, 0.32f);
        _actionStyleHover.BorderColor = new Color(0.55f, 0.55f, 0.68f);
        _actionStyleHover.SetBorderWidthAll(1);
        _actionStyleHover.CornerRadiusTopLeft = _actionStyleHover.CornerRadiusTopRight =
        _actionStyleHover.CornerRadiusBottomLeft = _actionStyleHover.CornerRadiusBottomRight = 3;

        _actionStylePressed = new StyleBoxFlat();
        _actionStylePressed.BgColor     = new Color(0.10f, 0.10f, 0.14f);
        _actionStylePressed.BorderColor = new Color(0.60f, 0.60f, 0.75f);
        _actionStylePressed.SetBorderWidthAll(1);
        _actionStylePressed.CornerRadiusTopLeft = _actionStylePressed.CornerRadiusTopRight =
        _actionStylePressed.CornerRadiusBottomLeft = _actionStylePressed.CornerRadiusBottomRight = 3;

        for (int i = 0; i < 4; i++)
        {
            int capturedI = i;

            var btn = new Button();
            btn.Position = new Vector2(StartX + i * (CardW + Gap), y);
            btn.Size     = new Vector2(CardW, CardH);
            btn.Text     = _skills[i]?.Name ?? "";
            btn.AddThemeStyleboxOverride("normal",  _actionStyleNormal);
            btn.AddThemeStyleboxOverride("hover",   _actionStyleHover);
            btn.AddThemeStyleboxOverride("pressed", _actionStylePressed);
            btn.AddThemeStyleboxOverride("focus",   _actionStyleNormal);
            btn.Pressed  += () => OnSkillActivated(capturedI);

            var num = new Label();
            num.Text        = (i + 1).ToString();
            num.Position    = new Vector2(CardW - 14, CardH - 17);
            num.Size        = new Vector2(12, 14);
            num.AddThemeColorOverride("font_color",   new Color(0.55f, 0.55f, 0.65f));
            num.AddThemeFontSizeOverride("font_size", 10);
            num.MouseFilter = Control.MouseFilterEnum.Ignore;
            btn.AddChild(num);

            _actionButtons[i] = btn;
            hud.AddChild(btn);
        }

        const int BarH   = 6;
        const int BarGap = 3;
        int barY = y + CardH + BarGap;

        for (int i = 0; i < 4; i++)
        {
            float bx = StartX + i * (CardW + Gap);

            var cooldownBg = new ColorRect();
            cooldownBg.Position    = new Vector2(bx, barY);
            cooldownBg.Size        = new Vector2(CardW, BarH);
            cooldownBg.Color       = new Color(0.15f, 0.15f, 0.20f);
            cooldownBg.MouseFilter = Control.MouseFilterEnum.Ignore;
            cooldownBg.Visible     = _skills[i] != null;
            hud.AddChild(cooldownBg);

            var cooldownFill = new ColorRect();
            cooldownFill.Position    = new Vector2(bx, barY);
            cooldownFill.Size        = new Vector2(0f, BarH);
            cooldownFill.Color       = _skills[i]?.Color ?? new Color(0.30f, 0.65f, 1.00f);
            cooldownFill.MouseFilter = Control.MouseFilterEnum.Ignore;
            cooldownFill.Visible     = false;
            hud.AddChild(cooldownFill);

            _skillCooldownFills[i] = cooldownFill;
        }
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
