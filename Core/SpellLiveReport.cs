#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace terrarianoita.Core;

public sealed class SpellLiveCase
{
    public SpellAuditCase Test { get; set; } = new();
    public int RootProjectilesSpawned { get; set; }
    public bool VisualPreview { get; set; }
    public int DiagnosticCardsSpawned { get; set; }
    public List<VisualEntityEvidence> VisualEntities { get; set; } = new();
    public CastDiagnostics Trace { get; set; } = new();
}

public sealed class VisualEntityEvidence
{
    public string Entity { get; set; } = "";
    public int ProjectileId { get; set; }
    public string Appearance { get; set; } = "";
    public List<string> Sources { get; set; } = new();
    public List<string> Components { get; set; } = new();
    public List<string> SpritePaths { get; set; } = new();
    public List<string> Gaps { get; set; } = new();
    public NoitaVisualProfile? PreviewProfile { get; set; }
    public int PreviewLifetimeFrames { get; set; }
    public double PreviewSpeedPerFrame { get; set; }
    [JsonIgnore] public Dictionary<string, NoitaXmlNode> ImportedSources { get; set; } = new();
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
        version = 2, loaded_mod_version = loadedVersion, build = BuildStamp.Id, loaded_mod_path = loadedPath,
        started_utc = StartedUtc, dropped_cases = DroppedCases,
        imported_xml = Cases.SelectMany(c => c.VisualEntities).SelectMany(e => e.ImportedSources).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value),
        notes = "Visual preview imports local XML and sprites and creates harmless entities with approximate movement. Labeled entity markers count as emitted entities; diagnostic cards do not. Lua failures, no output and unexercised actions remain distinct. Deferred native components/configuration are not executed. Gameplay demo still supports only three entities. Cleanup suppresses payloads and can cut effects short. GPU errors update appearance/gaps after spawn. Noita fidelity and visual appearance are not automatically verified.",
        cases = Cases.Select(c => new { test = c.Test, root_projectiles_spawned = c.RootProjectilesSpawned,
            visual_preview = c.VisualPreview, diagnostic_cards_spawned = c.DiagnosticCardsSpawned, visual_entities = c.VisualEntities,
            trace = JsonSerializer.Deserialize<JsonElement>(c.Trace.Json()) }).ToArray()
    }, new JsonSerializerOptions { WriteIndented = true });
}
