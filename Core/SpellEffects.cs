#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace terrarianoita.Core;

public enum NoitaEffectKind { None, Teleport, TeleportCloser, BlackHole, Tentacle, Saw }

/// <summary>Explicit adapters for supplied definitions, never a name-based substitute for unknown entities.</summary>
public sealed class SpellEffectProfile
{
    public NoitaEffectKind Kind { get; set; }
    public NoitaVisualProfile Visual { get; set; } = new();
    public double Damage { get; set; }
    public double EatRadius { get; set; }
    public double AttractionRadius { get; set; }
    public bool LargeHole { get; set; }
    public bool Repels { get; set; }
    public bool Giga { get; set; }
    public int SawTier { get; set; }
    public int Points { get; set; } = 16;
    public double SegmentLength { get; set; } = 4;
    public List<NoitaSpriteAsset> Segments { get; set; } = new();
    public static NoitaEffectKind KindFor(string entity) => entity switch {
        "data/entities/projectiles/deck/teleport_projectile.xml" or "data/entities/projectiles/deck/teleport_projectile_short.xml" or
        "data/entities/projectiles/deck/teleport_projectile_static.xml" => NoitaEffectKind.Teleport,
        "data/entities/projectiles/deck/teleport_projectile_closer.xml" => NoitaEffectKind.TeleportCloser,
        "data/entities/projectiles/deck/black_hole.xml" or "data/entities/projectiles/deck/black_hole_big.xml" or "data/entities/projectiles/deck/black_hole_giga.xml" or "data/entities/projectiles/deck/white_hole.xml" or "data/entities/projectiles/deck/white_hole_big.xml" or "data/entities/projectiles/deck/white_hole_giga.xml" => NoitaEffectKind.BlackHole,
        "data/entities/projectiles/deck/tentacle.xml" => NoitaEffectKind.Tentacle,
        "data/entities/projectiles/deck/disc_bullet.xml" or "data/entities/projectiles/deck/disc_bullet_big.xml" or
        "data/entities/projectiles/deck/disc_bullet_bigger.xml" => NoitaEffectKind.Saw,
        _ => NoitaEffectKind.None
    };
    public static bool Supports(string entity) => KindFor(entity) != NoitaEffectKind.None;
    public static SpellEffectProfile Load(NoitaAssetCatalog catalog, string entity)
    {
        var kind = KindFor(entity);
        if (kind == NoitaEffectKind.None) throw new NotSupportedException("No effect adapter: " + entity);
        var asset = catalog.Entity(entity); var p = asset.Component("ProjectileComponent");
        var result = new SpellEffectProfile { Kind = kind, Visual = catalog.Profile(asset) };
        var types = p?.Children.LastOrDefault(c => c.Name == "damage_by_type");
        result.Damage = (p?.Number("damage") ?? 0) + (types?.Number("slice") ?? 0) + (types?.Number("melee") ?? 0) + (types?.Number("projectile") ?? 0);
        if (kind == NoitaEffectKind.BlackHole)
        {
            result.Giga = entity.EndsWith("_giga.xml", StringComparison.Ordinal);
            result.Repels = entity.Contains("/white_hole", StringComparison.Ordinal);
            result.LargeHole = entity.EndsWith("_big.xml", StringComparison.Ordinal) || result.Giga;
            result.EatRadius = Math.Clamp(asset.Component("CellEaterComponent")?.Number("radius", 12) ?? 12, 1, 64);
            result.AttractionRadius = result.Giga ? 260 : 150; // Supplied black_hole_gravity.lua distance_full.
            if (result.LargeHole) result.Damage = result.Giga ? .14 : .25; // Terraria contact damage prototype; native damage_probability is not HP.
        }
        if (kind == NoitaEffectKind.Saw)
            result.SawTier = entity.EndsWith("disc_bullet_bigger.xml", StringComparison.Ordinal) ? 2 : entity.EndsWith("disc_bullet_big.xml", StringComparison.Ordinal) ? 1 : 0;
        if (kind == NoitaEffectKind.Tentacle)
        {
            // VERLET speed is per simulation step, unlike velocity projectiles' per-second speed.
            result.Visual.SpeedPerFrame = Math.Clamp(p?.Number("speed_min", 8) ?? 8, 0, 120);
            var child = asset.Definition.Children.FirstOrDefault(c => c.Name == "Entity");
            var physics = child?.Children.FirstOrDefault(c => c.Name == "VerletPhysicsComponent");
            result.Points = (int)Math.Clamp(physics?.Number("num_points", 16) ?? 16, 2, 32);
            result.SegmentLength = Math.Clamp(physics?.Number("resting_distance", 4) ?? 4, 1, 16);
            foreach (var basis in child?.Children.Where(c => c.Name == "Base") ?? Enumerable.Empty<NoitaXmlNode>())
                foreach (var component in catalog.Entity(basis.Get("file")).Definition.Children.Where(c => c.Name == "SpriteComponent" && c.Enabled))
                { var sprite = catalog.Sprite(component.Get("image_file"), component); sprite.Rotate = true; result.Segments.Add(sprite); }
        }
        return result;
    }
    public float RadiusAt(int age) => (float)(Giga ? 60 : LargeHole ? Math.Min(64, 1 + age / 3) : EatRadius);
    public string Limits => Kind switch {
        NoitaEffectKind.Teleport => "Collision/lifetime teleport with a nearby clear landing; blocked destinations cancel. Native particle/audio effects are approximated.",
        NoitaEffectKind.TeleportCloser => "Moves a hit non-boss enemy to the launch point if it fits. Native enemy scripts are not executed.",
        NoitaEffectKind.BlackHole => "Original animation, projectile/item attraction and terrain removal; large variant also pulls/damages enemies and caster. Terraria protected/important tiles and liquids remain intact; native material/particle simulation is deferred.",
        NoitaEffectKind.Tentacle => "Imported segment sprites and constrained whip collision; XML speed/damage/lifetime. Approximate anchored chain, not Noita's Verlet/material engine.",
        _ => "XML slice damage, spin, bounces and tier-specific return paths; caster can be hit after six frames. Native cell cutting and speed-dependent damage remain deferred."
    };
}

public static class SpellLanding
{
    /// <summary>Nearest open position within 64 pixels; do not teleport into solids when no fit exists.</summary>
    public static Vector2? Find(Vector2 center, Func<Vector2, bool> fits)
    {
        if (fits(center)) return center;
        for (int radius = 8; radius <= 64; radius += 8)
            for (int i = 0; i < 16; i++)
            { float angle = -MathF.PI / 2 + i * MathF.Tau / 16; var candidate = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius; if (fits(candidate)) return candidate; }
        return null;
    }
}
