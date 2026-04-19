using Godot;
using System.Collections.Generic;

public partial class BaseEncounter : Node2D
{
    private const int RoomOffsetY = -90;
    private const int TileSize    = 70;
    private const int CardW       = 75;
    private const int CardH       = 80;
    private const int Gap         = 5;
    private const int HudSlots    = 4;

    private static readonly Color TileA = new(0.15f, 0.15f, 0.20f);
    private static readonly Color TileB = new(0.12f, 0.12f, 0.17f);

    private int _roomHalfX;
    private int _roomHalfY;

    private PackedScene _fireboltScene;

    // ── Player health ─────────────────────────────────────────────────────────

    private HealthBar _playerHealthBar;

    // ── Mobs ──────────────────────────────────────────────────────────────────

    private List<MobActor> _mobs = new();

    // ── Deck / queue ─────────────────────────────────────────────────────────

    private List<CardData> _deckCards  = new();
    private int            _deckIndex  = 0;

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

    private CanvasLayer _hud;
    private bool        _encounterOver = false;

    // ─────────────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _fireboltScene = GD.Load<PackedScene>("res://scenes/Firebolt.tscn");

        var encounter = RunState.CurrentEncounter;
        _roomHalfX = encounter != null ? encounter.Width  / 2 : 350;
        _roomHalfY = encounter != null ? encounter.Height / 2 : 350;

        ApplyRoomSize();

        ClassStore.EnsureSkillsLoaded();
        LoadSkillsFromClass();

        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadDecks();
        LoadDeckCards();

        if (RunState.PlayerMaxHp <= 0)
            RunState.PlayerMaxHp = RunState.PlayerCurrentHp = 100;

        var player = GetNode<Node2D>("Player");
        player.GlobalPosition = RandomPlayerPosition();

        _playerHealthBar          = new HealthBar();
        _playerHealthBar.Position = new Vector2(0, -28);
        _playerHealthBar.Init(RunState.PlayerCurrentHp, RunState.PlayerMaxHp);
        player.AddChild(_playerHealthBar);

        SpawnMobs(player);

        _hud = new CanvasLayer();
        AddChild(_hud);
        BuildCardHud(_hud);
    }

    // ── Room size ─────────────────────────────────────────────────────────────

    private void ApplyRoomSize()
    {
        var room = GetNode<Node2D>("Room");
        int w = _roomHalfX * 2;
        int h = _roomHalfY * 2;

        SetWall(room, "Wall_Top",
            new Vector2(0, -_roomHalfY),
            new Vector2(w + 40, 20),
            new Vector2[] { new(-w / 2f - 20, -10), new(w / 2f + 20, -10), new(w / 2f + 20, 10), new(-w / 2f - 20, 10) });

        SetWall(room, "Wall_Bottom",
            new Vector2(0, _roomHalfY),
            new Vector2(w + 40, 20),
            new Vector2[] { new(-w / 2f - 20, -10), new(w / 2f + 20, -10), new(w / 2f + 20, 10), new(-w / 2f - 20, 10) });

        SetWall(room, "Wall_Left",
            new Vector2(-_roomHalfX, 0),
            new Vector2(20, h + 40),
            new Vector2[] { new(-10, -h / 2f - 20), new(10, -h / 2f - 20), new(10, h / 2f + 20), new(-10, h / 2f + 20) });

        SetWall(room, "Wall_Right",
            new Vector2(_roomHalfX, 0),
            new Vector2(20, h + 40),
            new Vector2[] { new(-10, -h / 2f - 20), new(10, -h / 2f - 20), new(10, h / 2f + 20), new(-10, h / 2f + 20) });

        QueueRedraw();
    }

    private static void SetWall(Node2D room, string name, Vector2 pos, Vector2 shapeSize, Vector2[] poly)
    {
        var wall = room.GetNode<StaticBody2D>(name);
        wall.Position = pos;
        ((RectangleShape2D)wall.GetNode<CollisionShape2D>("CollisionShape2D").Shape).Size = shapeSize;
        wall.GetNode<Polygon2D>("Polygon2D").Polygon = poly;
    }

    // ── Player placement ──────────────────────────────────────────────────────

    private Vector2 RandomPlayerPosition()
    {
        const int margin = 200;
        float x = (float)GD.RandRange(-_roomHalfX + margin, _roomHalfX - margin);
        float y = (float)GD.RandRange(RoomOffsetY - _roomHalfY + margin, RoomOffsetY + _roomHalfY - margin);
        return new Vector2(x, y);
    }

    // ── Mob spawning ──────────────────────────────────────────────────────────

    private void SpawnMobs(Node2D player)
    {
        MobStore.LoadMobs();
        var encounter = RunState.CurrentEncounter;
        var mobNames  = encounter?.Mobs ?? new List<string> { "Goblin Mage" };

        var usedPositions = new List<Vector2> { player.GlobalPosition };

        foreach (string mobName in mobNames)
        {
            var entry = MobStore.Mobs.Find(m => m.Name == mobName);
            if (entry == null) continue;

            var pos  = RandomMobPosition(usedPositions);
            var deck = BuildDeckFromName(entry.DeckName);
            var mob  = new MobActor();
            mob.InitData(entry, deck, player, _fireboltScene);
            mob.OnDied += m => OnMobDied(m);
            AddChild(mob);
            mob.GlobalPosition = pos;
            _mobs.Add(mob);
            usedPositions.Add(pos);
        }
    }

    private Vector2 RandomMobPosition(List<Vector2> occupied)
    {
        const int wallMargin    = 60;
        const int minFromPlayer = 100;
        const int minFromMob    = 60;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            float x = (float)GD.RandRange(-_roomHalfX + wallMargin, _roomHalfX - wallMargin);
            float y = (float)GD.RandRange(RoomOffsetY - _roomHalfY + wallMargin, RoomOffsetY + _roomHalfY - wallMargin);
            var   p = new Vector2(x, y);

            bool ok = true;
            for (int i = 0; i < occupied.Count; i++)
            {
                float minDist = i == 0 ? minFromPlayer : minFromMob;
                if (p.DistanceTo(occupied[i]) < minDist) { ok = false; break; }
            }
            if (ok) return p;
        }
        return new Vector2(_roomHalfX / 2f, RoomOffsetY);
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

    // ── Encounter end detection ───────────────────────────────────────────────

    private void OnMobDied(MobActor mob)
    {
        _mobs.Remove(mob);
        if (_mobs.Count == 0)
            ShowResultModal(victory: true);
    }

    private void FreezeGameplay()
    {
        GetNode<Node2D>("Player").ProcessMode = ProcessModeEnum.Disabled;
        foreach (var mob in _mobs)
            if (IsInstanceValid(mob)) mob.ProcessMode = ProcessModeEnum.Disabled;
        foreach (var child in GetChildren())
            if (child is Firebolt bolt) bolt.ProcessMode = ProcessModeEnum.Disabled;
    }

    private void ShowResultModal(bool victory)
    {
        if (_encounterOver) return;
        _encounterOver = true;

        FreezeGameplay();

        var overlay = new ColorRect();
        overlay.Color       = new Color(0f, 0f, 0f, 0.65f);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        _hud.AddChild(overlay);

        var panel = new Panel();
        panel.Size     = new Vector2(400, 200);
        panel.Position = new Vector2((900 - 400) / 2f, (860 - 200) / 2f);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor     = new Color(0.12f, 0.12f, 0.18f);
        panelStyle.BorderColor = new Color(0.45f, 0.45f, 0.60f);
        panelStyle.SetBorderWidthAll(2);
        panelStyle.CornerRadiusTopLeft = panelStyle.CornerRadiusTopRight =
        panelStyle.CornerRadiusBottomLeft = panelStyle.CornerRadiusBottomRight = 6;
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        overlay.AddChild(panel);

        var title = new Label();
        title.Text                = victory ? "Victory!" : "Defeat";
        title.Position            = new Vector2(0, 50);
        title.Size                = new Vector2(400, 60);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color",   victory ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.35f, 0.35f));
        title.AddThemeFontSizeOverride("font_size", 36);
        panel.AddChild(title);

        var continueBtn = new Button();
        continueBtn.Text     = "Continue";
        continueBtn.Size     = new Vector2(160, 44);
        continueBtn.Position = new Vector2((400 - 160) / 2f, 130);
        continueBtn.Pressed += () => OnContinuePressed(victory);
        panel.AddChild(continueBtn);
    }

    private void OnContinuePressed(bool victory)
    {
        if (RunState.IsTestMode)
        {
            GetTree().ChangeSceneToFile(RunState.TestReturnScene);
            return;
        }

        if (!victory)
        {
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            return;
        }

        EncounterStore.LoadEncounters();
        int nextIndex = RunState.EncounterIndex + 1;
        string nextName = $"Level{nextIndex}";
        var next = EncounterStore.Encounters.Find(e => e.Name == nextName);
        if (next == null)
            next = EncounterStore.Encounters.Find(e => e.Name == "boss");

        if (next != null)
        {
            RunState.CurrentEncounter = next;
            RunState.EncounterIndex   = nextIndex;
            GetTree().ChangeSceneToFile("res://scenes/BaseEncounter.tscn");
        }
        else
        {
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        }
    }

    // ── Player hit callback (called by mob firebolt) ──────────────────────────

    public void OnPlayerHit(int damage)
    {
        if (_encounterOver) return;
        RunState.PlayerCurrentHp = Mathf.Max(0, RunState.PlayerCurrentHp - damage);
        _playerHealthBar?.Update(RunState.PlayerCurrentHp, RunState.PlayerMaxHp);
        if (RunState.PlayerCurrentHp <= 0)
            ShowResultModal(victory: false);
    }

    // ── Draw ──────────────────────────────────────────────────────────────────

    public override void _Draw()
    {
        int startX = -_roomHalfX;
        int startY = RoomOffsetY - _roomHalfY;
        int endX   =  _roomHalfX;
        int endY   = RoomOffsetY + _roomHalfY;

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

        _deckIndex = 0;
        for (int i = 0; i < HudSlots; i++)
            _slotCards[i] = GetDeckCardAt(i);
    }

    private CardData GetDeckCardAt(int offset)
    {
        if (_deckCards.Count == 0) return null;
        return _deckCards[((offset % _deckCards.Count) + _deckCards.Count) % _deckCards.Count];
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < HudSlots; i++)
        {
            _slotCards[i] = GetDeckCardAt(_deckIndex + i);
            UpdateSlotVisual(_hudPanels[i], _slotCards[i]);
        }
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

        if (idx < 0 || _encounterOver) return;

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
        if (_encounterOver) return;

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
        if (_deckCards.Count == 0) return;
        var played = _slotCards[0];

        if (_duplicateNextCard && played != null)
        {
            _duplicateNextCard = false;
            var clone = played.Clone();
            _deckCards.Insert(_deckIndex + 1, clone);
            _deckIndex = (_deckIndex + 1) % _deckCards.Count;
            RefreshSlots();
            _elapsed = 0.0;
            _progressFill.Size = new Vector2(0f, _progressFill.Size.Y);
            SpawnCardEffect(played);
            return;
        }

        _deckIndex = (_deckIndex + 1) % _deckCards.Count;
        RefreshSlots();
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
