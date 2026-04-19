using System.Collections.Generic;
using Godot;

public class MobEntry
{
    public string Name         { get; set; } = "New Mob";
    public float  R            { get; set; } = 1f;
    public float  G            { get; set; } = 1f;
    public float  B            { get; set; } = 1f;
    public string BehaviorName { get; set; } = "";
    public string DeckName     { get; set; } = "";
    public int    Size         { get; set; } = 30;
    public float  Speed        { get; set; } = 90f;
    public int    Health       { get; set; } = 50;

    public Color Color => new Color(R, G, B);
}

public class MobsFile
{
    public List<MobEntry> Mobs { get; set; } = new();
}
