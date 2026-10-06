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
string modName = reader.ReadString(); reader.ReadString();
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
return 0;
