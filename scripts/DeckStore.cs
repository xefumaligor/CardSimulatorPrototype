using Godot;
using System.Collections.Generic;
using System.Text.Json;

// Static singleton for cross-scene deck data — persists for the app lifetime.
public static class DeckStore
{
    private class CardDef
    {
        public string       Id      { get; set; }
        public string       Name    { get; set; }
        public string       Text    { get; set; }
        public float        R       { get; set; }
        public float        G       { get; set; }
        public float        B       { get; set; }
        public float        UseTime { get; set; } = 1.0f;
        public List<string> Tags    { get; set; } = new();
    }

    private class DecksFile
    {
        public List<DeckEntry> Decks { get; set; } = new();
    }

    private const string CardsPath          = "res://data/cards.json";
    private const string CardsOverridePath  = "user://cards.json";
    private const string DecksPath          = "user://decks.json";
    private const string TagsPath           = "res://data/tags.json";

    public static List<CardData>  AllCards     { get; } = new();
    public static List<DeckEntry> Decks        { get; } = new();
    public static List<string>    AllTags      { get; } = new();

    // -1 = creating a new deck; >= 0 = editing Decks[EditingIndex].
    public static int       EditingIndex          { get; set; } = -1;
    // Id of the card being edited in CardEditor.
    public static string    EditingCardId         { get; set; } = "";
    // Set before entering DeckSelectScreen; loaded after a deck is chosen.
    public static string    PendingEncounterScene { get; set; } = "";
    // The deck chosen on the DeckSelectScreen; read by BaseEncounter.
    public static DeckEntry ActiveDeck            { get; set; }

    public static void EnsureCardsLoaded()
    {
        if (AllCards.Count > 0) return;

        var merged = new Dictionary<string, CardDef>();

        // Load base definitions first.
        LoadCardDefs(CardsPath, merged);

        // User overrides win; new user-created cards are also added.
        if (FileAccess.FileExists(CardsOverridePath))
            LoadCardDefs(CardsOverridePath, merged);

        foreach (var d in merged.Values)
            AllCards.Add(new CardData(d.Id, d.Name, d.Text ?? "", new Color(d.R, d.G, d.B), d.UseTime, d.Tags ?? new()));
    }

    private static void LoadCardDefs(string path, Dictionary<string, CardDef> target)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return;
        try
        {
            var defs = JsonSerializer.Deserialize<List<CardDef>>(file.GetAsText());
            if (defs == null) return;
            foreach (var d in defs)
                if (d.Id != null) target[d.Id] = d;
        }
        catch { }
    }

    public static void LoadTags()
    {
        if (AllTags.Count > 0) return;
        using var file = FileAccess.Open(TagsPath, FileAccess.ModeFlags.Read);
        if (file == null) return;
        try
        {
            var tags = JsonSerializer.Deserialize<List<string>>(file.GetAsText());
            if (tags != null) AllTags.AddRange(tags);
        }
        catch { }
    }

    public static void SaveCards()
    {
        var defs = new List<CardDef>();
        foreach (var c in AllCards)
            defs.Add(new CardDef
            {
                Id      = c.Id,
                Name    = c.Name,
                Text    = c.Text,
                R       = c.Color.R,
                G       = c.Color.G,
                B       = c.Color.B,
                UseTime = c.UseTime,
                Tags    = new List<string>(c.Tags)
            });
        using var file = FileAccess.Open(CardsOverridePath, FileAccess.ModeFlags.Write);
        file?.StoreString(JsonSerializer.Serialize(defs));
    }

    public static void LoadDecks()
    {
        Decks.Clear();
        if (!FileAccess.FileExists(DecksPath)) return;
        try
        {
            using var file = FileAccess.Open(DecksPath, FileAccess.ModeFlags.Read);
            if (file == null) return;
            var data = JsonSerializer.Deserialize<DecksFile>(file.GetAsText());
            if (data?.Decks != null)
                Decks.AddRange(data.Decks);
        }
        catch { }
    }

    public static void SaveDecks()
    {
        using var file = FileAccess.Open(DecksPath, FileAccess.ModeFlags.Write);
        file?.StoreString(JsonSerializer.Serialize(new DecksFile { Decks = Decks }));
    }

    public static void DeleteDeck(int index)
    {
        if (index >= 0 && index < Decks.Count)
        {
            Decks.RemoveAt(index);
            SaveDecks();
        }
    }

    public static string NextDeckName() => $"Deck {Decks.Count + 1}";
}
