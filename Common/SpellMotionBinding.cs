#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using terrarianoita.Core;
using terrarianoita.Content.Projectiles;

namespace terrarianoita.Common;

/// <summary>Opt-in movement only for projectiles spawned by this mod. Unrelated Terraria ammunition is untouched.</summary>
public sealed class SpellMotionBinding : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public bool Bound { get; private set; }
    private SpellMotion? motion;
    private CastDiagnostics? trace;
    private int age, targetId = -1, bounces;
    public void Configure(ShotPlan shot, CastDiagnostics? diagnostics, VisualEntityEvidence? evidence = null)
    {
        motion = SpellMotion.Load(shot, ModContent.GetInstance<AdapterSystem>().Assets.Catalog);
        Bound = true; age = 0; trace = diagnostics; bounces = motion.Bounces;
        var augments=SpellAugments.Load(shot,ModContent.GetInstance<AdapterSystem>().Assets.Catalog);
        motion.Gaps.RemoveAll(g=>augments.Sources.Exists(path=>g=="Deferred extra entity: "+path));
        evidence?.Gaps.AddRange(motion.Gaps);
        if (evidence != null)
        {
            evidence.MovementSources.AddRange(motion.Sources);
            var catalog = ModContent.GetInstance<AdapterSystem>().Assets.Catalog;
            foreach (string path in motion.Sources) evidence.ImportedSources[path] = catalog.Entity(path).SourceDefinition;
        }
        if (motion.Sources.Count > 0 || bounces > 0)
            trace?.Event($"Movement bound: {string.Join(", ", motion.Sources)}; added bounces {bounces}. Bounded Terraria trajectory adapters; native component math differs.");
        foreach (string gap in motion.Gaps) trace?.Event(gap);
    }
    public override void PostAI(Projectile projectile)
    {
        if (!Bound || motion == null || !projectile.active || motion.Sources.Count == 0) return;
        age++;
        if (motion.AreaTeleport)
            foreach(var npc in Main.npc) if(npc.active && npc.CanBeChasedBy(projectile) && Vector2.DistanceSquared(npc.Center,projectile.Center)<32*32) {projectile.Center=npc.Center;break;}
        System.Numerics.Vector2? aim = null;
        if (motion.HomingRange > 0)
        {
            NPC? target = null; float distance = (float)motion.HomingRange * (float)motion.HomingRange;
            foreach (var npc in Main.npc)
                if (npc.active && npc.CanBeChasedBy(projectile))
                {
                    float next = Vector2.DistanceSquared(projectile.Center, npc.Center);
                    if (next < distance && Collision.CanHitLine(projectile.Center, 1, 1, npc.position, npc.width, npc.height))
                    { target = npc; distance = next; }
                }
            int id = target?.whoAmI ?? -1;
            if (id != targetId) { targetId = id; trace?.Event("Homing target: " + (target?.FullName ?? "none")); }
            if (target != null) { var delta = target.Center - projectile.Center; aim = new(delta.X, delta.Y); }
        }
        if (motion.Target == "cursor" && projectile.owner == Main.myPlayer) { var delta = Main.MouseWorld - projectile.Center; aim = new(delta.X, delta.Y); }
        if (motion.Target == "shooter" && projectile.owner >= 0 && projectile.owner < Main.maxPlayers) { var delta = Main.player[projectile.owner].Center - projectile.Center; aim = new(delta.X, delta.Y); }
        if (motion.Target == "projectile")
        {
            float distance = 900 * 900; aim = null;
            foreach (var other in Main.projectile) if (other.active && other.hostile && Vector2.DistanceSquared(other.Center, projectile.Center) < distance)
            { var delta = other.Center - projectile.Center; distance = delta.LengthSquared(); aim = new(delta.X, delta.Y); }
        }
        var velocity = motion.Step(new(projectile.velocity.X, projectile.velocity.Y), age, aim);
        projectile.velocity = new(velocity.X, velocity.Y);
        if (projectile.ModProjectile is NoitaVisualProjectile or NoitaSpark or NoitaComponentProjectile && projectile.velocity.LengthSquared() > .001f)
            projectile.rotation = projectile.velocity.ToRotation();
    }
    public static bool TryBounce(Projectile projectile, Vector2 oldVelocity)
    {
        if (!projectile.TryGetGlobalProjectile<SpellMotionBinding>(out var binding) || !binding.Bound || binding.bounces <= 0) return false;
        binding.bounces--;
        if (projectile.velocity.X != oldVelocity.X) projectile.velocity.X = -oldVelocity.X;
        if (projectile.velocity.Y != oldVelocity.Y) projectile.velocity.Y = -oldVelocity.Y;
        binding.trace?.Event($"Modifier bounce #{projectile.whoAmI}; {binding.bounces} remaining");
        return true;
    }
}
