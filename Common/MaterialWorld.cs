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
    private MaterialChemistry? chemistry;
    private bool adaptedReactions;
    private string root = "";
    private ulong lastReaction;
    private int reactionCursor;
    private int releaseCursor;
    public NoitaMaterialCatalog Catalog
    {
        get { string next = ModContent.GetInstance<NoitaConfig>().ExtractedDataRoot;
            if (catalog == null || next != root) { catalog = new(next); root = next; chemistry = null; } return catalog; }
    }
    public MaterialChemistry Chemistry { get { var data = Catalog; bool custom = ModContent.GetInstance<NoitaConfig>().AdaptedLiquidReactions;
        if (chemistry == null || custom != adaptedReactions) { chemistry = new(data,custom); adaptedReactions = custom; } return chemistry; } }
    public override void ClearWorld() { Grid = new(); lastReaction = 0; reactionCursor = 0; releaseCursor = 0; chemistry = null; }
    public override void Unload() { catalog = null; chemistry = null; root = ""; Grid = new(); }
    public static bool Blocked(int x, int y) => !WorldGen.InWorld(x, y, 2) ||
        Main.tile[x, y].HasUnactuatedTile && Main.tileSolid[Main.tile[x, y].TileType] && !Main.tileSolidTop[Main.tile[x, y].TileType];
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
    private static string NativeName(int type) => type switch { LiquidID.Lava => "lava", LiquidID.Honey => "honey", LiquidID.Shimmer => "shimmer", _ => "water" };
    // Only active contact regions cross into the shared solver. Unrelated vanilla pools stay native.
    private void CaptureNative(int x, int y) => CaptureNativeAt(x,y,false);
    private void CaptureNativeAt(int x, int y, bool force)
    {
        if (Blocked(x,y)) return;
        if (!force && Grid.AmountAt(x,y)==0 && !NearCustom(x,y)) return;
        var tile = Main.tile[x,y]; if (tile.LiquidAmount == 0 || tile.LiquidType == LiquidID.Shimmer) return;
        int accepted = Grid.ImportNative(x,y,NativeName(tile.LiquidType),tile.LiquidAmount,Blocked);
        if (accepted > 0) { tile.LiquidAmount -= (byte)accepted; Liquid.AddWater(x,y); }
    }
    private bool NearCustom(int x, int y)
    {
        for (int dx=-1;dx<=1;dx++) for (int dy=-1;dy<=1;dy++)
            if (Grid.LayersAt(x+dx,y+dy).Any(c => NativeType(c.Material)<0)) return true;
        return false;
    }
    private void ReleaseNative(int budget = 512)
    {
        var positions=Grid.Positions.ToArray();int work=Math.Min(budget,positions.Length);
        for(int i=0;i<work;i++)
        {
            var p=positions[(releaseCursor+i)%positions.Length];
            var layers=Grid.LayersAt(p.X,p.Y); if(layers.Length!=1 || NearCustom(p.X,p.Y) || Blocked(p.X,p.Y)) continue;
            var cell=layers[0]; int type=NativeType(cell.Material); if(type<0 || type==LiquidID.Shimmer) continue;
            var tile=Main.tile[p.X,p.Y]; if(tile.LiquidAmount>0 && tile.LiquidType!=type) continue;
            int amount=Math.Min(cell.Amount,255-tile.LiquidAmount); if(amount<=0)continue;
            tile.LiquidType=type;tile.LiquidAmount+=(byte)amount;Grid.Take(p.X,p.Y,amount,cell.Material);Liquid.AddWater(p.X,p.Y);
        }
        releaseCursor=positions.Length==0?0:(releaseCursor+work)%positions.Length;
    }
    public void ClearCustomLiquids()
    { foreach(var cell in Grid.Cells.Where(c=>NativeType(c.Material)<0).ToArray())Grid.Take(cell.X,cell.Y,cell.Amount,cell.Material); ReleaseNative(MaterialGrid.MaximumCells); }
    private int AddCell(int x, int y, string material, int amount)
    {
        int type = NativeType(material);
        if (type != LiquidID.Shimmer && (type < 0 || Grid.AmountAt(x,y)>0 || NearCustom(x,y)))
        { CaptureNativeAt(x,y,true); return Grid.Add(x,y,material,amount,FlowBlocked); }
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
            if (Grid.AmountAt(tx,ty)>0)
            {
                CaptureNative(tx,ty);
                accepted+=Grid.Scoop(tx,ty,flask,amount-accepted,name=>Catalog.Get(name).Scoopable);
            }
            if (accepted < amount)
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
            if (erase || target == "air") { Grid.Take(cell.X, cell.Y, cell.Amount,cell.Material); count++; continue; }
            if (FlowBlocked(cell.X, cell.Y)) continue;
            if(Grid.Transform(cell,target,cell.Amount)>0)count++;
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
    public override void SaveWorldData(TagCompound tag)
    { tag["noita_materials_version"]=2; tag["noita_materials"] = Grid.Cells.Select(c => new TagCompound {
        ["x"] = c.X, ["y"] = c.Y, ["material"] = c.Material, ["amount"] = c.Amount }).ToList(); }
    public override void LoadWorldData(TagCompound tag) => Grid=RestoreGrid(tag,(x,y)=>!WorldGen.InWorld(x,y,2));
    public static MaterialGrid RestoreGrid(TagCompound tag, Func<int,int,bool> blocked)
    {
        var grid = new MaterialGrid();
        foreach (var cell in tag.GetList<TagCompound>("noita_materials").Take(MaterialGrid.MaximumCells*MaterialGrid.MaximumMaterialsPerCell))
            grid.Add(cell.GetInt("x"), cell.GetInt("y"), cell.GetString("material"), Math.Clamp(cell.GetInt("amount"), 0, MaterialGrid.MaximumStored), blocked,allowPressure:true);
        return grid;
    }
    public override void PostUpdateWorld()
    {
        if (Main.netMode != NetmodeID.SinglePlayer || Grid.Count == 0) return;
        NoitaMaterialCatalog data; try { data = Catalog; } catch { return; }
        if (Main.GameUpdateCount % 120 == 0) Grid.WakeAll();
        Grid.Step(FlowBlocked, data.Get, 512, (long)Main.GameUpdateCount,CaptureNative);
        if (Main.GameUpdateCount % 15 != 0) return;
        Chemistry.Step(Grid,Blocked,(long)Main.GameUpdateCount);
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
            if (meta.Has("acid") && Grid.AmountOf(cell.X,cell.Y,cell.Material)>0 && SpellTerrain.Corrode(cell.X, cell.Y + 1)) Grid.Take(cell.X, cell.Y, Math.Min(16, cell.Amount),cell.Material);
            else if (meta.Kind is "gas" or "fire") Grid.Take(cell.X, cell.Y, Math.Min(8, cell.Amount),cell.Material);
        }
        reactionCursor = reactionCells.Length == 0 ? 0 : (reactionCursor + work) % reactionCells.Length;
        ReleaseNative();
    }
    private IEnumerable<MaterialCell> Touching(Rectangle hitbox)
    {
        var names = new HashSet<string>();
        for (int y = hitbox.Top / 16; y <= hitbox.Bottom / 16; y++) for (int x = hitbox.Left / 16; x <= hitbox.Right / 16; x++)
            foreach(var cell in Grid.LayersAt(x,y)) if(cell.Amount>12 && names.Add(cell.Material)) yield return cell;
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Grid.Count == 0) return;
        NoitaMaterialCatalog data; try { data = Catalog; } catch { return; }
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        foreach (var position in Grid.Positions)
        {
            Vector2 p = new(position.X * 16 - Main.screenPosition.X, position.Y * 16 - Main.screenPosition.Y);
            if (p.X < -16 || p.Y < -16 || p.X > Main.screenWidth + 16 || p.Y > Main.screenHeight + 16) continue;
            var layers=Grid.LayersAt(position.X,position.Y).OrderByDescending(c=>MaterialGrid.Density(data.Get(c.Material))).ToArray();
            double scale=16d/Math.Max(255,layers.Sum(c=>c.Amount)), bottom=16;
            foreach(var cell in layers)
            {
                var material=data.Get(cell.Material);double top=bottom-cell.Amount*scale;
                int from=(int)Math.Round(top),height=(int)Math.Round(bottom)-from;bottom=top;if(height<=0)continue;
                var color=Tint(material);if(material.Kind is "gas" or "fire")color*=.55f;
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,new Rectangle((int)p.X,(int)p.Y+from,16,height),color*.72f);
            }
            int surface=(int)Math.Round(bottom);
            if(surface<16)Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,new Rectangle((int)p.X,(int)p.Y+surface,16,1),Tint(data.Get(layers[^1].Material)));
        }
        Main.spriteBatch.End();
    }
}
