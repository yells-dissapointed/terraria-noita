#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using terrarianoita.Content.Items;
using terrarianoita.Core;

namespace terrarianoita.Common;

public sealed class WandEditorState : UIState
{
    private static readonly string[] DemoSpells = {
        "LIGHT_BULLET", "LIGHT_BULLET_TRIGGER", "LIGHT_BULLET_TRIGGER_2", "LIGHT_BULLET_TIMER", "CHAINSAW",
        "BURST_2", "BURST_3", "BURST_4", "DAMAGE", "MANA_REDUCE", "RECHARGE", "SPREAD_REDUCE",
        "SPEED", "LIFETIME", "LIFETIME_DOWN", "ADD_TRIGGER", "ADD_TIMER", "ADD_DEATH_TRIGGER", "GAMMA"
    };
    public NoitaWand Wand { get; }
    private WandDefinition draft;
    private readonly Action close;
    private UIPanel panel = null!;
    private UIList palette = null!, slots = null!, log = null!;
    private UIText status = null!;
    private UITextPanel<string> destination = null!, catalogMode = null!, debug = null!;
    private string[] catalog = DemoSpells;
    private bool alwaysCast, allSpells;
    private int historyIndex, shownVersion = -1;
    private CastDiagnostics? shownTrace;
    private readonly List<Action> refreshStats = new();
    public WandEditorState(NoitaWand wand, Action closeEditor) { Wand = wand; draft = wand.Definition.Copy(); close = closeEditor; }
    private static void Place(UIElement e, float left, float top, float width, float height)
    {
        e.Left.Set(left, 0); e.Top.Set(top, 0); e.Width.Set(width, 0); e.Height.Set(height, 0);
    }
    private static UITextPanel<string> Button(UIElement parent, string text, Action action, float left, float top, float width)
    {
        var b = new UITextPanel<string>(text, .65f); Place(b, left, top, width, 28);
        b.OnLeftClick += (_, _) => action(); parent.Append(b); return b;
    }
    private static UIText Label(UIElement parent, string text, float left, float top)
    {
        var label = new UIText(text, .7f); Place(label, left, top, 300, 24); label.IgnoresMouseInteraction = true; parent.Append(label); return label;
    }
    public override void OnInitialize()
    {
        panel = new UIPanel { HAlign = .5f, VAlign = .5f, BackgroundColor = new Color(25, 28, 45) };
        panel.Width.Set(0, .92f); panel.Height.Set(0, .82f); panel.SetPadding(12); Append(panel);
        Label(panel, "Noita wand editor — non-shuffle prototype", 0, 2);
        var closeButton = Button(panel, "Close", close, -72, 0, 72); closeButton.Left.Set(-72, 1);
        Label(panel, "Edit a draft, test it, then Apply. Switching items closes this window.", 0, 28);
        Stat(0, "Delay", () => draft.CastDelay, v => draft.CastDelay = v, 1, 0, 600, "f");
        Stat(1, "Reload", () => draft.ReloadTime, v => draft.ReloadTime = v, 5, 0, 600, "f");
        Stat(2, "Draw", () => draft.ActionsPerRound, v => draft.ActionsPerRound = (int)v, 1, 1, 16, "");
        Stat(3, "Mana max", () => draft.ManaMax, v => draft.ManaMax = v, 25, 1, 10000, "");
        Stat(4, "Mana regen", () => draft.ManaRecharge, v => draft.ManaRecharge = v, 10, 0, 10000, "/s");
        debug = Button(panel, "Debug: ON", () => { Wand.DebugVisible = !Wand.DebugVisible; RefreshDebug(); }, 0, 136, 100);
        RefreshDebug();
        Button(panel, "Apply", () => {
            try { Wand.Apply(draft); Message("Applied. Live deck reset; saved with this wand."); }
            catch (Exception e) { Message(e.Message); }
        }, 106, 136, 72);
        Button(panel, "Test draft", () => { Wand.Preview(draft); historyIndex = 0; RefreshLog(); }, 184, 136, 96);
        Button(panel, "Reset draft", () => { draft = Wand.Definition.Copy(); RefreshSlots(); RefreshStats(); Message("Draft restored from the equipped wand."); }, 286, 136, 104);
        Button(panel, "Save log", () => {
            try {
                var trace = SelectedTrace();
                if (trace == null) { Message("Cast or test a draft first."); return; }
                string path = Wand.SaveLog(trace); Message("Log saved; file path printed in chat."); Main.NewText(path, Color.LightPink);
            } catch (Exception e) { Message(e.Message); }
        }, 396, 136, 88);
        Button(panel, "Older", () => { if (Wand.History.Count > 0) historyIndex = Math.Min(historyIndex + 1, Wand.History.Count - 1); RefreshLog(); }, 490, 136, 64);
        Button(panel, "Newer", () => { historyIndex = Math.Max(0, historyIndex - 1); RefreshLog(); }, 560, 136, 64);
        status = new UIText("Chainsaw: short-range NPC slice; no block digging. Test experimental spells first.", .62f);
        Place(status, 0, 171, 0, 24); status.Width.Set(0, 1); panel.Append(status);
        Label(panel, "Spell palette", 0, 198);
        Label(panel, "Ordered slots / always cast", 0, 198).Left.Set(0, .26f);
        Label(panel, "Cast log: last 8", 0, 198).Left.Set(0, .64f);
        destination = Button(panel, "Add to: Deck", () => { alwaysCast = !alwaysCast; destination.SetText(alwaysCast ? "Add to: Always" : "Add to: Deck"); }, 0, 224, 112);
        catalogMode = Button(panel, "Demo set", () => { allSpells = !allSpells; catalogMode.SetText(allSpells ? "All / experimental" : "Demo set"); RefreshPalette(); }, 118, 224, 118);
        palette = MakeList(0, .24f); slots = MakeList(.26f, .36f); log = MakeList(.64f, .36f);
        try { catalog = Wand.SpellIds(); }
        catch (Exception e) { Message("Catalog could not load: " + e.Message); }
        RefreshPalette(); RefreshSlots(); Recalculate(); RefreshLog();
    }
    private void Stat(int index, string name, Func<double> read, Action<double> write, double step, double min, double max, string suffix)
    {
        var group = new UIElement(); group.Left.Set(0, (index % 3) / 3f); group.Top.Set(62 + (index / 3) * 34, 0);
        group.Width.Set(-12, 1 / 3f); group.Height.Set(30, 0); panel.Append(group);
        var value = new UIText("", .67f); Place(value, 32, 6, 0, 24); value.Width.Set(-64, 1); group.Append(value);
        Action refresh = () => value.SetText($"{name}: {read():0.##}{suffix}"); refreshStats.Add(refresh); refresh();
        Button(group, "−", () => { write(Math.Clamp(read() - step, min, max)); refresh(); }, 0, 0, 26);
        var plus = Button(group, "+", () => { write(Math.Clamp(read() + step, min, max)); refresh(); }, -26, 0, 26); plus.Left.Set(-26, 1);
    }
    private UIList MakeList(float left, float width)
    {
        var list = new UIList { ListPadding = 5 }; list.Left.Set(0, left); list.Top.Set(260, 0);
        list.Width.Set(-22, width); list.Height.Set(-260, 1); panel.Append(list);
        var bar = new UIScrollbar(); bar.SetView(100, 1000); bar.Left.Set(-18, left + width);
        bar.Top.Set(260, 0); bar.Width.Set(18, 0); bar.Height.Set(-260, 1); panel.Append(bar); list.SetScrollbar(bar);
        return list;
    }
    private void RefreshDebug() => debug.SetText(Wand.DebugVisible ? "Debug: ON" : "Debug: OFF");
    private void RefreshStats() { foreach (var update in refreshStats) update(); }
    private void Message(string message) => status.SetText(message.Length <= 130 ? message : message[..127] + "...");
    private void RefreshPalette()
    {
        palette.Clear();
        foreach (string id in allSpells ? catalog : DemoSpells.Where(id => catalog.Contains(id)))
        {
            var row = new UITextPanel<string>(id, .62f); row.Width.Set(0, 1); row.Height.Set(28, 0);
            row.OnLeftClick += (_, _) => {
                var target = alwaysCast ? draft.AlwaysCast : draft.Deck;
                if (target.Count >= (alwaysCast ? 8 : 64)) { Message("Slot limit reached."); return; }
                target.Add(id); RefreshSlots();
            }; palette.Add(row);
        }
    }
    private void RefreshSlots()
    {
        slots.Clear(); AddSlots(draft.Deck, false); AddSlots(draft.AlwaysCast, true);
    }
    private void AddSlots(List<string> ids, bool permanent)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            int index = i;
            var row = new UIElement(); row.Width.Set(0, 1); row.Height.Set(54, 0);
            Label(row, (permanent ? "AC " : "") + (i + 1) + ". " + ids[i], 0, 0);
            Button(row, "↑", () => { if (index > 0) (ids[index], ids[index - 1]) = (ids[index - 1], ids[index]); RefreshSlots(); }, 0, 24, 28);
            Button(row, "↓", () => { if (index + 1 < ids.Count) (ids[index], ids[index + 1]) = (ids[index + 1], ids[index]); RefreshSlots(); }, 34, 24, 28);
            Button(row, "X", () => { ids.RemoveAt(index); RefreshSlots(); }, 68, 24, 28);
            Button(row, permanent ? "To deck" : "Always", () => {
                var target = permanent ? draft.Deck : draft.AlwaysCast;
                if (target.Count >= (permanent ? 64 : 8)) { Message("Destination slot limit reached."); return; }
                target.Add(ids[index]); ids.RemoveAt(index); RefreshSlots();
            }, 102, 24, 70);
            slots.Add(row);
        }
    }
    private CastDiagnostics? SelectedTrace() => Wand.History.Count == 0 ? null : Wand.History[Math.Clamp(historyIndex, 0, Wand.History.Count - 1)];
    private void RefreshLog()
    {
        var trace = SelectedTrace(); log.Clear(); shownTrace = trace; shownVersion = trace?.Version ?? -1;
        int width = Math.Clamp((int)(log.GetInnerDimensions().Width / 7), 20, 100);
        foreach (string line in trace?.Lines ?? new List<string> { "No casts yet. Test draft to inspect draws, mana and trigger trees.", "Preview uses a fresh deck. Live casting retains deck state.", "Save log exports the full plan and collision events as JSON." })
            log.Add(new LogLine(line, width));
    }
    public override void Update(GameTime gameTime)
    {
        if (panel.ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        base.Update(gameTime);
        var trace = SelectedTrace();
        if (!ReferenceEquals(trace, shownTrace) || (trace?.Version ?? -1) != shownVersion) RefreshLog();
    }
    private sealed class LogLine : UIElement
    {
        private readonly string text;
        private readonly Color color;
        public LogLine(string line, int width)
        {
            var pieces = new List<string>();
            for (int i = 0; i < line.Length; i += width) pieces.Add(line.Substring(i, Math.Min(width, line.Length - i)));
            text = string.Join("\n", pieces); color = line.StartsWith("ERROR") ? Color.OrangeRed : Color.LightGray;
            Width.Set(0, 1); Height.Set(Math.Max(1, pieces.Count) * 17 + 4, 0); IgnoresMouseInteraction = true;
        }
        protected override void DrawSelf(SpriteBatch spriteBatch) => Utils.DrawBorderString(spriteBatch, text, GetDimensions().Position(), color, .65f);
    }
}
