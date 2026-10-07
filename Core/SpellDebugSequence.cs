#nullable enable
using System;
using System.Linq;

namespace terrarianoita.Core;

public enum SpellDebugMode { FollowedBySparks, Solo, AlwaysCast, AllContexts }

/// <summary>Finite deterministic scan shared by manual stepping and automatic live testing.</summary>
public sealed class SpellDebugSequence
{
    private readonly string[] ids;
    public SpellDebugMode Mode { get; private set; } = SpellDebugMode.FollowedBySparks;
    public int Index { get; private set; }
    public int Total => ids.Length * (Mode == SpellDebugMode.AllContexts ? 3 : 1);
    public bool Complete => Index >= Total;
    public int ContextIndex => Mode switch { SpellDebugMode.Solo => 0, SpellDebugMode.AlwaysCast => 2, SpellDebugMode.AllContexts => Index % 3, _ => 1 };
    public string Spell => ids[Math.Min(Mode == SpellDebugMode.AllContexts ? Index / 3 : Index, ids.Length - 1)];
    public SpellDebugSequence(string[] spellIds)
    {
        if (spellIds.Length == 0 || spellIds.Distinct().Count() != spellIds.Length) throw new ArgumentException("Invalid spell catalog");
        ids = spellIds.ToArray();
    }
    public SpellAuditCase Current() => !Complete ? SpellAudit.CreateCase(Spell, ContextIndex) : throw new InvalidOperationException("Spell scan complete; restart to run again.");
    public void Advance(bool holdSpell = false) { if (!holdSpell && !Complete) Index++; }
    public void Move(int delta) => Index = Math.Clamp(Index + delta, 0, Total - 1);
    public void Restart() => Index = 0;
    public void Select(string spell)
    {
        int index = Array.IndexOf(ids, spell);
        if (index < 0) throw new ArgumentException("Unknown spell: " + spell);
        Index = index * (Mode == SpellDebugMode.AllContexts ? 3 : 1);
    }
    public void SetMode(SpellDebugMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        Mode = mode; Restart();
    }
}
