using System;
using System.IO;
using System.Linq;
using terrarianoita.Core;

if (args.Length != 4) { Console.Error.WriteLine("Usage: SpellAudit <native-library> <extracted-data-root> <bridge.lua> <output.json>"); return 2; }
string native = Path.GetFullPath(args[0]), data = Path.GetFullPath(args[1]), bridge = File.ReadAllText(args[2]);
Lua51Runtime New() => new(native, data, bridge);
using var catalog = New();
var audit = new SpellAudit(catalog.SpellIds(), New, catalog.DefaultConfiguration());
while (audit.Step())
    if (audit.Report.Cases.Count % 100 == 0) Console.WriteLine($"{audit.Report.Cases.Count}/{audit.Report.ExpectedCases}");
File.WriteAllText(args[3], audit.Report.Json());
Console.WriteLine($"COMPLETE: {audit.Report.SpellCount} spells / {audit.Report.Cases.Count} cases; {audit.Report.CandidateSpells} spells have a demo candidate");
foreach (var count in audit.Report.Counts) Console.WriteLine($"{count.Key}: {count.Value}");
Console.WriteLine(DemoCapabilities.Profile);
return 0;
