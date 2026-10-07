#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.Projectiles;

/// <summary>Harmless visualization of an emitted entity; not an implementation of its native components.</summary>
public sealed class NoitaVisualProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagicMissile;
    public CastDiagnostics? Diagnostics => result?.Trace;
    private SpellLiveCase? result;
    private VisualEntityEvidence? evidence;
    private NoitaVisualProfile profile = new();
    private TriggerRunner? triggers;
    private Vector2 direction;
    private int age, bounces;
    private bool card, contacted;
    private string label = "ENTITY";
    private readonly HashSet<string> drawErrors = new();
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8; Projectile.damage = 0;
        Projectile.friendly = Projectile.hostile = false; Projectile.penetrate = -1;
        Projectile.tileCollide = true; Projectile.timeLeft = 60; Projectile.ignoreWater = true;
    }
    public override bool? CanDamage() => false;
    public void Configure(ProjectilePlan node, NoitaVisualProfile settings, Vector2 aim, SpellLiveCase test, VisualEntityEvidence record)
    {
        profile = settings; direction = aim; result = test; evidence = record;
        triggers = new(node.Triggers); bounces = profile.Bounces;
        Projectile.tileCollide = profile.TileCollide;
        label = Path.GetFileNameWithoutExtension(node.Entity);
    }
    public void ConfigureCard(NoitaVisualProfile settings, string text, SpellLiveCase test)
    { profile = settings; result = test; card = true; label = text; Projectile.tileCollide = false; }
    public void CancelDebugPayloads()
    {
        triggers = null; result?.Trace.Event($"Visual cleanup #{Projectile.whoAmI}; payloads suppressed");
    }
    private void Emit(ShotPlan payload)
    {
        if (result == null || Main.netMode != NetmodeID.SinglePlayer || Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
        var player = Main.player[Projectile.owner];
        result.Trace.Event($"Visual payload #{Projectile.whoAmI}, age {age}; {payload.Projectiles.Count} child roots");
        if (player.active) VisualProjectileAdapter.Emit(payload, Projectile.GetSource_FromThis(), player, Projectile.Center, direction, result);
    }
    public override void AI()
    {
        age++; triggers?.Observe(TriggerSignal.Tick, age, Emit);
        if (card) return;
        Projectile.velocity *= (float)profile.DragPerFrame;
        Projectile.velocity.Y += (float)profile.GravityPerFrame;
        Projectile.velocity = Vector2.Clamp(Projectile.velocity, new Vector2(-120), new Vector2(120));
        if (profile.Rotate && Projectile.velocity.LengthSquared() > .001f) Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, (float)profile.LightR, (float)profile.LightG, (float)profile.LightB);
        if (profile.EntityCollide && !contacted)
            foreach (var npc in Main.npc)
                if (npc.active && !npc.friendly && Projectile.Hitbox.Intersects(npc.Hitbox))
                {
                    contacted = true; result?.Trace.Event($"Visual contact with {npc.FullName}, age {age}; no damage; approximate hitbox");
                    triggers?.Observe(TriggerSignal.Impact, age, Emit);
                    if (profile.DieOnCollision) Projectile.Kill();
                    break;
                }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        result?.Trace.Event($"Visual tile impact #{Projectile.whoAmI}, age {age}");
        triggers?.Observe(TriggerSignal.Impact, age, Emit);
        if (bounces-- > 0)
        {
            if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y;
            return false;
        }
        return profile.DieOnCollision;
    }
    public override void OnKill(int timeLeft)
    {
        result?.Trace.Event($"Visual death #{Projectile.whoAmI}, age {age}");
        triggers?.Observe(TriggerSignal.Death, age, Emit);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        bool drawn = false;
        foreach (var sprite in profile.Sprites)
        {
            bool ok = ModContent.GetInstance<AdapterSystem>().Assets.Draw(sprite, Projectile.Center, Projectile.rotation, age, card ? 2 : SpellVisuals.Scale, out string error);
            drawn |= ok;
            if (!ok && drawErrors.Add(error))
            { evidence?.Gaps.Add(error); result?.Trace.Event("Sprite draw fallback: " + error); }
        }
        if (evidence != null) evidence.Appearance = drawn ? "original sprite drawn" : "entity marker";
        Vector2 center = Projectile.Center - Main.screenPosition;
        Color color = card ? Color.Cyan : Color.Orange;
        if (!drawn)
        {
            var pixel = TextureAssets.MagicPixel.Value;
            int size = card ? 10 : (int)Math.Round(10 * SpellVisuals.Scale);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)center.X - size / 2, (int)center.Y - size / 2, size, size), color);
        }
        // An outline remains visible even for transparent frames and one-frame particles.
        var p = TextureAssets.MagicPixel.Value;
        Main.spriteBatch.Draw(p, new Rectangle((int)center.X - 7, (int)center.Y - 7, 14, 1), Color.Yellow);
        Main.spriteBatch.Draw(p, new Rectangle((int)center.X - 7, (int)center.Y + 6, 14, 1), Color.Yellow);
        if (card || !drawn)
        {
            string text = card ? label : "ENTITY: " + label;
            if (text.Length > 64) text = text[..64];
            Utils.DrawBorderString(Main.spriteBatch, text, center + new Vector2(0, -24), color, .65f, .5f, 0);
        }
        return false;
    }
}
