using Godot;
using System.Collections.Generic;
using System.Text.Json;

// Static singleton for cross-scene deck data — persists for the app lifetime.
public static class DeckStore
{
    private class CardDef
    {
        public string Id      { get; set; }
        public string Name    { get; set; }
        public string Text    { get; set; }
        public float  R       { get; set; }
        public float  G       { get; set; }
        public float  B       { get; set; }
        public float  UseTime { get; set; } = 1.0f;
    }

    private class DecksFile
    {
        public List<DeckEntry> Decks { get; set; } = new();
    }

    private const string CardsPath = "res://data/cards.json";
    private const string DecksPath = "user://decks.json";

    public static List<CardData>  AllCards     { get; } = new();
    public static List<DeckEntry> Decks        { get; } = new();

    // -1 = creating a new deck; >= 0 = editing Decks[EditingIndex].
    public static int       EditingIndex        { get; set; } = -1;
    // Set before entering DeckSelectScreen; loaded after a deck is chosen.
    public static string    PendingEncounterScene { get; set; } = "";
    // The deck chosen on the DeckSelectScreen; read by BaseEncounter.
    public static DeckEntry ActiveDeck          { get; set; }

    public static void EnsureCardsLoaded()
    {
        if (AllCards.Count > 0) return;
        using var file = FileAccess.Open(CardsPath, FileAccess.ModeFlags.Read);
        if (file == null) return;
        try
        {
            var defs = JsonSerializer.Deserialize<List<CardDef>>(file.GetAsText());
            if (defs == null) return;
            foreach (var d in defs)
                AllCards.Add(new CardData(d.Id, d.Name, d.Text ?? "", new Color(d.R, d.G, d.B), d.UseTime));
        }
        catch { }
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
