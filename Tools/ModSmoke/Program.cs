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
// Compilation does not parse localization. Exercise the same packaged-file loader
// used during Mod.Autoload, including filename prefixes and flattened keys.
var loaderAssembly = context.LoadFromAssemblyPath(Path.Combine(root, "tModLoader.dll"));
var archiveType = loaderAssembly.GetType("Terraria.ModLoader.Core.TmodFile")!;
var cultureType = loaderAssembly.GetType("Terraria.Localization.GameCulture")!;
object archive = Activator.CreateInstance(archiveType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
    null, new object?[] { Path.GetFullPath(args[0]), null, null }, null)!;
using (var archiveScope = (IDisposable)archiveType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(archive, null)!)
{
    var fromCulture = cultureType.GetMethod("FromCultureName")!;
    object english = fromCulture.Invoke(null, new[] { Enum.Parse(fromCulture.GetParameters()[0].ParameterType, "English") })!;
    var loadTranslations = loaderAssembly.GetType("Terraria.ModLoader.LocalizationLoader")!.GetMethod("LoadTranslations",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { archiveType, cultureType }, null)!;
    var translations = ((IEnumerable<(string key, string value)>)loadTranslations.Invoke(null, new[] { archive, english })!)
        .ToDictionary(t => t.key, t => t.value);
    foreach (var expected in new[] {
        ("Items.MaterialFlask.DisplayName", "Material Flask"),
        ("Items.DebugMaterialFlask.DisplayName", "Debug Material Flask"),
        ("Items.SpellDebugWand.DisplayName", "Spell Debug Wand"),
        ("Items.NoitaWand.DisplayName", "Noita Wand"),
        ("NPCs.NoitaRainWorm.DisplayName", "Noita Rain Worm"),
        ("Configs.NoitaConfig.DisplayName", "Noita compatibility"),
        ("Configs.NoitaConfig.AdaptedLiquidReactions.Label", "Adapted liquid reactions"),
        ("Configs.NoitaConfig.ExtractedDataRoot.Label", "Extracted Noita folder") })
        if (!translations.TryGetValue("Mods." + modName + "." + expected.Item1, out var value) || value != expected.Item2)
            throw new Exception("Packaged localization is missing or corrupt: " + expected.Item1);
    foreach (string key in new[] { "Items.SpellDebugWand.Tooltip", "Items.NoitaWand.Tooltip", "Configs.NoitaConfig.ExtractedDataRoot.Tooltip", "Configs.NoitaConfig.AdaptedLiquidReactions.Tooltip" })
        if (!translations.TryGetValue("Mods." + modName + "." + key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new Exception("Packaged localization is missing tooltip: " + key);
    Console.WriteLine($"PASS: tModLoader loads packaged HJSON and resolves all {translations.Count} expected localization keys");
}
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
dt.GetProperty("Shuffle")!.SetValue(draft, true); dt.GetProperty("CastDelay")!.SetValue(draft, 23d); dt.GetProperty("ManaMax")!.SetValue(draft, 250d);
wt.GetMethod("Apply")!.Invoke(wand, new[] { draft }); wt.GetProperty("DebugVisible")!.SetValue(wand, false);
object tag = Activator.CreateInstance(tagType)!; wt.GetMethod("SaveData")!.Invoke(wand, new[] { tag });
object restored = Activator.CreateInstance(wt)!; wt.GetMethod("LoadData")!.Invoke(restored, new[] { tag });
object def = wt.GetProperty("Definition")!.GetValue(restored)!;
if (!((List<string>)dt.GetProperty("Deck")!.GetValue(def)!).SequenceEqual(new[] { "LIGHT_BULLET_TRIGGER", "DAMAGE", "CHAINSAW" }) ||
    ((List<string>)dt.GetProperty("AlwaysCast")!.GetValue(def)!).Single() != "MANA_REDUCE" ||
    !(bool)dt.GetProperty("Shuffle")!.GetValue(def)! || (double)dt.GetProperty("CastDelay")!.GetValue(def)! != 23 || (double)dt.GetProperty("ManaMax")!.GetValue(def)! != 250 ||
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
debugType.GetMethod("ChangeMovement")!.Invoke(debugWand, null);
debugType.GetMethod("ChangeTracking")!.Invoke(debugWand, null);
debugType.GetMethod("ChangeSpeed")!.Invoke(debugWand, null);
debugType.GetMethod("ToggleBounce")!.Invoke(debugWand, null);
object debugTag = Activator.CreateInstance(tagType)!; debugType.GetMethod("SaveData")!.Invoke(debugWand, new[] { debugTag });
object debugRestored = Activator.CreateInstance(debugType)!; debugType.GetMethod("LoadData")!.Invoke(debugRestored, new[] { debugTag });
if ((int)debugType.GetProperty("Interval")!.GetValue(debugRestored)! != 300 ||
    debugType.GetProperty("Mode")!.GetValue(debugRestored)!.ToString() != "Solo" ||
    (bool)debugType.GetProperty("Automatic")!.GetValue(debugRestored)! ||
    (bool)debugType.GetProperty("VisualPreview")!.GetValue(debugRestored)! ||
    !(bool)debugType.GetProperty("FixedSpell")!.GetValue(debugRestored)!) throw new Exception("Debug wand save/load changes controls or resumes automatic firing");
Console.WriteLine("PASS: debug wand defaults to harmless preview, saves fixed/cycle controls and loads with auto firing stopped");
if ((int)debugType.GetProperty("Movement")!.GetValue(debugRestored)! != 1 || (int)debugType.GetProperty("Tracking")!.GetValue(debugRestored)! != 1 ||
    (int)debugType.GetProperty("Speed")!.GetValue(debugRestored)! != 1 || !(bool)debugType.GetProperty("Bounce")!.GetValue(debugRestored)! ||
    !((string[])debugType.GetProperty("ModifierCards")!.GetValue(debugRestored)!).SequenceEqual(new[] { "SINEWAVE", "HOMING", "SPEED", "BOUNCE" })) throw new Exception("Debug modifier controls lost on save/load");
Console.WriteLine("PASS: actual debug item saves and restores the combined movement, tracking, speed and bounce recipe");
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
parameters[0].ParameterType.GetProperty("Entity")!.SetValue(unsupported, "data/entities/projectiles/deck/unported_fixture.xml");
object nested = Activator.CreateInstance(triggerType)!;
object payload = triggerType.GetProperty("Payload")!.GetValue(nested)!;
((System.Collections.IList)shotType.GetProperty("Projectiles")!.GetValue(payload)!).Add(unsupported);
((System.Collections.IList)parameters[0].ParameterType.GetProperty("Triggers")!.GetValue(bombNode)!).Add(nested);
bool rejectedTree = false;
try { gameplay.GetMethod("Validate")!.Invoke(null, new[] { nativeTree }); }
catch (TargetInvocationException e) when (e.InnerException is NotSupportedException) { rejectedTree = true; }
if (!rejectedTree) throw new Exception("Ordinary wand can partially emit a tree containing unsupported native payloads");
Console.WriteLine("PASS: ordinary gameplay accepts mapped Bomb but rejects an unsupported nested payload before emission");
var effectType = mod.GetType("terrarianoita.Content.Projectiles.NoitaEffectProjectile")!;
var effectSettingsType = mod.GetType("terrarianoita.Core.SpellEffectProfile")!;
var kindType = mod.GetType("terrarianoita.Core.NoitaEffectKind")!;
foreach (string kind in new[] { "Teleport", "TeleportCloser", "BlackHole", "Tentacle", "Saw" })
{
    object effect = Activator.CreateInstance(effectType)!;
    object effectProjectile = Activator.CreateInstance(entity.PropertyType)!; entity.SetValue(effect, effectProjectile);
    effectType.GetMethod("SetDefaults")!.Invoke(effect, null);
    ptype.GetField("active")!.SetValue(effectProjectile, true); ptype.GetField("damage")!.SetValue(effectProjectile, 20);
    ptype.GetField("timeLeft")!.SetValue(effectProjectile, 60);
    object settings = Activator.CreateInstance(effectSettingsType)!; effectSettingsType.GetProperty("Kind")!.SetValue(settings, Enum.Parse(kindType, kind));
    effectType.GetMethod("Configure")!.Invoke(effect, new object?[] { node, settings, direction, null, liveCase, visualEvidence, false });
    bool nonCombat = kind is "Teleport" or "TeleportCloser" or "BlackHole";
    if ((bool)ptype.GetField("friendly")!.GetValue(effectProjectile)! == nonCombat ||
        nonCombat && !false.Equals(effectType.GetMethod("CanDamage")!.Invoke(effect, null)) ||
        (bool)effectType.GetMethod("ShouldUpdatePosition")!.Invoke(effect, null)! != (kind != "Tentacle")) throw new Exception("Effect combat or position flags wrong: " + kind);
    if (kind == "Saw")
    {
        var rectType = effectType.GetMethod("Colliding")!.GetParameters()[0].ParameterType;
        object hitbox = Activator.CreateInstance(rectType, new object[] { -4, -4, 8, 8 })!;
        object effectCenter = ptype.GetProperty("Center")!.GetValue(effectProjectile)!;
        int cx = (int)(float)effectCenter.GetType().GetField("X")!.GetValue(effectCenter)!;
        int cy = (int)(float)effectCenter.GetType().GetField("Y")!.GetValue(effectCenter)!;
        object near = Activator.CreateInstance(rectType, new object[] { cx - 1, cy - 1, 2, 2 })!;
        int radius = (int)ptype.GetField("width")!.GetValue(effectProjectile)! / 2;
        object corner = Activator.CreateInstance(rectType, new object[] { cx + radius - 1, cy + radius - 1, 1, 1 })!;
        if (!true.Equals(effectType.GetMethod("Colliding")!.Invoke(effect, new[] { hitbox, near })) ||
            !false.Equals(effectType.GetMethod("Colliding")!.Invoke(effect, new[] { hitbox, corner }))) throw new Exception("Saw collision hits outside its circular sprite");
    }
    effectType.GetMethod("RemoveForDebug")!.Invoke(effect, null);
    // OnKill after cancellation must return before accessing world/player state or dispatching callbacks.
    effectType.GetMethod("OnKill")!.Invoke(effect, new object[] { 0 });
    if ((bool)ptype.GetField("active")!.GetValue(effectProjectile)! || (int)ptype.GetField("damage")!.GetValue(effectProjectile)! != 0 ||
        effectType.GetField("triggers", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(effect) != null) throw new Exception("Custom cleanup keeps effects/payloads: " + kind);
}
Console.WriteLine("PASS: every custom effect configures combat/position flags and cleanup cancels teleport/death callbacks");
parameters[0].ParameterType.GetProperty("Entity")!.SetValue(unsupported, "data/entities/projectiles/deck/black_hole.xml");
gameplay.GetMethod("Validate")!.Invoke(null, new[] { nativeTree });
Console.WriteLine("PASS: ordinary wand accepts custom black-hole payloads atomically alongside native Bomb");
object unboundMotion = Activator.CreateInstance(mod.GetType("terrarianoita.Common.SpellMotionBinding")!)!;
var velocityField = ptype.GetField("velocity")!; object untouchedVelocity = Activator.CreateInstance(velocityField.FieldType, new object[] { 2f, 3f })!;
velocityField.SetValue(nativeProjectile, untouchedVelocity);
unboundMotion.GetType().GetMethod("PostAI")!.Invoke(unboundMotion, new[] { nativeProjectile });
if (!untouchedVelocity.Equals(velocityField.GetValue(nativeProjectile))) throw new Exception("Unbound vanilla projectile movement changed");
Console.WriteLine("PASS: unbound vanilla projectile movement remains untouched");

var flaskType=mod.GetType("terrarianoita.Content.Items.MaterialFlask")!;
object flask=Activator.CreateInstance(flaskType)!;
object contents=flaskType.GetProperty("Contents")!.GetValue(flask)!;
var contentsType=contents.GetType();
contentsType.GetMethod("Add")!.Invoke(contents,new object[]{"water",600});
contentsType.GetMethod("Add")!.Invoke(contents,new object[]{"oil",400});
object flaskTag=Activator.CreateInstance(tagType)!;flaskType.GetMethod("SaveData")!.Invoke(flask,new[]{flaskTag});
object restoredFlask=Activator.CreateInstance(flaskType)!;flaskType.GetMethod("LoadData")!.Invoke(restoredFlask,new[]{flaskTag});
object restoredContents=flaskType.GetProperty("Contents")!.GetValue(restoredFlask)!;
var mixture=(Dictionary<string,int>)contentsType.GetProperty("Materials")!.GetValue(restoredContents)!;
if(mixture.Count!=2||mixture["water"]!=600||mixture["oil"]!=400)throw new Exception("Packaged flask loses mixed contents on save/load");
Console.WriteLine("PASS: actual flask TagCompound preserves mixed liquid quantities");
object clonedFlask=flaskType.GetMethod("Clone")!.Invoke(flask,new[]{Activator.CreateInstance(itemType)!})!;
object clonedContents=flaskType.GetProperty("Contents")!.GetValue(clonedFlask)!;
contentsType.GetMethod("Remove")!.Invoke(clonedContents,new object[]{"oil",400});
if((int)contentsType.GetProperty("Total")!.GetValue(contents)! !=1000)throw new Exception("Cloned flask aliases original liquid quantities");
Console.WriteLine("PASS: actual flask clone owns independent contents");
var worldType=mod.GetType("terrarianoita.Common.MaterialWorld")!;
var materialGridType=mod.GetType("terrarianoita.Core.MaterialGrid")!;
{
    object world=Activator.CreateInstance(worldType)!;
    object worldGrid=worldType.GetProperty("Grid")!.GetValue(world)!;
    var add=materialGridType.GetMethod("Add")!;Func<int,int,bool> openCell=(_,_)=>false;
    add.Invoke(worldGrid,new object[]{10,10,"water",90,openCell,false});
    add.Invoke(worldGrid,new object[]{10,10,"oil",80,openCell,false});
    add.Invoke(worldGrid,new object[]{11,10,"water",400,openCell,true});
    object worldTag=Activator.CreateInstance(tagType)!;worldType.GetMethod("SaveWorldData")!.Invoke(world,new[]{worldTag});
    Func<int,int,bool> worldBounds=(x,y)=>x<2||y<2||x>=62||y>=62;
    object loadedGrid=worldType.GetMethod("RestoreGrid")!.Invoke(null,new object[]{worldTag,worldBounds})!;
    int Stored(object grid,int x,int y,string name)=>(int)materialGridType.GetMethod("AmountOf")!.Invoke(grid,new object[]{x,y,name})!;
    if(Stored(loadedGrid,10,10,"water")!=90||Stored(loadedGrid,10,10,"oil")!=80||Stored(loadedGrid,11,10,"water")!=400||
       (long)materialGridType.GetProperty("Volume")!.GetValue(loadedGrid)! !=570)throw new Exception("World mixture or retained native pressure lost on save/load");
    Console.WriteLine("PASS: actual world TagCompound preserves mixed cells and retained native overflow");
    var tagIndex=tagType.GetProperty("Item")!;
    object oldCell=Activator.CreateInstance(tagType)!;
    foreach(var pair in new Dictionary<string,object>{{"x",12},{"y",14},{"material","oil"},{"amount",120}})
        tagIndex.SetValue(oldCell,pair.Value,new object[]{pair.Key});
    var oldCells=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tagType))!;oldCells.Add(oldCell);
    object legacyTag=Activator.CreateInstance(tagType)!;tagIndex.SetValue(legacyTag,oldCells,new object[]{"noita_materials"});
    loadedGrid=worldType.GetMethod("RestoreGrid")!.Invoke(null,new object[]{legacyTag,worldBounds})!;
    if(Stored(loadedGrid,12,14,"oil")!=120||(long)materialGridType.GetProperty("Volume")!.GetValue(loadedGrid)! !=120)throw new Exception("Legacy single-material world record failed migration");
    Console.WriteLine("PASS: old v0.8 world records load with their exact material quantity and position");
}
var componentType=mod.GetType("terrarianoita.Content.Projectiles.NoitaComponentProjectile")!;
object component=Activator.CreateInstance(componentType)!;object componentProjectile=Activator.CreateInstance(entity.PropertyType)!;entity.SetValue(component,componentProjectile);
componentType.GetMethod("SetDefaults")!.Invoke(component,null);
var componentProfileType=mod.GetType("terrarianoita.Core.ComponentSpellProfile")!;object componentProfile=Activator.CreateInstance(componentProfileType)!;
componentProfileType.GetField("AreaRadius")!.SetValue(componentProfile,28d);
componentType.GetMethod("Configure")!.Invoke(component,new object?[]{componentProfile,node,nativeTree,direction,null,null,visualEvidence});
if((int)ptype.GetField("width")!.GetValue(componentProjectile)! !=56 || (bool)componentType.GetMethod("CanDamage")!.Invoke(component,null)!)throw new Exception("Zero-damage field has wrong geometry or can damage");
Console.WriteLine("PASS: packaged zero-damage field keeps imported radius without becoming damaging");
componentType.GetMethod("RemoveForDebug")!.Invoke(component,null);
componentType.GetMethod("OnKill")!.Invoke(component,new object[]{0});
if((bool)ptype.GetField("active")!.GetValue(componentProjectile)! || componentType.GetField("triggers",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(component)!=null)throw new Exception("Component cleanup can execute payloads or a blast");
Console.WriteLine("PASS: component cleanup suppresses blast/material/payload death effects");
var augmentType=mod.GetType("terrarianoita.Common.SpellAugmentBinding")!;object unboundAugment=Activator.CreateInstance(augmentType)!;
ptype.GetField("damage")!.SetValue(nativeProjectile,77);augmentType.GetMethod("PostAI")!.Invoke(unboundAugment,new[]{nativeProjectile});augmentType.GetMethod("OnKill")!.Invoke(unboundAugment,new object[]{nativeProjectile,0});
if((int)ptype.GetField("damage")!.GetValue(nativeProjectile)! !=77)throw new Exception("Unbound vanilla projectile modified by augments");
Console.WriteLine("PASS: unbound native projectiles bypass new augment physics and death effects");
debugType.GetMethod("ChangeEffectModifier")!.Invoke(debugWand,null);debugType.GetMethod("SaveData")!.Invoke(debugWand,new[]{debugTag});debugType.GetMethod("LoadData")!.Invoke(debugRestored,new[]{debugTag});
if((int)debugType.GetProperty("EffectModifier")!.GetValue(debugRestored)! !=1 || !((string[])debugType.GetProperty("ModifierCards")!.GetValue(debugRestored)!).Contains("FREEZE"))throw new Exception("Debug elemental modifier not persisted");
Console.WriteLine("PASS: debug effect modifier is a real saved FREEZE card");
return 0;
