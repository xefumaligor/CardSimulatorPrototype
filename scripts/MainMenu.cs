using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBoxContainer/BtnWaves").Pressed          += OnWavesPressed;
        GetNode<Button>("VBoxContainer/BtnSwarm").Pressed          += OnSwarmPressed;
        GetNode<Button>("VBoxContainer/BtnBoss").Pressed           += OnBossPressed;
        GetNode<Button>("VBoxContainer/BtnDeckManagement").Pressed += OnDeckManagementPressed;
    }

    private void OnWavesPressed() => GoToEncounter("res://scenes/BaseEncounter.tscn");
    private void OnSwarmPressed() => GoToEncounter("res://scenes/BaseEncounter.tscn");
    private void OnBossPressed()  => GoToEncounter("res://scenes/BaseEncounter.tscn");

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
