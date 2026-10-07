#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace terrarianoita.Core;

/// <summary>Read-only Terraria snapshot supplied before a cast. Resource writes become events applied only after validation.</summary>
public sealed class CastHostContext
{
    [JsonPropertyName("frame")] public long Frame { get; set; }
    [JsonPropertyName("x")] public double X { get; set; }
    [JsonPropertyName("y")] public double Y { get; set; }
    [JsonPropertyName("hp")] public double Hp { get; set; } = 4;
    [JsonPropertyName("max_hp")] public double MaxHp { get; set; } = 4;
    [JsonPropertyName("money")] public long Money { get; set; }
    [JsonPropertyName("black_holes")] public int BlackHoles { get; set; }
    [JsonPropertyName("enemies")] public List<HostPoint> Enemies { get; set; } = new();
    [JsonPropertyName("projectiles")] public List<HostPoint> Projectiles { get; set; } = new();
    [JsonPropertyName("wands")] public List<List<string>> Wands { get; set; } = new();
}
public sealed record HostPoint([property: JsonPropertyName("x")] double X, [property: JsonPropertyName("y")] double Y);
