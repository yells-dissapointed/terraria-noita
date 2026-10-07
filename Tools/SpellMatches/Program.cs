using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using terrarianoita.Core;

if (args.Length != 3) { Console.Error.WriteLine("Usage: SpellMatches <extracted-root> <spell-audit.json> <output.json>"); return 2; }
var assets = new NoitaAssetCatalog(args[0]);
var audit = JsonSerializer.Deserialize<SpellAuditReport>(File.ReadAllText(args[1]))!;
var paths = audit.Cases.SelectMany(c => c.Inspection?.Entities ?? new List<string>()).Distinct().OrderBy(x => x).ToArray();
var rows = paths.Select(path => {
    var asset = assets.Entity(path); var match = TerrariaSpellMatches.Find(path);
    return new { entity = path, spells = audit.Cases.Where(c => c.Inspection?.Entities.Contains(path) == true).Select(c => c.Spell).Distinct().OrderBy(x => x).ToArray(),
        status = match.Status, terraria_projectile = match.TerrariaProjectile, implemented = match.Implemented,
        visual_policy = match.UseNoitaSprite ? "Retain Noita visual" : "Use Terraria sprite", notes = SpellEffectProfile.Supports(path) ? SpellEffectProfile.Load(assets, path).Limits : match.Notes,
        sources = asset.Sources, components = asset.Definition.Children.Select(c => c.Name).Distinct().OrderBy(x => x).ToArray() };
}).ToArray();
var counts = rows.GroupBy(r => r.status).ToDictionary(g => g.Key, g => g.Count());
File.WriteAllText(args[2], JsonSerializer.Serialize(new { version = BuildStamp.Version, entities = paths.Length, counts,
    notes = "Reviewed prototype inventory for entities emitted by 422 spells in 1266 reference casts. Candidates are engineering matches, not native equivalence or completed implementations. No auto replacement of unmatched/iconic spells. Actual game behavior and visuals require client checks.",
    sources = new[] { "https://docs.tmodloader.net/docs/stable/class_projectile_i_d.html", "https://docs.tmodloader.net/docs/stable/class_projectile.html", "https://docs.tmodloader.net/docs/stable/class_global_projectile.html" }, rows }, new JsonSerializerOptions { WriteIndented = true }));
var md = new StringBuilder("# Noita → Terraria projectile inventory\n\n");
md.AppendLine($"Inspected {paths.Length} emitted entity paths from {audit.SpellCount} spells / {audit.Cases.Count} casts. Complete details are in the companion JSON.\n");
foreach (var count in counts) md.AppendLine($"- {count.Key}: {count.Value} entity paths");
md.AppendLine("\nImplemented vanilla prototypes reuse real Terraria projectile types. Implemented custom effects retain Noita artwork and add explicit teleport/terrain/chain/saw rules. Other matches are candidates requiring custom rules. These are bounded adapters, not the complete native Noita engine. Candidate names are suggestions, not claimed equivalents.\n");
md.AppendLine("| Noita spells | Status | Terraria reuse | Visual policy | Entity |\n| --- | --- | --- | --- | --- |");
foreach (var row in rows) md.AppendLine($"| {string.Join(", ", row.spells)} | {row.status} | {row.terraria_projectile} | {row.visual_policy} | {row.entity} |");
md.AppendLine("\nPrimary references: [Projectile IDs](https://docs.tmodloader.net/docs/stable/class_projectile_i_d.html), [projectile API](https://docs.tmodloader.net/docs/stable/class_projectile.html), [global projectile hooks](https://docs.tmodloader.net/docs/stable/class_global_projectile.html). Noita definitions inspected locally; original code/assets are not bundled.\n");
File.WriteAllText(Path.ChangeExtension(args[2], ".md"), md.ToString());
Console.WriteLine($"Reviewed {rows.Length} entity paths: " + string.Join(", ", counts.Select(p => p.Key + "=" + p.Value)));
return 0;
