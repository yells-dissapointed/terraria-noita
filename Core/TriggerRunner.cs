#nullable enable
using System;
using System.Collections.Generic;

namespace terrarianoita.Core;

public enum TriggerSignal { Tick, Impact, Death }

/// <summary>Executes already-built payloads once; never resumes wand interpretation.</summary>
public sealed class TriggerRunner
{
    private readonly IReadOnlyList<TriggerPlan> triggers;
    private readonly bool[] fired;
    public TriggerRunner(IReadOnlyList<TriggerPlan> triggers)
    {
        this.triggers = triggers;
        fired = new bool[triggers.Count];
    }
    public void Observe(TriggerSignal signal, int ageFrames, Action<ShotPlan> emit)
    {
        for (int i = 0; i < triggers.Count; i++)
        {
            var trigger = triggers[i];
            bool matches = trigger.Kind switch {
                "hit_world" => signal == TriggerSignal.Impact,
                "death" => signal == TriggerSignal.Death,
                "timer" => signal == TriggerSignal.Tick && ageFrames >= Math.Ceiling(trigger.DelayFrames),
                _ => throw new NotSupportedException("Unsupported trigger kind: " + trigger.Kind)
            };
            if (!fired[i] && matches)
            {
                fired[i] = true; // Before emission: callbacks may re-enter through a death event.
                emit(trigger.Payload);
            }
        }
    }
}
