using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using terrarianoita.Core;

if (args.Length != 3) { Console.Error.WriteLine("ExpansionAudit <extracted-root> <spell-audit.json> <output.json>"); return 2; }
var catalog = new NoitaAssetCatalog(args[0]); var materials = new NoitaMaterialCatalog(args[0]);
var audit = JsonSerializer.Deserialize<SpellAuditReport>(File.ReadAllText(args[1]))!;
var paths = audit.Cases.SelectMany(c => c.Inspection?.Entities ?? new()).Distinct().OrderBy(p => p).ToArray();
var rows = paths.Select(path => {
    string error = ""; ComponentSpellProfile? profile = null;
    try { profile = ComponentSpellProfile.Load(catalog, path); } catch (Exception e) { error = e.Message; }
    var match = TerrariaSpellMatches.Find(path);
    string adapter = SpellEffectProfile.Supports(path) ? "Custom " + SpellEffectProfile.KindFor(path) : DemoCapabilities.SupportsEntity(path) ? "Spark/chainsaw" : match.Implemented ? "Terraria " + match.TerrariaProjectile : profile?.Supported == true ? "XML component subset" : "Preview only";
    return new { entity = path, adapter, live_adapter = adapter != "Preview only", features = profile?.Features ?? new(), gaps = profile?.Gaps ?? new(), error };
}).ToArray();
var byPath = rows.ToDictionary(r => r.entity);
var spells = audit.Cases.GroupBy(c => c.Spell).Select(g => {
    var entities = g.SelectMany(c => c.Inspection?.Entities ?? new()).Distinct().OrderBy(p => p).ToArray();
    return new { spell = g.Key, cast_contexts = g.Count(), lua_passed = g.Count(c => c.Status != SpellAuditStatus.ScriptError),
        errors = g.Where(c => c.Error.Length > 0).Select(c => c.Error).Distinct().ToArray(),
        entities, preview_only = entities.Where(p => !byPath[p].live_adapter).ToArray(),
        has_live_entity = entities.Any(p => byPath[p].live_adapter),
        note = "A live entity does not establish full card semantics: follow-up spark contexts, deferred modifiers and unported entity scripts are reported separately." };
}).ToArray();
var extraPaths = new HashSet<string>(); var modifierCoverage = new Dictionary<string, string>();
void Walk(ShotPlan shot) {
    var motion = SpellMotion.Load(shot,catalog); var augments = SpellAugments.Load(shot,catalog);
    foreach(string path in shot.Text("extra_entities").Split(',',StringSplitOptions.RemoveEmptyEntries)) {
        extraPaths.Add(path); modifierCoverage[path] = motion.Sources.Contains(path) ? "Movement adapter" : augments.Sources.Contains(path) ? "Gameplay augment adapter" : path.StartsWith("data/entities/particles/") ? "Cosmetic extra entity not reproduced" : "Deferred extra entity";
    }
    foreach(var node in shot.Projectiles) foreach(var trigger in node.Triggers) Walk(trigger.Payload);
}
foreach(var c in audit.Cases) if(c.Plan != null) Walk(c.Plan.Root);
var result = new { version = BuildStamp.Version, spell_count = audit.SpellCount, cast_count = audit.Cases.Count,
    lua_passed_cases = audit.Cases.Count(c=>c.Status != SpellAuditStatus.ScriptError),
    lua_passed_spells = spells.Count(s=>s.lua_passed==s.cast_contexts),
    imported_materials = materials.Materials.Count, imported_liquids = materials.Liquids.Length,
    emitted_entities = rows.Length, live_entity_adapters = rows.Count(r=>r.live_adapter), preview_only_entities = rows.Count(r=>!r.live_adapter),
    notes = "Static cast/import coverage, not rendered or combat playtesting. Original Noita entity Lua is not executed. XML subset adapters and modifier approximations are not full native parity. Native liquid chemistry is not ported; custom fluids use bounded tile cells.",
    liquids = materials.Liquids.Select(name=>materials.Get(name)).ToArray(),
    spells, entities = rows, modifier_entities = modifierCoverage.OrderBy(p=>p.Key).ToDictionary(p=>p.Key,p=>p.Value) };
File.WriteAllText(args[2], JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
var md=new StringBuilder("# Expansion coverage\n\n");
md.AppendLine($"Build {BuildStamp.Version}: {result.lua_passed_cases}/{result.cast_count} reference casts completed; {result.lua_passed_spells}/{result.spell_count} cards passed all three Lua contexts.\n");
md.AppendLine($"{result.live_entity_adapters}/{result.emitted_entities} emitted entity paths have a live adapter; preview-only paths: {result.preview_only_entities}. {result.imported_materials} material definitions and {result.imported_liquids} liquids imported.\n");
md.AppendLine(result.notes+"\n");
md.AppendLine("## Lua exceptions\n"); foreach(var s in spells.Where(s=>s.errors.Length>0)) md.AppendLine("- "+s.spell+": "+s.errors[0]);
md.AppendLine("\n## Entity adapters\n\n| Entity | Live adapter | Remaining gaps |\n| --- | --- | --- |");
foreach(var r in rows) md.AppendLine($"| {r.entity} | {r.adapter} | {r.gaps.Count} recorded; {r.error} |");
md.AppendLine("\n## Modifier entities\n\n| Extra entity | Coverage |\n| --- | --- |");
foreach(var m in modifierCoverage.OrderBy(p=>p.Key)) md.AppendLine($"| {m.Key} | {m.Value} |");
File.WriteAllText(Path.ChangeExtension(args[2],".md"),md.ToString());
Console.WriteLine($"Lua {result.lua_passed_cases}/{result.cast_count}; entities {result.live_entity_adapters}/{result.emitted_entities}; materials {result.imported_materials}, liquids {result.imported_liquids}");
foreach(var r in rows.Where(r=>!r.live_adapter)) Console.WriteLine("PREVIEW: "+r.entity+" "+r.error);
return 0;
