using System.Collections.Generic;

public class EncounterEntry
{
    public string       Name   { get; set; } = "New Encounter";
    public string       Type   { get; set; } = "";
    public int          Height { get; set; } = 10;
    public int          Width  { get; set; } = 10;
    public List<string> Mobs   { get; set; } = new();
}
