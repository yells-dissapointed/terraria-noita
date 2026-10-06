#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.Projectiles;

public sealed class NoitaSpark : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagicMissile;
    private TriggerRunner? triggers;
    private CastDiagnostics? trace;
    private Vector2 direction;
    private int age;
    private bool chainsaw, debugVisible;
    private Color sparkColor = Color.HotPink;
    public void Configure(ProjectilePlan node, bool isChainsaw, Vector2 castDirection, CastDiagnostics? diagnostics, bool showHitbox)
    {
        triggers = new TriggerRunner(node.Triggers); chainsaw = isChainsaw;
        direction = castDirection; trace = diagnostics; debugVisible = showHitbox;
        sparkColor = node.Entity.EndsWith("light_bullet_blue.xml") ? Color.LightBlue : Color.HotPink;
        if (chainsaw)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = 28; Projectile.Center = center;
            Projectile.penetrate = -1; Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 10;
        }
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = 1;
        Projectile.tileCollide = true; Projectile.timeLeft = 40; Projectile.ignoreWater = true;
    }
    public override bool ShouldUpdatePosition() => !chainsaw;
    private void Emit(ShotPlan payload)
    {
        if (Main.netMode != NetmodeID.SinglePlayer || Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
        var player = Main.player[Projectile.owner];
        trace?.Event($"Payload #{Projectile.whoAmI} fired at age {age}; {payload.Projectiles.Count} child projectile(s)");
        if (player.active) DemoProjectileAdapter.Emit(payload, Projectile.GetSource_FromThis(), player, Projectile.Center, direction, trace, debugVisible);
    }
    public override void AI()
    {
        age++; triggers?.Observe(TriggerSignal.Tick, age, Emit);
        Lighting.AddLight(Projectile.Center, chainsaw ? .6f : .5f, .15f, .3f);
        Projectile.rotation = chainsaw ? age * .7f : Projectile.velocity.ToRotation();
        if (!chainsaw) Projectile.velocity.Y += .02f;
        else if (age <= 8) Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0, 0, 0, default, .8f);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        trace?.Event($"Hit {target.FullName} for {damageDone} damage, age {age}");
        triggers?.Observe(TriggerSignal.Impact, age, Emit);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        trace?.Event($"Tile impact #{Projectile.whoAmI}, age {age}");
        triggers?.Observe(TriggerSignal.Impact, age, Emit); return true;
    }
    public override void OnKill(int timeLeft)
    {
        trace?.Event($"Death #{Projectile.whoAmI}, age {age}");
        triggers?.Observe(TriggerSignal.Death, age, Emit);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        var pixel = TextureAssets.MagicPixel.Value;
        Vector2 center = Projectile.Center - Main.screenPosition;
        if (chainsaw)
        {
            for (int i = 0; i < 4; i++)
                Main.EntitySpriteDraw(pixel, center, null, Color.Orange, Projectile.rotation + i * MathHelper.PiOver4,
                    new Vector2(.5f), new Vector2(28, 3), SpriteEffects.None);
            Main.EntitySpriteDraw(pixel, center, null, Color.LightYellow, 0, new Vector2(.5f), new Vector2(8), SpriteEffects.None);
        }
        else
        {
            Main.EntitySpriteDraw(pixel, center, null, sparkColor * .6f, Projectile.rotation,
                new Vector2(.5f), new Vector2(12, 8), SpriteEffects.None);
            Main.EntitySpriteDraw(pixel, center, null, Color.White, Projectile.rotation,
                new Vector2(.5f), new Vector2(6, 3), SpriteEffects.None);
        }
        if (debugVisible)
        {
            Rectangle r = Projectile.Hitbox; r.Offset(-(int)Main.screenPosition.X, -(int)Main.screenPosition.Y);
            Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 1), Color.Yellow);
            Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), Color.Yellow);
            Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, 1, r.Height), Color.Yellow);
            Main.spriteBatch.Draw(pixel, new Rectangle(r.Right - 1, r.Y, 1, r.Height), Color.Yellow);
        }
        return false;
    }
}
