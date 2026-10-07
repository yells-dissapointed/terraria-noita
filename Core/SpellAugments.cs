#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace terrarianoita.Core;

public sealed class SpellAugments
{
    public bool Piercing, Phasing, Floating, NullDamage, ExplosionsDisabled, LifetimeExtended, Shield, Fire, Electric, Freeze;
    public float DigRadius, AreaRadius, AreaDamage;
    public List<string> Statuses { get; } = new();
    public List<string> Trails { get; } = new();
    public List<MaterialConversion> Conversions { get; } = new();
    public List<string> Sources { get; } = new();
    public List<string> Gaps { get; } = new();
    public static SpellAugments Load(ShotPlan shot, NoitaAssetCatalog catalog)
    {
        var result = new SpellAugments { NullDamage = shot.Number("damage_null_all") > 0, Electric = shot.Number("damage_electricity_add") > 0, Freeze = shot.Number("damage_ice_add") > 0 };
        result.Statuses.AddRange(shot.Text("game_effect_entities").Split(',', StringSplitOptions.RemoveEmptyEntries));
        result.Trails.AddRange(shot.Text("trail_material").Split(',', StringSplitOptions.RemoveEmptyEntries));
        foreach (string path in shot.Text("extra_entities").Split(',', StringSplitOptions.RemoveEmptyEntries).Distinct())
        {
            string name = Path.GetFileNameWithoutExtension(path); bool known = false;
            switch (name)
            {
                case "piercing_shot": result.Piercing = known = true; break;
                case "phasing_arc": case "clipping_shot": result.Phasing = known = true; break;
                case "floating_arc": result.Floating = known = true; break;
                case "zero_damage": result.NullDamage = known = true; break;
                case "explosion_remove": result.ExplosionsDisabled = known = true; break;
                case "lifetime_infinite": result.LifetimeExtended = known = true; break;
                case "energy_shield_shot": result.Shield = known = true; break;
                case "burn": result.Fire = known = true; break;
                case "electricity": result.Electric = known = true; break;
                case "freeze_charge": result.Freeze = known = true; break;
                case "nolla": known = true; break;
            }
            try
            {
                var entity = catalog.Entity(path);
                if (entity.Component("CellEaterComponent") is { } eater) { result.DigRadius = (float)Math.Clamp(eater.Number("radius", 8), 1, 32); known = true; }
                if (entity.Component("AreaDamageComponent") is { } area) { result.AreaRadius = (float)Math.Clamp(area.Number("circle_radius", area.Number("aabb_max.x", 16)), 1, 80); result.AreaDamage = (float)area.Number("damage_per_frame", .1); known = true; }
                foreach (var conversion in entity.Definition.Children.Where(c => c.Name == "MagicConvertMaterialComponent" && c.Enabled))
                {
                    if (conversion.Get("to_material").Length == 0) continue;
                    result.Conversions.Add(new(conversion.Get("from_material"), conversion.Get("to_material"), conversion.Number("radius", 24), conversion.Flag("from_any_material"), false)); known = true;
                }
                if (entity.Component("ArcComponent") is { } arc && arc.Get("material").Length > 0)
                { result.Trails.Add(arc.Get("material")); result.Gaps.Add("Arc links approximated as persistent material trails"); known = true; }
                if (known) result.Sources.AddRange(entity.Sources);
            }
            catch (Exception e) { result.Gaps.Add("Augment import: " + e.Message); }
        }
        if (result.LifetimeExtended) result.Gaps.Add("Infinite lifetime is capped at 3600 frames for the prototype");
        return result;
    }
}
