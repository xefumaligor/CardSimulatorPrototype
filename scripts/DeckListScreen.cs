using Godot;

public partial class DeckListScreen : Control
{
    private VBoxContainer      _deckList;
    private ConfirmationDialog _confirmDialog;
    private int                _pendingDeleteIndex = -1;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadDecks();

        BuildUI();

        _confirmDialog = new ConfirmationDialog();
        _confirmDialog.Confirmed += OnDeleteConfirmed;
        AddChild(_confirmDialog);
    }

    private void BuildUI()
    {
        var title = new Label();
        title.Text     = "Decks";
        title.Position = new Vector2(50, 28);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var backBtn = new Button();
        backBtn.Text    = "Back to Menu";
        backBtn.Size    = new Vector2(160, 36);
        backBtn.Position = new Vector2(900 - 50 - 160, 26);
        backBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        AddChild(backBtn);

        // List panel
        var listPanel = new Panel();
        listPanel.Position = new Vector2(50, 80);
        listPanel.Size     = new Vector2(800, 740);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor     = new Color(0.12f, 0.12f, 0.18f);
        panelStyle.BorderColor = new Color(0.30f, 0.30f, 0.40f);
        panelStyle.SetBorderWidthAll(1);
        listPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(listPanel);

        var scroll = new ScrollContainer();
        scroll.Position = new Vector2(10, 10);
        scroll.Size     = new Vector2(780, 660);
        listPanel.AddChild(scroll);

        _deckList = new VBoxContainer();
        _deckList.CustomMinimumSize = new Vector2(760, 0);
        _deckList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_deckList);

        var newDeckBtn = new Button();
        newDeckBtn.Text     = "+ New Deck";
        newDeckBtn.Position = new Vector2(10, 685);
        newDeckBtn.Size     = new Vector2(200, 40);
        newDeckBtn.Pressed += OnNewDeckPressed;
        listPanel.AddChild(newDeckBtn);

        RebuildList();
    }

    private void RebuildList()
    {
        foreach (Node child in _deckList.GetChildren())
            child.QueueFree();

        if (DeckStore.Decks.Count == 0)
        {
            var empty = new Label();
            empty.Text = "No decks saved yet.";
            empty.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
            empty.CustomMinimumSize = new Vector2(0, 44);
            _deckList.AddChild(empty);
            return;
        }

        for (int i = 0; i < DeckStore.Decks.Count; i++)
        {
            int capturedIndex = i;

            var row = new HBoxContainer();
            row.CustomMinimumSize = new Vector2(0, 48);
            row.AddThemeConstantOverride("separation", 10);
            _deckList.AddChild(row);

            var deckBtn = new Button();
            deckBtn.Text = DeckStore.Decks[i].Name;
            deckBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            deckBtn.CustomMinimumSize   = new Vector2(0, 44);
            deckBtn.Pressed += () => OnDeckSelected(capturedIndex);
            row.AddChild(deckBtn);

            var delBtn = new Button();
            delBtn.Text              = "✕";
            delBtn.CustomMinimumSize = new Vector2(44, 44);
            delBtn.Pressed += () => ShowDeleteConfirm(capturedIndex);
            row.AddChild(delBtn);
        }
    }

    private void ShowDeleteConfirm(int index)
    {
        _pendingDeleteIndex        = index;
        _confirmDialog.DialogText  = $"Delete \"{DeckStore.Decks[index].Name}\"?";
        _confirmDialog.PopupCentered();
    }

    private void OnDeleteConfirmed()
    {
        if (_pendingDeleteIndex < 0) return;
        DeckStore.DeleteDeck(_pendingDeleteIndex);
        _pendingDeleteIndex = -1;
        RebuildList();
    }

    private void OnNewDeckPressed()
    {
        DeckStore.EditingIndex = -1;
        GetTree().ChangeSceneToFile("res://scenes/DeckManagement.tscn");
    }

    private void OnDeckSelected(int index)
    {
        DeckStore.EditingIndex = index;
        GetTree().ChangeSceneToFile("res://scenes/DeckManagement.tscn");
    }
}
