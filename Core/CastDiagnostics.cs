#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace terrarianoita.Core;

public sealed class CastDiagnostics
{
    public CastPlan? Plan { get; private set; }
    public WandDefinition? Definition { get; private set; }
    public int Version { get; private set; }
    public List<string> Lines { get; } = new();
    private readonly List<string> runtimeEvents = new();
    private string label = "No cast recorded", error = "";
    private double manaBefore;
    public void Begin(string title, WandDefinition definition, double mana)
    {
        label = title; Definition = definition.Copy(); manaBefore = mana;
        Plan = null; error = ""; runtimeEvents.Clear(); Rebuild();
    }
    public void Capture(CastPlan plan) { Plan = plan; Rebuild(); }
    public void Fail(string message) { error = message; Rebuild(); }
    public void Event(string message)
    {
        if (runtimeEvents.Count >= 64) runtimeEvents.RemoveAt(0);
        runtimeEvents.Add(message); Rebuild();
    }
    private void Rebuild()
    {
        Lines.Clear(); Lines.Add(label);
        if (error.Length > 0) Lines.Add("ERROR: " + error);
        if (Plan is { } plan)
        {
            Lines.Add($"Mana {manaBefore:0.##} → {plan.Mana:0.##} | reload request {plan.ReloadRequest?.ToString("0.##") ?? "none"} frames");
            foreach (var e in plan.Events)
            {
                if (e.Kind == "action_mana") Lines.Add("Draw " + e.Value.GetProperty("id").GetString() + " | mana " + e.Value.GetProperty("mana").GetDouble().ToString("0.##"));
                else if (e.Kind == "insufficient_mana") Lines.Add("Skipped a card: insufficient mana");
                else if (e.Kind == "uses_changed") Lines.Add("Uses changed: " + e.Value.GetRawText());
            }
            Lines.Add("Remaining deck: " + string.Join(", ", plan.Deck));
            Lines.Add("Discard: " + string.Join(", ", plan.Discarded));
            DescribeShot(plan.Root, "Root", 0);
        }
        foreach (string e in runtimeEvents) Lines.Add(e);
        Version++;
    }
    private void DescribeShot(ShotPlan shot, string name, int depth)
    {
        if (Lines.Count >= 240) return;
        string prefix = new(' ', depth * 2);
        Lines.Add(prefix + name + $": {shot.Projectiles.Count} projectile(s), delay {shot.Number("fire_rate_wait"):0.##}");
        Lines.Add(prefix + $"Damage add {shot.Number("damage_projectile_add"):0.##}, slice add {shot.Number("damage_slice_add"):0.##}, speed ×{shot.Number("speed_multiplier", 1):0.##}");
        Lines.Add(prefix + $"Spread {shot.Number("spread_degrees"):0.##}°, lifetime add {shot.Number("lifetime_add"):0.##}");
        foreach (var node in shot.Projectiles)
        {
            if (Lines.Count >= 240) { Lines.Add("Tree display truncated; save the JSON log for the complete plan."); return; }
            Lines.Add(prefix + "  Entity: " + Path.GetFileName(node.Entity));
            foreach (var trigger in node.Triggers)
                DescribeShot(trigger.Payload, trigger.Kind + (trigger.Kind == "timer" ? $" at {trigger.DelayFrames:0.##} frames" : ""), depth + 1);
        }
    }
    public string Json() => JsonSerializer.Serialize(new {
        label, error, mana_before = manaBefore, definition = Definition, plan = Plan,
        runtime_events = runtimeEvents, notes = "Terraria renderer and scheduling are approximate. Unsupported entities are diagnostic only."
    }, new JsonSerializerOptions { WriteIndented = true });
}
