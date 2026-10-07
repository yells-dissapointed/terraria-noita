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

public static class VisualProjectileAdapter
{
    public static int Emit(ShotPlan shot, IEntitySource source, Player player, Vector2 position, Vector2 direction, SpellLiveCase result)
    {
        if (direction.LengthSquared() < .001f) direction = Vector2.UnitX;
        direction.Normalize(); int count = 0;
        var catalog = ModContent.GetInstance<AdapterSystem>().Assets.Catalog;
        foreach (var node in shot.Projectiles)
        {
            var evidence = new VisualEntityEvidence { Entity = node.Entity };
            var profile = new NoitaVisualProfile();
            try
            {
                var asset = catalog.Entity(node.Entity); profile = catalog.Profile(asset);
                evidence.Sources.AddRange(asset.Sources);
                foreach (string path in asset.Sources) evidence.ImportedSources[path] = catalog.Entity(path).SourceDefinition;
                evidence.Components.AddRange(asset.Definition.Children.Select(c => c.Name));
                evidence.SpritePaths.AddRange(profile.Sprites.Select(s => s.ImagePath));
            }
            catch (Exception e) { profile.Gaps.Add("XML import failed: " + e.Message); }
            evidence.Gaps.AddRange(profile.Gaps);
            evidence.Appearance = profile.Sprites.Count > 0 ? "sprite pending GPU draw" : "entity marker";
            int xmlLife = profile.Lifetime;
            int life = (int)Math.Clamp(xmlLife + shot.Number("lifetime_add"), 12, 3600);
            if (xmlLife + shot.Number("lifetime_add") < 12) evidence.Gaps.Add("Lifetime extended to 12 frames for visibility");
            double speed = Math.Clamp(profile.SpeedPerFrame * shot.Number("speed_multiplier", 1), 0, 120);
            evidence.PreviewProfile = profile; evidence.PreviewLifetimeFrames = life; evidence.PreviewSpeedPerFrame = speed;
            // Deterministic center direction makes isolated tests repeatable. Native random spread is deferred.
            if (shot.Number("spread_degrees") != 0) evidence.Gaps.Add("Random spread not applied in visual preview");
            Vector2 velocity = direction * (float)speed;
            int id = Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NoitaVisualProjectile>(), 0, 0, player.whoAmI);
            if (id < 0 || id >= Main.maxProjectiles) { result.Trace.Event("Visual spawn failed: projectile slots full"); continue; }
            var projectile = Main.projectile[id]; projectile.timeLeft = life;
            ((NoitaVisualProjectile)projectile.ModProjectile).Configure(node, profile, direction, result, evidence);
            projectile.GetGlobalProjectile<SpellMotionBinding>().Configure(shot, result.Trace, evidence);
            evidence.ProjectileId = id; result.VisualEntities.Add(evidence); count++;
            result.Trace.Event($"Visual entity {System.IO.Path.GetFileName(node.Entity)} #{id}: {evidence.Appearance}; life {life}; native gameplay deferred");
        }
        return count;
    }
    public static int Card(IEntitySource source, Player player, Vector2 position, SpellCard? card, string label, SpellLiveCase result)
    {
        var profile = new NoitaVisualProfile { SpeedPerFrame = 0, TileCollide = false, Rotate = false, Lifetime = 90 };
        if (card?.Sprite.Length > 0)
        {
            try { profile.Sprites.Add(ModContent.GetInstance<AdapterSystem>().Assets.Catalog.Sprite(card.Sprite)); }
            catch (Exception e) { result.Trace.Event("Diagnostic card image: " + e.Message); }
        }
        int id = Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<NoitaVisualProjectile>(), 0, 0, player.whoAmI);
        if (id < 0 || id >= Main.maxProjectiles) return 0;
        var projectile = Main.projectile[id]; projectile.timeLeft = 90;
        ((NoitaVisualProjectile)projectile.ModProjectile).ConfigureCard(profile, label, result);
        result.Trace.Event("Diagnostic card only: " + label + "; no target projectile claimed"); return 1;
    }
}
