using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBoxContainer/BtnWaves").Pressed          += OnWavesPressed;
        GetNode<Button>("VBoxContainer/BtnTestEncounter").Pressed  += () => GetTree().ChangeSceneToFile("res://scenes/SelectEncounterScreen.tscn");
        GetNode<Button>("VBoxContainer/BtnDeckManagement").Pressed += OnDeckManagementPressed;
    }

    private void OnWavesPressed() => GoToEncounter("res://scenes/BaseEncounter.tscn");

    private void GoToEncounter(string scene)
    {
        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadDecks();
        DeckStore.PendingEncounterScene = scene;
        GetTree().ChangeSceneToFile("res://scenes/DeckSelectScreen.tscn");
    }

    private void OnDeckManagementPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/DeckListScreen.tscn");
    }
}
