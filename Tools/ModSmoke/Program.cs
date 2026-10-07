using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 2) { Console.Error.WriteLine("Usage: ModSmoke <mod.tmod> <tModLoader folder>"); return 2; }
string root = Path.GetFullPath(args[1]);
using var file = File.OpenRead(args[0]);
using var reader = new BinaryReader(file);
if (new string(reader.ReadChars(4)) != "TMOD") throw new Exception("Invalid mod package");
reader.ReadString(); reader.ReadBytes(276); reader.ReadInt32();
string modName = reader.ReadString(), packageVersion = reader.ReadString();
int count = reader.ReadInt32(), offset = 0;
var table = new List<(string name, int size, int stored, int offset)>();
for (int i = 0; i < count; i++) { string name = reader.ReadString(); int size = reader.ReadInt32(), stored = reader.ReadInt32(); table.Add((name, size, stored, offset)); offset += stored; }
long start = file.Position;
var entry = table.Single(e => e.name == modName + ".dll"); file.Position = start + entry.offset;
byte[] bytes = reader.ReadBytes(entry.stored);
if (entry.stored != entry.size) {
    using var deflate = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress);
    using var output = new MemoryStream(); deflate.CopyTo(output); bytes = output.ToArray();
}
var context = new AssemblyLoadContext("NoitaModSmoke", true);
var paths = Directory.GetFiles(root, "*.dll", SearchOption.AllDirectories)
    .Where(p => !p.Replace('\\', '/').Contains("/Native/") && !p.EndsWith(".resources.dll"))
    .GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(g => g.Key!, g => g.First());
context.Resolving += (_, name) => paths.TryGetValue(name.Name!, out var path) ? context.LoadFromAssemblyPath(path) : null;
var mod = context.LoadFromStream(new MemoryStream(bytes));
var stamp = mod.GetType("terrarianoita.Core.BuildStamp")!;
if (packageVersion != (string)stamp.GetField("Version")!.GetRawConstantValue()!) throw new Exception("Package version differs from compiled build stamp");
Console.WriteLine("PASS: package version matches compiled build identity: " + packageVersion);
var wt = mod.GetType("terrarianoita.Content.Items.NoitaWand")!;
var dt = mod.GetType("terrarianoita.Core.WandDefinition")!;
var tagType = wt.GetMethod("SaveData")!.GetParameters()[0].ParameterType;
object wand = Activator.CreateInstance(wt)!, draft = Activator.CreateInstance(dt)!;
dt.GetProperty("Deck")!.SetValue(draft, new List<string> { "LIGHT_BULLET_TRIGGER", "DAMAGE", "CHAINSAW" });
dt.GetProperty("AlwaysCast")!.SetValue(draft, new List<string> { "MANA_REDUCE" });
dt.GetProperty("CastDelay")!.SetValue(draft, 23d); dt.GetProperty("ManaMax")!.SetValue(draft, 250d);
wt.GetMethod("Apply")!.Invoke(wand, new[] { draft }); wt.GetProperty("DebugVisible")!.SetValue(wand, false);
object tag = Activator.CreateInstance(tagType)!; wt.GetMethod("SaveData")!.Invoke(wand, new[] { tag });
object restored = Activator.CreateInstance(wt)!; wt.GetMethod("LoadData")!.Invoke(restored, new[] { tag });
object def = wt.GetProperty("Definition")!.GetValue(restored)!;
if (!((List<string>)dt.GetProperty("Deck")!.GetValue(def)!).SequenceEqual(new[] { "LIGHT_BULLET_TRIGGER", "DAMAGE", "CHAINSAW" }) ||
    ((List<string>)dt.GetProperty("AlwaysCast")!.GetValue(def)!).Single() != "MANA_REDUCE" ||
    (double)dt.GetProperty("CastDelay")!.GetValue(def)! != 23 || (double)dt.GetProperty("ManaMax")!.GetValue(def)! != 250 ||
    (double)wt.GetProperty("Mana")!.GetValue(restored)! != 100 || (bool)wt.GetProperty("DebugVisible")!.GetValue(restored)!)
    throw new Exception("Saved item data mismatch");
Console.WriteLine("PASS: actual TagCompound round-trip preserves deck, always cast, stats, mana and debug preference");
var itemType = wt.GetMethod("Clone")!.GetParameters()[0].ParameterType;
object cloned = wt.GetMethod("Clone")!.Invoke(wand, new[] { Activator.CreateInstance(itemType)! })!;
object cloneDef = wt.GetProperty("Definition")!.GetValue(cloned)!;
((List<string>)dt.GetProperty("Deck")!.GetValue(cloneDef)!).Clear();
object originalDef = wt.GetProperty("Definition")!.GetValue(wand)!;
if (((List<string>)dt.GetProperty("Deck")!.GetValue(originalDef)!).Count != 3) throw new Exception("Cloned wand aliases original deck");
Console.WriteLine("PASS: actual ModItem clone owns an independent saved definition");
var sparkType = mod.GetType("terrarianoita.Content.Projectiles.NoitaSpark")!;
object spark = Activator.CreateInstance(sparkType)!;
var entity = sparkType.GetProperty("Entity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
object projectile = Activator.CreateInstance(entity.PropertyType)!; entity.SetValue(spark, projectile);
sparkType.GetMethod("SetDefaults")!.Invoke(spark, null);
var method = sparkType.GetMethod("Configure")!; var parameters = method.GetParameters();
object node = Activator.CreateInstance(parameters[0].ParameterType)!;
parameters[0].ParameterType.GetProperty("Entity")!.SetValue(node, "data/entities/projectiles/deck/chainsaw.xml");
object direction = Activator.CreateInstance(parameters[2].ParameterType, new object[] { 1f, 0f })!;
method.Invoke(spark, new object?[] { node, true, direction, null, false });
var ptype = projectile.GetType();
if ((int)ptype.GetField("width")!.GetValue(projectile)! != 28 || (int)ptype.GetField("height")!.GetValue(projectile)! != 28 ||
    !(bool)ptype.GetField("friendly")!.GetValue(projectile)! || (bool)ptype.GetField("tileCollide")!.GetValue(projectile)! ||
    (int)ptype.GetField("penetrate")!.GetValue(projectile)! != -1 || (bool)sparkType.GetMethod("ShouldUpdatePosition")!.Invoke(spark, null)!)
    throw new Exception("Chainsaw hitbox configuration mismatch");
Console.WriteLine("PASS: actual chainsaw config creates a stationary 28x28 friendly NPC hitbox");
var quadType = mod.GetType("terrarianoita.Core.PixelQuad")!;
foreach (var texture in new[] { (1, 1), (2, 2), (20, 20), (256, 32) })
{
    object quad = quadType.GetMethod("Fit")!.Invoke(null, new object[] { texture.Item1, texture.Item2, 12f, 8f })!;
    float Value(string property) => (float)quadType.GetProperty(property)!.GetValue(quad)!;
    if (Math.Abs(Value("ScaleX") * texture.Item1 - 12) > .0001f || Math.Abs(Value("ScaleY") * texture.Item2 - 8) > .0001f ||
        Math.Abs(Value("OriginX") * Value("ScaleX") - 6) > .0001f || Math.Abs(Value("OriginY") * Value("ScaleY") - 4) > .0001f)
        throw new Exception("Packaged sprite geometry depends on texture size");
}
Console.WriteLine("PASS: packaged draw geometry preserves a centered 12x8 spark across four texture dimensions");
var debugType = mod.GetType("terrarianoita.Content.Items.SpellDebugWand")!;
object debugWand = Activator.CreateInstance(debugType)!;
if (!(bool)debugType.GetProperty("VisualPreview")!.GetValue(debugWand)!) throw new Exception("Visual preview must default on");
debugType.GetMethod("ChangeVisualization")!.Invoke(debugWand, null);
debugType.GetMethod("ChangeContext")!.Invoke(debugWand, null);
debugType.GetMethod("ChangeInterval")!.Invoke(debugWand, null);
debugType.GetMethod("ToggleFixedSpell")!.Invoke(debugWand, null);
object debugTag = Activator.CreateInstance(tagType)!; debugType.GetMethod("SaveData")!.Invoke(debugWand, new[] { debugTag });
object debugRestored = Activator.CreateInstance(debugType)!; debugType.GetMethod("LoadData")!.Invoke(debugRestored, new[] { debugTag });
if ((int)debugType.GetProperty("Interval")!.GetValue(debugRestored)! != 300 ||
    debugType.GetProperty("Mode")!.GetValue(debugRestored)!.ToString() != "Solo" ||
    (bool)debugType.GetProperty("Automatic")!.GetValue(debugRestored)! ||
    (bool)debugType.GetProperty("VisualPreview")!.GetValue(debugRestored)! ||
    !(bool)debugType.GetProperty("FixedSpell")!.GetValue(debugRestored)!) throw new Exception("Debug wand save/load changes controls or resumes automatic firing");
Console.WriteLine("PASS: debug wand defaults to harmless preview, saves fixed/cycle controls and loads with auto firing stopped");
object debugReport = debugType.GetProperty("Report")!.GetValue(debugWand)!;
var liveCases = (System.Collections.IList)debugReport.GetType().GetProperty("Cases")!.GetValue(debugReport)!;
liveCases.Add(Activator.CreateInstance(mod.GetType("terrarianoita.Core.SpellLiveCase")!)!);
object debugClone = debugType.GetMethod("Clone")!.Invoke(debugWand, new[] { Activator.CreateInstance(itemType)! })!;
object cloneReport = debugType.GetProperty("Report")!.GetValue(debugClone)!;
if (ReferenceEquals(cloneReport, debugReport) || ((System.Collections.IList)cloneReport.GetType().GetProperty("Cases")!.GetValue(cloneReport)!).Count != 0 ||
    (bool)debugType.GetProperty("Automatic")!.GetValue(debugClone)!) throw new Exception("Cloned debug wand shares a live scan/report");
Console.WriteLine("PASS: cloned debug wand starts with an independent empty scan and report");
var triggerType = mod.GetType("terrarianoita.Core.TriggerPlan")!;
object deathTrigger = Activator.CreateInstance(triggerType)!; triggerType.GetProperty("Kind")!.SetValue(deathTrigger, "death");
((System.Collections.IList)parameters[0].ParameterType.GetProperty("Triggers")!.GetValue(node)!).Add(deathTrigger);
method.Invoke(spark, new object?[] { node, true, direction, null, false });
sparkType.GetMethod("CancelDebugPayloads")!.Invoke(spark, null);
if (sparkType.GetField("triggers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(spark) != null) throw new Exception("Debug cleanup can fire a death payload");
Console.WriteLine("PASS: debug cleanup detaches death/impact/timer payloads before projectile removal");
var visualType = mod.GetType("terrarianoita.Content.Projectiles.NoitaVisualProjectile")!;
object visual = Activator.CreateInstance(visualType)!;
object visualProjectile = Activator.CreateInstance(entity.PropertyType)!; entity.SetValue(visual, visualProjectile);
visualType.GetMethod("SetDefaults")!.Invoke(visual, null);
if ((bool)ptype.GetField("friendly")!.GetValue(visualProjectile)! || (bool)ptype.GetField("hostile")!.GetValue(visualProjectile)! ||
    (int)ptype.GetField("damage")!.GetValue(visualProjectile)! != 0 || (bool)visualType.GetMethod("CanDamage")!.Invoke(visual, null)!)
    throw new Exception("XML visualization can deal damage");
Console.WriteLine("PASS: packaged XML visualization is neutral and explicitly refuses all damage");
object visualProfile = Activator.CreateInstance(mod.GetType("terrarianoita.Core.NoitaVisualProfile")!)!;
object liveCase = Activator.CreateInstance(mod.GetType("terrarianoita.Core.SpellLiveCase")!)!;
object visualEvidence = Activator.CreateInstance(mod.GetType("terrarianoita.Core.VisualEntityEvidence")!)!;
visualType.GetMethod("Configure")!.Invoke(visual, new[] { node, visualProfile, direction, liveCase, visualEvidence });
visualType.GetMethod("CancelDebugPayloads")!.Invoke(visual, null);
if (visualType.GetField("triggers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(visual) != null) throw new Exception("Visual cleanup retains payloads");
Console.WriteLine("PASS: cleanup of imported entities suppresses payloads independently of gameplay demo");
var nativeType = mod.GetType("terrarianoita.Common.TerrariaProjectileBinding")!;
object native = Activator.CreateInstance(nativeType)!;
object nativeProjectile = Activator.CreateInstance(entity.PropertyType)!;
ptype.GetField("active")!.SetValue(nativeProjectile, true); ptype.GetField("damage")!.SetValue(nativeProjectile, 125);
nativeType.GetMethod("Configure")!.Invoke(native, new object?[] { nativeProjectile, node, direction, null, liveCase, visualEvidence, visualProfile, false, true });
ptype.GetField("damage")!.SetValue(nativeProjectile, 100);
nativeType.GetMethod("PrepareBombToBlow")!.Invoke(native, new[] { nativeProjectile });
if ((int)ptype.GetField("damage")!.GetValue(nativeProjectile)! != 125) throw new Exception("Native bomb preparation loses mapped Noita damage");
Console.WriteLine("PASS: native explosion preparation preserves mapped spell damage");
nativeType.GetMethod("RemoveForDebug")!.Invoke(native, new[] { nativeProjectile });
if ((bool)ptype.GetField("active")!.GetValue(nativeProjectile)! || (int)ptype.GetField("damage")!.GetValue(nativeProjectile)! != 0 ||
    (bool)nativeType.GetProperty("Bound")!.GetValue(native)! || nativeType.GetField("triggers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(native) != null)
    throw new Exception("Native cleanup leaves a damaging projectile or payload attached");
Console.WriteLine("PASS: native debug cleanup deactivates the bound test without explosion/payload callbacks");
var gameplay = mod.GetType("terrarianoita.Common.GameplayProjectileAdapter")!;
var prototype = mod.GetType("terrarianoita.Core.TerrariaSpellPrototype")!;
foreach (var sample in new[] { ("Bomb", 28), ("Arrow", 1), ("Bullet", 14), ("Rocket", 134) })
    if ((int)gameplay.GetMethod("VanillaType")!.Invoke(null, new[] { Enum.Parse(prototype, sample.Item1) })! != sample.Item2) throw new Exception("Terraria adapter resolves to wrong projectile");
Console.WriteLine("PASS: packaged adapters resolve the actual Bomb, friendly Arrow, Bullet and RocketI IDs");
object unbound = Activator.CreateInstance(nativeType)!;
ptype.GetField("damage")!.SetValue(nativeProjectile, 77);
nativeType.GetMethod("PrepareBombToBlow")!.Invoke(unbound, new[] { nativeProjectile });
object light = Activator.CreateInstance(mod.GetType("terrarianoita.Content.Projectiles.NoitaVisualProjectile")!.GetMethod("PreDraw")!.GetParameters()[0].ParameterType.GetElementType()!)!;
if ((int)ptype.GetField("damage")!.GetValue(nativeProjectile)! != 77 || !(bool)nativeType.GetMethod("PreDraw")!.Invoke(unbound, new[] { nativeProjectile, light })!)
    throw new Exception("Unbound vanilla projectile is affected by Noita hooks");
Console.WriteLine("PASS: unbound vanilla projectiles keep their original damage and rendering");
var shotType = mod.GetType("terrarianoita.Core.ShotPlan")!;
object nativeTree = Activator.CreateInstance(shotType)!;
object bombNode = Activator.CreateInstance(parameters[0].ParameterType)!;
parameters[0].ParameterType.GetProperty("Entity")!.SetValue(bombNode, "data/entities/projectiles/bomb.xml");
((System.Collections.IList)shotType.GetProperty("Projectiles")!.GetValue(nativeTree)!).Add(bombNode);
gameplay.GetMethod("Validate")!.Invoke(null, new[] { nativeTree });
object unsupported = Activator.CreateInstance(parameters[0].ParameterType)!;
parameters[0].ParameterType.GetProperty("Entity")!.SetValue(unsupported, "data/entities/projectiles/deck/black_hole.xml");
object nested = Activator.CreateInstance(triggerType)!;
object payload = triggerType.GetProperty("Payload")!.GetValue(nested)!;
((System.Collections.IList)shotType.GetProperty("Projectiles")!.GetValue(payload)!).Add(unsupported);
((System.Collections.IList)parameters[0].ParameterType.GetProperty("Triggers")!.GetValue(bombNode)!).Add(nested);
bool rejectedTree = false;
try { gameplay.GetMethod("Validate")!.Invoke(null, new[] { nativeTree }); }
catch (TargetInvocationException e) when (e.InnerException is NotSupportedException) { rejectedTree = true; }
if (!rejectedTree) throw new Exception("Ordinary wand can partially emit a tree containing unsupported native payloads");
Console.WriteLine("PASS: ordinary gameplay accepts mapped Bomb but rejects an unsupported nested payload before emission");
return 0;
