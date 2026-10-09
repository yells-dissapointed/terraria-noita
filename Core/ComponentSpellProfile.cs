#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace terrarianoita.Core;

public sealed record MaterialEmission(string Material, int Interval, int Amount, double Radius, string Shape);
public sealed record BeamProfile(double Angle, double Length, double Width, double Damage, bool Cuts);
public sealed record MaterialConversion(string From, string To, double Radius, bool Any, bool Entities);

/// <summary>Executable subset of imported component data. Every unhandled component/script remains visible in the coverage report.</summary>
public sealed class ComponentSpellProfile
{
    public string Entity { get; init; } = "";
    public string Name => Path.GetFileNameWithoutExtension(Entity);
    public NoitaVisualProfile Visual { get; init; } = new();
    public double Damage, Healing, BlastDamage, BlastRadius, DigRadius, AreaRadius, AreaDamage, HomingRange, HomingTurn;
    public bool ExplodeOnDeath, ExplodeOnExpiry, BlastHurtsCaster, BlastCuts, Penetrates, FriendlyFire, Lightning;
    public int HitInterval = 10, ShooterGrace = 10, ChargeFrames;
    public string BlastMaterial = "", DeathMaterial = "", DeathEntity = "";
    public List<string> Statuses { get; } = new();
    public List<MaterialEmission> Emissions { get; } = new();
    public List<BeamProfile> Beams { get; } = new();
    public List<MaterialConversion> Conversions { get; } = new();
    public List<string> Features { get; } = new();
    public List<string> Gaps { get; } = new();
    public bool Supported => Features.Count > 0;
    public static IEnumerable<NoitaXmlNode> Walk(NoitaXmlNode root)
    { foreach (var child in root.Children) { yield return child; if (child.Name == "Entity") foreach (var nested in Walk(child)) yield return nested; } }
    public static ComponentSpellProfile Load(NoitaAssetCatalog catalog, string path)
    {
        var entity = catalog.Entity(path);
        bool chargedBeam = path == "data/entities/projectiles/deck/megalaser_beam.xml";
        if (chargedBeam)
        {
            entity = new NoitaEntityAsset { Path = path, SourceDefinition = entity.SourceDefinition, Definition = entity.Definition.Copy(), Sources = entity.Sources, Warnings = entity.Warnings };
            foreach (var component in entity.Definition.Children) component.Attributes["_enabled"] = "1";
        }
        var result = new ComponentSpellProfile { Entity = path, Visual = catalog.Profile(entity) };
        if (chargedBeam) { result.ChargeFrames = 30; result.Features.Add("Charged accelerating beam"); }
        var p = entity.Component("ProjectileComponent");
        var components = Walk(entity.Definition).Where(n => n.Enabled).ToArray();
        var lifetime = entity.Component("LifetimeComponent");
        if (lifetime != null) result.Visual.Lifetime = Math.Min(result.Visual.Lifetime, (int)Math.Clamp(lifetime.Number("lifetime", 3600), 1, 3600));
        if (p != null)
        {
            result.Features.Add("XML projectile motion, collision, lifetime and trigger payloads");
            var types = p.Children.LastOrDefault(n => n.Name == "damage_by_type");
            result.Damage = p.Number("damage") + (types?.Attributes.Where(a => a.Key != "healing").Sum(a => types.Number(a.Key)) ?? 0);
            result.Healing = Math.Max(0, -(types?.Number("healing") ?? 0));
            result.Penetrates = p.Flag("penetrate_entities"); result.FriendlyFire = p.Flag("friendly_fire");
            result.ShooterGrace = (int)Math.Clamp(p.Number("collide_with_shooter_frames", 10), 0, 600);
            result.HitInterval = (int)Math.Clamp(p.Number("damage_every_x_frames", 10), 1, 120);
            if (p.Flag("penetrate_world")) result.Visual.TileCollide = false;
            if (p.Number("ground_penetration_coeff") > 0) result.DigRadius = 5;
            result.Statuses.AddRange(p.Get("damage_game_effect_entities").Split(',', StringSplitOptions.RemoveEmptyEntries));
            if ((types?.Number("fire") ?? 0) > 0) result.Statuses.Add("fire");
            if ((types?.Number("electricity") ?? 0) > 0) result.Statuses.Add("electricity");
            if ((types?.Number("poison") ?? 0) > 0) result.Statuses.Add("poison");
            if (p.Flag("on_death_emit_particle")) result.DeathMaterial = p.Get("on_death_emit_particle_type");
            if (p.Flag("on_death_explode") || p.Flag("on_lifetime_out_explode"))
            {
                var blast = p.Children.LastOrDefault(n => n.Name == "config_explosion");
                if (blast != null)
                {
                    result.BlastRadius = Math.Clamp(blast.Number("explosion_radius"), 0, 160);
                    result.BlastDamage = blast.Flag("damage_mortals", true) ? blast.Number("damage", 1) : 0;
                    result.BlastCuts = blast.Flag("hole_enabled"); result.BlastHurtsCaster = !p.Flag("explosion_dont_damage_shooter");
                    result.ExplodeOnDeath = p.Flag("on_death_explode"); result.ExplodeOnExpiry = p.Flag("on_lifetime_out_explode");
                    if (blast.Number("create_cell_probability") > 0) result.BlastMaterial = blast.Get("create_cell_material");
                    if (result.BlastRadius > 0 && (result.BlastDamage > 0 || result.BlastCuts)) result.Features.Add("Explosion damage and bounded terrain cutting");
                }
            }
        }
        else { result.Visual.SpeedPerFrame = 0; result.Visual.TileCollide = false; result.Visual.Lifetime = (int)Math.Clamp(lifetime?.Number("lifetime", 60) ?? 60, 1, 3600); }
        var physics = entity.Component("PhysicsBodyComponent") ?? entity.Component("PhysicsBody2Component");
        if (physics != null)
        { result.Visual.GravityPerFrame = .18; result.Visual.SpeedPerFrame = 6; result.Visual.TileCollide = true; result.Features.Add("Throwable Terraria body"); result.Gaps.Add("Rigid body shapes, rotation and Noita physics joints are approximated"); }
        foreach (var effect in components.Where(n => n.Name == "ExplodeOnDamageComponent" || n.Name == "ExplosionComponent"))
        {
            var blast = effect.Children.FirstOrDefault(n => n.Name == "config_explosion"); if (blast == null) continue;
            result.BlastRadius = Math.Clamp(blast.Number("explosion_radius", 24), 1, 160); result.BlastDamage = blast.Number("damage", 1);
            result.BlastCuts = blast.Flag("hole_enabled"); result.BlastHurtsCaster = true; result.ExplodeOnDeath = result.ExplodeOnExpiry = true;
            result.Features.Add("Impact/expiry detonation");
        }
        var eater = entity.Component("CellEaterComponent");
        if (eater != null) result.DigRadius = Math.Clamp(eater.Number("radius", 5), 1, 64);
        if (result.DigRadius > 0) { result.Visual.TileCollide = false; result.Features.Add("Protected-tile-aware digging"); }
        var area = entity.Component("GameAreaEffectComponent"); var damageArea = entity.Component("AreaDamageComponent");
        result.AreaRadius = area?.Number("radius") ?? 0;
        if (damageArea != null)
        { result.AreaRadius = Math.Max(result.AreaRadius, damageArea.Number("aabb_max.x", 24)); result.AreaDamage = damageArea.Number("damage_per_frame"); result.Features.Add("Area damage"); }
        if (result.AreaRadius > 0 && result.Statuses.Count > 0) result.Features.Add("Area status effects");
        if (result.Healing > 0) result.Features.Add("Healing contact");
        var homing = entity.Component("HomingComponent");
        if (homing != null) { result.HomingRange = homing.Number("detect_distance", 240); result.HomingTurn = Math.Clamp(homing.Number("homing_targeting_coeff", 6) / 60, .01, .3); result.Features.Add("Intrinsic homing"); }
        foreach (var beam in components.Where(n => n.Name == "LaserEmitterComponent"))
        {
            var settings = beam.Children.FirstOrDefault(n => n.Name == "laser"); if (settings == null) continue;
            result.Beams.Add(new(beam.Number("laser_angle_add_rad"), Math.Clamp(settings.Number("max_length", 160), 1, 640), Math.Clamp(settings.Number("beam_radius", 2), 1, 12), settings.Number("damage_to_entities", .1), settings.Number("damage_to_cells") > 0));
        }
        if (result.Beams.Count > 0) result.Features.Add("Raycast plasma beams");
        result.Lightning = entity.Component("LightningComponent") != null;
        if (result.Lightning) { result.Beams.Add(new(0, 320, 5, 1, true)); result.Visual.Lifetime = 6; result.Features.Add("Raycast lightning strike"); }
        foreach (var emitter in components.Where(n => n.Name == "ParticleEmitterComponent" && n.Flag("is_emitting", true) && (n.Flag("create_real_particles") || n.Flag("emit_real_particles"))))
        {
            string material = emitter.Get("emitted_material_name"); if (material.Length == 0 || material.StartsWith("spark") || material.StartsWith("plasma")) continue;
            string shape = result.Name.StartsWith("cloud_") ? "rain" : result.Name.StartsWith("circle_") ? "circle" : "point";
            result.Emissions.Add(new(material, (int)Math.Clamp(emitter.Number("emission_interval_min_frames", 6), 3, 60), (int)Math.Clamp(emitter.Number("count_max", 1) * 8, 8, 128), Math.Clamp(emitter.Number("area_circle_radius.max", shape == "circle" ? 80 : 28), 0, 128), shape));
        }
        foreach (var sea in components.Where(n => n.Name == "MaterialSeaSpawnerComponent"))
        { result.Emissions.Add(new(sea.Get("material", "water"), 1, 255, Math.Clamp(sea.Number("size.x", 300) / 2, 16, 160), "sea")); result.Features.Add("Bounded sea fill"); }
        if (result.Emissions.Count > 0 || result.DeathMaterial.Length > 0) result.Features.Add("Persistent material emission");
        foreach (var conversion in components.Where(n => n.Name == "MagicConvertMaterialComponent"))
        {
            string target = conversion.Get("to_material", conversion.Get("to_material_array").Split(',')[0]);
            if (target.Length == 0) continue;
            result.Conversions.Add(new(conversion.Get("from_material", conversion.Get("from_material_array")), target, Math.Clamp(conversion.Number("radius", 24), 1, 120), conversion.Flag("from_any_material"), conversion.Flag("convert_entities")));
        }
        if (result.Conversions.Count > 0) { result.Features.Add("Material conversion"); result.Gaps.Add("Conversion uses whole Terraria tiles/cells; material arrays use first output; entity conversion excludes bosses and town NPCs"); }
        if (result.Name.StartsWith("vacuum_")) result.Features.Add("Vacuum field");
        if (result.Name.StartsWith("projectile_") && result.Name.EndsWith("_field")) { result.AreaRadius = Math.Max(result.AreaRadius, 64); result.Features.Add("Projectile field"); }
        if (result.Name is "swapper" or "teleport_projectile_closer") result.Features.Add("Position swap on contact");
        if (result.Name.StartsWith("cloud_")) result.Gaps.Add("Cloud placement and rain shape use a Terraria adapter");
        if (result.Name.StartsWith("egg_")) { result.Features.Add("Egg hatches a Terraria creature"); result.Gaps.Add("Egg creatures use Terraria slime variants instead of imported Noita creature AI"); }
        if (result.Name is "all_acid" or "all_blackholes" or "all_deathcrosses" or "all_discs" or "all_nukes" or "all_rockets")
        { result.Features.Add("Converts up to 128 existing projectiles"); result.Gaps.Add("All-projectile conversions are capped at 128 replacements per cast; replaced projectile payloads are cancelled"); }
        if (result.Name is "destruction" or "mass_polymorph") { result.Features.Add("Nearby enemy transformation/destruction"); result.Visual.Lifetime = 2; }
        if (result.Name == "tentacle_portal") result.Features.Add("Periodic tentacle spawning");
        if (result.Name == "worm_rain") { result.Features.Add("Imported worm bodies, pursuit and terrain eating"); result.Gaps.Add("Worm steering, contact damage and lifespan are Terraria adaptations; body segments are visual"); }
        var handled = new HashSet<string> { "ProjectileComponent", "VelocityComponent", "SpriteComponent", "LifetimeComponent", "LightComponent", "HitboxComponent", "VariableStorageComponent", "AudioComponent", "AudioLoopComponent", "MusicEnergyAffectorComponent", "InheritTransformComponent", "Entity", "ParticleEmitterComponent", "SpriteParticleEmitterComponent", "HomingComponent", "LaserEmitterComponent", "LightningComponent", "MaterialSeaSpawnerComponent", "MagicConvertMaterialComponent", "GameAreaEffectComponent", "AreaDamageComponent", "CellEaterComponent", "PhysicsBodyComponent", "PhysicsBody2Component", "PhysicsImageShapeComponent", "PhysicsThrowableComponent", "ExplodeOnDamageComponent", "ExplosionComponent" };
        foreach (var component in components.Where(c => c.Name.EndsWith("Component") && !handled.Contains(c.Name)).Select(c => c.Name).Distinct())
            result.Gaps.Add("Deferred component: " + component);
        foreach (var script in components.Where(c => c.Name == "LuaComponent"))
            foreach (var attr in script.Attributes.Where(a => a.Key.StartsWith("script_") && a.Value.Length > 0)) result.Gaps.Add("Original entity Lua not executed: " + attr.Value);
        result.Gaps.Add("25 Terraria HP per Noita damage unit; tile-scale materials, Terraria hit immunity, bounded emission and blast radii; no native pixel chemistry");
        return result;
    }
}
