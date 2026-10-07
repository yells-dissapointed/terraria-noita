#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using terrarianoita.Core;

namespace terrarianoita.Common;

/// <summary>Adds Noita payloads and diagnostics only to vanilla projectiles explicitly spawned by this mod.</summary>
public sealed class TerrariaProjectileBinding : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public bool Bound { get; private set; }
    public CastDiagnostics? Diagnostics { get; private set; }
    private TriggerRunner? triggers;
    private NoitaVisualProfile? profile;
    private SpellLiveCase? result;
    private VisualEntityEvidence? evidence;
    private Vector2 direction;
    private int age;
    private int mappedDamage;
    private bool noitaSprite, debugVisible;
    private HashSet<string> errors = new();
    public void Configure(Projectile projectile, ProjectilePlan node, Vector2 aim, CastDiagnostics? trace, SpellLiveCase? live,
        VisualEntityEvidence? record, NoitaVisualProfile settings, bool keepNoitaSprite, bool showDebug)
    {
        Bound = true; Diagnostics = trace; result = live; evidence = record; direction = aim;
        triggers = new(node.Triggers); profile = settings; noitaSprite = keepNoitaSprite; debugVisible = showDebug;
        errors = new(); age = 0;
        mappedDamage = projectile.damage;
    }
    public void RemoveForDebug(Projectile projectile)
    {
        triggers = null; Bound = false;
        Diagnostics?.Event($"Native debug cleanup #{projectile.whoAmI}; removed without Kill/explosion/payload callbacks");
        projectile.friendly = projectile.hostile = false; projectile.damage = 0;
        // Calling Kill on a vanilla Bomb would execute the real explosion and terrain code.
        // Debug scans are single-player; directly deactivate only this bound test instance.
        projectile.active = false;
    }
    private void Emit(Projectile projectile, ShotPlan payload)
    {
        if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer || projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return;
        var player = Main.player[projectile.owner];
        Diagnostics?.Event($"Native payload #{projectile.whoAmI}, age {age}: {payload.Projectiles.Count} child roots");
        if (player.active) GameplayProjectileAdapter.Emit(payload, projectile.GetSource_FromThis(), player, projectile.Center, direction, Diagnostics, debugVisible, result);
    }
    public override void PostAI(Projectile projectile)
    {
        if (!Bound) return;
        age++; triggers?.Observe(TriggerSignal.Tick, age, shot => Emit(projectile, shot));
    }
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Bound) return;
        Diagnostics?.Event($"Native hit {target.FullName} for {damageDone}, age {age}");
        triggers?.Observe(TriggerSignal.Impact, age, shot => Emit(projectile, shot));
    }
    public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
    {
        if (Bound)
        {
            Diagnostics?.Event($"Native tile impact #{projectile.whoAmI}, age {age}");
            triggers?.Observe(TriggerSignal.Impact, age, shot => Emit(projectile, shot));
        }
        return true; // Keep the actual vanilla projectile's tile collision handling.
    }
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (!Bound) return;
        Diagnostics?.Event($"Native death #{projectile.whoAmI}, age {age}; vanilla effects executed");
        triggers?.Observe(TriggerSignal.Death, age, shot => Emit(projectile, shot));
    }
    public override void PrepareBombToBlow(Projectile projectile)
    {
        if (Bound) projectile.damage = mappedDamage; // Vanilla explosive preparation may replace the spawn damage.
    }
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (!Bound) return true;
        bool drawn = false;
        if (noitaSprite && profile != null)
            foreach (var sprite in profile.Sprites)
            {
                bool ok = ModContent.GetInstance<AdapterSystem>().Assets.Draw(sprite, projectile.Center, projectile.velocity.ToRotation(), age, SpellVisuals.Scale, out string error);
                drawn |= ok;
                if (!ok && errors.Add(error)) { evidence?.Gaps.Add(error); Diagnostics?.Event("Native sprite fallback: " + error); }
            }
        if (!drawn)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            int frames = Math.Max(1, Main.projFrames[projectile.type]), height = texture.Height / frames;
            var frame = new Rectangle(0, Math.Clamp(projectile.frame, 0, frames - 1) * height, texture.Width, height);
            Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, frame, projectile.GetAlpha(lightColor), projectile.rotation,
                new Vector2(frame.Width / 2f, frame.Height / 2f), projectile.scale * SpellVisuals.Scale, SpriteEffects.None);
        }
        if (evidence != null) evidence.Appearance = drawn ? "Noita sprite with Terraria behavior" : "Terraria sprite and behavior";
        if (debugVisible)
        {
            var r = projectile.Hitbox; r.Offset(-(int)Main.screenPosition.X, -(int)Main.screenPosition.Y);
            var pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 1), Color.Yellow);
            Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), Color.Yellow);
        }
        return false;
    }
}
