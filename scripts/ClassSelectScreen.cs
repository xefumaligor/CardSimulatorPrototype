using Godot;

public partial class ClassSelectScreen : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        ClassStore.EnsureSkillsLoaded();
        ClassStore.LoadClasses();
        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadDecks();

        BuildUI();
    }

    private void BuildUI()
    {
        var title = new Label();
        title.Text     = "Select a Class";
        title.Position = new Vector2(50, 28);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var backBtn = new Button();
        backBtn.Text     = "Back";
        backBtn.Size     = new Vector2(120, 36);
        backBtn.Position = new Vector2(900 - 50 - 120, 26);
        backBtn.Pressed  += () =>
        {
            string dest = RunState.IsTestMode
                ? RunState.TestReturnScene
                : "res://scenes/MainMenu.tscn";
            GetTree().ChangeSceneToFile(dest);
        };
        AddChild(backBtn);

        if (ClassStore.Classes.Count == 0)
        {
            BuildNoClassMessage();
            return;
        }

        BuildClassList();
    }

    private void BuildNoClassMessage()
    {
        var msg = new Label();
        msg.Text                = "You need to create a class first.";
        msg.Position            = new Vector2(0, 380);
        msg.Size                = new Vector2(900, 40);
        msg.HorizontalAlignment = HorizontalAlignment.Center;
        msg.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        msg.AddThemeFontSizeOverride("font_size", 18);
        AddChild(msg);

        var btn = new Button();
        btn.Text              = "Go to Class Management";
        btn.CustomMinimumSize = new Vector2(260, 44);
        btn.Position          = new Vector2((900 - 260) / 2f, 430);
        btn.Pressed           += () => GetTree().ChangeSceneToFile("res://scenes/ClassListScreen.tscn");
        AddChild(btn);
    }

    private void BuildClassList()
    {
        var listPanel = new Panel();
        listPanel.Position = new Vector2(50, 80);
        listPanel.Size     = new Vector2(800, 780);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor     = new Color(0.12f, 0.12f, 0.18f);
        panelStyle.BorderColor = new Color(0.30f, 0.30f, 0.40f);
        panelStyle.SetBorderWidthAll(1);
        listPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(listPanel);

        var scroll = new ScrollContainer();
        scroll.Position = new Vector2(10, 10);
        scroll.Size     = new Vector2(780, 760);
        listPanel.AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(760, 0);
        vbox.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(vbox);

        for (int i = 0; i < ClassStore.Classes.Count; i++)
        {
            int capturedIndex = i;
            var entry = ClassStore.Classes[i];

            var btn = new Button();
            string deckInfo = entry.DeckName.Length > 0 ? $"  [{entry.DeckName}]" : "  [No deck]";
            btn.Text              = entry.Name + deckInfo;
            btn.CustomMinimumSize = new Vector2(0, 52);
            btn.Disabled          = entry.DeckName.Length == 0;
            btn.Pressed           += () => OnClassSelected(capturedIndex);
            vbox.AddChild(btn);
        }
    }

    private void OnClassSelected(int index)
    {
        var chosen = ClassStore.Classes[index];
        ClassStore.ActiveClass = chosen;
        RunState.StartRun(chosen);

        if (!RunState.IsTestMode)
        {
            EncounterStore.LoadEncounters();
            RunState.EncounterIndex   = 1;
            RunState.CurrentEncounter = EncounterStore.Encounters.Find(e => e.Name == "Level1");
        }

        GetTree().ChangeSceneToFile("res://scenes/EncounterPreviewScreen.tscn");
    }
}
