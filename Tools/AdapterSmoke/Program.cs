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
CastPlan Cast(string name, string[] cards, double mana = 100, string[]? permanent = null, double reload = 40)
{
    using var runtime = New();
    runtime.Configure(new(cards.Select(c => new SpellSlot(c)).ToArray(), ReloadTime: reload));
    var plan = runtime.Cast(mana, permanent);
    output[name] = plan;
    return plan;
}
using (var runtime = New())
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
Console.WriteLine($"PASS: {checks} checks against original Noita scripts");
if (args.Length > 3) File.WriteAllText(args[3], JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
return 0;
