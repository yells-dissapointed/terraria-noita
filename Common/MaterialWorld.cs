#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using terrarianoita.Core;

namespace terrarianoita.Common;

public sealed class MaterialWorld : ModSystem
{
    public MaterialGrid Grid { get; private set; } = new();
    private NoitaMaterialCatalog? catalog;
    private string root = "";
    private ulong lastReaction;
    private int reactionCursor;
    public NoitaMaterialCatalog Catalog
    {
        get { string next = ModContent.GetInstance<NoitaConfig>().ExtractedDataRoot;
            if (catalog == null || next != root) { catalog = new(next); root = next; } return catalog; }
    }
    public override void ClearWorld() { Grid = new(); lastReaction = 0; }
    public override void Unload() { catalog = null; root = ""; Grid = new(); }
    public static bool Blocked(int x, int y) => !WorldGen.InWorld(x, y, 2) ||
        Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType] && !Main.tileSolidTop[Main.tile[x, y].TileType];
    private static bool FlowBlocked(int x, int y) => Blocked(x, y) || Main.tile[x, y].LiquidAmount > 0;
    public static Color Tint(NoitaMaterial material)
    {
        uint c = material.Color; return new Color((byte)(c >> 16), (byte)(c >> 8), (byte)c, (byte)210);
    }
    public int Pour(Vector2 point, string material, int amount, int spread = 1)
    {
        if (Main.netMode != NetmodeID.SinglePlayer) return 0;
        int x = (int)(point.X / 16), y = (int)(point.Y / 16), accepted = 0;
        spread = Math.Clamp(spread, 0, 8);
        for (int r = 0; r <= spread && accepted < amount; r++)
            for (int dx = -r; dx <= r && accepted < amount; dx++)
                accepted += AddCell(x + dx, y - r, material, amount - accepted);
        return accepted;
    }
    private static int NativeType(string material) => material switch { "water" => LiquidID.Water, "lava" => LiquidID.Lava, "honey" => LiquidID.Honey, "shimmer" => LiquidID.Shimmer, _ => -1 };
    private int AddCell(int x, int y, string material, int amount)
    {
        int type = NativeType(material);
        if (type < 0) return Grid.Add(x, y, material, amount, FlowBlocked);
        if (Blocked(x, y) || Grid.At(x,y) != null) return 0;
        var tile = Main.tile[x,y];
        if (tile.LiquidAmount > 0 && tile.LiquidType != type) return 0;
        int added = Math.Min(Math.Max(0, amount), 255 - tile.LiquidAmount);
        if (added > 0) { tile.LiquidType = type; tile.LiquidAmount += (byte)added; Liquid.AddWater(x,y); }
        return added;
    }
    public int Scoop(Vector2 point, FlaskContents flask, int amount)
    {
        if (Main.netMode != NetmodeID.SinglePlayer) return 0;
        int x = (int)(point.X / 16), y = (int)(point.Y / 16), accepted = 0;
        for (int dy = -1; dy <= 1 && accepted < amount; dy++) for (int dx = -1; dx <= 1 && accepted < amount; dx++)
        {
            int tx = x + dx, ty = y + dy; if (!WorldGen.InWorld(tx, ty, 2)) continue;
            if (Grid.At(tx, ty) is { } cell && Catalog.Get(cell.Material).Scoopable)
            { int taken = flask.Add(cell.Material, Math.Min(amount - accepted, cell.Amount)); Grid.Take(tx, ty, taken); accepted += taken; }
            else
            {
                var tile = Main.tile[tx, ty]; if (tile.LiquidAmount == 0) continue;
                string native = tile.LiquidType switch { LiquidID.Lava => "lava", LiquidID.Honey => "honey", LiquidID.Shimmer => "shimmer", _ => "water" };
                int taken = flask.Add(native, Math.Min(amount - accepted, tile.LiquidAmount));
                tile.LiquidAmount -= (byte)taken; if (taken > 0) Liquid.AddWater(tx, ty); accepted += taken;
            }
        }
        return accepted;
    }
    public int Convert(Vector2 center, float radius, string target, string from = "", bool erase = false)
    {
        int count = 0; radius = Math.Clamp(radius, 1, 120);
        var selected = Grid.Cells.Where(c => Vector2.DistanceSquared(center, new Vector2(c.X * 16 + 8, c.Y * 16 + 8)) <= radius * radius &&
            (from.Length == 0 || from.Split(',').Contains(c.Material))).Take(256).ToArray();
        foreach (var cell in selected)
        {
            if (erase || target == "air") { Grid.Take(cell.X, cell.Y, cell.Amount); count++; continue; }
            if (FlowBlocked(cell.X, cell.Y)) continue;
            Grid.Take(cell.X, cell.Y, cell.Amount);
            int added = AddCell(cell.X, cell.Y, target, cell.Amount);
            if (added != cell.Amount) Grid.Add(cell.X, cell.Y, cell.Material, cell.Amount - added, FlowBlocked);
            count++;
        }
        int left = Math.Max(2, (int)((center.X-radius)/16)), right = Math.Min(Main.maxTilesX-3,(int)((center.X+radius)/16));
        int top = Math.Max(2,(int)((center.Y-radius)/16)), bottom = Math.Min(Main.maxTilesY-3,(int)((center.Y+radius)/16));
        for (int y=top;y<=bottom && count<256;y++) for (int x=left;x<=right && count<256;x++)
        {
            var tile=Main.tile[x,y]; if (tile.LiquidAmount==0 || Vector2.DistanceSquared(center,new Vector2(x*16+8,y*16+8))>radius*radius) continue;
            string old = tile.LiquidType switch { LiquidID.Lava => "lava", LiquidID.Honey => "honey", LiquidID.Shimmer => "shimmer", _ => "water" };
            if (from.Length>0 && !from.Split(',').Contains(old) || old==target) continue;
            int amount=tile.LiquidAmount; tile.LiquidAmount=0;
            if (!erase && target!="air") { int added=AddCell(x,y,target,amount); if(added<amount) {tile.LiquidType=NativeType(old);tile.LiquidAmount=(byte)(amount-added);} }
            Liquid.AddWater(x,y);count++;
        }
        return count;
    }
    public override void SaveWorldData(TagCompound tag) => tag["noita_materials"] = Grid.Cells.Select(c => new TagCompound {
        ["x"] = c.X, ["y"] = c.Y, ["material"] = c.Material, ["amount"] = c.Amount }).ToList();
    public override void LoadWorldData(TagCompound tag)
    {
        Grid = new();
        foreach (var cell in tag.GetList<TagCompound>("noita_materials").Take(MaterialGrid.MaximumCells))
            Grid.Add(cell.GetInt("x"), cell.GetInt("y"), cell.GetString("material"), Math.Clamp(cell.GetInt("amount"), 0, 255), (x,y) => !WorldGen.InWorld(x,y,2));
    }
    public override void PostUpdateWorld()
    {
        if (Main.netMode != NetmodeID.SinglePlayer || Grid.Count == 0) return;
        NoitaMaterialCatalog data; try { data = Catalog; } catch { return; }
        if (Main.GameUpdateCount % 120 == 0) Grid.WakeAll();
        Grid.Step(FlowBlocked, data.Get, 1024, (long)Main.GameUpdateCount);
        if (Main.GameUpdateCount % 15 != 0) return;
        foreach (var player in Main.player)
            if (player.active && !player.dead) foreach (var cell in Touching(player.Hitbox)) SpellStatus.Apply(player, data.Get(cell.Material), 90);
        foreach (var npc in Main.npc)
            if (npc.active) foreach (var cell in Touching(npc.Hitbox)) SpellStatus.Apply(npc, data.Get(cell.Material), 90);
        if (lastReaction + 30 > Main.GameUpdateCount) return; lastReaction = Main.GameUpdateCount;
        // Reactions are explicit volume-changing conversions, separate from conservative flow.
        var reactionCells = Grid.Cells.ToArray(); int work = Math.Min(256, reactionCells.Length);
        for (int i = 0; i < work; i++)
        {
            var cell = reactionCells[(reactionCursor + i) % reactionCells.Length];
            var meta = data.Get(cell.Material);
            if (meta.Flammable && !meta.Hot && Neighbour(cell, m => m.Hot))
            { Grid.Take(cell.X, cell.Y, cell.Amount); Grid.Add(cell.X, cell.Y, "fire", cell.Amount, FlowBlocked); }
            else if (meta.Hot && Neighbour(cell, m => m.Name.StartsWith("water", StringComparison.Ordinal)))
            { Grid.Take(cell.X, cell.Y, cell.Amount); Grid.Add(cell.X, cell.Y, "steam", cell.Amount, FlowBlocked); }
            else if (meta.Has("acid") && SpellTerrain.Corrode(cell.X, cell.Y + 1)) Grid.Take(cell.X, cell.Y, Math.Min(16, cell.Amount));
            else if (meta.Kind is "gas" or "fire") Grid.Take(cell.X, cell.Y, Math.Min(8, cell.Amount));
        }
        reactionCursor = reactionCells.Length == 0 ? 0 : (reactionCursor + work) % reactionCells.Length;
        bool Neighbour(MaterialCell cell, Func<NoitaMaterial, bool> predicate) =>
            new[] { Grid.At(cell.X - 1, cell.Y), Grid.At(cell.X + 1, cell.Y), Grid.At(cell.X, cell.Y - 1), Grid.At(cell.X, cell.Y + 1) }
                .Any(n => n.HasValue && predicate(data.Get(n.Value.Material))) ||
            new[] { (cell.X-1,cell.Y), (cell.X+1,cell.Y), (cell.X,cell.Y-1), (cell.X,cell.Y+1) }.Any(p => WorldGen.InWorld(p.Item1,p.Item2,2) && Main.tile[p.Item1,p.Item2].LiquidAmount > 0 && Main.tile[p.Item1,p.Item2].LiquidType == LiquidID.Water && predicate(data.Get("water")));
    }
    private IEnumerable<MaterialCell> Touching(Rectangle hitbox)
    {
        var names = new HashSet<string>();
        for (int y = hitbox.Top / 16; y <= hitbox.Bottom / 16; y++) for (int x = hitbox.Left / 16; x <= hitbox.Right / 16; x++)
            if (Grid.At(x, y) is { Amount: > 12 } cell && names.Add(cell.Material)) yield return cell;
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Grid.Count == 0) return;
        NoitaMaterialCatalog data; try { data = Catalog; } catch { return; }
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        foreach (var cell in Grid.Cells)
        {
            Vector2 p = new(cell.X * 16 - Main.screenPosition.X, cell.Y * 16 - Main.screenPosition.Y);
            if (p.X < -16 || p.Y < -16 || p.X > Main.screenWidth + 16 || p.Y > Main.screenHeight + 16) continue;
            var material = data.Get(cell.Material); int height = Math.Max(2, (int)Math.Ceiling(cell.Amount * 16d / 255));
            var color = Tint(material); if (material.Kind is "gas" or "fire") color *= .55f;
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)p.X, (int)p.Y + 16 - height, 16, height), color * .72f);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)p.X, (int)p.Y + 16 - height, 16, 1), color);
        }
        Main.spriteBatch.End();
    }
}
