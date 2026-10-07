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
        notes = "Visual preview remains harmless. Live integration binds native Terraria projectiles, explicit Noita effects, an XML component subset, and shared gameplay/movement augments. GameplayExecuted means a live adapter was bound; traces record actual contacts/emission. Deferred components, entity scripts and extra entities are listed per case; a rendered projectile is not proof of full card semantics. The original three-entity audit status is retained only for comparison and does not measure v0.8 coverage. Host resource writes (blood/money/cessation) apply only in live mode; shuffle/random cards use deterministic host RNG, not Noita's exact RNG. Native Terraria liquids retain native simulation; other materials use persisted tile-scale cells, bounded flow and selected reactions/statuses. Player polymorph uses temporary curse/weakness; eligible NPCs turn into bunnies. Native Terraria artwork stays 1x; original Noita sprites use 1.75x. Cleanup cancels future effects but cannot undo prior terrain/liquid/resource changes. Client playtesting is required.",
        cases = Cases.Select(c => new { test = c.Test, root_projectiles_spawned = c.RootProjectilesSpawned,
            visual_preview = c.VisualPreview, fixed_spell = c.FixedSpell, modifiers = c.Modifiers, diagnostic_cards_spawned = c.DiagnosticCardsSpawned, visual_entities = c.VisualEntities,
            trace = JsonSerializer.Deserialize<JsonElement>(c.Trace.Json()) }).ToArray()
    }, new JsonSerializerOptions { WriteIndented = true });
}
