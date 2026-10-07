#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace terrarianoita.Common;

public static class SpellTerrain
{
    private static ulong lastFrame;
    private static int remaining;
    private static readonly Projectile AcidProbe = new() { type = Terraria.ID.ProjectileID.Bomb };
    public static bool Corrode(int x, int y)
    {
        if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer || !WorldGen.InWorld(x,y,2)) return false;
        var tile = Main.tile[x,y]; bool damaged = false;
        if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileFrameImportant[tile.TileType] || !AcidProbe.CanExplodeTile(x,y) || !TileLoader.CanKillTile(x,y,tile.TileType,ref damaged)) return false;
        WorldGen.KillTile(x,y,noItem:true); return !tile.HasTile;
    }
    public static int Eat(Projectile projectile, float radius)
    {
        if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return 0;
        if (lastFrame != Main.GameUpdateCount) { lastFrame = Main.GameUpdateCount; remaining = 512; }
        var center = projectile.Center; radius = Math.Clamp(radius, 1, 64); int count = 0;
        int left = Math.Max(1, (int)((center.X - radius) / 16)), right = Math.Min(Main.maxTilesX - 2, (int)((center.X + radius) / 16));
        int top = Math.Max(1, (int)((center.Y - radius) / 16)), bottom = Math.Min(Main.maxTilesY - 2, (int)((center.Y + radius) / 16));
        for (int y = top; y <= bottom && remaining > 0; y++)
            for (int x = left; x <= right && remaining > 0; x++)
            {
                var tile = Main.tile[x, y];
                if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileFrameImportant[tile.TileType]) continue;
                var nearest = Vector2.Clamp(center, new Vector2(x * 16, y * 16), new Vector2((x + 1) * 16, (y + 1) * 16));
                if (Vector2.DistanceSquared(center, nearest) > radius * radius) continue;
                remaining--;
                bool blockDamaged = false;
                if (!projectile.CanExplodeTile(x, y) || !TileLoader.CanKillTile(x, y, tile.TileType, ref blockDamaged)) continue;
                WorldGen.KillTile(x, y, noItem: true);
                if (!tile.HasTile) count++;
            }
        return count;
    }
}
