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
    private HealthBar       _healthBar;

    public void InitData(MobEntry entry, List<CardData> deck, Node2D player, PackedScene fireboltScene)
    {
        _entry         = entry;
        _deck          = deck;
        MaxHp          = entry.Health > 0 ? entry.Health : 50;
        CurrentHp      = MaxHp;
        _playerRef     = player;
        _fireboltScene = fireboltScene;
    }

    public override void _Ready()
    {
        // Collision shape
        var collision   = new CollisionShape2D();
        var shape       = new RectangleShape2D();
        shape.Size      = new Vector2(30, 30);
        collision.Shape = shape;
        AddChild(collision);

        // Visual body
        var poly     = new Polygon2D();
        poly.Polygon = new Vector2[] { new(-15, -15), new(15, -15), new(15, 15), new(-15, 15) };
        poly.Color   = _entry?.Color ?? Colors.White;
        AddChild(poly);

        // Health bar positioned above the sprite
        _healthBar          = new HealthBar();
        _healthBar.Position = new Vector2(0, -28);
        _healthBar.Init(CurrentHp, MaxHp);
        AddChild(_healthBar);

        // Seed the card queue
        foreach (var card in _deck)
            _cardQueue.Enqueue(card);
    }

    private const float MoveSpeed    = 90f;
    private const float KitingRange  = 150f;
    private const float KitingDeadZone = 10f;

    public override void _PhysicsProcess(double delta)
    {
        if (_playerRef == null) return;

        Vector2 toPlayer = _playerRef.GlobalPosition - GlobalPosition;
        float   dist     = toPlayer.Length();
        Vector2 dir      = dist > 0.01f ? toPlayer / dist : Vector2.Zero;

        Velocity = _entry?.BehaviorName switch
        {
            "Aggressive" => dir * MoveSpeed,
            "Kiting"     => KitingVelocity(dist, dir),
            _            => Vector2.Zero,
        };

        MoveAndSlide();
    }

    private Vector2 KitingVelocity(float dist, Vector2 toPlayer)
    {
        float delta = dist - KitingRange;
        if (Mathf.Abs(delta) <= KitingDeadZone) return Vector2.Zero;
        // Move toward player if too far, away if too close
        return toPlayer * Mathf.Sign(delta) * MoveSpeed;
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

    private void PlayCard(CardData card)
    {
        if (card.Id == "firebolt")  SpawnFirebolt();
        if (card.Id == "fireball")  SpawnFireball();
        // Other card ids are silently consumed (e.g. shuffle)
    }

    private void SpawnFirebolt()
    {
        if (_playerRef == null || _fireboltScene == null || !IsInsideTree()) return;

        var dir  = (_playerRef.GlobalPosition - GlobalPosition).Normalized();
        var bolt = _fireboltScene.Instantiate<Firebolt>();
        bolt.IsPlayerOwned = false;
        bolt.Damage        = 10;
        GetParent().AddChild(bolt);
        bolt.Init(dir, GlobalPosition);
    }

    private void SpawnFireball()
    {
        if (_playerRef == null || !IsInsideTree()) return;

        var dir      = (_playerRef.GlobalPosition - GlobalPosition).Normalized();
        var fireball = new Fireball
        {
            IsPlayerOwned = false,
            AreaDamage    = 5,
            PlayerRef     = _playerRef,
        };
        GetParent().AddChild(fireball);
        fireball.Init(dir, GlobalPosition);
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
