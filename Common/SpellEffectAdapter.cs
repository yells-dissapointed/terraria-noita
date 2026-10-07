#nullable enable
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using terrarianoita.Core;
using terrarianoita.Content.Projectiles;

namespace terrarianoita.Common;

public static class SpellEffectAdapter
{
    public static int Emit(ShotPlan shot, ProjectilePlan node, IEntitySource source, Player player, Vector2 position, Vector2 direction,
        CastDiagnostics? trace, bool debugVisible, SpellLiveCase? live)
    {
        var catalog = ModContent.GetInstance<AdapterSystem>().Assets.Catalog;
        var settings = SpellEffectProfile.Load(catalog, node.Entity); var asset = catalog.Entity(node.Entity);
        double additional = shot.Number("damage_projectile_add") + shot.Number("damage_slice_add") + shot.Number("damage_melee_add");
        int damage = settings.Kind is NoitaEffectKind.Teleport or NoitaEffectKind.TeleportCloser || settings.Kind == NoitaEffectKind.BlackHole && !settings.LargeHole ? 0 :
            (int)Math.Clamp(player.GetTotalDamage(DamageClass.Magic).ApplyTo((float)((settings.Damage + additional) * 25)), 1, 100000);
        float speed = (float)Math.Clamp(settings.Visual.SpeedPerFrame * shot.Number("speed_multiplier", 1), 0, 120);
        float spread = MathHelper.ToRadians((float)Math.Clamp(shot.Number("spread_degrees"), -180, 180)) * Main.rand.NextFloat(-.5f, .5f);
        int id = Projectile.NewProjectile(source, position, direction.RotatedBy(spread) * speed, ModContent.ProjectileType<NoitaEffectProjectile>(), damage, 1, player.whoAmI);
        if (id < 0 || id >= Main.maxProjectiles) return 0;
        var projectile = Main.projectile[id]; projectile.timeLeft = (int)Math.Clamp(settings.Visual.Lifetime + shot.Number("lifetime_add"), 1, 3600);
        var evidence = new VisualEntityEvidence { Entity = node.Entity, ProjectileId = id, Appearance = "Noita effect pending draw",
            TerrariaAdapter = "Custom Noita " + settings.Kind, GameplayExecuted = true, PreviewProfile = settings.Visual,
            PreviewLifetimeFrames = projectile.timeLeft, PreviewSpeedPerFrame = speed };
        evidence.Sources.AddRange(asset.Sources); evidence.Components.AddRange(asset.Definition.Children.Select(c => c.Name));
        evidence.SpritePaths.AddRange(settings.Visual.Sprites.Concat(settings.Segments).Select(s => s.ImagePath));
        foreach (string path in asset.Sources) evidence.ImportedSources[path] = catalog.Entity(path).SourceDefinition;
        if (settings.Kind == NoitaEffectKind.Tentacle)
            foreach (var basis in asset.Definition.Children.Where(c => c.Name == "Entity").SelectMany(c => c.Children).Where(c => c.Name == "Base"))
                evidence.ImportedSources[basis.Get("file")] = catalog.Entity(basis.Get("file")).SourceDefinition;
        evidence.Gaps.Add(settings.Limits); live?.VisualEntities.Add(evidence);
        ((NoitaEffectProjectile)projectile.ModProjectile).Configure(node, settings, direction, trace, live, evidence, debugVisible);
        projectile.GetGlobalProjectile<SpellMotionBinding>().Configure(shot, trace, evidence);
        projectile.GetGlobalProjectile<SpellAugmentBinding>().Configure(projectile, shot, trace, evidence);
        trace?.Event($"Noita {settings.Kind} #{id}: damage {damage}, life {projectile.timeLeft}; {settings.Limits}");
        return 1;
    }
}
