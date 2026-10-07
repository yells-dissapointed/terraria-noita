#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.Projectiles;

public sealed class NoitaEffectProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagicMissile;
    public CastDiagnostics? Diagnostics { get; private set; }
    private SpellEffectProfile settings = new();
    private SpellLiveCase? live;
    private VisualEntityEvidence? evidence;
    private TriggerRunner? triggers;
    private Vector2 origin, direction;
    private Vector2[] points = Array.Empty<Vector2>();
    private int age, bounces, initialLife, tilesEaten;
    private float launchSpeed;
    private bool cancelled, debugVisible;
    private readonly HashSet<string> drawErrors = new();
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.friendly = true; Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = -1; Projectile.timeLeft = 60;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 10;
    }
    public void Configure(ProjectilePlan node, SpellEffectProfile profile, Vector2 aim, CastDiagnostics? trace, SpellLiveCase? report,
        VisualEntityEvidence? record, bool showDebug)
    {
        settings = profile; direction = aim; origin = Projectile.Center; Diagnostics = trace; live = report; evidence = record;
        triggers = new(node.Triggers); debugVisible = showDebug; bounces = settings.Visual.Bounces;
        launchSpeed = Projectile.velocity.Length(); initialLife = Projectile.timeLeft;
        Projectile.friendly = settings.Kind is NoitaEffectKind.Saw or NoitaEffectKind.Tentacle || settings.LargeHole;
        Projectile.tileCollide = settings.Kind is NoitaEffectKind.Saw or NoitaEffectKind.Teleport or NoitaEffectKind.TeleportCloser && settings.Visual.TileCollide;
        if (settings.Kind == NoitaEffectKind.Saw)
        {
            int size = (int)Math.Clamp((settings.Visual.Sprites.Count > 0 ? settings.Visual.Sprites[0].FrameWidth : 12) * SpellVisuals.Scale, 8, 128);
            Projectile.width = Projectile.height = size; Projectile.Center = origin;
        }
        if (settings.Kind == NoitaEffectKind.Tentacle)
        { points = new Vector2[settings.Points]; Array.Fill(points, origin); }
    }
    public void RemoveForDebug()
    {
        cancelled = true; triggers = null; Projectile.damage = 0; Projectile.friendly = false; Projectile.active = false;
        Diagnostics?.Event($"Effect cleanup #{Projectile.whoAmI}: no teleport, terrain removal or payload callback");
    }
    public override bool? CanDamage() => settings.Kind is NoitaEffectKind.Teleport or NoitaEffectKind.TeleportCloser || settings.Kind == NoitaEffectKind.BlackHole && !settings.LargeHole ? false : null;
    public override bool ShouldUpdatePosition() => settings.Kind != NoitaEffectKind.Tentacle;
    private Player? Owner => Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers && Main.player[Projectile.owner].active && !Main.player[Projectile.owner].dead ? Main.player[Projectile.owner] : null;
    private Vector2 Heading => Projectile.velocity.LengthSquared() > .001f ? Vector2.Normalize(Projectile.velocity) : direction;
    private void Emit(ShotPlan shot)
    {
        if (cancelled || Main.netMode != NetmodeID.SinglePlayer || Owner is not { } player) return;
        Diagnostics?.Event($"Effect payload #{Projectile.whoAmI}, age {age}");
        GameplayProjectileAdapter.Emit(shot, Projectile.GetSource_FromThis(), player, Projectile.Center, Heading, Diagnostics, debugVisible, live);
    }
    public override void AI()
    {
        if (Main.netMode != NetmodeID.SinglePlayer || Owner == null) { RemoveForDebug(); return; }
        age++;
        triggers?.Observe(TriggerSignal.Tick, age, Emit);
        switch (settings.Kind)
        {
            case NoitaEffectKind.BlackHole: BlackHole(); break;
            case NoitaEffectKind.Tentacle: Tentacle(); break;
            case NoitaEffectKind.Saw:
                if (settings.SawTier == 1)
                    Projectile.velocity += direction * Math.Clamp((launchSpeed - MathF.Floor(age / 3f)) * .1f, -.34f, .42f);
                else if (settings.SawTier == 2 && Owner is { } owner)
                    Projectile.velocity += (owner.Center - Projectile.Center) * (.33f / 60);
                Projectile.velocity *= (float)settings.Visual.DragPerFrame;
                Projectile.velocity.Y += (float)settings.Visual.GravityPerFrame;
                Projectile.rotation += .35f;
                HurtOwner(6); break;
            case NoitaEffectKind.Teleport:
            case NoitaEffectKind.TeleportCloser:
                Projectile.velocity *= (float)settings.Visual.DragPerFrame; Projectile.velocity.Y += (float)settings.Visual.GravityPerFrame;
                Dust.NewDustPerfect(Projectile.Center, DustID.MagicMirror, Vector2.Zero, 100, Color.LightBlue, .8f).noGravity = true;
                foreach (var npc in Main.npc)
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage && Projectile.Hitbox.Intersects(npc.Hitbox))
                    {
                        if (settings.Kind == NoitaEffectKind.TeleportCloser) MoveEnemy(npc);
                        triggers?.Observe(TriggerSignal.Impact, age, Emit); Projectile.Kill(); break;
                    }
                break;
        }
        Projectile.velocity = Vector2.Clamp(Projectile.velocity, new(-120), new(120));
    }
    private void BlackHole()
    {
        float radius = settings.RadiusAt(age);
        Projectile.velocity *= (float)settings.Visual.DragPerFrame;
        if (age % 3 == 0)
        {
            int removed = SpellTerrain.Eat(Projectile, radius); tilesEaten += removed;
            if (removed > 0) Diagnostics?.Event($"Black hole removed {removed} terrain tiles; radius {radius:0}");
        }
        float reach = (float)settings.AttractionRadius;
        Vector2 Pull(Vector2 center)
        {
            var delta = Projectile.Center - center; float distance = delta.Length();
            return distance > 1 && distance < reach ? delta / distance * (196f / 60) * (1 - distance / reach) : Vector2.Zero;
        }
        foreach (var other in Main.projectile)
            if (other.active && other.whoAmI != Projectile.whoAmI && other.ModProjectile is not NoitaEffectProjectile { settings.Kind: NoitaEffectKind.BlackHole })
            { other.velocity += Pull(other.Center); other.velocity = Vector2.Clamp(other.velocity, new(-120), new(120)); }
        foreach (var item in Main.item) if (item.active) item.velocity += Pull(item.Center) * .2f;
        if (settings.LargeHole)
        {
            foreach (var npc in Main.npc) if (npc.active && !npc.friendly && !npc.boss && !npc.dontTakeDamage) npc.velocity += Pull(npc.Center) * .2f;
            if (Owner is { } player) player.velocity += Pull(player.Center) * .2f;
            HurtOwner(10);
        }
        Lighting.AddLight(Projectile.Center, .2f, .05f, .3f);
    }
    private void Tentacle()
    {
        if (points.Length < 2) return;
        float spacing = (float)settings.SegmentLength * SpellVisuals.Scale;
        // Anchored chain: tip extends, curls and retracts; pair constraints bound every segment.
        float extension = Math.Min(1, age * launchSpeed / Math.Max(1, spacing * (points.Length - 1)));
        float fade = Math.Min(1, Projectile.timeLeft / 20f);
        var heading = Heading; var normal = new Vector2(-heading.Y, heading.X);
        var tip = origin + heading * (spacing * (points.Length - 1) * extension * fade) + normal * (MathF.Sin(age * .16f) * 14 * extension * fade);
        Vector2 travel = tip - points[^1];
        tip = points[^1] + Collision.TileCollision(points[^1] - new Vector2(3), travel, 6, 6);
        for (int i = 1; i < points.Length - 1; i++) points[i] += normal * (MathF.Sin(age * .13f - i * .4f) * .5f);
        points[0] = origin; points[^1] = tip;
        for (int pass = 0; pass < 12; pass++)
        {
            for (int i = points.Length - 2; i >= 0; i--)
            {
                Vector2 delta = points[i + 1] - points[i]; float distance = delta.Length();
                if (distance < .001f) continue;
                Vector2 correction = delta * ((distance - spacing) / distance);
                if (i > 0) points[i] += correction * .5f;
                if (i + 1 < points.Length - 1) points[i + 1] -= correction * .5f;
            }
            points[0] = origin; points[^1] = tip;
        }
        Projectile.Center = tip;
    }
    public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
    {
        if (settings.Kind == NoitaEffectKind.Tentacle)
        {
            for (int i = 1; i < points.Length; i++)
            { float contact = 0; if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), points[i - 1], points[i], 8, ref contact)) return true; }
            return false;
        }
        if (settings.Kind == NoitaEffectKind.BlackHole)
            return settings.LargeHole && Vector2.DistanceSquared(Projectile.Center, Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight())) < MathF.Pow(settings.RadiusAt(age), 2);
        if (settings.Kind == NoitaEffectKind.Saw)
            return Vector2.DistanceSquared(Projectile.Center, Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight())) <= MathF.Pow(Projectile.width / 2f, 2);
        return null;
    }
    private void HurtOwner(int grace)
    {
        if (age <= grace || Projectile.damage <= 0 || Owner is not { } player || player.immune) return;
        bool contact = Colliding(Projectile.Hitbox, player.Hitbox) == true;
        if (!contact) return;
        player.Hurt(PlayerDeathReason.ByProjectile(player.whoAmI, Projectile.whoAmI), Projectile.damage, player.Center.X < Projectile.Center.X ? -1 : 1);
        Diagnostics?.Event($"Effect contacted caster at age {age}");
    }
    private bool Fits(Vector2 center, int width, int height)
    {
        Vector2 position = center - new Vector2(width, height) / 2;
        return position.X >= 16 && position.Y >= 16 && position.X + width <= Main.maxTilesX * 16 - 16 && position.Y + height <= Main.maxTilesY * 16 - 16 && !Collision.SolidCollision(position, width, height);
    }
    private Vector2? Landing(Vector2 center, int width, int height)
    {
        var found = SpellLanding.Find(new(center.X, center.Y), p => Fits(new(p.X, p.Y), width, height));
        return found.HasValue ? new Vector2(found.Value.X, found.Value.Y) : null;
    }
    private void MoveEnemy(NPC npc)
    {
        if (npc.boss) { Diagnostics?.Event("Teleport closer: boss excluded from relocation"); return; }
        var landing = Landing(origin, npc.width, npc.height);
        if (landing == null) { Diagnostics?.Event("Teleport closer cancelled: no clear enemy landing"); return; }
        npc.Center = landing.Value; npc.velocity = Vector2.Zero; npc.netUpdate = true;
        Diagnostics?.Event("Teleport closer moved " + npc.FullName + " to launch point");
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    { Diagnostics?.Event($"{settings.Kind} hit {target.FullName} for {damageDone}, age {age}"); triggers?.Observe(TriggerSignal.Impact, age, Emit); }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Diagnostics?.Event($"{settings.Kind} tile impact, age {age}"); triggers?.Observe(TriggerSignal.Impact, age, Emit);
        if (SpellMotionBinding.TryBounce(Projectile, oldVelocity)) return false;
        if (settings.Kind == NoitaEffectKind.Saw && bounces-- > 0)
        {
            if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y;
            return false;
        }
        return true;
    }
    public override void OnKill(int timeLeft)
    {
        if (cancelled) return;
        if (settings.Kind == NoitaEffectKind.Teleport && Owner is { } player)
        {
            var landing = Landing(Projectile.Center, player.width, player.height);
            if (landing != null)
            { player.Teleport(landing.Value - player.Size / 2, 1); player.velocity = Vector2.Zero; player.fallStart = (int)(player.position.Y / 16); Diagnostics?.Event($"Teleport landed at {landing.Value}"); }
            else Diagnostics?.Event("Teleport cancelled: no clear landing within 64 pixels");
        }
        Diagnostics?.Event($"{settings.Kind} ended at age {age}; terrain removed {tilesEaten}");
        triggers?.Observe(TriggerSignal.Death, age, Emit);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        var cache = ModContent.GetInstance<AdapterSystem>().Assets; bool drawn = false;
        bool Draw(NoitaSpriteAsset sprite, Vector2 position, float rotation)
        {
            bool ok = cache.Draw(sprite, position, rotation, age, SpellVisuals.Scale, out string error);
            if (!ok && drawErrors.Add(error)) { evidence?.Gaps.Add(error); Diagnostics?.Event("Effect sprite fallback: " + error); }
            return ok;
        }
        if (settings.Kind == NoitaEffectKind.Tentacle)
            for (int i = 1; i < points.Length; i++)
            {
                var delta = points[i] - points[i - 1];
                if (settings.Segments.Count > 0) drawn |= Draw(settings.Segments[Math.Min(i - 1, settings.Segments.Count - 1)], points[i - 1], delta.ToRotation());
                else Line(points[i - 1], points[i], Color.Purple, 6);
            }
        else foreach (var sprite in settings.Visual.Sprites) drawn |= Draw(sprite, Projectile.Center, settings.Kind == NoitaEffectKind.Saw ? Projectile.rotation : Heading.ToRotation());
        if (!drawn && settings.Kind != NoitaEffectKind.Tentacle)
        {
            Color color = settings.Kind is NoitaEffectKind.Teleport or NoitaEffectKind.TeleportCloser ? Color.LightBlue : Color.Purple;
            Line(Projectile.Center - new Vector2(6, 0), Projectile.Center + new Vector2(6, 0), color, 6);
        }
        if (settings.Kind == NoitaEffectKind.BlackHole && settings.LargeHole)
        {
            float radius = settings.RadiusAt(age);
            for (int i = 0; i < 24; i++)
            { float a = i * MathHelper.TwoPi / 24; float b = (i + 1) * MathHelper.TwoPi / 24; Line(Projectile.Center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius, Projectile.Center + new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius, Color.MediumPurple * .5f, 1); }
        }
        if (evidence != null) evidence.Appearance = drawn ? "original Noita sprite with custom " + settings.Kind + " gameplay" : "custom " + settings.Kind + " effect fallback";
        if (debugVisible)
        {
            var r = Projectile.Hitbox; Line(r.TopLeft(), r.TopRight(), Color.Yellow, 1); Line(r.BottomLeft(), r.BottomRight(), Color.Yellow, 1);
        }
        return false;
    }
    private static void Line(Vector2 start, Vector2 end, Color color, float width)
    {
        var pixel = TextureAssets.MagicPixel.Value; var delta = end - start;
        var quad = PixelQuad.Fit(pixel.Width, pixel.Height, Math.Max(1, delta.Length()), width);
        Main.EntitySpriteDraw(pixel, (start + end) / 2 - Main.screenPosition, pixel.Bounds, color, delta.ToRotation(),
            new Vector2(quad.OriginX, quad.OriginY), new Vector2(quad.ScaleX, quad.ScaleY), SpriteEffects.None);
    }
}
