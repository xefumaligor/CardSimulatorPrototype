using System.Collections.Generic;

// Snapshot of class skills and deck copied at run-start.
// BaseEncounter reads from here instead of ClassStore/DeckStore directly,
// so mid-run/encounter mutations never touch the base class definition.
public static class RunState
{
    public static SkillData[]    Skills          { get; } = new SkillData[5];
    public static List<CardData> Deck            { get; } = new();
    public static int            PlayerMaxHp     { get; set; } = 100;
    public static int            PlayerCurrentHp { get; set; } = 100;

    public static EncounterEntry CurrentEncounter { get; set; }
    public static int            EncounterIndex   { get; set; } = 1;
    public static bool           IsTestMode       { get; set; }
    public static string         TestReturnScene  { get; set; }

    public static void StartRun(ClassEntry chosenClass)
    {
        for (int i = 0; i < Skills.Length; i++) Skills[i] = null;
        Deck.Clear();

        PlayerMaxHp     = chosenClass.Health > 0 ? chosenClass.Health : 100;
        PlayerCurrentHp = PlayerMaxHp;

        foreach (var entry in chosenClass.Skills)
        {
            if (entry.Slot < 0 || entry.Slot >= Skills.Length) continue;
            var src = ClassStore.AllSkills.Find(s => s.Id == entry.SkillId);
            if (src != null) Skills[entry.Slot] = src.Clone();
        }

        var srcDeck = DeckStore.Decks.Find(d => d.Name == chosenClass.DeckName);
        if (srcDeck == null) return;

        var byId = new Dictionary<string, CardData>();
        foreach (var c in DeckStore.AllCards) byId[c.Id] = c;

        var sorted = new List<SlotEntry>(srcDeck.Slots);
        sorted.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        foreach (var entry in sorted)
            if (byId.TryGetValue(entry.CardId, out var card))
                Deck.Add(card.Clone());
    }
}
