# Spell audit — Terraria Noita v0.3.0

Historical baseline only. For current v0.8.0 cast and adapter coverage, see
[EXPANSION_COVERAGE.md](EXPANSION_COVERAGE.md).

The reference counts below were reproduced unchanged in v0.5.0. They measure
the conservative three-entity gameplay adapter. The separate XML/sprite preview
now loads all 197 entity paths emitted by these casts: 93 with sprite metadata
and 104 labeled markers. This visualization does not change native support
classifications. See [DEBUG_WAND.md](DEBUG_WAND.md) for current tests and limits.

All 422 spell IDs from the supplied original Noita action table were exercised in
three independent contexts: solo, followed by four spark bolts, and as an
always-cast with four spark bolts. Each context uses a fresh LuaJIT 2.0.4 state,
10000 mana, unlimited card uses and a non-shuffle wand. Nothing is spawned.

| Cast outcome | Cases |
| --- | ---: |
| Drawable entity tree with missing effects | 384 |
| Unsupported projectile entities | 627 |
| Native API/script error | 66 |
| No projectile (often a utility card) | 189 |
| Total | 1266 |

190 distinct spells have at least one drawable test setup. None of those setups
is a complete Noita implementation. The default spark itself has ignored
critical chance and screen shake. The JSON records the exact missing config
fields, effect entities and APIs in every context, including nested payloads.

Native failures include RNG (30 cases), GetUpdatedEntityID (24), EntityGetWithTag
(9) and GlobalsGetValue (3). These counts describe cases, not unique spells.

This audit checks script execution and the current adapter's declared entity and
configuration coverage. It does not run rendering, collisions, native physics,
world conditions, spell acquisition, perks, unlocks or multiplayer. One cast in
three contexts does not exhaust arbitrary combinations or repeated deck state.
NoProjectile is an observation, not a failure verdict. A utility can work only
when another card follows it. DemoWithGaps means the adapter can draw the known
entity paths while omitting listed effects; it does not mean the spell works fully.

## Use the tools

Right-click the wand, then click **Audit all**. Keep the editor open while one
isolated case runs each frame. **Stop audit** retains the partial report;
**Audit all** then starts over. **Results** switches the right-hand panel between
audit results and the cast log. Click a result to load its best tested context
into a draft and run a preview. Review the gaps before clicking **Apply**.
Closing the editor cancels the run and discards its in-memory report.

A completed in-game audit automatically saves JSON to
`<tModLoader save folder>/terrarianoita/debug/`. **Save audit** can export a partial
or completed report; the file path is printed in chat. No test changes live mana,
live deck state or world projectiles. Test drafts now also list ignored effects.

To reproduce outside Terraria with .NET 8, from the repository root:

```sh
dotnet run --project Tools/SpellAudit -- Native/libterrarianoita_lua51.so /absolute/path/to/extracted-noita Compatibility/bridge.lua spell-audit-results.json
```

On Windows, use `Native/terrarianoita_lua51.dll` as the native library argument.

## Graphics fix and validation

Spark glow/core and chainsaw bars now normalize scale by the actual MagicPixel
texture dimensions and use its center as their origin. A spark stays 12x8 world
pixels, with a 6x3 core; chainsaw bars stay 28x3. A texture larger than 1x1 no
longer stretches the effect into a screen-spanning stripe.

92 standalone checks pass, including geometry across four texture sizes, audit
classification, native errors, utility follow-ups and nested payload analysis.
Four packaged-mod checks cover saved wand data, clone isolation, chainsaw
hitboxes and actual compiled draw geometry. tModLoader 2026.08.3.0 builds v0.3.0
with zero errors and warnings. A graphical Terraria client playtest is still
needed to confirm how the corrected graphics and audit UI look in-game.
