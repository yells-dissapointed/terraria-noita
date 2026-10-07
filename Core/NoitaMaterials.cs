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
    public bool Scoopable => Kind == "liquid" && !Sand;
    public bool Has(string tag) => Tags.Contains("[" + tag + "]", StringComparison.Ordinal);
}

/// <summary>Original material names, inheritance, color and status metadata. Native chemical reaction tables are retained on disk, not executed.</summary>
public sealed class NoitaMaterialCatalog
{
    private readonly Dictionary<string, NoitaMaterial> materials = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, NoitaMaterial> Materials => materials;
    public NoitaMaterialCatalog(string root)
    {
        var source = NoitaXml.Parse(File.ReadAllText(Path.Combine(NoitaDataPaths.ResolveRoot(root), "data/materials.xml")), new());
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
            string color = node.Children.FirstOrDefault(c => c.Name == "Graphics")?.Get("color") ?? node.Get("wang_color", "ff888888");
            if (!uint.TryParse(color, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgba)) rgba = 0xff888888;
            materials[name] = new() { Name = name, Kind = node.Get("cell_type", "solid"), Tags = node.Get("tags"), Status = node.Get("status_effects"), Color = rgba,
                Density = node.Number("density", 4), Sand = node.Flag("liquid_sand"), Flammable = node.Flag("burnable", name == "alcohol" || node.Get("tags").Contains("[burnable]")), Hot = node.Flag("on_fire") || name == "lava" };
        }
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

/// <summary>Sparse 16-pixel cells with conserved volume, bounded updates and deterministic flow. Different materials retain their identity.</summary>
public sealed class MaterialGrid
{
    public const int CellCapacity = 255, MaximumCells = 32768;
    private readonly Dictionary<(int X, int Y), MaterialCell> cells = new();
    private readonly Queue<(int X, int Y)> queue = new();
    private readonly HashSet<(int X, int Y)> queued = new();
    public IEnumerable<MaterialCell> Cells => cells.Values;
    public int Count => cells.Count;
    public long Volume => cells.Values.Sum(c => (long)c.Amount);
    public MaterialCell? At(int x, int y) => cells.TryGetValue((x, y), out var cell) ? cell : null;
    private void Wake(int x, int y)
    {
        // Empty cells need not occupy the queue; changes wake existing neighbours.
        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
        { var key = (x + dx, y + dy); if (cells.ContainsKey(key) && queued.Add(key)) queue.Enqueue(key); }
    }
    public int Add(int x, int y, string material, int amount, Func<int, int, bool> blocked)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(material) || material.Length > 128 || blocked(x, y)) return 0;
        var key = (x, y); bool exists = cells.TryGetValue(key, out var old);
        if (exists && old.Material != material || !exists && cells.Count >= MaximumCells) return 0;
        int accepted = Math.Min(amount, CellCapacity - old.Amount);
        if (accepted > 0) { cells[key] = new(x, y, material, old.Amount + accepted); Wake(x, y); }
        return accepted;
    }
    public int Take(int x, int y, int amount)
    {
        if (!cells.TryGetValue((x, y), out var old)) return 0;
        int removed = Math.Min(Math.Max(0, amount), old.Amount);
        if (removed > 0)
        {
            if (removed == old.Amount) cells.Remove((x, y)); else cells[(x, y)] = old with { Amount = old.Amount - removed };
            Wake(x, y);
        }
        return removed;
    }
    public void WakeAll() { foreach (var key in cells.Keys) if (queued.Add(key)) queue.Enqueue(key); }
    public int Step(Func<int, int, bool> blocked, Func<string, NoitaMaterial> material, int budget, long frame)
    {
        int steps = Math.Min(Math.Max(0, budget), queue.Count);
        for (int i = 0; i < steps; i++)
        {
            var key = queue.Dequeue(); queued.Remove(key);
            if (!cells.TryGetValue(key, out var cell) || blocked(cell.X, cell.Y)) continue;
            var meta = material(cell.Material); if (meta.Kind is not ("liquid" or "gas" or "fire")) continue;
            int dy = meta.Kind is "gas" or "fire" ? -1 : 1;
            Move(cell.X, cell.Y, cell.X, cell.Y + dy, CellCapacity, blocked);
            if (meta.Sand) continue;
            int side = ((frame + cell.X + cell.Y) & 1) == 0 ? -1 : 1;
            for (int j = 0; j < 2; j++, side = -side)
            {
                if (!cells.TryGetValue(key, out cell)) break;
                int adjacent = At(cell.X + side, cell.Y)?.Amount ?? 0;
                Move(cell.X, cell.Y, cell.X + side, cell.Y, Math.Max(0, (cell.Amount - adjacent) / 2), blocked);
            }
        }
        return steps;
    }
    private void Move(int x, int y, int tx, int ty, int limit, Func<int, int, bool> blocked)
    {
        if (!cells.TryGetValue((x, y), out var source) || limit <= 0) return;
        int moved = Add(tx, ty, source.Material, Math.Min(source.Amount, limit), blocked);
        Take(x, y, moved);
    }
}
