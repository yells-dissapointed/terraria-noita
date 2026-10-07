using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using terrarianoita.Core;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: AdapterSmoke <native-library> <extracted-data-root> <bridge.lua> [output.json]");
    return 2;
}
string native = Path.GetFullPath(args[0]), data = Path.GetFullPath(args[1]), bridge = File.ReadAllText(args[2]);
int checks = 0;
var output = new Dictionary<string, CastPlan>();
Lua51Runtime New() => new(native, data, bridge);
void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    checks++;
}
string originalRoot = NoitaDataPaths.ResolveRoot(data);
foreach (string input in new[] {
    originalRoot, Path.Combine(originalRoot, "data"),
    Path.Combine(originalRoot, "data", "scripts"),
    Path.Combine(originalRoot, "data", "scripts", "gun"),
    Path.Combine(originalRoot, "data", "scripts", "gun", "gun.lua") })
    Check(NoitaDataPaths.ResolveRoot(input) == originalRoot, "data path layout: " + input);
Check(NoitaDataPaths.ResolveRoot("  \"" + originalRoot + "\"  ") == originalRoot, "quoted path from Copy as path");
string? priorEnvironment = Environment.GetEnvironmentVariable("TERRARIANOITA_SMOKE_DATA_ROOT");
try
{
    Environment.SetEnvironmentVariable("TERRARIANOITA_SMOKE_DATA_ROOT", originalRoot);
    Check(NoitaDataPaths.ResolveRoot("%TERRARIANOITA_SMOKE_DATA_ROOT%") == originalRoot, "environment variable path");
}
finally { Environment.SetEnvironmentVariable("TERRARIANOITA_SMOKE_DATA_ROOT", priorEnvironment); }
string fixture = Path.Combine(Path.GetTempPath(), "Noita path checks " + Guid.NewGuid().ToString("N"));
try
{
    bool rejected = false;
    try { NoitaDataPaths.ResolveRoot(fixture); }
    catch (DirectoryNotFoundException e) { rejected = e.Message.Contains(fixture); }
    Check(rejected, "missing configured folder is identified");
    Directory.CreateDirectory(fixture);
    File.WriteAllText(Path.Combine(fixture, "data.wak"), "fixture");
    rejected = false;
    try { NoitaDataPaths.ResolveRoot(fixture); }
    catch (FileNotFoundException e) { rejected = e.Message.Contains("extract it first"); }
    Check(rejected, "packed data gives extraction guidance");
    string gunFolder = Path.Combine(fixture, "data", "scripts", "gun");
    Directory.CreateDirectory(gunFolder);
    File.WriteAllText(Path.Combine(gunFolder, "gun.lua"), "fixture");
    rejected = false;
    try { NoitaDataPaths.ResolveRoot(gunFolder); }
    catch (FileNotFoundException e) { rejected = e.Message.Contains("incomplete") && e.FileName == Path.Combine(gunFolder, "gun_enums.lua"); }
    Check(rejected, "incomplete extraction identifies the missing dependency");
}
finally { if (Directory.Exists(fixture)) Directory.Delete(fixture, true); }
CastPlan Cast(string name, string[] cards, double mana = 100, string[]? permanent = null, double reload = 40)
{
    using var runtime = New();
    runtime.Configure(new(cards.Select(c => new SpellSlot(c)).ToArray(), ReloadTime: reload));
    var plan = runtime.Cast(mana, permanent);
    output[name] = plan;
    return plan;
}
using (var runtime = new Lua51Runtime(native, Path.Combine(originalRoot, "data", "scripts", "gun"), bridge))
{
    Console.WriteLine("Native runtime: " + runtime.RuntimeVersion);
    Check(runtime.Evaluate("return tostring(io == nil and os == nil and package == nil)") == "true", "file/process libraries removed");
}
var spark = Cast("spark", new[] { "LIGHT_BULLET" });
Check(spark.Mana == 95 && spark.Root.Projectiles.Count == 1, "spark mana/projectile");
var damage = Cast("damage_spark", new[] { "DAMAGE", "LIGHT_BULLET" });
Check(damage.Mana == 90 && damage.Root.Number("damage_projectile_add") == .4, "additive damage");
var multi = Cast("multicast_late_modifier", new[] { "BURST_2", "LIGHT_BULLET", "DAMAGE", "LIGHT_BULLET" });
Check(multi.Root.Projectiles.Count == 2 && multi.Root.Number("damage_projectile_add") == .4, "multicast final shared config");
var trigger = Cast("trigger", new[] { "LIGHT_BULLET_TRIGGER", "DAMAGE", "LIGHT_BULLET" });
var payload = trigger.Root.Projectiles.Single().Triggers.Single().Payload;
Check(trigger.Mana == 80 && payload.Number("damage_projectile_add") == .4 && trigger.Root.Number("damage_projectile_add") == 0, "trigger fresh state and upfront mana");
Check(payload.Projectiles.Count == 1 && payload.Committed, "trigger payload tree committed");
var outer = Cast("outer_modifier", new[] { "DAMAGE", "LIGHT_BULLET_TRIGGER", "LIGHT_BULLET" });
Check(outer.Root.Number("damage_projectile_add") == .4 && outer.Root.Projectiles[0].Triggers[0].Payload.Number("damage_projectile_add") == 0, "outer config does not copy into payload");
var nested = Cast("nested_trigger", new[] { "LIGHT_BULLET_TRIGGER", "LIGHT_BULLET_TRIGGER", "LIGHT_BULLET" });
Check(nested.Root.Projectiles[0].Triggers[0].Payload.Projectiles[0].Triggers[0].Payload.Projectiles.Count == 1, "nested payload tree");
var chainsawLast = Cast("chainsaw_last", new[] { "BURST_2", "LIGHT_BULLET", "CHAINSAW" });
var chainsawFirst = Cast("chainsaw_first", new[] { "BURST_2", "CHAINSAW", "LIGHT_BULLET" });
Check(chainsawLast.Root.Number("fire_rate_wait") == 0 && chainsawFirst.Root.Number("fire_rate_wait") == 3, "chainsaw order");
Check(chainsawLast.ReloadRequest == 30, "chainsaw recharge request");
var skip = Cast("mana_skip", new[] { "BOMB", "LIGHT_BULLET" }, 5);
Check(skip.Mana == 0 && skip.Root.Projectiles.Single().Entity.EndsWith("light_bullet.xml"), "mana skip");
var addMana = Cast("add_mana", new[] { "MANA_REDUCE", "LIGHT_BULLET" }, 0);
Check(addMana.Mana == 25, "negative mana cost");
var permanentDamage = Cast("always_cast", new[] { "LIGHT_BULLET" }, permanent: new[] { "DAMAGE" });
Check(permanentDamage.Mana == 95 && permanentDamage.Root.Projectiles.Count == 1, "always cast draw suppression and mana");
var addedTrigger = Cast("add_trigger", new[] { "ADD_TRIGGER", "DAMAGE", "LIGHT_BULLET", "LIGHT_BULLET" });
Check(addedTrigger.Mana == 85 && addedTrigger.Root.Projectiles[0].Triggers.Count == 1, "add trigger bypass path");
var gamma = Cast("gamma_copy", new[] { "GAMMA", "LIGHT_BULLET" });
Check(gamma.Mana == 60 && gamma.Root.Projectiles.Count == 1, "direct copy bypass path");
var fraction = Cast("fractional_reload", new[] { "CHAINSAW" }, reload: 5.75);
Check(fraction.ReloadRequest == -4.25, "raw native-boundary reload preserved");
using (var first = New())
using (var second = New())
{
    first.Configure(new(new[] { new SpellSlot("LIGHT_BULLET"), new SpellSlot("DAMAGE") }));
    second.Configure(new(new[] { new SpellSlot("CHAINSAW") }));
    var a = first.Cast(100);
    var b = second.Cast(100);
    var c = first.Cast(a.Mana);
    Check(a.Deck.SequenceEqual(new[] { "DAMAGE" }) && c.Root.Number("damage_projectile_add") == .4, "deck survives between casts and wraps");
    Check(b.Root.Number("fire_rate_wait") == 0, "independent wand state");
}
using (var runtime = New())
{
    bool rejected = false;
    try { runtime.Configure(new(new[] { new SpellSlot("LIGHT_BULLET") }, Shuffle: true)); }
    catch (NotSupportedException) { rejected = true; }
    Check(rejected, "shuffle fails explicitly");
    runtime.Configure(new(new[] { new SpellSlot("LIGHT_BULLET") }));
    rejected = false;
    try { runtime.Cast(double.NaN); } catch (ArgumentOutOfRangeException) { rejected = true; }
    Check(rejected && runtime.Cast(100).Mana == 95, "bad managed input does not poison Lua state");
}
using (var runtime = New())
{
    bool rejected = false;
    try { runtime.Configure(new(new[] { new SpellSlot("NOT_A_SPELL") })); }
    catch (InvalidOperationException) { rejected = true; }
    Check(rejected, "unknown original action fails");
    rejected = false;
    try { runtime.Configure(new(new[] { new SpellSlot("LIGHT_BULLET") })); }
    catch (InvalidOperationException) { rejected = true; }
    Check(rejected, "failed runtime cannot be reused");
}
var timer = Cast("timer", new[] { "LIGHT_BULLET_TIMER", "LIGHT_BULLET" });
var runner = new TriggerRunner(timer.Root.Projectiles[0].Triggers);
int fired = 0;
runner.Observe(TriggerSignal.Tick, 9, _ => fired++);
Check(fired == 0, "timer waits");
runner.Observe(TriggerSignal.Tick, 10, _ => fired++);
runner.Observe(TriggerSignal.Tick, 11, _ => fired++);
runner.Observe(TriggerSignal.Death, 11, _ => fired++);
Check(fired == 1, "timer fires once");
runner = new TriggerRunner(trigger.Root.Projectiles[0].Triggers);
fired = 0;
runner.Observe(TriggerSignal.Impact, 1, _ => fired++);
runner.Observe(TriggerSignal.Impact, 1, _ => fired++);
runner.Observe(TriggerSignal.Death, 1, _ => fired++);
Check(fired == 1, "impact payload fires once across collision/death callbacks");
bool badTree = false;
try { CastPlan.Parse("{\"version\":1,\"mana\":100,\"root\":{\"committed\":false}}"); }
catch (InvalidOperationException) { badTree = true; }
Check(badTree, "uncommitted plan rejected");
using (var runtime = New())
{
    runtime.Execute("for _, a in ipairs(actions) do if a.id == 'LIGHT_BULLET' then a.action = function() while true do end end end end");
    runtime.Configure(new(new[] { new SpellSlot("LIGHT_BULLET") }));
    bool rejected = false;
    try { runtime.Cast(100); }
    catch (InvalidOperationException e) { rejected = e.Message.Contains("instruction budget"); }
    Check(rejected, "hot loop stops at instruction budget");
}
using (var runtime = New())
{
    runtime.Configure(new(Enumerable.Repeat(new SpellSlot("LIGHT_BULLET_TRIGGER"), 18).ToArray()));
    bool rejected = false;
    try { runtime.Cast(1000); }
    catch (InvalidOperationException e) { rejected = e.Message.Contains("trigger depth"); }
    Check(rejected, "nested original triggers stop at depth limit");
}
using (var runtime = New())
{
    bool rejected = System.Threading.Tasks.Task.Run(() => {
        try { runtime.Evaluate("return _VERSION"); return false; }
        catch (InvalidOperationException) { return true; }
    }).GetAwaiter().GetResult();
    Check(rejected, "active calls remain on the owning thread");
    System.Threading.Tasks.Task.Run(runtime.Dispose).GetAwaiter().GetResult();
    Check(runtime.IsDisposed, "loader-thread disposal is supported");
}
runner = new TriggerRunner(new[] { new TriggerPlan { Kind = "death", Payload = payload } });
fired = 0;
runner.Observe(TriggerSignal.Impact, 1, _ => fired++);
runner.Observe(TriggerSignal.Death, 1, _ => { fired++; runner.Observe(TriggerSignal.Death, 1, _ => fired++); });
Check(fired == 1, "death payload fires once even during callback reentry");
var definition = new WandDefinition {
    Deck = new() { "BURST_2", "LIGHT_BULLET", "DAMAGE", "LIGHT_BULLET" },
    AlwaysCast = new() { "MANA_REDUCE" }, CastDelay = 20, ReloadTime = 60,
    ActionsPerRound = 2, ManaMax = 250, ManaRecharge = 75
};
var copied = definition.Copy(); copied.Deck.RemoveAt(0); copied.AlwaysCast.Clear();
Check(definition.Deck.Count == 4 && definition.AlwaysCast.Count == 1, "editor drafts do not alias the equipped definition");
var savedDefinition = JsonSerializer.Deserialize<WandDefinition>(JsonSerializer.Serialize(definition))!;
Check(savedDefinition.Deck.SequenceEqual(definition.Deck) && savedDefinition.AlwaysCast.SequenceEqual(definition.AlwaysCast) &&
      savedDefinition.CastDelay == 20 && savedDefinition.ReloadTime == 60 && savedDefinition.ManaMax == 250 &&
      savedDefinition.ManaRecharge == 75 && savedDefinition.ActionsPerRound == 2, "saved editor definition preserves order, always cast and stats");
var invalidDefinition = definition.Copy(); invalidDefinition.Deck.Clear();
bool invalidDraft = false;
try { invalidDefinition.Validate(); } catch (ArgumentException) { invalidDraft = true; }
Check(invalidDraft, "empty deck rejected before applying");
invalidDefinition = definition.Copy(); invalidDefinition.ManaRecharge = double.NaN; invalidDraft = false;
try { invalidDefinition.Validate(); } catch (ArgumentException) { invalidDraft = true; }
Check(invalidDraft, "invalid editor stats rejected");
using (var runtime = New())
{
    var ids = runtime.SpellIds();
    Check(ids.Length == 422 && ids.Contains("CHAINSAW") && ids.Contains("LIGHT_BULLET"), "editor reads original complete spell catalog");
    runtime.Configure(savedDefinition.Configuration());
    var plan = runtime.Cast(250, savedDefinition.AlwaysCast);
    Check(plan.Root.Projectiles.Count > 0 && plan.Mana > 250 - 30, "edited definition casts with always-cast mana behavior");
}
var trace = new CastDiagnostics(); trace.Begin("Preview", new WandDefinition(), 100); trace.Capture(trigger);
Check(trace.Lines.Any(l => l.Contains("Draw DAMAGE | mana")) && trace.Lines.Any(l => l.Contains("hit_world")), "debug display includes per-action mana and trigger payloads");
trace.Event("Hit test dummy for 13 damage"); trace.Fail("Unsupported demo entity");
using (var json = JsonDocument.Parse(trace.Json()))
{
    Check(json.RootElement.GetProperty("plan").GetProperty("root").GetProperty("projectiles")[0].GetProperty("triggers")[0].GetProperty("payload").GetProperty("committed").GetBoolean() &&
          json.RootElement.GetProperty("runtime_events")[0].GetString()!.Contains("dummy") &&
          json.RootElement.GetProperty("error").GetString()!.Contains("Unsupported"), "export retains full trigger tree, runtime collision events and failures");
}
var demoIds = new[] { "LIGHT_BULLET", "LIGHT_BULLET_TRIGGER", "LIGHT_BULLET_TRIGGER_2", "LIGHT_BULLET_TIMER", "CHAINSAW",
    "BURST_2", "BURST_3", "BURST_4", "DAMAGE", "MANA_REDUCE", "RECHARGE", "SPREAD_REDUCE", "SPEED", "LIFETIME", "LIFETIME_DOWN", "ADD_TRIGGER", "ADD_TIMER", "ADD_DEATH_TRIGGER", "GAMMA" };
using (var runtime = New())
{
    var ids = runtime.SpellIds();
    foreach (string id in demoIds.Where(ids.Contains))
    {
        using var sample = New();
        sample.Configure(new(new[] { new SpellSlot(id), new SpellSlot("LIGHT_BULLET"), new SpellSlot("LIGHT_BULLET"), new SpellSlot("LIGHT_BULLET") }));
        var plan = sample.Cast(1000);
        bool EntitiesSupported(ShotPlan shot) => shot.Projectiles.All(p =>
            (p.Entity.EndsWith("light_bullet.xml") || p.Entity.EndsWith("light_bullet_blue.xml") || p.Entity.EndsWith("chainsaw.xml")) && p.Triggers.All(t => EntitiesSupported(t.Payload)));
        Check(EntitiesSupported(plan.Root), "demo palette emits supported entity paths: " + id);
    }
}
// Reproduce the old full-texture scaling bug with 1x1, 2x2 and large textures.
foreach (var texture in new[] { (1, 1), (2, 2), (20, 20), (256, 32) })
foreach (var size in new[] { (12f, 8f), (6f, 3f), (28f, 3f), (8f, 8f) })
{
    var quad = PixelQuad.Fit(texture.Item1, texture.Item2, size.Item1, size.Item2);
    Check(Math.Abs(quad.ScaleX * texture.Item1 - size.Item1) < .0001f &&
          Math.Abs(quad.ScaleY * texture.Item2 - size.Item2) < .0001f &&
          Math.Abs(quad.OriginX * quad.ScaleX - size.Item1 / 2) < .0001f &&
          Math.Abs(quad.OriginY * quad.ScaleY - size.Item2 / 2) < .0001f,
          "sprite size and center are independent of the pixel texture dimensions");
}
using (var catalog = New())
{
    var neutral = catalog.DefaultConfiguration();
    var audit = new SpellAudit(new[] { "LIGHT_BULLET", "DAMAGE", "BOMB", "DAMAGE_RANDOM", "MANA_REDUCE" }, New, neutral);
    while (audit.Step()) { }
    Check(audit.Report.Complete && audit.Report.Cases.Count == 15, "audit runs three isolated contexts per spell");
    var sparkCase = audit.Report.Cases.First(c => c.Spell == "LIGHT_BULLET");
    Check(sparkCase.Status == SpellAuditStatus.DemoWithGaps && sparkCase.Inspection!.Missing.Any(s => s.Contains("damage_critical_chance")), "audit flags ignored spark critical chance despite a renderable entity");
    Check(audit.Report.Cases.First(c => c.Spell == "BOMB").Status == SpellAuditStatus.Unsupported, "audit detects unported bomb entity");
    Check(audit.Report.Cases.Where(c => c.Spell == "DAMAGE_RANDOM").All(c => c.Status == SpellAuditStatus.ScriptError), "audit captures native API errors without stopping the batch");
    Check(audit.Report.Cases.Any(c => c.Spell == "MANA_REDUCE" && c.Status == SpellAuditStatus.NoProjectile) &&
          audit.Report.Cases.Any(c => c.Spell == "MANA_REDUCE" && c.Status == SpellAuditStatus.DemoWithGaps), "utility spells are tested with follow-up projectiles");
    Check(audit.Report.Cases.Any(c => c.Spell == "DAMAGE" && c.Inspection != null && c.Inspection.Missing.Any(m => m.Contains("extra_entities"))), "audit reports ignored native effect entities");
    var nestedInspection = DemoCapabilities.Inspect(trigger, neutral);
    Check(nestedInspection.ProjectileCount > 1 && nestedInspection.TriggerCount > 0 && nestedInspection.Missing.Any(m => m.Contains("extra_entities")), "audit inspects nested payload configuration");
    using var parsed = JsonDocument.Parse(audit.Report.Json());
    Check(parsed.RootElement.GetProperty("Complete").GetBoolean() && parsed.RootElement.GetProperty("Cases").GetArrayLength() == 15 &&
          parsed.RootElement.GetProperty("Limits").GetString()!.Contains("No collision"), "audit export retains every case and states coverage limits");
}
Console.WriteLine($"PASS: {checks} checks against original Noita scripts");
if (args.Length > 3) File.WriteAllText(args[3], JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
return 0;
