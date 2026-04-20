using Godot;
using System;
using System.Collections.Generic;

public partial class MobActor : CharacterBody2D
{
    public event Action<MobActor> OnDied;

    public int MaxHp     { get; private set; }
    public int CurrentHp { get; private set; }

    private MobEntry        _entry;
    private List<CardData>  _deck        = new();
    private Queue<CardData> _cardQueue   = new();
    private double          _cardElapsed = 0.0;
    private PackedScene     _fireboltScene;
    private Node2D          _playerRef;
    private List<MobActor>  _mobs;
    private HealthBar       _healthBar;

    public void InitData(MobEntry entry, List<CardData> deck, Node2D player, PackedScene fireboltScene, List<MobActor> mobs)
    {
        _entry         = entry;
        _deck          = deck;
        MaxHp          = entry.Health > 0 ? entry.Health : 50;
        CurrentHp      = MaxHp;
        _playerRef     = player;
        _fireboltScene = fireboltScene;
        _mobs          = mobs;
    }

    public override void _Ready()
    {
        float half = Mathf.Max(1, _entry?.Size ?? 30) / 2f;

        CollisionLayer = 2;
        CollisionMask  = 1;

        // Collision shape
        var collision   = new CollisionShape2D();
        var shape       = new RectangleShape2D();
        shape.Size      = new Vector2(half * 2f, half * 2f);
        collision.Shape = shape;
        AddChild(collision);

        // Visual body
        var poly     = new Polygon2D();
        poly.Polygon = new Vector2[] { new(-half, -half), new(half, -half), new(half, half), new(-half, half) };
        poly.Color   = _entry?.Color ?? Colors.White;
        AddChild(poly);

        // Health bar positioned above the sprite
        _healthBar          = new HealthBar();
        _healthBar.Position = new Vector2(0, -half - 13f);
        _healthBar.Init(CurrentHp, MaxHp);
        AddChild(_healthBar);

        // Seed the card queue
        foreach (var card in _deck)
            _cardQueue.Enqueue(card);
    }

    private const float KitingRange    = 250f;
    private const float KitingDeadZone = 10f;

    public override void _PhysicsProcess(double delta)
    {
        if (_playerRef == null) return;

        Vector2 toPlayer = _playerRef.GlobalPosition - GlobalPosition;
        float   dist     = toPlayer.Length();
        Vector2 dir      = dist > 0.01f ? toPlayer / dist : Vector2.Zero;

        float speed = _entry?.Speed > 0f ? _entry.Speed : 90f;
        Velocity = _entry?.BehaviorName switch
        {
            "Aggressive" => dir * speed,
            "Kiting"     => KitingVelocity(dist, dir, speed),
            _            => Vector2.Zero,
        };

        MoveAndSlide();
    }

    private Vector2 KitingVelocity(float dist, Vector2 toPlayer, float speed)
    {
        float delta = dist - KitingRange;
        if (Mathf.Abs(delta) <= KitingDeadZone) return Vector2.Zero;
        return toPlayer * Mathf.Sign(delta) * speed;
    }

    public override void _Process(double delta)
    {
        if (_isBurning)
        {
            _burnElapsed += (float)delta;
            if (_burnElapsed >= BurnTickRate)
            {
                _burnElapsed -= BurnTickRate;
                TakeDamage(BurnTickDamage);
            }
        }

        if (_cardQueue.Count == 0) return;

        var card = _cardQueue.Peek();
        _cardElapsed += delta;

        if (_cardElapsed >= card.UseTime)
        {
            _cardElapsed = 0.0;
            _cardQueue.Dequeue();
            PlayCard(card);

            if (_cardQueue.Count == 0)
                foreach (var c in _deck)
                    _cardQueue.Enqueue(c);
        }
    }

    private float _aoeAreaMult    = 1.0f;
    private float _attackDamageMult = 1.0f;

    private void PlayCard(CardData card)
    {
        if (card.Id == "firebolt")      SpawnFirebolt(card);
        if (card.Id == "fireball")      SpawnFireball(card);
        if (card.Id == "whirlwind")     SpawnWhirlwind(card);
        if (card.Id == "cleave")        SpawnCleave(card);
        if (card.Id == "holyprayer")    Heal((int)card.GetValue(1, 20));
        if (card.Id == "mobheal")       HealMostWounded(card);
        if (card.Id == "greateraoe")    _aoeAreaMult      *= 1f + card.GetValue(1, 100f) / 100f;
        if (card.Id == "greaterattack") _attackDamageMult *= 1f + card.GetValue(1, 100f) / 100f;

        bool isArea   = card.Tags.Contains("Area")   || card.Id is "fireball" or "whirlwind" or "cleave";
        bool isAttack = card.Tags.Contains("Attack") || card.Id is "firebolt" or "fireball" or "cleave" or "whirlwind";
        if (isArea)   _aoeAreaMult      = 1.0f;
        if (isAttack) _attackDamageMult  = 1.0f;
    }

    private void SpawnCleave(CardData card)
    {
        if (_playerRef == null || !IsInsideTree()) return;

        var dir    = (_playerRef.GlobalPosition - GlobalPosition).Normalized();
        int damage = (int)(card.GetValue(1, 15) * _attackDamageMult);

        var cleave = new CleaveAttack
        {
            Damage     = damage,
            Range      = card.GetValue(2, 150f) * _aoeAreaMult,
            ArcDegrees = card.GetValue(3, 180f),
        };
        GetParent().AddChild(cleave);
        cleave.Init(GlobalPosition, dir, _playerRef);
    }

    private void SpawnFirebolt(CardData card)
    {
        if (_playerRef == null || _fireboltScene == null || !IsInsideTree()) return;

        var dir  = (_playerRef.GlobalPosition - GlobalPosition).Normalized();
        var bolt = _fireboltScene.Instantiate<Firebolt>();
        bolt.IsPlayerOwned = false;
        bolt.Damage        = (int)(card.GetValue(1, 10) * _attackDamageMult);
        GetParent().AddChild(bolt);
        bolt.Init(dir, GlobalPosition);
    }

    private void SpawnWhirlwind(CardData card)
    {
        if (!IsInsideTree()) return;

        var whirlwind = new WhirlwindEffect
        {
            IsPlayerOwned = false,
            Damage        = (int)(card.GetValue(1, 10) * _attackDamageMult),
            Radius        = card.GetValue(2, 100f) * _aoeAreaMult,
            Duration      = card.GetValue(3, 3f),
            OwnerRef      = this,
        };
        GetParent().AddChild(whirlwind);
    }

    private void SpawnFireball(CardData card)
    {
        if (_playerRef == null || !IsInsideTree()) return;

        var dir      = (_playerRef.GlobalPosition - GlobalPosition).Normalized();
        var fireball = new Fireball
        {
            IsPlayerOwned    = false,
            AreaDamage       = (int)(card.GetValue(1, 5) * _attackDamageMult),
            ProjectileRadius = card.GetValue(2, 8f),
            ProjectileSpeed  = card.GetValue(3, 400f),
            BlastRadius      = card.GetValue(4, 80f) * _aoeAreaMult,
            BurnDuration     = card.GetValue(5, 5f),
            PlayerRef        = _playerRef,
        };
        GetParent().AddChild(fireball);
        fireball.Init(dir, GlobalPosition);
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        _healthBar?.Update(CurrentHp, MaxHp);
    }

    private void HealMostWounded(CardData card)
    {
        int amount = (int)card.GetValue(1, 20);
        MobActor target = null;
        int mostMissing = -1;
        if (_mobs != null)
        {
            foreach (var m in _mobs)
            {
                if (!IsInstanceValid(m)) continue;
                int missing = m.MaxHp - m.CurrentHp;
                if (missing > mostMissing) { mostMissing = missing; target = m; }
            }
        }
        if (target == null) target = this;
        target.Heal(amount);
    }

    public void TakeDamage(int amount)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        _healthBar?.Update(CurrentHp, MaxHp);
        if (CurrentHp <= 0)
        {
            OnDied?.Invoke(this);
            QueueFree();
        }
    }

    public bool IsBurning => _isBurning;

    private ColorRect _burningIndicator;
    private bool      _isBurning;
    private float     _burnElapsed;
    private const float BurnTickRate   = 0.5f;
    private const int   BurnTickDamage = 1;    // 2 dmg/sec

    public void SetBurning(bool burning)
    {
        if (burning == _isBurning) return;
        _isBurning   = burning;
        _burnElapsed = 0f;

        if (burning && _burningIndicator == null)
        {
            _burningIndicator          = new ColorRect();
            _burningIndicator.Size     = new Vector2(8, 8);
            _burningIndicator.Position = new Vector2(8, -38);
            _burningIndicator.Color    = new Color(1f, 0.4f, 0.0f);
            _burningIndicator.ZIndex   = 1;
            AddChild(_burningIndicator);
        }
        else if (!burning && _burningIndicator != null)
        {
            _burningIndicator.QueueFree();
            _burningIndicator = null;
        }
    }
}
