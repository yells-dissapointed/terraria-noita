#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace terrarianoita.Core;

/// <summary>Shared capability inventory for the demo renderer, previews and batch audits.</summary>
public static class DemoCapabilities
{
    public const string Spark = "data/entities/projectiles/deck/light_bullet.xml";
    public const string BlueSpark = "data/entities/projectiles/deck/light_bullet_blue.xml";
    public const string Chainsaw = "data/entities/projectiles/deck/chainsaw.xml";
    public const string Profile = "Three-entity Terraria demo; approximate physics, damage and scheduling. Visual fidelity is unverified.";
    public static bool SupportsEntity(string entity) => entity is Spark or BlueSpark or Chainsaw;
    private static readonly HashSet<string> Mapped = new(StringComparer.Ordinal) {
        "fire_rate_wait", "reload_time", "damage_projectile_add", "speed_multiplier", "spread_degrees", "lifetime_add"
    };
    public static DemoInspection Inspect(CastPlan plan, IReadOnlyDictionary<string, JsonElement> defaults)
    {
        var result = new DemoInspection();
        var entities = new HashSet<string>(StringComparer.Ordinal);
        var missing = new HashSet<string>(StringComparer.Ordinal);
        void Visit(ShotPlan shot)
        {
            foreach (var node in shot.Projectiles)
            {
                result.ProjectileCount++; entities.Add(node.Entity);
                if (!SupportsEntity(node.Entity)) missing.Add("Entity: " + node.Entity);
                foreach (var trigger in node.Triggers) { result.TriggerCount++; Visit(trigger.Payload); }
            }
            foreach (var field in shot.Config)
            {
                if (Mapped.Contains(field.Key) || field.Key == "custom_xml_file" || field.Key.StartsWith("action_", StringComparison.Ordinal) || field.Key.StartsWith("state_", StringComparison.Ordinal)) continue;
                if (field.Key == "damage_slice_add" && shot.Projectiles.Count > 0 && shot.Projectiles.All(p => p.Entity == Chainsaw)) continue;
                if (defaults.TryGetValue(field.Key, out var neutral) && Equal(field.Value, neutral)) continue;
                // Sound tags may be absent rather than empty on normal cards.
                if (field.Value.ValueKind == JsonValueKind.Null) continue;
                missing.Add("Config: " + field.Key + " = " + field.Value.GetRawText());
            }
        }
        Visit(plan.Root);
        foreach (var effect in plan.Events.Where(e => e.Kind == "shot_effects"))
            if (effect.Value.EnumerateArray().Any(v => v.ValueKind == JsonValueKind.Number && v.GetDouble() != 0))
                missing.Add("Shot effect: recoil/knockback " + effect.Value.GetRawText());
        result.Entities = entities.OrderBy(x => x, StringComparer.Ordinal).ToList();
        result.Missing = missing.OrderBy(x => x, StringComparer.Ordinal).ToList();
        return result;
    }
    private static bool Equal(JsonElement a, JsonElement b) => a.ValueKind == b.ValueKind &&
        (a.ValueKind == JsonValueKind.Number ? a.GetDouble() == b.GetDouble() : a.GetRawText() == b.GetRawText());
}

public sealed class DemoInspection
{
    public int ProjectileCount { get; set; }
    public int TriggerCount { get; set; }
    public List<string> Entities { get; set; } = new();
    public List<string> Missing { get; set; } = new();
}
