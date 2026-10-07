#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace terrarianoita.Core;

public sealed class SpellLiveCase
{
    public SpellAuditCase Test { get; set; } = new();
    public int RootProjectilesSpawned { get; set; }
    public CastDiagnostics Trace { get; set; } = new();
}

public sealed class SpellLiveReport
{
    public DateTime StartedUtc { get; } = DateTime.UtcNow;
    public List<SpellLiveCase> Cases { get; } = new();
    public int DroppedCases { get; private set; }
    public void Add(SpellLiveCase result)
    {
        if (Cases.Count >= 2048) { Cases.RemoveAt(0); DroppedCases++; }
        Cases.Add(result);
    }
    public string Json(string loadedVersion, string loadedPath) => JsonSerializer.Serialize(new {
        version = 1, loaded_mod_version = loadedVersion, build = BuildStamp.Id, loaded_mod_path = loadedPath,
        started_utc = StartedUtc, dropped_cases = DroppedCases,
        notes = "Live Terraria demo. Root spawn counts and bounded collision/trigger events are recorded. Unsupported entities and native API failures are skipped. Missing config effects remain unimplemented. Cleanup between tests suppresses death payloads; long-lived effects may be cut short. Visual appearance and full Noita fidelity are not automatically verified.",
        cases = Cases.Select(c => new { test = c.Test, root_projectiles_spawned = c.RootProjectilesSpawned,
            trace = JsonSerializer.Deserialize<JsonElement>(c.Trace.Json()) }).ToArray()
    }, new JsonSerializerOptions { WriteIndented = true });
}
