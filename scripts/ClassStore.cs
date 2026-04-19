using Godot;
using System.Collections.Generic;
using System.Text.Json;

public static class ClassStore
{
    private class SkillDef
    {
        public string Id       { get; set; }
        public string Name     { get; set; }
        public float  Cooldown { get; set; }
        public float  R        { get; set; }
        public float  G        { get; set; }
        public float  B        { get; set; }
    }

    private class ClassesFile
    {
        public List<ClassEntry> Classes { get; set; } = new();
    }

    private const string SkillsPath  = "res://data/skills.json";
    private const string ClassesPath = "user://classes.json";

    public static List<SkillData>  AllSkills    { get; } = new();
    public static List<ClassEntry> Classes      { get; } = new();
    public static int              EditingIndex { get; set; } = -1;
    public static ClassEntry       ActiveClass  { get; set; }

    public static void EnsureSkillsLoaded()
    {
        if (AllSkills.Count > 0) return;
        using var file = FileAccess.Open(SkillsPath, FileAccess.ModeFlags.Read);
        if (file == null) return;
        try
        {
            var defs = JsonSerializer.Deserialize<List<SkillDef>>(file.GetAsText());
            if (defs == null) return;
            foreach (var d in defs)
                AllSkills.Add(new SkillData(d.Id, d.Name, d.Cooldown, new Color(d.R, d.G, d.B)));
        }
        catch { }
    }

    public static void LoadClasses()
    {
        Classes.Clear();
        if (!FileAccess.FileExists(ClassesPath)) return;
        try
        {
            using var file = FileAccess.Open(ClassesPath, FileAccess.ModeFlags.Read);
            if (file == null) return;
            var data = JsonSerializer.Deserialize<ClassesFile>(file.GetAsText());
            if (data?.Classes != null)
                Classes.AddRange(data.Classes);
        }
        catch { }
    }

    public static void SaveClasses()
    {
        using var file = FileAccess.Open(ClassesPath, FileAccess.ModeFlags.Write);
        file?.StoreString(JsonSerializer.Serialize(new ClassesFile { Classes = Classes }));
    }

    public static void DeleteClass(int index)
    {
        if (index >= 0 && index < Classes.Count)
        {
            Classes.RemoveAt(index);
            SaveClasses();
        }
    }

    public static string NextClassName() => $"Class {Classes.Count + 1}";
}
