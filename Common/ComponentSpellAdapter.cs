#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using terrarianoita.Content.Projectiles;
using terrarianoita.Core;

namespace terrarianoita.Common;

public static class ComponentSpellAdapter
{
    private static readonly Dictionary<string, ComponentSpellProfile> profiles = new();
    private static string root = "";
    public static ComponentSpellProfile Profile(string path)
    {
        var catalog = ModContent.GetInstance<AdapterSystem>().Assets.Catalog;
        if (root != catalog.Root) { profiles.Clear(); root = catalog.Root; }
        if (!profiles.TryGetValue(path, out var profile)) profiles[path] = profile = ComponentSpellProfile.Load(catalog, path);
        return profile;
    }
    public static bool Supports(string path) { try { return Profile(path).Supported; } catch { return false; } }
    public static int Emit(ShotPlan shot, ProjectilePlan node, IEntitySource source, Player player, Vector2 position, Vector2 direction, CastDiagnostics? trace, SpellLiveCase? live)
    {
        var settings = Profile(node.Entity); var catalog = ModContent.GetInstance<AdapterSystem>().Assets.Catalog;
        float speed = (float)Math.Clamp(settings.Visual.SpeedPerFrame * shot.Number("speed_multiplier", 1), 0, 120);
        float spread = MathHelper.ToRadians((float)Math.Clamp(shot.Number("spread_degrees"), -180, 180)) * Main.rand.NextFloat(-.5f, .5f);
        if (settings.Name.StartsWith("cloud_")) position += new Vector2(0, -96);
        double damage = settings.Damage + shot.Config.Where(k => k.Key.StartsWith("damage_") && k.Key.EndsWith("_add") && k.Key is not ("damage_explosion_add" or "damage_healing_add")).Sum(k => shot.Number(k.Key));
        int hp = Math.Clamp((int)Math.Round(player.GetTotalDamage(DamageClass.Magic).ApplyTo((float)(damage * 25))), 0, 100000);
        int id = Projectile.NewProjectile(source, position, direction.RotatedBy(spread) * speed, ModContent.ProjectileType<NoitaComponentProjectile>(), hp, 1, player.whoAmI);
        if (id < 0 || id >= Main.maxProjectiles) return 0;
        var projectile = Main.projectile[id]; projectile.timeLeft = (int)Math.Clamp(settings.Visual.Lifetime + shot.Number("lifetime_add"), 1, 3600);
        var asset = catalog.Entity(node.Entity);
        var evidence = new VisualEntityEvidence { Entity = node.Entity, ProjectileId = id, TerrariaAdapter = "XML components: " + string.Join(", ", settings.Features), GameplayExecuted = true, Appearance = "Pending original sprite / component effect", PreviewProfile = settings.Visual, PreviewLifetimeFrames = projectile.timeLeft, PreviewSpeedPerFrame = speed };
        evidence.Sources.AddRange(asset.Sources); evidence.SpritePaths.AddRange(settings.Visual.Sprites.Select(s => s.ImagePath));
        evidence.Components.AddRange(asset.Definition.Children.Select(c => c.Name)); evidence.Gaps.AddRange(settings.Gaps);
        foreach (string path in asset.Sources) evidence.ImportedSources[path] = catalog.Entity(path).SourceDefinition;
        live?.VisualEntities.Add(evidence);
        ((NoitaComponentProjectile)projectile.ModProjectile).Configure(settings, node, shot, direction, trace, live, evidence);
        projectile.GetGlobalProjectile<SpellMotionBinding>().Configure(shot, trace, evidence);
        projectile.GetGlobalProjectile<SpellAugmentBinding>().Configure(projectile, shot, trace, evidence);
        trace?.Event($"XML live #{id} {settings.Name}: damage {hp}, life {projectile.timeLeft}; {string.Join("; ", settings.Features)}");
        return 1;
    }
}
