using Godot;

public partial class EscapeHandler : Node
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode != Key.Escape) return;

        var current = GetTree().CurrentScene;
        if (current?.SceneFilePath == "res://scenes/MainMenu.tscn") return;

        GetViewport().SetInputAsHandled();
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
}
