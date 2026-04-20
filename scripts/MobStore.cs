using Godot;
using System.Collections.Generic;
using System.Text.Json;

public static class MobStore
{
    private class MobsFileWrapper
    {
        public List<MobEntry> Mobs { get; set; } = new();
    }

    private const string MobsPath = "res://data/mobs.json";

    public static List<MobEntry> Mobs         { get; } = new();
    public static int            EditingIndex { get; set; } = -1;

    public static void LoadMobs()
    {
        Mobs.Clear();
        if (!FileAccess.FileExists(MobsPath)) return;
        try
        {
            using var file = FileAccess.Open(MobsPath, FileAccess.ModeFlags.Read);
            if (file == null) return;
            var data = JsonSerializer.Deserialize<MobsFileWrapper>(file.GetAsText());
            if (data?.Mobs != null)
                Mobs.AddRange(data.Mobs);
        }
        catch { }
    }

    public static void SaveMobs()
    {
        using var file = FileAccess.Open(MobsPath, FileAccess.ModeFlags.Write);
        file?.StoreString(JsonSerializer.Serialize(new MobsFileWrapper { Mobs = Mobs }));
    }

    public static void DeleteMob(int index)
    {
        if (index >= 0 && index < Mobs.Count)
        {
            Mobs.RemoveAt(index);
            SaveMobs();
        }
    }

    public static string NextMobName() => $"Mob {Mobs.Count + 1}";
}
