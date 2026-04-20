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

    private ColorRect _playerHpFill;
    private Label     _playerHpLabel;

    // ── Mobs ──────────────────────────────────────────────────────────────────

    private List<MobActor> _mobs = new();

    // ── Deck / queue ─────────────────────────────────────────────────────────

    private List<CardData> _deckCards  = new();
    private int            _deckIndex  = 0;

    // ── HUD state ─────────────────────────────────────────────────────────────

    private CardData[]  _slotCards     = new CardData[HudSlots];
    private Panel[]     _hudPanels     = new Panel[HudSlots];
    private Button[]    _actionButtons      = new Button[5];
    private StyleBoxFlat _actionStyleNormal;
    private StyleBoxFlat _actionStyleHover;
    private StyleBoxFlat _actionStylePressed;
    private ColorRect   _progressFill;
    private double      _elapsed       = 0.0;
    private bool        _duplicateNextCard = false;

    // ── Buffs / turn state ────────────────────────────────────────────────────

    private List<ActiveBuff> _playerBuffs     = new();
    private int              _turnNumber      = 0;
    private bool             _turnActive      = false;
    private float            _spellDamageMult  = 1.0f;
    private float            _aoeAreaMult      = 1.0f;
    private float            _attackDamageMult = 1.0f;
    private Control          _buffContainer;
    private Panel            _tooltip;
    private Label            _tooltipName;
    private Label            _tooltipTags;
    private Label            _tooltipDesc;

    private SkillData[]  _skills             = new SkillData[5];
    private double[]     _skillCooldowns     = new double[5];
    private ColorRect[]  _skillCooldownFills = new ColorRect[5];

    private CanvasLayer _hud;
    private bool        _encounterOver    = false;
    private bool        _burnHover        = false;
    private bool        _playerWasBurning = false;

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
            mob.InitData(entry, deck, player, _fireboltScene, _mobs);
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
        {
            if (child is Firebolt bolt)             bolt.ProcessMode      = ProcessModeEnum.Disabled;
            if (child is Fireball fb)               fb.ProcessMode        = ProcessModeEnum.Disabled;
            if (child is BurningGroundEffect bge)   bge.ProcessMode       = ProcessModeEnum.Disabled;
            if (child is WhirlwindEffect ww)        ww.ProcessMode        = ProcessModeEnum.Disabled;
        }
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

        var currentName = RunState.CurrentEncounter?.Name ?? "";
        bool currentIsLevel = currentName.StartsWith("Level", System.StringComparison.OrdinalIgnoreCase);
        if (!currentIsLevel)
        {
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            return;
        }

        EncounterStore.LoadEncounters();
        int nextIndex = RunState.EncounterIndex + 1;
        string nextName = $"Level{nextIndex}";
        var next = EncounterStore.Encounters.Find(e => string.Equals(e.Name, nextName, System.StringComparison.OrdinalIgnoreCase));
        if (next == null)
            next = EncounterStore.Encounters.Find(e => !e.Name.StartsWith("Level", System.StringComparison.OrdinalIgnoreCase));

        if (next != null)
        {
            RunState.CurrentEncounter = next;
            RunState.EncounterIndex   = nextIndex;
            GetTree().ChangeSceneToFile("res://scenes/EncounterPreviewScreen.tscn");
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
        UpdatePlayerHpBar();
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
            bool  even  = (((x - startX) / TileSize) + ((y - startY) / TileSize)) % 2 == 0;
            float tileW = Mathf.Min(TileSize, endX - x);
            float tileH = Mathf.Min(TileSize, endY - y);
            DrawRect(new Rect2(x, y, tileW, tileH), even ? TileA : TileB);
        }
    }

    // ── Deck / skill loading ──────────────────────────────────────────────────

    private void LoadSkillsFromClass()
    {
        for (int i = 0; i < _skills.Length; i++)
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
            Key.Key1   => 0,
            Key.Key2   => 1,
            Key.Key3   => 2,
            Key.Key4   => 3,
            Key.Space  => 4,
            _          => -1,
        };

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
        if (_tooltip != null && _tooltip.Visible)
        {
            var m   = GetViewport().GetMousePosition();
            float x = Mathf.Clamp(m.X + 14, 0, 900 - _tooltip.Size.X);
            float y = Mathf.Clamp(m.Y - _tooltip.Size.Y - 10, 0, 860 - _tooltip.Size.Y);
            _tooltip.Position = new Vector2(x, y);
        }

        var playerNode  = GetNode<Player>("Player");
        bool nowBurning = playerNode.IsBurning;
        if (nowBurning != _playerWasBurning)
        {
            _playerWasBurning = nowBurning;
            RefreshBuffDisplay();
        }

        var mouseWorld = GetGlobalMousePosition();
        bool nearBurn  = false;
        foreach (var mob in _mobs)
        {
            if (!IsInstanceValid(mob) || !mob.IsBurning) continue;
            if (mouseWorld.DistanceTo(mob.GlobalPosition + new Vector2(12f, -34f)) <= 10f)
            { nearBurn = true; break; }
        }
        if (nearBurn && !_burnHover)      { _burnHover = true;  ShowTooltip("Burning", "Deals 2 damage per second."); }
        else if (!nearBurn && _burnHover) { _burnHover = false; HideTooltip(); }

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

        for (int i = 0; i < _skills.Length; i++)
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
        if (skill.Id == "duplicate") _duplicateNextCard = true;
        if (skill.Id == "dash")      ApplyDash(skill);
        if (skill.Id == "teleport")  ApplyTeleport();
    }

    private void ApplyDash(SkillData skill)
    {
        var player = GetNode<Player>("Player");
        var toward = GetGlobalMousePosition() - player.GlobalPosition;
        if (toward.LengthSquared() < 0.01f) return;
        player.StartDash(toward.Normalized(), skill.GetValue(1, 200f));
    }

    private void ApplyTeleport()
    {
        var   player = GetNode<Player>("Player");
        var   target = GetGlobalMousePosition();
        const int Margin = 25;
        float x = Mathf.Clamp(target.X, -_roomHalfX + Margin, _roomHalfX - Margin);
        float y = Mathf.Clamp(target.Y, RoomOffsetY - _roomHalfY + Margin, RoomOffsetY + _roomHalfY - Margin);
        player.GlobalPosition = new Vector2(x, y);
    }

    private void PlayCurrentCard()
    {
        if (_deckCards.Count == 0) return;
        var played  = _slotCards[0];
        bool isPower = played?.Tags.Contains("Power") ?? false;

        if (_deckIndex == 0 && !_turnActive)
        {
            _turnNumber++;
            _turnActive = true;
            OnTurnStart();
        }

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

        if (isPower)
        {
            _deckCards.RemoveAt(_deckIndex);
            if (_deckCards.Count > 0)
            {
                int prevIdx = _deckIndex;
                _deckIndex %= _deckCards.Count;
                if (_deckIndex == 0 && prevIdx > 0)
                    OnTurnEnd();
            }
            else
            {
                _deckIndex = 0;
            }
        }
        else
        {
            int prevIdx = _deckIndex;
            _deckIndex = (_deckIndex + 1) % _deckCards.Count;
            if (_deckIndex == 0 && prevIdx > 0)
                OnTurnEnd();
        }

        RefreshSlots();
        _elapsed = 0.0;
        _progressFill.Size = new Vector2(0f, _progressFill.Size.Y);
        SpawnCardEffect(played);
    }

    private void OnTurnStart()
    {
        foreach (var buff in _playerBuffs)
            if (buff.Data.Effect == BuffEffectType.SpellDamageMultiplier)
                _spellDamageMult *= buff.Data.Value;
    }

    private void OnTurnEnd()
    {
        _turnActive       = false;
        _aoeAreaMult      = 1.0f;
        _attackDamageMult = 1.0f;
        for (int i = _playerBuffs.Count - 1; i >= 0; i--)
        {
            var b = _playerBuffs[i];
            if (b.RemainingTurns < 0) continue;
            b.RemainingTurns--;
            if (b.RemainingTurns <= 0)
                _playerBuffs.RemoveAt(i);
        }
        RefreshBuffDisplay();
    }

    // ── Buff display ──────────────────────────────────────────────────────────

    private const int BuffSquare = 28;
    private const int BuffBarH   = 4;
    private const int BuffGap    = 4;

    private void BuildBuffDisplay(CanvasLayer hud)
    {
        // Anchored just above the skill buttons (x=80, y=780 − square − bar − gaps).
        const int StartX = 80;
        int       startY = 780 - BuffSquare - BuffBarH - BuffGap * 2;

        _buffContainer             = new Control();
        _buffContainer.Position    = new Vector2(StartX, startY);
        _buffContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        hud.AddChild(_buffContainer);
    }

    private void RefreshBuffDisplay()
    {
        if (_buffContainer == null) return;

        foreach (Node child in _buffContainer.GetChildren())
        {
            _buffContainer.RemoveChild(child);
            child.QueueFree();
        }

        int x = 0;
        foreach (var buff in _playerBuffs)
        {
            var square = new ColorRect();
            square.Position    = new Vector2(x, 0);
            square.Size        = new Vector2(BuffSquare, BuffSquare);
            square.Color       = buff.Data.Color;
            square.MouseFilter = Control.MouseFilterEnum.Pass;
            var capturedBuff   = buff;
            square.MouseEntered += () => ShowTooltip(capturedBuff.Data.Name, capturedBuff.Data.Description);
            square.MouseExited  += HideTooltip;
            _buffContainer.AddChild(square);

            if (buff.RemainingTurns >= 0)
            {
                float fraction = buff.Data.Duration > 0
                    ? Mathf.Clamp((float)buff.RemainingTurns / buff.Data.Duration, 0f, 1f)
                    : 0f;

                var barBg = new ColorRect();
                barBg.Position    = new Vector2(x, BuffSquare + BuffGap);
                barBg.Size        = new Vector2(BuffSquare, BuffBarH);
                barBg.Color       = new Color(0.10f, 0.10f, 0.15f);
                barBg.MouseFilter = Control.MouseFilterEnum.Ignore;
                _buffContainer.AddChild(barBg);

                var barFill = new ColorRect();
                barFill.Position    = new Vector2(x, BuffSquare + BuffGap);
                barFill.Size        = new Vector2(BuffSquare * fraction, BuffBarH);
                barFill.Color       = buff.Data.Color;
                barFill.MouseFilter = Control.MouseFilterEnum.Ignore;
                _buffContainer.AddChild(barFill);
            }

            x += BuffSquare + BuffGap;
        }

        if (_playerWasBurning)
        {
            var square = new ColorRect();
            square.Position    = new Vector2(x, 0);
            square.Size        = new Vector2(BuffSquare, BuffSquare);
            square.Color       = new Color(1f, 0.4f, 0.0f);
            square.MouseFilter = Control.MouseFilterEnum.Pass;
            square.MouseEntered += () => ShowTooltip("Burning", "Deals 2 damage per second.");
            square.MouseExited  += HideTooltip;
            _buffContainer.AddChild(square);
        }
    }

    private void ApplyBuff(ActiveBuff buff)
    {
        _playerBuffs.Add(buff);
        RefreshBuffDisplay();
    }

    private void SpawnCardEffect(CardData card)
    {
        if (card == null) return;
        if (card.Id == "firebolt")    SpawnPlayerFirebolt(card);
        if (card.Id == "fireball")    SpawnPlayerFireball(card);
        if (card.Id == "mysticism")   ApplyMysticismBuff(card);
        if (card.Id == "cleave")      SpawnPlayerCleave(card);
        if (card.Id == "whirlwind")   SpawnPlayerWhirlwind(card);
        if (card.Id == "holyprayer")  HealPlayer(card);
        if (card.Id == "mobheal")     HealMostWoundedMob(card);
        if (card.Id == "greateraoe")    _aoeAreaMult      *= 1f + card.GetValue(1, 100f) / 100f;
        if (card.Id == "greaterattack") _attackDamageMult *= 1f + card.GetValue(1, 100f) / 100f;

        bool isArea   = card.Tags.Contains("Area")   || card.Id is "fireball" or "whirlwind" or "cleave";
        bool isAttack = card.Tags.Contains("Attack") || card.Id is "firebolt" or "fireball" or "cleave" or "whirlwind";
        if (isArea)   _aoeAreaMult     = 1.0f;
        if (isAttack) _attackDamageMult = 1.0f;
    }

    private void SpawnPlayerWhirlwind(CardData card)
    {
        var player = GetNode<Node2D>("Player");

        int baseDamage = (int)card.GetValue(1, 10);
        int damage     = card.Tags.Contains("Spell") ? (int)(baseDamage * _spellDamageMult) : baseDamage;
        damage         = (int)(damage * _attackDamageMult);

        var whirlwind = new WhirlwindEffect
        {
            IsPlayerOwned = true,
            Damage        = damage,
            Radius        = card.GetValue(2, 100f) * _aoeAreaMult,
            Duration      = card.GetValue(3, 3f),
            OwnerRef      = player,
        };
        AddChild(whirlwind);
    }

    private void SpawnPlayerCleave(CardData card)
    {
        var player = GetNode<Node2D>("Player");
        var origin = player.GlobalPosition;
        var dir    = (GetGlobalMousePosition() - origin).Normalized();

        int baseDamage = (int)card.GetValue(1, 15);
        int damage     = card.Tags.Contains("Spell") ? (int)(baseDamage * _spellDamageMult) : baseDamage;
        damage         = (int)(damage * _attackDamageMult);

        var cleave = new CleaveAttack
        {
            Damage     = damage,
            Range      = card.GetValue(2, 150f) * _aoeAreaMult,
            ArcDegrees = card.GetValue(3, 180f),
        };
        AddChild(cleave);
        cleave.Init(origin, dir, _mobs);
    }

    private void ApplyMysticismBuff(CardData card = null)
    {
        float multiplier = card != null ? card.GetValue(1, 2.0f) : 2.0f;
        ApplyBuff(new ActiveBuff(new BuffData
        {
            Id          = "mysticism",
            Name        = "Mysticism",
            Duration    = -1,
            Effect      = BuffEffectType.SpellDamageMultiplier,
            Value       = multiplier,
            Color       = new Color(0.6f, 0.20f, 0.90f),
            Description = $"At the start of each turn, multiplies all spell card damage by {multiplier}x (stacks every turn).",
        }));
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    private void BuildTooltip(CanvasLayer hud)
    {
        const int W = 220;
        const int H = 95;

        var style = new StyleBoxFlat();
        style.BgColor     = new Color(0.06f, 0.06f, 0.12f, 0.20f);
        style.BorderColor = new Color(0.55f, 0.55f, 0.70f, 0.35f);
        style.SetBorderWidthAll(1);
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
        style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 4;

        _tooltip = new Panel();
        _tooltip.Size        = new Vector2(W, H);
        _tooltip.Visible     = false;
        _tooltip.MouseFilter = Control.MouseFilterEnum.Ignore;
        _tooltip.ZIndex      = 200;
        _tooltip.AddThemeStyleboxOverride("panel", style);
        hud.AddChild(_tooltip);

        _tooltipName = new Label();
        _tooltipName.Position = new Vector2(8, 7);
        _tooltipName.Size     = new Vector2(W - 16, 20);
        _tooltipName.AddThemeColorOverride("font_color",   Colors.White);
        _tooltipName.AddThemeFontSizeOverride("font_size", 13);
        _tooltipName.MouseFilter = Control.MouseFilterEnum.Ignore;
        _tooltip.AddChild(_tooltipName);

        _tooltipTags = new Label();
        _tooltipTags.Position    = new Vector2(8, 27);
        _tooltipTags.Size        = new Vector2(W - 16, 14);
        _tooltipTags.AddThemeColorOverride("font_color",   new Color(0.55f, 0.75f, 0.90f));
        _tooltipTags.AddThemeFontSizeOverride("font_size", 10);
        _tooltipTags.MouseFilter = Control.MouseFilterEnum.Ignore;
        _tooltipTags.Visible     = false;
        _tooltip.AddChild(_tooltipTags);

        _tooltipDesc = new Label();
        _tooltipDesc.Position     = new Vector2(8, 44);
        _tooltipDesc.Size         = new Vector2(W - 16, H - 50);
        _tooltipDesc.AutowrapMode = TextServer.AutowrapMode.Word;
        _tooltipDesc.AddThemeColorOverride("font_color",   new Color(0.80f, 0.80f, 0.80f));
        _tooltipDesc.AddThemeFontSizeOverride("font_size", 11);
        _tooltipDesc.MouseFilter = Control.MouseFilterEnum.Ignore;
        _tooltip.AddChild(_tooltipDesc);
    }

    private void ShowTooltip(string name, string desc, string tags = "")
    {
        if (_tooltip == null) return;
        _tooltipName.Text    = name;
        _tooltipTags.Text    = tags ?? "";
        _tooltipTags.Visible = !string.IsNullOrEmpty(tags);
        _tooltipDesc.Text    = desc ?? "";
        _tooltip.Visible     = true;
    }

    private void HideTooltip()
    {
        if (_tooltip != null) _tooltip.Visible = false;
    }

    private void SpawnPlayerFireball(CardData card)
    {
        var player = GetNode<Node2D>("Player");
        var origin = player.GlobalPosition;
        var dir    = (GetGlobalMousePosition() - origin).Normalized();

        int baseDamage = (int)card.GetValue(1, 5);
        int damage     = card.Tags.Contains("Spell") ? (int)(baseDamage * _spellDamageMult) : baseDamage;
        damage         = (int)(damage * _attackDamageMult);

        var fireball = new Fireball
        {
            IsPlayerOwned    = true,
            AreaDamage       = damage,
            ProjectileRadius = card.GetValue(2, 8f),
            ProjectileSpeed  = card.GetValue(3, 400f),
            BlastRadius      = card.GetValue(4, 80f) * _aoeAreaMult,
            BurnDuration     = card.GetValue(5, 5f),
            Mobs             = _mobs,
        };
        AddChild(fireball);
        fireball.Init(dir, origin);
    }

    private void HealPlayer(CardData card)
    {
        int amount = (int)card.GetValue(1, 20);
        RunState.PlayerCurrentHp = Mathf.Min(RunState.PlayerMaxHp, RunState.PlayerCurrentHp + amount);
        UpdatePlayerHpBar();
    }

    private void HealMostWoundedMob(CardData card)
    {
        int amount = (int)card.GetValue(1, 20);
        MobActor target   = null;
        int      mostMissing = -1;
        foreach (var mob in _mobs)
        {
            if (!IsInstanceValid(mob)) continue;
            int missing = mob.MaxHp - mob.CurrentHp;
            if (missing > mostMissing) { mostMissing = missing; target = mob; }
        }
        target?.Heal(amount);
    }

    private void SpawnPlayerFirebolt(CardData card)
    {
        var player = GetNode<Node2D>("Player");
        var origin = player.GlobalPosition;
        var dir    = (GetGlobalMousePosition() - origin).Normalized();

        int baseDamage = (int)card.GetValue(1, 10);
        int damage     = card.Tags.Contains("Spell") ? (int)(baseDamage * _spellDamageMult) : baseDamage;
        damage         = (int)(damage * _attackDamageMult);

        var bolt = _fireboltScene.Instantiate<Firebolt>();
        bolt.IsPlayerOwned    = true;
        bolt.Damage           = damage;
        bolt.ProjectileRadius = card.GetValue(2, 0f);
        bolt.ProjectileSpeed  = card.GetValue(3, 0f);
        AddChild(bolt);
        bolt.Init(dir, origin);
    }

    // ── Player HP bar (vertical, left side) ──────────────────────────────────

    private const int HpBarX   = 10;
    private const int HpBarW   = 20;
    private const int HpBarTop = 100;
    private const int HpBarBot = 800;

    private void BuildPlayerHpBar(CanvasLayer hud)
    {
        int h = HpBarBot - HpBarTop;

        var bg = new ColorRect();
        bg.Position    = new Vector2(HpBarX, HpBarTop);
        bg.Size        = new Vector2(HpBarW, h);
        bg.Color       = new Color(0.20f, 0.06f, 0.06f);
        bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        hud.AddChild(bg);

        _playerHpFill              = new ColorRect();
        _playerHpFill.Position     = new Vector2(HpBarX, HpBarTop);
        _playerHpFill.Size         = new Vector2(HpBarW, h);
        _playerHpFill.Color        = new Color(0.18f, 0.75f, 0.18f);
        _playerHpFill.MouseFilter  = Control.MouseFilterEnum.Ignore;
        hud.AddChild(_playerHpFill);

        _playerHpLabel                        = new Label();
        _playerHpLabel.Position               = new Vector2(HpBarX - 2, HpBarBot + 4);
        _playerHpLabel.Size                   = new Vector2(HpBarW + 4, 20);
        _playerHpLabel.HorizontalAlignment    = HorizontalAlignment.Center;
        _playerHpLabel.AddThemeColorOverride("font_color",   Colors.White);
        _playerHpLabel.AddThemeFontSizeOverride("font_size", 10);
        _playerHpLabel.MouseFilter            = Control.MouseFilterEnum.Ignore;
        hud.AddChild(_playerHpLabel);

        UpdatePlayerHpBar();
    }

    private void UpdatePlayerHpBar()
    {
        if (_playerHpFill == null) return;
        int   h        = HpBarBot - HpBarTop;
        float fraction = RunState.PlayerMaxHp > 0
            ? Mathf.Clamp((float)RunState.PlayerCurrentHp / RunState.PlayerMaxHp, 0f, 1f)
            : 0f;
        float fillH = h * fraction;
        _playerHpFill.Position = new Vector2(HpBarX, HpBarBot - fillH);
        _playerHpFill.Size     = new Vector2(HpBarW, fillH);
        if (_playerHpLabel != null)
            _playerHpLabel.Text = $"{RunState.PlayerCurrentHp}/{RunState.PlayerMaxHp}";
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
            int ci = i;
            panel.MouseEntered += () => { var c = _slotCards[ci]; if (c != null) ShowTooltip(c.Name, c.Text, string.Join(", ", c.Tags)); };
            panel.MouseExited  += HideTooltip;
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
        BuildBuffDisplay(hud);
        BuildTooltip(hud);
        BuildPlayerHpBar(hud);
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

        for (int i = 0; i < _skills.Length; i++)
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
            btn.Pressed       += () => OnSkillActivated(capturedI);
            btn.MouseEntered  += () => { var s = _skills[capturedI]; if (s != null) ShowTooltip(s.Name, s.Description); };
            btn.MouseExited   += HideTooltip;

            var num = new Label();
            num.Text        = i < 4 ? (i + 1).ToString() : "Spc";
            num.Position    = new Vector2(CardW - 14, CardH - 17);
            num.Size        = new Vector2(i < 4 ? 12 : 22, 14);
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

        for (int i = 0; i < _skills.Length; i++)
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
