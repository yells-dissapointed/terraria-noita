#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using terrarianoita.Core;
using terrarianoita.Content.Projectiles;

namespace terrarianoita.Common;

/// <summary>Explicit three-entity rendering demo, not a Noita component engine.</summary>
public static class DemoProjectileAdapter
{
    public static void Validate(ShotPlan shot)
    {
        foreach (var node in shot.Projectiles)
        {
            if (!DemoCapabilities.SupportsEntity(node.Entity))
                throw new NotSupportedException("The projectile demo cannot render " + node.Entity);
            foreach (var trigger in node.Triggers) Validate(trigger.Payload);
        }
    }
    public static int Emit(ShotPlan shot, IEntitySource source, Player player, Vector2 position, Vector2 direction,
        CastDiagnostics? trace = null, bool debugVisible = false, SpellLiveCase? liveReport = null)
    {
        if (direction.LengthSquared() < 0.001f) direction = Vector2.UnitX;
        direction.Normalize();
        int spawned = 0;
        foreach (var node in shot.Projectiles)
        {
            bool chainsaw = node.Entity == DemoCapabilities.Chainsaw;
            double baseDamage = chainsaw ? 13 : node.Entity == DemoCapabilities.BlueSpark ? 4 : 3;
            double addition = shot.Number("damage_projectile_add") + shot.Number("damage_electricity_add") + shot.Number("damage_ice_add") + (chainsaw ? shot.Number("damage_slice_add") : 0);
            // Prototype conversion: 25 HP per internal damage unit. Native entity effects are not replicated.
            int damage = Math.Max(0, (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo((float)(baseDamage + addition * 25)));
            float speed = (float)Math.Clamp(shot.Number("speed_multiplier", 1), .01, 100) * (chainsaw ? .1f : 12f);
            double spread = Math.Clamp(shot.Number("spread_degrees"), -180, 180);
            float angle = MathHelper.ToRadians((float)spread) * Main.rand.NextFloat(-.5f, .5f);
            Vector2 velocity = direction.RotatedBy(angle) * speed;
            Vector2 spawn = chainsaw ? position + direction * 12 : position;
            int id = Projectile.NewProjectile(source, spawn, chainsaw ? Vector2.Zero : velocity, ModContent.ProjectileType<NoitaSpark>(), damage, 0, player.whoAmI);
            if (id < 0 || id >= Main.maxProjectiles) continue;
            var projectile = Main.projectile[id];
            projectile.timeLeft = Math.Clamp((chainsaw ? 8 : 40) + (int)shot.Number("lifetime_add"), 1, 3600);
            ((NoitaSpark)projectile.ModProjectile).Configure(node, chainsaw, direction, trace, debugVisible);
            ((NoitaSpark)projectile.ModProjectile).BindLiveReport(liveReport);
            projectile.GetGlobalProjectile<SpellMotionBinding>().Configure(shot, trace);
            projectile.GetGlobalProjectile<SpellAugmentBinding>().Configure(projectile, shot, trace);
            spawned++;
            trace?.Event($"Spawn {System.IO.Path.GetFileName(node.Entity)} #{id}, damage {damage}, life {projectile.timeLeft} frames");
        }
        return spawned;
    }
}
