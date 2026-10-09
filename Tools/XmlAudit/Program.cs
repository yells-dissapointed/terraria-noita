using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using terrarianoita.Core;

if (args.Length != 3) { Console.Error.WriteLine("Usage: XmlAudit <extracted-data-root> <spell-audit.json> <output.json>"); return 2; }
var catalog = new NoitaAssetCatalog(args[0]);
var audit = JsonSerializer.Deserialize<SpellAuditReport>(File.ReadAllText(args[1]))!;
var paths = audit.Cases.SelectMany(c => c.Inspection?.Entities ?? new List<string>()).Distinct().OrderBy(x => x).ToArray();
var results = new List<object>(); int loaded = 0, spriteEntities = 0, markers = 0, failures = 0;
foreach (string path in paths)
{
    try
    {
        var asset = catalog.Entity(path); var profile = catalog.Profile(asset); loaded++;
        if (profile.Sprites.Count > 0) spriteEntities++; else markers++;
        results.Add(new { path, sources = asset.Sources, components = asset.Definition.Children.Select(n => n.Name).ToArray(), profile });
    }
    catch (Exception e) { failures++; results.Add(new { path, error = e.Message }); }
}
File.WriteAllText(args[2], JsonSerializer.Serialize(new { entities = paths.Length, loaded, sprite_entities = spriteEntities, marker_entities = markers, failures,
    notes = "Definition/sprite-data audit only. Components are retained as data, not all executed. PNG headers/frame bounds are checked; GPU decoding/rendering is not tested. Native precedence and physics are unverified.", results }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"XML: {loaded}/{paths.Length} definitions; {spriteEntities} entities with sprite data; {markers} marker-only; {failures} load failures");
return failures == 0 ? 0 : 1;
