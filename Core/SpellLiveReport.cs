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
        notes = "Visual preview is harmless, including teleports/holes; supported movement modifiers can steer neutral previews. Live integration executes vanilla Bomb/Arrow/Bullet/Rocket, spark/chainsaw, and explicit teleport/black-hole/tentacle/saw adapters. Unmapped paths remain harmless previews. GameplayExecuted and TerrariaAdapter identify the adapter; MovementSources identifies imported movement data. Added debug modifiers are real cards preceding the target in its deck; Always cast retains original Lua ordering. Original conservative audit statuses remain unchanged and may report legacy gaps now covered by adapters. Terrain destruction, relocation and caster damage occur only in live mode. Native Terraria sprites use 1x; Noita sprites use 1.75x. Cleanup cancels teleports, detonations and payloads but cannot undo prior world effects. Short intervals cut effects short. Per-entity gaps state remaining fidelity limits.",
        cases = Cases.Select(c => new { test = c.Test, root_projectiles_spawned = c.RootProjectilesSpawned,
            visual_preview = c.VisualPreview, fixed_spell = c.FixedSpell, modifiers = c.Modifiers, diagnostic_cards_spawned = c.DiagnosticCardsSpawned, visual_entities = c.VisualEntities,
            trace = JsonSerializer.Deserialize<JsonElement>(c.Trace.Json()) }).ToArray()
    }, new JsonSerializerOptions { WriteIndented = true });
}
