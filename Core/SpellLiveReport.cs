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
    public bool FixedSpell { get; set; }
    public List<string> Modifiers { get; set; } = new();
    public int DiagnosticCardsSpawned { get; set; }
    public List<VisualEntityEvidence> VisualEntities { get; set; } = new();
    public CastDiagnostics Trace { get; set; } = new();
}

public sealed class VisualEntityEvidence
{
    public string Entity { get; set; } = "";
    public int ProjectileId { get; set; }
    public string Appearance { get; set; } = "";
    public string TerrariaAdapter { get; set; } = "";
    public bool GameplayExecuted { get; set; }
    public List<string> Sources { get; set; } = new();
    public List<string> Components { get; set; } = new();
    public List<string> SpritePaths { get; set; } = new();
    public List<string> MovementSources { get; set; } = new();
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
        spell_visual_scale = SpellVisuals.Scale,
        terraria_sprite_scale = SpellVisuals.TerrariaScale,
        started_utc = StartedUtc, dropped_cases = DroppedCases,
        imported_xml = Cases.SelectMany(c => c.VisualEntities).SelectMany(e => e.ImportedSources).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.First().Value),
        notes = "Visual preview remains harmless. GameplayExecuted means a live adapter was bound, not full native parity; deferred components, entity scripts and extras remain recorded. The original three-entity audit is historical. Resource writes apply only in live mode; host RNG is not Noita's exact RNG. Materials use shared tile-volume mixtures, imported density, binary XML reaction adapters and optional adapted chemistry. Contacting native water/lava/honey temporarily join the shared solver; isolated native fluid returns to Terraria. Shimmer stays native. This is not Noita's pixel physics or reaction scheduling. Player polymorph uses curse/weakness; eligible NPCs become bunnies. Native Terraria artwork stays 1x; original Noita sprites use 1.75x. Cleanup cannot undo existing world/resource changes. Client playtesting is required.",
        cases = Cases.Select(c => new { test = c.Test, root_projectiles_spawned = c.RootProjectilesSpawned,
            visual_preview = c.VisualPreview, fixed_spell = c.FixedSpell, modifiers = c.Modifiers, diagnostic_cards_spawned = c.DiagnosticCardsSpawned, visual_entities = c.VisualEntities,
            trace = JsonSerializer.Deserialize<JsonElement>(c.Trace.Json()) }).ToArray()
    }, new JsonSerializerOptions { WriteIndented = true });
}
