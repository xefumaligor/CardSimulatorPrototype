using System.Collections.Generic;
using Godot;

public class SkillData
{
    public string  Id          { get; set; }
    public string  Name        { get; set; }
    public string  Description { get; set; }
    public float   Cooldown    { get; set; }
    public Color   Color       { get; set; }
    public float[] Values      { get; set; } = new float[10];

    public SkillData(string id, string name, string description, float cooldown, Color color, float[] values = null)
    {
        Id          = id;
        Name        = name;
        Description = description;
        Cooldown    = cooldown;
        Color       = color;
        Values      = values != null && values.Length == 10 ? values : new float[10];
    }

    // 1-based: GetValue(1) returns Values[0]. Returns defaultVal when value is 0 (unset).
    public float GetValue(int n, float defaultVal = 0f)
    {
        if (n < 1 || n > 10) return defaultVal;
        float v = Values[n - 1];
        return v != 0f ? v : defaultVal;
    }

    public SkillData Clone() => new SkillData(Id, Name, Description, Cooldown, Color, (float[])Values.Clone());
}

public class ClassEntry
{
    public string                Name     { get; set; } = "New Class";
    public List<ClassSkillEntry> Skills   { get; set; } = new();
    public string                DeckName { get; set; } = "";
    public int                   Health   { get; set; } = 100;
}

public class ClassSkillEntry
{
    public int    Slot    { get; set; }
    public string SkillId { get; set; }
}
