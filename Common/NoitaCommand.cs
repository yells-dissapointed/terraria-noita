#nullable enable
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Core;
using terrarianoita.Content.Items;

namespace terrarianoita.Common;

public sealed class NoitaCommand : ModCommand
{
    public override string Command => "noita";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/noita kit | spell ID | material NAME | liquids [filter] | fill NAME | probe | reactions A B | status | clearliquids";
    public override string Description => "Noita development kit, spell selection and liquid controls";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (Main.netMode != NetmodeID.SinglePlayer) { caller.Reply("Noita gameplay currently requires single-player."); return; }
        var player = caller.Player;
        try
        {
            var world = ModContent.GetInstance<MaterialWorld>(); string action = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
            switch (action)
            {
                case "kit":
                    foreach (int type in new[] { ModContent.ItemType<NoitaWand>(), ModContent.ItemType<SpellDebugWand>(), ModContent.ItemType<MaterialFlask>(), ModContent.ItemType<DebugMaterialFlask>() })
                        player.QuickSpawnItem(player.GetSource_Misc("Noita development kit"), type);
                    caller.Reply("Wands, empty flask and unlimited test flask added.", Color.Cyan); break;
                case "spell":
                    if (args.Length < 2 || player.HeldItem.ModItem is not SpellDebugWand debug) { caller.Reply("Hold the Spell Debug Wand, then /noita spell FIREBALL"); break; }
                    debug.Select(args[1].ToUpperInvariant()); caller.Reply(debug.Status, Color.Cyan); break;
                case "liquids":
                    string filter = args.Length > 1 ? args[1] : "";
                    var names = world.Catalog.Liquids.Where(n => n.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
                    caller.Reply($"{names.Length} imported liquids match; showing up to 40:"); caller.Reply(string.Join(", ", names.Take(40)), Color.Cyan); break;
                case "material": case "fill":
                    if (args.Length < 2) { caller.Reply("Supply an imported liquid name; /noita liquids lists names."); break; }
                    string name = args[1].ToLowerInvariant();
                    if (!world.Catalog.Materials.TryGetValue(name, out var material) || !material.Scoopable)
                    { caller.Reply("Unknown/non-liquid material: " + name); break; }
                    if (player.HeldItem.ModItem is DebugMaterialFlask testFlask) testFlask.Selected = name;
                    else if (action == "fill" && player.HeldItem.ModItem is MaterialFlask flask) flask.Fill(name);
                    else { caller.Reply("Hold the debug flask to select a material, or /noita fill NAME with a normal flask for test contents."); break; }
                    caller.Reply("Selected " + name, Color.Cyan); break;
                case "probe":
                    int x=(int)(Main.MouseWorld.X/16),y=(int)(Main.MouseWorld.Y/16);
                    caller.Reply($"Cell {x},{y}: {world.Grid.AmountAt(x,y)}/255 shared units (excess is retained pressure)");
                    foreach(var layer in world.Grid.LayersAt(x,y).OrderByDescending(c=>MaterialGrid.Density(world.Catalog.Get(c.Material)))) caller.Reply($"{layer.Material}: {layer.Amount}, density {world.Catalog.Get(layer.Material).Density:0.##}",Color.Cyan);
                    if(WorldGen.InWorld(x,y,2)&&Main.tile[x,y].LiquidAmount>0)caller.Reply($"Native liquid: type {Main.tile[x,y].LiquidType}, {Main.tile[x,y].LiquidAmount} units");
                    caller.Reply("Last reaction: "+world.Chemistry.LastReaction);break;
                case "reactions":
                    if(args.Length<3){caller.Reply("Example: /noita reactions water radioactive_liquid");break;}
                    var reactions=world.Chemistry.Available(args[1].ToLowerInvariant(),args[2].ToLowerInvariant());
                    if(reactions.Length==0)caller.Reply("No active binary recipe for that pair. They still share space and settle by density.");
                    foreach(var reaction in reactions.Take(12))caller.Reply($"{reaction.Id}: {reaction.OutputA} + {reaction.OutputB} ({reaction.Probability:0.#}% per contact check; {(reaction.Adapted?"adapted":"imported")})",Color.Cyan);break;
                case "clearliquids": world.ClearCustomLiquids(); caller.Reply("Cleared custom materials; captured native liquids are retained and released as space permits."); break;
                case "status": caller.Reply(BuildIdentity.Label(Mod)); caller.Reply($"{world.Catalog.ImportedCount} imported + 1 adapted material; {world.Catalog.Liquids.Length} liquids; {world.Grid.Count} cells / {world.Grid.Volume} units.");
                    caller.Reply($"{world.Chemistry.ImportedDefinitions} reaction definitions; {world.Chemistry.EligibleDefinitions} binary candidates (pair/output checks also apply); {world.Chemistry.ReactionsApplied} applied.");break;
                default: caller.Reply(Usage, Color.Cyan); break;
            }
        }
        catch (Exception e) { caller.Reply(e.Message, Color.OrangeRed); }
    }
}
