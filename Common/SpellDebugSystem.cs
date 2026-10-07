#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using terrarianoita.Content.Items;

namespace terrarianoita.Common;

[Autoload(Side = ModSide.Client)]
public sealed class SpellDebugSystem : ModSystem
{
    private UserInterface? ui;
    private SpellDebugState? controls;
    private GameTime frameTime = new();
    public bool IsOpen => ui?.CurrentState != null;
    public void Open(SpellDebugWand wand)
    {
        ModContent.GetInstance<WandEditorSystem>().Close();
        ui ??= new(); controls = new(wand, Close); controls.Activate(); ui.SetState(controls);
    }
    public void Close() { ui?.SetState(null); controls = null; }
    public override void OnWorldUnload()
    {
        if (Main.LocalPlayer?.inventory != null)
            foreach (var item in Main.LocalPlayer.inventory) if (item?.ModItem is SpellDebugWand wand) wand.Stop();
        Close();
    }
    public override void Unload() { OnWorldUnload(); ui = null; }
    public override void UpdateUI(GameTime gameTime)
    {
        frameTime = gameTime;
        if (!IsOpen) return;
        if (Main.gameMenu || Main.LocalPlayer.dead || !ReferenceEquals(Main.LocalPlayer.HeldItem.ModItem, controls?.Wand)) { Close(); return; }
        ui!.Update(gameTime);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0) index = layers.Count;
        layers.Insert(index, new LegacyGameInterfaceLayer("terrarianoita: Spell scan", () => {
            if (IsOpen) ui!.Draw(Main.spriteBatch, frameTime);
            else if (!Main.gameMenu && Main.LocalPlayer.HeldItem.ModItem is SpellDebugWand wand)
            {
                var sequence = wand.Sequence;
                string next = sequence == null ? "Catalog loads on first test" : sequence.Complete ? "Scan finished" :
                    $"{(wand.FixedSpell ? "Fixed" : "Next")} {sequence.Index + 1}/{sequence.Total}: {sequence.Spell} / {sequence.Current().Context}";
                string text = BuildIdentity.Label(Mod) + "\nSpell debug wand | " + (wand.Automatic ? "AUTO" : "MANUAL") + " | " + next +
                    (wand.FixedSpell ? "\nLeft: repeat fixed spell | Right: controls\n" : "\nLeft: test next | Right: controls\n") + wand.Status;
                if (wand.ModifierCards.Length > 0) text += "\nModifiers: " + string.Join(" + ", wand.ModifierCards);
                if (wand.LastTrace is { Lines.Count: > 0 } trace) text += "\n" + trace.Lines[^1];
                Utils.DrawBorderString(Main.spriteBatch, text, new Vector2(20, 100), Color.Cyan, .65f);
            }
            return true;
        }, InterfaceScaleType.UI));
    }
}
