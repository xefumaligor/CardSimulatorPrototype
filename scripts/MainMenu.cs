using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBoxContainer/BtnWaves").Pressed          += OnWavesPressed;
        GetNode<Button>("VBoxContainer/BtnDeckManagement").Pressed  += OnDeckManagementPressed;
        GetNode<Button>("VBoxContainer/BtnClassManagement").Pressed += () =>
            GetTree().ChangeSceneToFile("res://scenes/ClassListScreen.tscn");
        GetNode<Button>("VBoxContainer/BtnMobManagement").Pressed += () =>
            GetTree().ChangeSceneToFile("res://scenes/MobListScreen.tscn");
        GetNode<Button>("VBoxContainer/BtnEncounterManagement").Pressed += () =>
            GetTree().ChangeSceneToFile("res://scenes/EncounterListScreen.tscn");
    }

    private void OnWavesPressed()
    {
        EncounterStore.LoadEncounters();
        RunState.CurrentEncounter = EncounterStore.Encounters.Find(e => e.Name == "Level1");
        RunState.IsTestMode       = false;
        GetTree().ChangeSceneToFile("res://scenes/ClassSelectScreen.tscn");
    }

    private void OnDeckManagementPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/DeckListScreen.tscn");
    }
}
