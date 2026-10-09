#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace terrarianoita.Core;

public sealed class NoitaMaterial
{
    public string Name { get; init; } = "water";
    public string Kind { get; init; } = "liquid";
    public string Tags { get; init; } = "";
    public string Status { get; init; } = "";
    public uint Color { get; init; } = 0x902f85cc;
    public double Density { get; init; } = 4;
    public bool Sand { get; init; }
    public bool Flammable { get; init; }
    public bool Hot { get; init; }
    public string[] ReactionParents { get; init; } = Array.Empty<string>();
    public bool Scoopable => Kind == "liquid" && !Sand;
    public bool Has(string tag) => Tags.Contains("[" + tag + "]", StringComparison.Ordinal);
}

/// <summary>Local material metadata and reaction definitions; the adapter executes a declared subset.</summary>
public sealed class NoitaMaterialCatalog
{
    private readonly Dictionary<string, NoitaMaterial> materials = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, NoitaMaterial> Materials => materials;
    public IReadOnlyList<NoitaXmlNode> ReactionNodes { get; }
    public int ImportedCount { get; }
    public NoitaMaterialCatalog(string root)
    {
        var source = NoitaXml.Parse(File.ReadAllText(Path.Combine(NoitaDataPaths.ResolveRoot(root), "data/materials.xml")), new());
        ReactionNodes = source.Children.Where(n => n.Name == "Reaction").ToArray();
        var nodes = source.Children.Where(n => n.Name is "CellData" or "CellDataChild").Where(n => n.Get("name").Length > 0)
            .GroupBy(n => n.Get("name")).ToDictionary(g => g.Key, g => g.Last());
        var merged = new Dictionary<string, NoitaXmlNode>();
        NoitaXmlNode Resolve(string name, HashSet<string> stack)
        {
            if (merged.TryGetValue(name, out var old)) return old;
            if (!stack.Add(name) || stack.Count > 32) throw new InvalidDataException("Material inheritance cycle: " + name);
            var node = nodes[name]; var result = node.Copy();
            string parent = node.Get("_parent");
            if (parent.Length > 0 && nodes.ContainsKey(parent))
            {
                var basis = Resolve(parent, stack);
                foreach (var pair in basis.Attributes) if (!result.Attributes.ContainsKey(pair.Key)) result.Attributes[pair.Key] = pair.Value;
                foreach (var child in basis.Children) if (!result.Children.Any(c => c.Name == child.Name)) result.Children.Add(child.Copy());
            }
            stack.Remove(name); merged[name] = result; return result;
        }
        foreach (string name in nodes.Keys)
        {
            var node = Resolve(name, new());
            var parents = new List<string>(); string ancestor = name;
            while (nodes.TryGetValue(ancestor, out var own) && own.Flag("_inherit_reactions") && own.Get("_parent") is { Length: > 0 } parent && !parents.Contains(parent))
            { parents.Add(parent); ancestor = parent; }
            string color = node.Children.FirstOrDefault(c => c.Name == "Graphics")?.Get("color") ?? node.Get("wang_color", "ff888888");
            if (!uint.TryParse(color, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgba)) rgba = 0xff888888;
            materials[name] = new() { Name = name, Kind = node.Get("cell_type", "solid"), Tags = node.Get("tags"), Status = node.Get("status_effects"), Color = rgba,
                Density = node.Number("density", 4), Sand = node.Flag("liquid_sand"), Flammable = node.Flag("burnable", name == "alcohol" || node.Get("tags").Contains("[burnable]")), Hot = node.Flag("on_fire") || name == "lava", ReactionParents = parents.ToArray() };
        }
        ImportedCount = materials.Count;
        materials["noita_diluted_acid"] = new() { Name = "noita_diluted_acid", Density = Get("water").Density,
            Color = 0xff89bd68, Tags = "[liquid]", Status = "POISONED" };
    }
    public NoitaMaterial Get(string name) => materials.TryGetValue(name, out var material) ? material : new() { Name = name };
    public string[] Liquids => materials.Values.Where(m => m.Scoopable && m.Name != "air").Select(m => m.Name).OrderBy(n => n).ToArray();
}

public sealed class FlaskContents
{
    public const int Capacity = 1000;
    public Dictionary<string, int> Materials { get; private set; } = new(StringComparer.Ordinal);
    public int Total => Materials.Values.Sum();
    public int Add(string name, int amount)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128 || amount <= 0) return 0;
        int accepted = Math.Min(amount, Math.Max(0, Capacity - Total));
        if (accepted > 0) Materials[name] = Materials.GetValueOrDefault(name) + accepted;
        return accepted;
    }
    public int Remove(string name, int amount)
    {
        int removed = Math.Min(Math.Max(0, amount), Materials.GetValueOrDefault(name));
        if (removed > 0) { Materials[name] -= removed; if (Materials[name] == 0) Materials.Remove(name); }
        return removed;
    }
    public FlaskContents Copy() { var copy = new FlaskContents(); foreach (var pair in Materials) copy.Add(pair.Key, pair.Value); return copy; }
}

public readonly record struct MaterialCell(int X, int Y, string Material, int Amount);

/// <summary>Shared-volume cells retain individual materials. Ordinary flow never exceeds 255 units.
/// External native-fluid incursions can be held under pressure until room opens; nothing is discarded.</summary>
public sealed class MaterialGrid
{
    public const int CellCapacity = 255, MaximumCells = 32768, MaximumMaterialsPerCell = 16, MaximumStored = 65535;
    private readonly Dictionary<(int X, int Y), Dictionary<string, int>> cells = new();
    private readonly Queue<(int X, int Y)> queue = new();
    private readonly HashSet<(int X, int Y)> queued = new();
    public IEnumerable<(int X, int Y)> Positions => cells.Keys;
    public IEnumerable<MaterialCell> Cells => cells.SelectMany(p => p.Value.Select(m => new MaterialCell(p.Key.X, p.Key.Y, m.Key, m.Value)));
    public int Count => cells.Count;
    public long Volume => cells.Values.Sum(c => (long)c.Values.Sum());
    public int AmountAt(int x, int y) => cells.TryGetValue((x, y), out var cell) ? cell.Values.Sum() : 0;
    public int AmountOf(int x, int y, string material) => cells.TryGetValue((x, y), out var cell) ? cell.GetValueOrDefault(material) : 0;
    public MaterialCell[] LayersAt(int x, int y) => cells.TryGetValue((x, y), out var cell) ? cell.Select(m => new MaterialCell(x, y, m.Key, m.Value)).ToArray() : Array.Empty<MaterialCell>();
    public MaterialCell? At(int x, int y) => LayersAt(x, y).OrderByDescending(c => c.Amount).ThenBy(c => c.Material, StringComparer.Ordinal).Cast<MaterialCell?>().FirstOrDefault();
    private void Wake(int x, int y)
    {
        // Empty cells need not occupy the queue; changes wake existing neighbours.
        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
        { var key = (x + dx, y + dy); if (cells.ContainsKey(key) && queued.Add(key)) queue.Enqueue(key); }
    }
    public int Add(int x, int y, string material, int amount, Func<int, int, bool> blocked, bool allowPressure = false)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(material) || material == "air" || material.Length > 128 || blocked(x, y)) return 0;
        var key = (x, y); bool exists = cells.TryGetValue(key, out var old);
        if (!exists && cells.Count >= MaximumCells || exists && !old!.ContainsKey(material) && old.Count >= MaximumMaterialsPerCell) return 0;
        int accepted = Math.Min(amount, Math.Max(0, (allowPressure ? MaximumStored : CellCapacity) - (old?.Values.Sum() ?? 0)));
        if (accepted > 0) { if (!exists) cells[key] = old = new(StringComparer.Ordinal); old![material] = old.GetValueOrDefault(material) + accepted; Wake(x, y); }
        return accepted;
    }
    public int Take(int x, int y, int amount, string? material = null)
    {
        if (!cells.TryGetValue((x, y), out var old)) return 0;
        material ??= old.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).First().Key;
        int removed = Math.Min(Math.Max(0, amount), old.GetValueOrDefault(material));
        if (removed > 0)
        {
            old[material] -= removed; if (old[material] == 0) old.Remove(material);
            if (old.Count == 0) cells.Remove((x, y));
            Wake(x, y);
        }
        return removed;
    }
    public void WakeAll() { foreach (var key in cells.Keys) if (queued.Add(key)) queue.Enqueue(key); }
    public int Step(Func<int, int, bool> blocked, Func<string, NoitaMaterial> material, int budget, long frame, Action<int, int>? prepare = null)
    {
        int steps = Math.Min(Math.Max(0, budget), queue.Count);
        for (int i = 0; i < steps; i++)
        {
            var key = queue.Dequeue(); queued.Remove(key);
            if (!cells.ContainsKey(key)) continue; // A stale wake must not re-import native fluid after release.
            prepare?.Invoke(key.X, key.Y);
            if (!cells.ContainsKey(key) || blocked(key.X, key.Y)) continue;
            foreach (var cell in LayersAt(key.X, key.Y).OrderByDescending(c => Density(material(c.Material))))
            {
                var meta = material(cell.Material); if (!Mobile(meta)) continue;
                int dy = meta.Kind is "gas" or "fire" ? -1 : 1;
                prepare?.Invoke(key.X, key.Y + dy);
                Move(cell.X, cell.Y, cell.X, cell.Y + dy, cell.Material, CellCapacity, blocked);
                // Equal-volume exchange lets heavy liquid sink through lighter liquid, and gas rise.
                if (AmountOf(cell.X, cell.Y, cell.Material) > 0 && !blocked(cell.X, cell.Y + dy))
                    foreach (var other in LayersAt(cell.X, cell.Y + dy).OrderBy(c => Density(material(c.Material))))
                        if (other.Material != cell.Material && Mobile(material(other.Material)) &&
                            (dy > 0 ? Density(meta) > Density(material(other.Material)) : Density(meta) < Density(material(other.Material))))
                            Exchange(cell, other, 32);
            }
            if (!cells.ContainsKey(key)) continue;
            int side = ((frame + key.X + key.Y) & 1) == 0 ? -1 : 1;
            for (int j = 0; j < 2; j++, side = -side)
            {
                if (!cells.ContainsKey(key)) break;
                prepare?.Invoke(key.X + side, key.Y);
                int limit = Math.Max(0, (AmountAt(key.X, key.Y) - AmountAt(key.X + side, key.Y)) / 2);
                MoveMixture(key.X, key.Y, key.X + side, key.Y, limit, blocked, material);
            }
            int overflow = AmountAt(key.X, key.Y) - CellCapacity;
            if (overflow > 0) { prepare?.Invoke(key.X, key.Y - 1); MoveMixture(key.X, key.Y, key.X, key.Y - 1, overflow, blocked, material); }
        }
        return steps;
    }
    public static bool Mobile(NoitaMaterial m) => m.Kind is "liquid" or "gas" or "fire";
    public static double Density(NoitaMaterial m) => m.Kind is "gas" or "fire" ? -1000 + m.Density : m.Density;
    private int Move(int x, int y, int tx, int ty, string material, int limit, Func<int, int, bool> blocked)
    {
        if (limit <= 0) return 0;
        int moved = Add(tx, ty, material, Math.Min(AmountOf(x, y, material), limit), blocked);
        Take(x, y, moved, material); return moved;
    }
    private void MoveMixture(int x, int y, int tx, int ty, int limit, Func<int, int, bool> blocked, Func<string, NoitaMaterial> material)
    {
        if (limit <= 0 || blocked(tx, ty)) return;
        var layers = LayersAt(x, y).Where(c => Mobile(material(c.Material)) && !material(c.Material).Sand).ToArray();
        int total = layers.Sum(c => c.Amount), remaining = Math.Min(limit, Math.Max(0, CellCapacity - AmountAt(tx, ty)));
        int desired = remaining;
        foreach (var layer in layers)
        { int share = Math.Min(remaining, (int)Math.Ceiling(desired * (double)layer.Amount / Math.Max(1, total)));
            remaining -= Move(x, y, tx, ty, layer.Material, share, blocked); }
    }
    private void Exchange(MaterialCell a, MaterialCell b, int limit)
    {
        int n = Math.Min(limit, Math.Min(AmountOf(a.X, a.Y, a.Material), AmountOf(b.X, b.Y, b.Material)));
        if (n > 0) TransformPair(a, b, b.Material, a.Material, n);
    }
    // Atomic changes also protect 16-material cells from losing inputs when products cannot fit.
    public int TransformPair(MaterialCell a, MaterialCell b, string outputA, string outputB, int limit)
    {
        bool same = a.X == b.X && a.Y == b.Y;
        int n = Math.Min(limit, Math.Min(AmountOf(a.X, a.Y, a.Material), AmountOf(b.X, b.Y, b.Material)));
        if (same && a.Material == b.Material) n = Math.Min(n, AmountOf(a.X, a.Y, a.Material) / 2);
        if (n <= 0) return 0;
        var first = new Dictionary<string,int>(cells[(a.X,a.Y)], StringComparer.Ordinal);
        var second = same ? first : new Dictionary<string,int>(cells[(b.X,b.Y)], StringComparer.Ordinal);
        Change(first, a.Material, -n); Change(second, b.Material, -n);
        Change(first, outputA, n); Change(second, outputB, n);
        if (first.Count > MaximumMaterialsPerCell || second.Count > MaximumMaterialsPerCell) return 0;
        Put(a.X,a.Y,first); if (!same) Put(b.X,b.Y,second); return n;
    }
    public int Transform(MaterialCell cell, string output, int limit)
    {
        int n = Math.Min(limit, AmountOf(cell.X, cell.Y, cell.Material)); if (n <= 0) return 0;
        var next = new Dictionary<string,int>(cells[(cell.X,cell.Y)], StringComparer.Ordinal);
        Change(next,cell.Material,-n); Change(next,output,n);
        if (next.Count > MaximumMaterialsPerCell) return 0;
        Put(cell.X,cell.Y,next); return n;
    }
    private static void Change(Dictionary<string,int> amounts, string material, int delta)
    { if (material == "air" || delta == 0) return; amounts[material] = amounts.GetValueOrDefault(material) + delta; if (amounts[material] <= 0) amounts.Remove(material); }
    private void Put(int x, int y, Dictionary<string,int> next)
    { if (next.Count == 0) cells.Remove((x,y)); else cells[(x,y)] = next; Wake(x,y); }
    public int ImportNative(int x, int y, string material, int amount, Func<int,int,bool> blocked) => Add(x,y,material,amount,blocked,allowPressure:true);
    public int Scoop(int x, int y, FlaskContents flask, int amount, Func<string,bool> scoopable)
    {
        var layers=LayersAt(x,y).Where(c=>scoopable(c.Material)).ToArray();
        int total=layers.Sum(c=>c.Amount),quota=Math.Max(0,Math.Min(amount,FlaskContents.Capacity-flask.Total)),remaining=quota;
        foreach(var cell in layers)
        {
            int share=Math.Min(remaining,(int)Math.Ceiling(quota*(double)cell.Amount/Math.Max(1,total)));
            int taken=flask.Add(cell.Material,Math.Min(share,cell.Amount));Take(x,y,taken,cell.Material);remaining-=taken;
        }
        return quota-remaining;
    }
}
