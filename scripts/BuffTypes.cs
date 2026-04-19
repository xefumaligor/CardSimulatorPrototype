using Godot;

public enum BuffEffectType
{
    SpellDamageMultiplier,
}

public class BuffData
{
    public string         Id       { get; set; }
    public string         Name     { get; set; }
    public int            Duration { get; set; } = -1; // -1 = permanent
    public BuffEffectType Effect   { get; set; }
    public float          Value       { get; set; } = 1.0f;
    public Color          Color       { get; set; } = new Color(0.5f, 0.5f, 0.5f);
    public string         Description { get; set; } = "";
}

public class ActiveBuff
{
    public BuffData Data           { get; set; }
    public int      RemainingTurns { get; set; }

    public ActiveBuff(BuffData data)
    {
        Data           = data;
        RemainingTurns = data.Duration;
    }
}
