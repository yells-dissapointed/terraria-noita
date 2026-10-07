#nullable enable
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Core;

namespace terrarianoita.Common;

public static class GameplayProjectileAdapter
{
    public static string DescribeGap(string missing) => missing.StartsWith("Entity: ", StringComparison.Ordinal) && TerrariaSpellMatches.Find(missing[8..]).Implemented ?
        "LEGACY AUDIT GAP, now covered by Terraria prototype: " + missing : "NOT IMPLEMENTED: " + missing;
    public static int VanillaType(TerrariaSpellPrototype prototype) => prototype switch {
        TerrariaSpellPrototype.Bomb => ProjectileID.Bomb, TerrariaSpellPrototype.Arrow => ProjectileID.WoodenArrowFriendly,
        TerrariaSpellPrototype.Bullet => ProjectileID.Bullet, TerrariaSpellPrototype.Rocket => ProjectileID.RocketI,
        _ => throw new NotSupportedException("No implemented Terraria adapter")
    };
    public static void Validate(ShotPlan shot)
    {
        foreach (var node in shot.Projectiles)
        {
            if (!DemoCapabilities.SupportsEntity(node.Entity) && !TerrariaSpellMatches.Find(node.Entity).Implemented)
                throw new NotSupportedException("Gameplay adapter unavailable for " + node.Entity + "; use the debug wand visual preview");
            foreach (var trigger in node.Triggers) Validate(trigger.Payload);
        }
    }
    public static int Emit(ShotPlan shot, IEntitySource source, Player player, Vector2 position, Vector2 direction,
        CastDiagnostics? trace = null, bool debugVisible = false, SpellLiveCase? result = null)
    {
        if (direction.LengthSquared() < .001f) direction = Vector2.UnitX;
        direction.Normalize(); int spawned = 0;
        foreach (var node in shot.Projectiles)
        {
            var single = new ShotPlan { Committed = true, Config = shot.Config, Projectiles = new() { node } };
            var match = TerrariaSpellMatches.Find(node.Entity);
            if (!match.Implemented)
            {
                // Preserve Noita visuals for unmapped/debug-only spells. They remain harmless.
                if (result != null && (!DemoCapabilities.SupportsEntity(node.Entity) || HasUnmappedChild(node)))
                {
                    trace?.Event("No Terraria gameplay match: harmless Noita preview for " + node.Entity);
                    spawned += VisualProjectileAdapter.Emit(single, source, player, position, direction, result);
                }
                else spawned += DemoProjectileAdapter.Emit(single, source, player, position, direction, trace, debugVisible, result);
                continue;
            }
            var catalog = ModContent.GetInstance<AdapterSystem>().Assets.Catalog;
            var asset = catalog.Entity(node.Entity); var profile = catalog.Profile(asset);
            var component = asset.Component("ProjectileComponent");
            bool explosive = match.Prototype is TerrariaSpellPrototype.Bomb or TerrariaSpellPrototype.Rocket;
            double baseDamage = explosive ? component?.Children.LastOrDefault(c => c.Name == "config_explosion")?.Number("damage", 1) ?? 1 : component?.Number("damage", .1) ?? .1;
            var damageTypes = component?.Children.LastOrDefault(c => c.Name == "damage_by_type");
            if (!explosive) baseDamage += damageTypes?.Number("projectile") ?? 0;
            double addition = shot.Number(explosive ? "damage_explosion_add" : "damage_projectile_add");
            int damage = Math.Clamp((int)player.GetTotalDamage(DamageClass.Magic).ApplyTo((float)Math.Clamp((baseDamage + addition) * 25, 1, 100000)), 1, 100000);
            double speed = (match.Prototype == TerrariaSpellPrototype.Bomb ? 6 : profile.SpeedPerFrame) * shot.Number("speed_multiplier", 1);
            speed = Math.Clamp(speed, .1, 120);
            double spread = Math.Clamp(shot.Number("spread_degrees"), -180, 180);
            Vector2 aim = direction.RotatedBy(MathHelper.ToRadians((float)spread) * Main.rand.NextFloat(-.5f, .5f));
            int type = VanillaType(match.Prototype);
            int id = Projectile.NewProjectile(source, position, aim * (float)speed, type, damage, 1, player.whoAmI);
            if (id < 0 || id >= Main.maxProjectiles) continue;
            var projectile = Main.projectile[id]; projectile.DamageType = DamageClass.Magic;
            projectile.timeLeft = (int)Math.Clamp(profile.Lifetime + shot.Number("lifetime_add"), 3, 3600);
            projectile.extraUpdates = 0; // XML lifetimes and payload timers are measured in 60 Hz frames.
            projectile.arrow = false; // Spell ammunition is not a recoverable arrow item.
            var evidence = new VisualEntityEvidence { Entity = node.Entity, ProjectileId = id, Appearance = "Terraria sprite pending draw",
                TerrariaAdapter = match.TerrariaProjectile, GameplayExecuted = true, PreviewProfile = profile,
                PreviewLifetimeFrames = projectile.timeLeft, PreviewSpeedPerFrame = speed };
            evidence.Sources.AddRange(asset.Sources); evidence.Components.AddRange(asset.Definition.Children.Select(c => c.Name));
            foreach (string path in asset.Sources) evidence.ImportedSources[path] = catalog.Entity(path).SourceDefinition;
            if (match.UseNoitaSprite) evidence.SpritePaths.AddRange(profile.Sprites.Select(s => s.ImagePath));
            evidence.Gaps.Add(match.Notes); evidence.Gaps.Add("Prototype conversion: 25 Terraria HP per Noita damage unit; vanilla physics/blast radius/status replace native rules; unmapped modifiers remain deferred");
            result?.VisualEntities.Add(evidence);
            projectile.GetGlobalProjectile<TerrariaProjectileBinding>().Configure(projectile, node, direction, trace, result, evidence, profile, match.UseNoitaSprite, debugVisible);
            trace?.Event($"Terraria {match.TerrariaProjectile} for {System.IO.Path.GetFileName(node.Entity)} #{id}: damage {damage}, life {projectile.timeLeft}; {match.Notes}");
            spawned++;
        }
        return spawned;
    }
    private static bool HasUnmappedChild(ProjectilePlan node) => node.Triggers.Any(t => t.Payload.Projectiles.Any(p =>
        (!DemoCapabilities.SupportsEntity(p.Entity) && !TerrariaSpellMatches.Find(p.Entity).Implemented) || HasUnmappedChild(p)));
}
