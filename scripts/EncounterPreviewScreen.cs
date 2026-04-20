using Godot;
using System.Collections.Generic;

public partial class EncounterPreviewScreen : Control
{
    private const int CardW       = 75;
    private const int CardH       = 80;
    private const int CardGap     = 5;
    private const int CardsPerRow = 8;

    private Tooltip _tooltip;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        MobStore.LoadMobs();
        DeckStore.EnsureCardsLoaded();
        DeckStore.LoadDecks();

        BuildUI();
    }

    public override void _Process(double delta)
    {
        _tooltip?.UpdatePosition(GetViewport().GetMousePosition());
    }

    private void BuildUI()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.12f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var encounter = RunState.CurrentEncounter;

        var title = new Label();
        title.Text     = $"Encounter: {encounter?.Name ?? "Unknown"}";
        title.Position = new Vector2(50, 28);
        title.AddThemeColorOverride("font_color",   Colors.White);
        title.AddThemeFontSizeOverride("font_size", 24);
        AddChild(title);

        var startBtn = new Button();
        startBtn.Text     = "Start Encounter";
        startBtn.Size     = new Vector2(200, 44);
        startBtn.Position = new Vector2((900 - 200) / 2f, 800);
        startBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/BaseEncounter.tscn");
        AddChild(startBtn);

        var listPanel = new Panel();
        listPanel.Position = new Vector2(50, 80);
        listPanel.Size     = new Vector2(800, 700);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor     = new Color(0.12f, 0.12f, 0.18f);
        panelStyle.BorderColor = new Color(0.30f, 0.30f, 0.40f);
        panelStyle.SetBorderWidthAll(1);
        listPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(listPanel);

        var scroll = new ScrollContainer();
        scroll.Position = new Vector2(10, 10);
        scroll.Size     = new Vector2(780, 680);
        listPanel.AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(760, 0);
        vbox.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(vbox);

        var mobNames = encounter?.Mobs ?? new List<string>();
        var seen     = new HashSet<string>();
        foreach (var name in mobNames)
        {
            if (!seen.Add(name)) continue;
            var entry = MobStore.Mobs.Find(m => m.Name == name);
            if (entry == null) continue;
            vbox.AddChild(BuildMobRow(entry));
        }

        _tooltip = new Tooltip();
        AddChild(_tooltip);
    }

    private Control BuildMobRow(MobEntry entry)
    {
        var cards    = GetDeckCards(entry.DeckName);
        int cardRows = cards.Count > 0 ? Mathf.CeilToInt(cards.Count / (float)CardsPerRow) : 0;
        int cardsBlockH = cardRows * (CardH + CardGap);
        int rowHeight   = 12 + 24 + 4 + 18 + (cards.Count > 0 ? 8 + cardsBlockH : 0) + 12;

        var rowPanel = new Panel();
        rowPanel.CustomMinimumSize = new Vector2(760, rowHeight);
        var rowStyle = new StyleBoxFlat();
        rowStyle.BgColor     = new Color(0.16f, 0.16f, 0.22f);
        rowStyle.BorderColor = new Color(0.35f, 0.35f, 0.50f);
        rowStyle.SetBorderWidthAll(1);
        rowStyle.CornerRadiusTopLeft = rowStyle.CornerRadiusTopRight =
        rowStyle.CornerRadiusBottomLeft = rowStyle.CornerRadiusBottomRight = 4;
        rowPanel.AddThemeStyleboxOverride("panel", rowStyle);

        // Mob visual (coloured square, same style as in-game)
        var visual = new ColorRect();
        visual.Position = new Vector2(12, 12);
        visual.Size     = new Vector2(60, 60);
        visual.Color    = entry.Color;
        rowPanel.AddChild(visual);

        int contentX = 12 + 60 + 16;

        var nameLabel = new Label();
        nameLabel.Text     = $"{entry.Name}   HP: {entry.Health}";
        nameLabel.Position = new Vector2(contentX, 12);
        nameLabel.AddThemeColorOverride("font_color",   Colors.White);
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        rowPanel.AddChild(nameLabel);

        var deckLabel = new Label();
        deckLabel.Text     = $"Deck: {entry.DeckName}";
        deckLabel.Position = new Vector2(contentX, 40);
        deckLabel.AddThemeColorOverride("font_color",   new Color(0.65f, 0.75f, 0.90f));
        deckLabel.AddThemeFontSizeOverride("font_size", 12);
        rowPanel.AddChild(deckLabel);

        int cardsY = 66;
        for (int i = 0; i < cards.Count; i++)
        {
            int col   = i % CardsPerRow;
            int row   = i / CardsPerRow;
            int px    = contentX + col * (CardW + CardGap);
            int py    = cardsY   + row * (CardH + CardGap);

            var panel = CreateCardPanel(px, py);
            UpdateCardPanelVisual(panel, cards[i]);
            var captured = cards[i];
            panel.MouseEntered += () => _tooltip?.Show(captured.Name, captured.Text, string.Join(", ", captured.Tags));
            panel.MouseExited  += () => _tooltip?.Hide();
            rowPanel.AddChild(panel);
        }

        return rowPanel;
    }

    private Panel CreateCardPanel(int x, int y)
    {
        var panel      = new Panel();
        panel.Position = new Vector2(x, y);
        panel.Size     = new Vector2(CardW, CardH);

        var style = new StyleBoxFlat();
        style.BgColor     = new Color(0.18f, 0.18f, 0.22f);
        style.BorderColor = new Color(0.38f, 0.38f, 0.48f);
        style.SetBorderWidthAll(1);
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
        style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 3;
        panel.AddThemeStyleboxOverride("panel", style);

        var cardBg = new ColorRect();
        cardBg.Name        = "CardBg";
        cardBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        cardBg.OffsetLeft  = 2;  cardBg.OffsetTop    = 2;
        cardBg.OffsetRight = -2; cardBg.OffsetBottom = -2;
        cardBg.Visible     = false;
        cardBg.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(cardBg);

        var lbl = new Label();
        lbl.Name                = "CardName";
        lbl.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        lbl.VerticalAlignment   = VerticalAlignment.Center;
        lbl.AutowrapMode        = TextServer.AutowrapMode.Word;
        lbl.AddThemeColorOverride("font_color",   Colors.White);
        lbl.AddThemeFontSizeOverride("font_size", 11);
        lbl.Visible     = false;
        lbl.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(lbl);

        return panel;
    }

    private static void UpdateCardPanelVisual(Panel panel, CardData card)
    {
        var cardBg = panel.GetNode<ColorRect>("CardBg");
        var lbl    = panel.GetNode<Label>("CardName");

        if (card == null)
        {
            cardBg.Visible = false;
            lbl.Visible    = false;
        }
        else
        {
            cardBg.Color   = card.Color;
            cardBg.Visible = true;
            lbl.Text       = card.Name;
            lbl.Visible    = true;
        }
    }

    private static List<CardData> GetDeckCards(string deckName)
    {
        var result    = new List<CardData>();
        var deckEntry = DeckStore.Decks.Find(d => d.Name == deckName);
        if (deckEntry == null) return result;

        var byId = new Dictionary<string, CardData>();
        foreach (var c in DeckStore.AllCards) byId[c.Id] = c;

        var sorted = new List<SlotEntry>(deckEntry.Slots);
        sorted.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        foreach (var s in sorted)
            if (byId.TryGetValue(s.CardId, out var card))
                result.Add(card);

        return result;
    }
}
