#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Core;
using terrarianoita.Content.Projectiles;

namespace terrarianoita.Common;

public sealed class SpellAugmentBinding : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public bool Bound { get; private set; }
    public bool Piercing => settings?.Piercing == true;
    public bool ExplosionsDisabled => settings?.ExplosionsDisabled == true || settings?.NullDamage == true;
    private SpellAugments? settings;
    private ShotPlan shot = new();
    private CastDiagnostics? trace;
    private int age;
    private bool extraExplosion;
    public void Configure(Projectile projectile, ShotPlan config, CastDiagnostics? diagnostics, VisualEntityEvidence? evidence = null)
    {
        if (projectile.ModProjectile is NoitaVisualProjectile) return;
        Bound = true; shot = config; trace = diagnostics; settings = SpellAugments.Load(config, ModContent.GetInstance<AdapterSystem>().Assets.Catalog);
        projectile.CritChance = (int)Math.Clamp(shot.Number("damage_critical_chance"), 0, 100);
        projectile.knockBack += (float)Math.Clamp(shot.Number("knockback_force"), -10, 20);
        if (settings.Piercing) { projectile.penetrate = -1; projectile.usesLocalNPCImmunity = true; projectile.localNPCHitCooldown = 10; }
        if (settings.Phasing || settings.DigRadius > 0) projectile.tileCollide = false;
        if (settings.NullDamage) projectile.damage = 0;
        if (settings.LifetimeExtended) projectile.timeLeft = 3600;
        if (shot.Text("extra_entities").Contains("/nolla.xml", StringComparison.Ordinal)) projectile.timeLeft = 1;
        extraExplosion = shot.Number("explosion_radius") > 0 && projectile.ModProjectile is not NoitaComponentProjectile && projectile.type is not (ProjectileID.Bomb or ProjectileID.RocketI);
        evidence?.Gaps.AddRange(settings.Gaps);
        foreach (string path in settings.Sources) { if (evidence != null) evidence.ImportedSources[path] = ModContent.GetInstance<AdapterSystem>().Assets.Catalog.Entity(path).SourceDefinition; }
        if (settings.Sources.Count > 0) trace?.Event("Gameplay augments: " + string.Join(", ", settings.Sources));
    }
    public override bool? CanDamage(Projectile projectile) => Bound && (projectile.damage <= 0 || settings?.NullDamage == true) ? false : null;
    public override void PostAI(Projectile projectile)
    {
        if (!Bound || settings == null || Main.netMode != NetmodeID.SinglePlayer) return; age++;
        projectile.velocity.Y += (float)Math.Clamp(shot.Number("gravity") / 3600, -.5, .5);
        if (settings.Floating) projectile.velocity.Y *= .8f;
        if (settings.Phasing) projectile.tileCollide = false;
        if (settings.NullDamage) projectile.damage = 0;
        if (settings.DigRadius > 0 && age % 3 == 0) SpellTerrain.Eat(projectile, settings.DigRadius);
        if (settings.Shield)
            foreach (var enemyShot in Main.projectile)
                if (enemyShot.active && enemyShot.hostile && Vector2.DistanceSquared(enemyShot.Center, projectile.Center) < 32 * 32) enemyShot.active = false;
        if (age % 6 != 0) return;
        var world = ModContent.GetInstance<MaterialWorld>();
        foreach (string trail in settings.Trails) world.Pour(projectile.Center, trail, (int)Math.Clamp(shot.Number("trail_material_amount", 8), 4, 80));
        if (settings.Fire) world.Pour(projectile.Center, "fire", 8);
        foreach (var conversion in settings.Conversions) world.Convert(projectile.Center, (float)conversion.Radius, conversion.To, conversion.From);
        if (settings.AreaRadius > 0)
            foreach (var npc in Main.npc)
                if (npc.active && !npc.friendly && npc.CanBeChasedBy(projectile) && Vector2.DistanceSquared(npc.Center, projectile.Center) < settings.AreaRadius * settings.AreaRadius)
                    npc.SimpleStrikeNPC((int)Math.Max(1, settings.AreaDamage * 25), projectile.direction, damageType: DamageClass.Magic);
        if (shot.Config.TryGetValue("friendly_fire", out var value) && value.ValueKind == System.Text.Json.JsonValueKind.True && age > 10 && projectile.damage > 0)
        { var player = Main.player[projectile.owner]; if (player.active && projectile.Hitbox.Intersects(player.Hitbox)) player.Hurt(PlayerDeathReason.ByProjectile(player.whoAmI, projectile.whoAmI), projectile.damage, projectile.direction); }
    }
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Bound || settings == null) return;
        foreach (string path in settings.Statuses) SpellStatus.Apply(target, SpellStatus.EffectName(path), 180);
        if (settings.Fire) target.AddBuff(BuffID.OnFire, 180);
        if (settings.Electric) target.AddBuff(BuffID.Electrified, 120);
        if (settings.Freeze) SpellStatus.Apply(target, "frozen", 120);
        string material = shot.Text("material"); if (material.Length > 0) ModContent.GetInstance<MaterialWorld>().Pour(target.Center, material, (int)Math.Clamp(shot.Number("material_amount"), 1, 255));
    }
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (!Bound || !extraExplosion || ExplosionsDisabled || Main.netMode != NetmodeID.SinglePlayer) return;
        int id = Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<NoitaBlast>(), (int)Math.Clamp(shot.Number("damage_explosion_add") * 25, 0, 100000), 2, projectile.owner);
        if (id >= 0 && id < Main.maxProjectiles) ((NoitaBlast)Main.projectile[id].ModProjectile).Configure((float)Math.Clamp(shot.Number("explosion_radius"), 1, 160), true, trace);
    }
    public static bool IsPiercing(Projectile projectile) => projectile.TryGetGlobalProjectile<SpellAugmentBinding>(out var binding) && binding.Piercing;
}
