#nullable enable
using System;
using System.IO;

namespace terrarianoita.Core;

public enum TerrariaSpellPrototype { None, Bomb, Arrow, Bullet, Rocket }
public sealed record TerrariaSpellMatch(string Status, string TerrariaProjectile, TerrariaSpellPrototype Prototype, bool UseNoitaSprite, string Notes)
{
    public bool Implemented => Prototype != TerrariaSpellPrototype.None;
}

/// <summary>Explicit prototype matches, not an assertion that similarly named spells are equivalent.</summary>
public static class TerrariaSpellMatches
{
    public static TerrariaSpellMatch Find(string entity)
    {
        if (entity == "data/entities/projectiles/bomb.xml") return new("Implemented prototype", "Bomb", TerrariaSpellPrototype.Bomb, false,
            "Native timed bomb, bounce, damage and terrain explosion. XML fuse/damage and wand speed/lifetime are mapped; Noita physics/materials remain different.");
        if (entity == "data/entities/projectiles/deck/arrow.xml") return new("Implemented prototype", "WoodenArrowFriendly", TerrariaSpellPrototype.Arrow, false,
            "Native arrow flight/collision with XML speed, life and damage. Noita material penetration is deferred.");
        if (entity is "data/entities/projectiles/deck/bullet.xml" or "data/entities/projectiles/deck/bullet_heavy.xml" or "data/entities/projectiles/deck/bullet_slow.xml")
            return new("Implemented prototype", "Bullet", TerrariaSpellPrototype.Bullet, true,
                "Native bullet hit/collision pipeline; preserve original Noita bolt sprite and map speed/life/direct damage. Heavy/slow explosion effects remain deferred.");
        if (entity == "data/entities/projectiles/deck/rocket.xml") return new("Implemented prototype", "RocketI", TerrariaSpellPrototype.Rocket, true,
            "Native impact rocket and explosion; preserve original Noita sprite. Native acceleration/explosion area replace Noita physics and blast rules.");
        string name = Path.GetFileNameWithoutExtension(entity);
        if (name.Contains("black_hole", StringComparison.Ordinal) || name.Contains("tentacle", StringComparison.Ordinal) ||
            name is "chainsaw" or "light_bullet" or "light_bullet_blue" or "light_bullet_air" or "megalaser" or "megalaser_beam" ||
            name.Contains("portal", StringComparison.Ordinal) || name.StartsWith("mist_", StringComparison.Ordinal) || name.Contains("bomb_holy", StringComparison.Ordinal))
            return new("Preserve Noita", "", TerrariaSpellPrototype.None, true,
                "Distinctive Noita identity: retain original sprite/animation or particle marker and implement its own behavior. No automatic Terraria replacement.");
        if (name.StartsWith("grenade", StringComparison.Ordinal) || name is "pipe_bomb" or "glitter_bomb")
            return Candidate("Grenade", "Reuse explosive motion/rendering helpers, but Noita impact/timer/payload rules require an adapter.");
        if (name.StartsWith("rocket", StringComparison.Ordinal) || name == "all_rockets")
            return Candidate("RocketI / RocketIII", "Reuse rocket flight/explosion; tiered radius, native particles and player/self-hit rules need comparison.");
        if (name is "bomb_cart" or "tnt" or "explosive_box") return Candidate("Bomb / Dynamite", "Shared fuse/explosion family; retain cart/box identity and adapt body, damage and blast size.");
        if (name is "fireball" or "fireball_big" or "firebomb" or "flamethrower") return Candidate("BallofFire / Flames", "Reuse fire rendering/debuff helpers. Noita blasts, materials and impact rules differ.");
        if (name == "bouncy_orb") return Candidate("WaterBolt", "Reusable bouncing collision pattern; Noita artwork, damage and bounce limits must remain explicit.");
        if (name.Contains("mine", StringComparison.Ordinal)) return Candidate("ProximityMineI", "Reusable mine arming/targeting; Noita trigger distance, detonation and lifespan need adaptation.");
        if (name.StartsWith("disc_bullet", StringComparison.Ordinal) || name == "all_discs") return Candidate("LightDisc / DeathSickle", "Spinning sprite/damage patterns can be reused; Terraria return/homing rules do not match Noita sawblades.");
        if (name is "laser" or "orb_laseremitter" or "orb_laseremitter_cutter" or "orb_laseremitter_four") return Candidate("PurpleLaser / LaserMachinegunLaser", "Reuse laser line/collision/rendering helpers; retain Noita colors, emitter arrangement and beam rules.");
        if (name is "lightning" or "ball_lightning") return Candidate("CultistBossLightningOrbArc", "Reusable segmented lightning logic; native hostility/targeting differs and Noita appearance should be preserved.");
        if (name.StartsWith("spitter", StringComparison.Ordinal)) return Candidate("Bullet", "Short-range bolt rendering/collision can be reused; native particles and burst/decay rules need custom work.");
        if (name == "heal_bullet") return Candidate("Healing helpers", "Reuse Terraria health APIs; targeting/negative damage require a custom healing projectile.");
        return new("No automatic match", "", TerrariaSpellPrototype.None, true, "Keep imported Noita visual data. No reliable reusable projectile match established.");
    }
    private static TerrariaSpellMatch Candidate(string vanilla, string notes) => new("Candidate only", vanilla, TerrariaSpellPrototype.None, true, notes);
}
