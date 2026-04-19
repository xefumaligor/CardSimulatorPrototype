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
    public float[]      Values  { get; set; } = new float[10];

    public CardData(string id, string name, string text, Color color, float useTime = 1.0f, List<string> tags = null, float[] values = null)
    {
        Id      = id;
        Name    = name;
        Text    = text;
        Color   = color;
        UseTime = useTime;
        Tags    = tags ?? new List<string>();
        Values  = values != null && values.Length == 10 ? values : new float[10];
    }

    // 1-based: GetValue(1) returns Values[0]. Returns defaultVal if index out of range or value is 0.
    public float GetValue(int n, float defaultVal = 0f)
    {
        if (n < 1 || n > 10) return defaultVal;
        float v = Values[n - 1];
        return v != 0f ? v : defaultVal;
    }

    public CardData Clone() => new CardData(Id, Name, Text, Color, UseTime, new List<string>(Tags), (float[])Values.Clone());
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
