using System.Collections.Generic;
using Godot;

public class SkillData
{
    public string Id       { get; set; }
    public string Name     { get; set; }
    public float  Cooldown { get; set; }
    public Color  Color    { get; set; }

    public SkillData(string id, string name, float cooldown, Color color)
    {
        Id       = id;
        Name     = name;
        Cooldown = cooldown;
        Color    = color;
    }
}

public class ClassEntry
{
    public string                Name     { get; set; } = "New Class";
    public List<ClassSkillEntry> Skills   { get; set; } = new();
    public string                DeckName { get; set; } = "";
}

public class ClassSkillEntry
{
    public int    Slot    { get; set; }
    public string SkillId { get; set; }
}
