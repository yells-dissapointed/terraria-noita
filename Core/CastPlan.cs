#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace terrarianoita.Core;

public sealed class CastPlan
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("mana")] public double Mana { get; set; }
    [JsonPropertyName("root")] public ShotPlan Root { get; set; } = new();
    [JsonPropertyName("events")] public List<CastEvent> Events { get; set; } = new();
    [JsonPropertyName("deck")] public List<string> Deck { get; set; } = new();
    [JsonPropertyName("discarded")] public List<string> Discarded { get; set; } = new();

    public static CastPlan Parse(string json)
    {
        var result = JsonSerializer.Deserialize<CastPlan>(json)
            ?? throw new InvalidOperationException("Missing cast output");
        if (result.Version != 1 || !double.IsFinite(result.Mana))
            throw new InvalidOperationException("Unsupported or invalid cast output");
        int nodes = 0;
        ValidateShot(result.Root, 0, ref nodes);
        return result;
    }

    private static void ValidateShot(ShotPlan shot, int depth, ref int nodes)
    {
        if (shot == null || !shot.Committed || depth > 16 || shot.Config == null || shot.Projectiles == null)
            throw new InvalidOperationException("Invalid shot structure");
        foreach (var field in shot.Config.Values)
            if (field.ValueKind == JsonValueKind.Number && !double.IsFinite(field.GetDouble()))
                throw new InvalidOperationException("Nonfinite cast configuration");
        foreach (var node in shot.Projectiles)
        {
            if (++nodes > 256 || node == null || string.IsNullOrWhiteSpace(node.Entity) || node.Triggers == null)
                throw new InvalidOperationException("Invalid projectile structure");
            foreach (var trigger in node.Triggers)
            {
                if (trigger == null || trigger.Kind is not ("hit_world" or "death" or "timer") ||
                    !double.IsFinite(trigger.DelayFrames) || trigger.DelayFrames < 0)
                    throw new InvalidOperationException("Invalid trigger structure");
                ValidateShot(trigger.Payload, depth + 1, ref nodes);
            }
        }
    }

    public double? ReloadRequest
    {
        get
        {
            double? value = null;
            foreach (var e in Events)
                if (e.Kind == "reload_request") value = e.Value.GetDouble();
            return value;
        }
    }
}

public sealed class ShotPlan
{
    [JsonPropertyName("config")] public Dictionary<string, JsonElement> Config { get; set; } = new();
    [JsonPropertyName("committed")] public bool Committed { get; set; }
    [JsonPropertyName("projectiles")] public List<ProjectilePlan> Projectiles { get; set; } = new();
    public double Number(string name, double fallback = 0) =>
        Config.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : fallback;
    public string Text(string name) =>
        Config.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
}

public sealed class ProjectilePlan
{
    [JsonPropertyName("entity")] public string Entity { get; set; } = "";
    [JsonPropertyName("triggers")] public List<TriggerPlan> Triggers { get; set; } = new();
}
public sealed class TriggerPlan
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    [JsonPropertyName("delay_frames")] public double DelayFrames { get; set; }
    [JsonPropertyName("payload")] public ShotPlan Payload { get; set; } = new();
}
public sealed class CastEvent
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    [JsonPropertyName("value")] public JsonElement Value { get; set; } = JsonSerializer.SerializeToElement<object?>(null);
}

public sealed record SpellSlot(string Id, int UsesRemaining = -1, bool Identified = true);
public sealed record WandConfiguration(IReadOnlyList<SpellSlot> Slots, double CastDelay = 10,
    double ReloadTime = 40, int ActionsPerRound = 1, bool Shuffle = false);
