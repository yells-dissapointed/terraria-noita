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
public sealed class WandEditorSystem : ModSystem
{
    private UserInterface? ui;
    private WandEditorState? editor;
    private GameTime frameTime = new();
    public bool IsOpen => ui?.CurrentState != null;
    public void Open(NoitaWand wand)
    {
        ui ??= new UserInterface(); editor = new WandEditorState(wand, Close);
        editor.Activate(); ui.SetState(editor);
    }
    public void Close() { ui?.SetState(null); editor = null; }
    public override void OnWorldUnload() => Close();
    public override void Unload() { Close(); ui = null; }
    public override void UpdateUI(GameTime gameTime)
    {
        frameTime = gameTime;
        if (!IsOpen) return;
        if (Main.gameMenu || Main.LocalPlayer.dead || !ReferenceEquals(Main.LocalPlayer.HeldItem.ModItem, editor?.Wand)) { Close(); return; }
        ui!.Update(gameTime);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0) index = layers.Count;
        layers.Insert(index, new LegacyGameInterfaceLayer("terrarianoita: Wand tools", () => {
            if (IsOpen) ui!.Draw(Main.spriteBatch, frameTime);
            else if (!Main.gameMenu && Main.LocalPlayer.HeldItem.ModItem is NoitaWand wand && wand.DebugVisible)
            {
                string text = $"Noita wand | mana {wand.Mana:0}/{wand.Definition.ManaMax:0} | cooldown {wand.Cooldown}\nRight-click: editor / cast log | yellow outlines: hitboxes";
                if (wand.Diagnostics is { } trace)
                {
                    text += "\n" + string.Join("\n", trace.Lines.GetRange(0, Math.Min(2, trace.Lines.Count)));
                    if (trace.Lines.Count > 2) text += "\n" + trace.Lines[^1];
                }
                Utils.DrawBorderString(Main.spriteBatch, text, new Vector2(20, 100), Color.LightPink, .7f);
            }
            return true;
        }, InterfaceScaleType.UI));
    }
}
