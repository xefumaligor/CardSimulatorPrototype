using System.Collections.Generic;
using Godot;

public class CardData
{
    public string Id      { get; set; }
    public string Name    { get; set; }
    public string Text    { get; set; }
    public Color  Color   { get; set; }
    public float  UseTime { get; set; }

    public CardData(string id, string name, string text, Color color, float useTime = 1.0f)
    {
        Id      = id;
        Name    = name;
        Text    = text;
        Color   = color;
        UseTime = useTime;
    }

    public CardData Clone() => new CardData(Id, Name, Text, Color, UseTime);
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
