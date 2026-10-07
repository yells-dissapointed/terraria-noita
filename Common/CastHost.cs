#nullable enable
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using terrarianoita.Core;
using terrarianoita.Content.Items;
using terrarianoita.Content.Projectiles;

namespace terrarianoita.Common;

public static class CastHost
{
    public static CastHostContext Snapshot(Player player) => new() {
        Frame = (long)Main.GameUpdateCount, X = player.Center.X, Y = player.Center.Y, Hp = player.statLife / 25d, MaxHp = Math.Max(1, player.statLifeMax2) / 25d,
        Money = Coins(player), BlackHoles = Main.projectile.Count(p => p.active && (p.ModProjectile is NoitaComponentProjectile c && c.EntityPath.EndsWith("black_hole_giga.xml") || p.ModProjectile is NoitaEffectProjectile e && e.EntityPath.EndsWith("_giga.xml"))),
        Enemies = Main.npc.Where(n => n.active && !n.friendly && n.CanBeChasedBy()).Select(n => new HostPoint(n.Center.X, n.Center.Y)).ToList(),
        Projectiles = Main.projectile.Where(p => p.active).Select(p => new HostPoint(p.Center.X, p.Center.Y)).ToList(),
        Wands = player.inventory.Where(i => i.ModItem is NoitaWand && !ReferenceEquals(i, player.HeldItem)).Select(i => ((NoitaWand)i.ModItem).Definition.Deck.ToList()).ToList()
    };
    private static long Coins(Player player) => player.inventory.Where(i => !i.IsAir).Sum(i => (long)i.stack * (i.type switch { ItemID.CopperCoin => 1, ItemID.SilverCoin => 100, ItemID.GoldCoin => 10000, ItemID.PlatinumCoin => 1000000, _ => 0 }));
    public static void Apply(CastPlan plan, Player player, CastDiagnostics trace)
    {
        long spent = plan.Events.Where(e => e.Kind == "host_money_spent").Sum(e => Math.Max(0, (long)e.Value.GetDouble()));
        if (spent > 0 && !player.BuyItem(spent)) throw new InvalidOperationException("The coins required by this spell are no longer available.");
        if (spent > 0) trace.Event("Money magic spent " + spent + " copper-equivalent coins");
        foreach (var e in plan.Events)
        {
            if (e.Kind == "host_hp") { int hp = Math.Clamp((int)Math.Round(e.Value.GetDouble() * 25), 1, player.statLifeMax2); trace.Event("Blood magic HP: " + player.statLife + " → " + hp); player.statLife = hp; }
            if (e.Kind == "host_cessation") { player.GetModPlayer<SpellStatusPlayer>().CessationTime = (int)Math.Clamp(e.Value.GetDouble(), 1, 600); trace.Event("Cessation: temporary invulnerability/invisibility; physical body remains in Terraria"); }
        }
    }
}
