#nullable enable
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.Projectiles;

public sealed class NoitaSpark : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagicMissile;
    private TriggerRunner? triggers;
    private Vector2 direction;
    private int age;
    private bool chainsaw;
    public void Configure(ProjectilePlan node, bool isChainsaw, Vector2 castDirection)
    {
        triggers = new TriggerRunner(node.Triggers);
        chainsaw = isChainsaw;
        direction = castDirection;
    }
    public override void SetDefaults()
    {
        Projectile.width = 6;
        Projectile.height = 6;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.tileCollide = true;
        Projectile.timeLeft = 40;
        Projectile.ignoreWater = true;
    }
    private void Emit(ShotPlan payload)
    {
        if (Main.netMode != NetmodeID.SinglePlayer || Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
        var player = Main.player[Projectile.owner];
        if (player.active) DemoProjectileAdapter.Emit(payload, Projectile.GetSource_FromThis(), player, Projectile.Center, direction);
    }
    public override void AI()
    {
        age++;
        triggers?.Observe(TriggerSignal.Tick, age, Emit);
        Lighting.AddLight(Projectile.Center, chainsaw ? .1f : .5f, .05f, .3f);
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!chainsaw) Projectile.velocity.Y += .02f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => triggers?.Observe(TriggerSignal.Impact, age, Emit);
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        triggers?.Observe(TriggerSignal.Impact, age, Emit);
        return true;
    }
    public override void OnKill(int timeLeft) => triggers?.Observe(TriggerSignal.Death, age, Emit);
}
