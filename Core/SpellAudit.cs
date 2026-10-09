#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace terrarianoita.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SpellAuditStatus { DemoCandidate, DemoWithGaps, Unsupported, ScriptError, NoProjectile, NotExercised }

public sealed class SpellAuditCase
{
    public string Spell { get; set; } = "";
    public string Context { get; set; } = "";
    public List<string> Deck { get; set; } = new();
    public List<string> AlwaysCast { get; set; } = new();
    public SpellAuditStatus Status { get; set; }
    public string Error { get; set; } = "";
    public string RuntimeVersion { get; set; } = "";
    public DemoInspection? Inspection { get; set; }
    public CastPlan? Plan { get; set; }
}

public sealed class SpellAuditReport
{
    public int Version { get; set; } = 1;
    public string RendererProfile { get; set; } = DemoCapabilities.Profile;
    public string Limits { get; set; } = "Fresh non-shuffle states, unlimited card uses, 10000 mana, one cast per context. No collision, rendering, inventory, unlock, perk or world simulation. Candidate does not mean full Noita support.";
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public bool Complete { get; set; }
    public int SpellCount { get; set; }
    public int ExpectedCases => SpellCount * 3;
    public string RuntimeVersion { get; set; } = "";
    public List<SpellAuditCase> Cases { get; set; } = new();
    public Dictionary<string, int> Counts => Cases.GroupBy(c => c.Status.ToString()).ToDictionary(g => g.Key, g => g.Count());
    public int CandidateSpells => Cases.Where(c => c.Status is SpellAuditStatus.DemoCandidate or SpellAuditStatus.DemoWithGaps).Select(c => c.Spell).Distinct().Count();
    public string Json() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}

/// <summary>One isolated cast per Step, on the caller's thread. Never emits Terraria projectiles.</summary>
public sealed class SpellAudit
{
    private readonly string[] ids;
    private readonly Func<Lua51Runtime> create;
    private readonly Action<Lua51Runtime> release;
    private readonly IReadOnlyDictionary<string, JsonElement> defaults;
    public SpellAuditReport Report { get; }
    public bool Done => Report.Cases.Count == Report.ExpectedCases;
    public SpellAudit(string[] spellIds, Func<Lua51Runtime> factory, IReadOnlyDictionary<string, JsonElement> neutralConfig, Action<Lua51Runtime>? dispose = null)
    {
        if (spellIds.Length == 0 || spellIds.Distinct().Count() != spellIds.Length) throw new ArgumentException("Invalid spell catalog");
        ids = spellIds.ToArray(); create = factory; defaults = neutralConfig;
        release = dispose ?? (r => r.Dispose()); Report = new() { SpellCount = ids.Length };
    }
    public bool Step()
    {
        if (Done) return false;
        int index = Report.Cases.Count, context = index % 3;
        var result = RunCase(ids[index / 3], context, create, defaults, release);
        Report.RuntimeVersion = result.RuntimeVersion;
        Report.Cases.Add(result); Report.Complete = Done;
        return true;
    }
    public static SpellAuditCase CreateCase(string spell, int context)
    {
        if (context is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(context));
        var result = new SpellAuditCase { Spell = spell, Context = new[] { "Solo", "Followed by sparks", "Always cast" }[context] };
        if (context != 2) result.Deck.Add(result.Spell);
        if (context != 0) result.Deck.AddRange(Enumerable.Repeat("LIGHT_BULLET", 4));
        if (context == 2) result.AlwaysCast.Add(result.Spell);
        return result;
    }
    public static SpellAuditCase RunCase(string spell, int context, Func<Lua51Runtime> create,
        IReadOnlyDictionary<string, JsonElement> defaults, Action<Lua51Runtime>? dispose = null, IReadOnlyList<string>? modifiers = null, CastHostContext? contextSnapshot = null)
    {
        var result = CreateCase(spell, context);
        if (modifiers != null) result.Deck.InsertRange(0, modifiers);
        // Factory failures are infrastructure failures: stop, rather than falsely blame every spell.
        var runtime = create();
        try
        {
            result.RuntimeVersion = runtime.RuntimeVersion;
            runtime.Configure(new(result.Deck.Select(id => new SpellSlot(id)).ToArray()));
            result.Plan = runtime.Cast(10000, result.AlwaysCast, contextSnapshot);
            result.Inspection = DemoCapabilities.Inspect(result.Plan, defaults);
            bool exercised = result.Plan.Events.Any(e => e.Kind == "action" && e.Value.GetString() == result.Spell);
            result.Status = !exercised ? SpellAuditStatus.NotExercised : result.Inspection.Entities.Any(e => !DemoCapabilities.SupportsEntity(e)) ? SpellAuditStatus.Unsupported :
                result.Inspection.ProjectileCount == 0 ? SpellAuditStatus.NoProjectile : result.Inspection.Missing.Count > 0 ? SpellAuditStatus.DemoWithGaps : SpellAuditStatus.DemoCandidate;
        }
        catch (Exception e) { result.Status = SpellAuditStatus.ScriptError; result.Error = e.Message; }
        finally { if (dispose != null) dispose(runtime); else runtime.Dispose(); }
        return result;
    }
}
