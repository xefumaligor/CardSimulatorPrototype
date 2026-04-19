using Godot;
using System.Collections.Generic;
using System.Text.Json;

public static class EncounterStore
{
    private class EncountersFileWrapper
    {
        public List<EncounterEntry> Encounters { get; set; } = new();
    }

    private const string EncountersPath = "user://encounters.json";

    public static List<EncounterEntry> Encounters        { get; } = new();
    public static int                  EditingIndex      { get; set; } = -1;
    public static bool                 ComingFromMobSelect { get; set; } = false;
    public static EncounterEntry       PendingEntry      { get; set; } = null;

    public static void LoadEncounters()
    {
        Encounters.Clear();
        if (!FileAccess.FileExists(EncountersPath)) return;
        try
        {
            using var file = FileAccess.Open(EncountersPath, FileAccess.ModeFlags.Read);
            if (file == null) return;
            var data = JsonSerializer.Deserialize<EncountersFileWrapper>(file.GetAsText());
            if (data?.Encounters != null)
                Encounters.AddRange(data.Encounters);
        }
        catch { }
    }

    public static void SaveEncounters()
    {
        using var file = FileAccess.Open(EncountersPath, FileAccess.ModeFlags.Write);
        file?.StoreString(JsonSerializer.Serialize(new EncountersFileWrapper { Encounters = Encounters }));
    }

    public static void DeleteEncounter(int index)
    {
        if (index >= 0 && index < Encounters.Count)
        {
            Encounters.RemoveAt(index);
            SaveEncounters();
        }
    }

    public static string NextEncounterName() => $"Encounter {Encounters.Count + 1}";

    public static void InitPending()
    {
        bool editing = EditingIndex >= 0 && EditingIndex < Encounters.Count;
        if (editing)
        {
            var e = Encounters[EditingIndex];
            PendingEntry = new EncounterEntry
            {
                Name   = e.Name,
                Type   = e.Type,
                Height = e.Height,
                Width  = e.Width,
                Mobs   = new List<string>(e.Mobs),
            };
        }
        else
        {
            PendingEntry = new EncounterEntry
            {
                Name   = NextEncounterName(),
                Type   = "",
                Height = 10,
                Width  = 10,
                Mobs   = new List<string>(),
            };
        }
    }
}
