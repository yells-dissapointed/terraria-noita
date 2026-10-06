#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace terrarianoita.Core;

public sealed class WandDefinition
{
    public List<string> Deck { get; set; } = new() { "DAMAGE", "LIGHT_BULLET" };
    public List<string> AlwaysCast { get; set; } = new();
    public double CastDelay { get; set; } = 10;
    public double ReloadTime { get; set; } = 40;
    public int ActionsPerRound { get; set; } = 1;
    public double ManaMax { get; set; } = 100;
    public double ManaRecharge { get; set; } = 50;
    public WandDefinition Copy() => new() {
        Deck = new(Deck), AlwaysCast = new(AlwaysCast), CastDelay = CastDelay,
        ReloadTime = ReloadTime, ActionsPerRound = ActionsPerRound,
        ManaMax = ManaMax, ManaRecharge = ManaRecharge
    };
    public void Validate()
    {
        if (Deck == null || AlwaysCast == null || Deck.Count == 0 || Deck.Count > 64 || AlwaysCast.Count > 8)
            throw new ArgumentException("Use 1–64 deck slots and at most 8 always-cast slots.");
        foreach (string id in Deck.Concat(AlwaysCast))
            if (string.IsNullOrWhiteSpace(id) || id.Length > 80 || id.Any(c => !(c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')))
                throw new ArgumentException("Invalid spell ID: " + id);
        if (!double.IsFinite(CastDelay) || CastDelay < 0 || CastDelay > 600 ||
            !double.IsFinite(ReloadTime) || ReloadTime < 0 || ReloadTime > 600 ||
            ActionsPerRound < 1 || ActionsPerRound > 16 ||
            !double.IsFinite(ManaMax) || ManaMax < 1 || ManaMax > 10000 ||
            !double.IsFinite(ManaRecharge) || ManaRecharge < 0 || ManaRecharge > 10000)
            throw new ArgumentException("Invalid wand stats: delays 0–600 frames, draw 1–16, mana 1–10000, recharge 0–10000.");
    }
    public WandConfiguration Configuration()
    {
        Validate();
        return new(Deck.Select(id => new SpellSlot(id)).ToArray(), CastDelay, ReloadTime, ActionsPerRound);
    }
}
