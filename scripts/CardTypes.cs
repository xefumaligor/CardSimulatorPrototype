using System.Collections.Generic;
using Godot;

public class CardData
{
    public string       Id      { get; set; }
    public string       Name    { get; set; }
    public string       Text    { get; set; }
    public Color        Color   { get; set; }
    public float        UseTime { get; set; }
    public List<string> Tags    { get; set; } = new();

    public CardData(string id, string name, string text, Color color, float useTime = 1.0f, List<string> tags = null)
    {
        Id      = id;
        Name    = name;
        Text    = text;
        Color   = color;
        UseTime = useTime;
        Tags    = tags ?? new List<string>();
    }

    public CardData Clone() => new CardData(Id, Name, Text, Color, UseTime, new List<string>(Tags));
}

public class DeckEntry
{
    public string          Name  { get; set; } = "New Deck";
    public List<SlotEntry> Slots { get; set; } = new();
}

public class SlotEntry
{
    public int    Slot   { get; set; }
    public string CardId { get; set; }
}
