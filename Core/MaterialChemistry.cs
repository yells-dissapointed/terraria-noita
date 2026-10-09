#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace terrarianoita.Core;

public sealed record MaterialReaction(string Id, string InputA, string InputB, string OutputA, string OutputB, double Probability, string Deferred = "");
public sealed record MaterialReactionOutcome(string Id, string OutputA, string OutputB, double Probability, bool Adapted);

/// <summary>Binary contact reactions from the user's materials.xml. Rates are adapted to coarse cells;
/// three-input, timed, directional, blob and entity-producing recipes remain explicit gaps.</summary>
public sealed class MaterialChemistry
{
    private readonly NoitaMaterialCatalog catalog;
    private readonly bool adapted;
    private readonly Dictionary<(string,string), MaterialReactionOutcome[]> cache = new();
    private int cursor;
    public MaterialReaction[] Definitions { get; }
    public int ImportedDefinitions => Definitions.Length;
    public int EligibleDefinitions => Definitions.Count(r => r.Deferred.Length == 0);
    public long ReactionsApplied { get; private set; }
    public string LastReaction { get; private set; } = "none";
    public MaterialChemistry(NoitaMaterialCatalog catalog, bool adapted = true)
    {
        this.catalog = catalog; this.adapted = adapted;
        Definitions = catalog.ReactionNodes.Select((n,i) => {
            string[] unsupported = { "input_cell3", "output_cell3", "req_lifetime", "direction", "entity", "blob_radius1", "blob_radius2", "destroy_horizontally_lonely_pixels" };
            string gap = string.Join(", ", unsupported.Where(k => n.Get(k).Length > 0 && n.Get(k) != "0"));
            return new MaterialReaction("xml:" + (i+1), n.Get("input_cell1"), n.Get("input_cell2"), n.Get("output_cell1"), n.Get("output_cell2"), Math.Clamp(n.Number("probability"),0,100), gap);
        }).ToArray();
    }
    private bool Matches(string selector, string name)
    {
        if (!selector.StartsWith("[", StringComparison.Ordinal))
            return selector == name || catalog.Get(name).ReactionParents.Contains(selector, StringComparer.Ordinal);
        int end = selector.IndexOf(']'); if (end < 0) return false;
        string suffix = selector[(end+1)..];
        if (!name.EndsWith(suffix, StringComparison.Ordinal)) return false;
        string basis = suffix.Length == 0 ? name : name[..^suffix.Length];
        return catalog.Get(basis).Has(selector[1..end]);
    }
    private string? Output(string pattern, MaterialReaction rule, string a, string b, int slot)
    {
        if (!pattern.StartsWith("[", StringComparison.Ordinal)) return pattern;
        int end = pattern.IndexOf(']'); if (end < 0) return null;
        string tag = pattern[..(end+1)], suffix = pattern[(end+1)..];
        var inputs = slot == 0 ? new[] { (rule.InputA,a), (rule.InputB,b) } : new[] { (rule.InputB,b), (rule.InputA,a) };
        foreach (var (selector, name) in inputs)
            if (selector.StartsWith(tag, StringComparison.Ordinal))
            { string oldSuffix = selector[tag.Length..]; if (name.EndsWith(oldSuffix, StringComparison.Ordinal)) return (oldSuffix.Length == 0 ? name : name[..^oldSuffix.Length]) + suffix; }
        return null; // Unbound output tags are not guessed.
    }
    private bool ValidOutput(string? output, string original) => output != null && (output == original || output == "air" ||
        catalog.Materials.TryGetValue(output, out var material) && MaterialGrid.Mobile(material));
    public MaterialReactionOutcome[] Available(string a, string b)
    {
        if (cache.TryGetValue((a,b), out var old)) return old;
        var result = new List<MaterialReactionOutcome>();
        if (adapted)
        {
            if ((a == "acid" && b == "water") || (a == "water" && b == "acid"))
                result.Add(new("adapter:acid-dilution", "noita_diluted_acid", "noita_diluted_acid",100,true));
            if (a == "water" && catalog.Get(b).Kind == "fire") result.Add(new("adapter:extinguish", "steam", "air",100,true));
            if (b == "water" && catalog.Get(a).Kind == "fire") result.Add(new("adapter:extinguish", "air", "steam",100,true));
        }
        foreach (var rule in Definitions.Where(r => r.Deferred.Length == 0 && r.Probability > 0))
        {
            bool forward = Matches(rule.InputA,a) && Matches(rule.InputB,b);
            bool reverse = !forward && Matches(rule.InputA,b) && Matches(rule.InputB,a);
            if (!forward && !reverse) continue;
            string first = forward ? a : b, second = forward ? b : a;
            string? outFirst = Output(rule.OutputA,rule,first,second,0), outSecond = Output(rule.OutputB,rule,first,second,1);
            if (!ValidOutput(outFirst, first) || !ValidOutput(outSecond, second)) continue;
            string outA = forward ? outFirst! : outSecond!, outB = forward ? outSecond! : outFirst!;
            if (outA == a && outB == b) continue;
            result.Add(new(rule.Id,outA,outB,rule.Probability,false));
        }
        if (adapted && result.Count == 0)
        {
            if (catalog.Get(a).Flammable && catalog.Get(b).Hot) result.Add(new("adapter:ignition","fire",b,25,true));
            else if (catalog.Get(b).Flammable && catalog.Get(a).Hot) result.Add(new("adapter:ignition",a,"fire",25,true));
        }
        var rows = result.ToArray(); if (cache.Count < 4096) cache[(a,b)] = rows; return rows;
    }
    private static double Roll(long frame, int x, int y, int ordinal)
    {
        uint n = unchecked((uint)(frame / 15) * 747796405u + (uint)x * 2891336453u + (uint)y * 277803737u + (uint)ordinal * 1597334677u);
        n = (n ^ (n >> 16)) * 2246822519u; n ^= n >> 13; return (n % 10000) / 100d;
    }
    public int Apply(MaterialGrid grid, MaterialCell a, MaterialCell b, MaterialReactionOutcome outcome, int amount = 16)
    {
        int changed = grid.TransformPair(a,b,outcome.OutputA,outcome.OutputB,amount);
        if (changed > 0) { ReactionsApplied++; LastReaction = outcome.Id + ": " + a.Material + " + " + b.Material + " -> " + outcome.OutputA + " + " + outcome.OutputB; }
        return changed;
    }
    public void Step(MaterialGrid grid, Func<int,int,bool> blocked, long frame, int budget = 64)
    {
        var positions = grid.Positions.ToArray(); if (positions.Length == 0) return;
        int work = Math.Min(Math.Max(0,budget),positions.Length);
        for (int i=0;i<work;i++)
        {
            var p = positions[(cursor+i)%positions.Length]; if (blocked(p.X,p.Y)) continue;
            var own = grid.LayersAt(p.X,p.Y);
            for (int a=0;a<own.Length;a++)
                for (int b=a+1;b<own.Length;b++) Contact(own[a],own[b]);
            foreach (var target in new[] { (p.X+1,p.Y), (p.X,p.Y+1) })
                if (!blocked(target.Item1,target.Item2))
                    foreach (var a in own) foreach (var b in grid.LayersAt(target.Item1,target.Item2)) Contact(a,b);
            foreach (var cell in own)
            {
                bool wall = blocked(p.X-1,p.Y) || blocked(p.X+1,p.Y) || blocked(p.X,p.Y-1) || blocked(p.X,p.Y+1);
                bool air = new[] { (p.X-1,p.Y),(p.X+1,p.Y),(p.X,p.Y-1),(p.X,p.Y+1) }.Any(n => !blocked(n.Item1,n.Item2) && grid.AmountAt(n.Item1,n.Item2)==0);
                if (wall) Catalyst(cell,"rock_static");
                if (air) Catalyst(cell,"air");
            }
        }
        cursor = (cursor+work)%positions.Length;
        void Contact(MaterialCell a, MaterialCell b)
        {
            int ordinal = 0;
            foreach (var outcome in Available(a.Material,b.Material))
                if (Roll(frame,a.X+b.X,a.Y+b.Y,++ordinal)<outcome.Probability && Apply(grid,a,b,outcome)>0) break;
        }
        void Catalyst(MaterialCell cell, string other)
        {
            int ordinal = 100;
            foreach (var outcome in Available(cell.Material,other))
                if (outcome.OutputB==other && Roll(frame,cell.X,cell.Y,++ordinal)<outcome.Probability)
                { if (grid.Transform(cell,outcome.OutputA,8)>0) { ReactionsApplied++; LastReaction=outcome.Id+": "+cell.Material+" -> "+outcome.OutputA; } break; }
        }
    }
}
