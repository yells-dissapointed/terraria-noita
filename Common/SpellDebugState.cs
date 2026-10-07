#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using terrarianoita.Content.Items;

namespace terrarianoita.Common;

public sealed class SpellDebugState : UIState
{
    public SpellDebugWand Wand { get; }
    private readonly Action close;
    private UIPanel panel = null!;
    private UIText cursor = null!, status = null!, detail = null!;
    private UITextPanel<string> auto = null!, context = null!, interval = null!, visualization = null!;
    public SpellDebugState(SpellDebugWand wand, Action closePanel) { Wand = wand; close = closePanel; }
    private static void Place(UIElement e, float x, float y, float width, float height)
    {
        e.Left.Set(x, 0); e.Top.Set(y, 0); e.Width.Set(width, 0); e.Height.Set(height, 0);
    }
    private UIText Label(string text, int y)
    {
        var label = new UIText(text, .65f); Place(label, 0, y, 570, 24); label.IgnoresMouseInteraction = true; panel.Append(label); return label;
    }
    private UITextPanel<string> Button(string text, Action action, int x, int y, int width)
    {
        var button = new UITextPanel<string>(text, .65f); Place(button, x, y, width, 28);
        button.OnLeftClick += (_, _) => action(); panel.Append(button); return button;
    }
    public override void OnInitialize()
    {
        panel = new UIPanel { BackgroundColor = new Color(20, 32, 45) }; panel.SetPadding(12);
        panel.Left.Set(-630, 1); panel.Top.Set(60, 0); panel.Width.Set(610, 0); panel.Height.Set(446, 0); Append(panel);
        Label($"Spell debugging wand | v{Wand.Mod.Version}", 0);
        Button("Close", close, 508, 0, 72);
        cursor = Label("", 34); status = Label("", 62); detail = Label("", 84);
        context = Button("", Wand.ChangeContext, 0, 118, 190);
        interval = Button("", Wand.ChangeInterval, 196, 118, 142);
        Button("Previous", () => Wand.Move(-1), 344, 118, 90);
        Button("Next", () => Wand.Move(1), 440, 118, 70);
        auto = Button("", Wand.ToggleAutomatic, 0, 154, 112);
        Button("Test current", () => { Wand.Stop(); Wand.TestNext(Main.LocalPlayer, advance: false); }, 118, 154, 110);
        Button("Restart", Wand.Restart, 234, 154, 86);
        Button("Save report", () => {
            try { Main.NewText("Spell debug report: " + Wand.SaveReport(), Color.Cyan); }
            catch (Exception e) { Main.NewText(e.Message, Color.OrangeRed); }
        }, 326, 154, 106);
        Button("Clear shots", Wand.CleanupCurrent, 438, 154, 104);
        Label("Fresh deck, unlimited uses and 10000 mana for each isolated test.", 198);
        Label("Auto stops at the end. Switching items stops; inventory/chat pauses.", 220);
        Label("Orange: emitted entity marker. Cyan card: no target projectile claimed.", 242);
        visualization = Button("", Wand.ChangeVisualization, 0, 272, 320);
        Button("Loaded file", () => Main.NewText(BuildIdentity.LoadedPath(Wand.Mod), Color.Cyan), 438, 272, 104);
        Label("XML preview imports sprites/physics; native effects remain deferred.", 312);
        string[] names = { "Spark", "Bomb", "Trigger", "Timer", "Chainsaw", "Mist", "Black hole", "Lua error" };
        string[] ids = { "LIGHT_BULLET", "BOMB", "LIGHT_BULLET_TRIGGER", "LIGHT_BULLET_TIMER", "CHAINSAW", "MIST_ALCOHOL", "BLACK_HOLE", "DAMAGE_RANDOM" };
        for (int i = 0; i < ids.Length; i++)
        {
            string spell = ids[i]; Button(names[i], () => Wand.Select(spell), i % 4 * 140, 342 + i / 4 * 34, 132);
        }
    }
    public override void Update(GameTime gameTime)
    {
        if (panel.ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        var sequence = Wand.Sequence;
        cursor.SetText(sequence == null ? "Catalog loads on the first test" : sequence.Complete ? "Scan finished; Restart to run again" :
            $"Next {sequence.Index + 1}/{sequence.Total}: {sequence.Spell}");
        string message = Wand.Status;
        status.SetText(message.Length > 85 ? message[..85] : message);
        detail.SetText(message.Length > 85 ? message.Substring(85, Math.Min(85, message.Length - 85)) : $"Recorded live tests: {Wand.Report.Cases.Count}");
        auto.SetText(Wand.Automatic ? "Stop auto" : "Start auto");
        context.SetText(Wand.Mode switch { Core.SpellDebugMode.Solo => "Context: Solo", Core.SpellDebugMode.AlwaysCast => "Context: Always cast", Core.SpellDebugMode.AllContexts => "Context: All three", _ => "Context: With sparks" });
        interval.SetText($"Interval: {Wand.Interval / 60f:0.#} seconds");
        visualization.SetText(Wand.VisualPreview ? "Mode: XML/sprite preview (harmless)" : "Mode: gameplay demo (3 entities)");
        base.Update(gameTime);
    }
}
