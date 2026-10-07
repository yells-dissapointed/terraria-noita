# Terraria Noita wand prototype

This prototype executes the original wand Lua scripts from your own extracted
Noita installation inside tModLoader. The C# bridge receives a completed cast
plan and renders a small demonstration in Terraria. Original Noita code and
assets are not included in this repository.

v0.5.1 enlarges spell sprites and fallback visuals to 1.75× around their existing
origins. Projectile positions and gameplay properties are unchanged. See
[DEBUG_WAND.md](DEBUG_WAND.md) for the live-report findings and focused tests.

## Try it

1. Clone this branch into your tModLoader `ModSources` folder, naming the folder
   **`terrarianoita`**. The folder name is the mod's internal name:
   ```sh
   git clone -b feature/noita-lua-adapter https://github.com/yells-dissapointed/terraria-noita.git terrarianoita
   ```
2. Extract the previously supplied `Noita-extracted-data.zip` somewhere on your
   computer. Find the folder that directly contains `data`.
3. In tModLoader's **Workshop → Develop Mods**, build and reload **Terraria Noita**.
4. In this mod's settings, set **Extracted Noita folder** to that folder's absolute
   path. You can also select the `data`, `scripts`, or `gun` folder, or `gun.lua`
   itself. The mod resolves the extracted root and checks all required scripts.
   Save and reload the mod.
5. Enter a **single-player** world. Craft the Noita Wand using **10 dirt blocks at
   a workbench**. Left-click to fire and right-click to open the wand editor / cast log.

Windows x64 and Linux x64 LuaJIT libraries are included. macOS requires building
an appropriate native library; it has not been validated. See [Native/README.md](Native/README.md).
The wand starts with its own 100 mana pool, displayed in its tooltip.

## Editor and debugging (v0.5.0)

Right-click while holding a wand. Changes are made to a draft until you click
**Apply**. Choose spells from the palette to append slots; use the up/down buttons
to reorder them, **X** to remove them, and **Always / To deck** to move cards
between the regular deck and always-cast slots. Edit cast delay, reload frames,
actions per cast, mana capacity and mana recharge with the plus/minus controls.
Applied deck order, always-cast cards, stats, mana and debug preference are saved
with each item. Closing or switching items discards unapplied edits.

The default palette contains 19 checked spell IDs using spark, blue spark and
chainsaw entity paths. **All / experimental** exposes the complete original
422-spell table. Other spells may fail because their native APIs or entity
renderers are unavailable. This is a development editor with unrestricted card
selection, not a spell acquisition or inventory system.

**Test draft** starts a separate, fresh Lua state with the draft's full mana
capacity and records a cast without
spending the live wand's mana, advancing its live deck or spawning projectiles.
Use the cast log to inspect draw order, per-action mana, skipped cards, reload
requests, shot modifiers and nested trigger payloads. A supported entity path
does not imply every native component or modifier effect has been replicated.
**Older / Newer** inspect the last eight tests or live casts. **Save log** writes
the complete cast plan and collision/trigger events as JSON under
`<tModLoader save folder>/terrarianoita/debug/`; the exact path is printed in chat.

**Debug: ON** shows a small live HUD and yellow projectile hitbox outlines. Sparks
are drawn at their hitbox centers and originate 16 pixels from the player's hand,
or at the hand if a wall blocks that offset. Chainsaw is a visible stationary
28×28 NPC damage area just in front of the muzzle, lasting at least the demo's
base 8 frames before lifetime modifiers. It does not dig Terraria blocks or
replicate Noita's terrain effects.

## Separate live debugging wand (v0.5.0)

Craft **Spell Debug Wand** with **one dirt block at a workbench**. It is a cyan
variant of the wand. Left-click casts the selected test case and advances to the
next spell. Right-click opens a compact scan panel with Previous/Next, Test
current, Start/Stop auto, context, interval, Restart, Clear shots and Save report.
Automatic mode waits 1, 2, 5 or 10 seconds between cases and stops after one scan.
Switching away stops auto; opening inventory/chat pauses it. Auto is never
restored from saved items or clones.

The default context adds four spark bolts after the tested spell, so modifiers
and trigger payloads have something to act on. Choose Solo, Always cast or All
three contexts to inspect different behavior. Every case starts with a fresh
non-shuffle deck, unlimited uses and 10000 test mana. The ordinary wand's mana
and deck are unaffected. **XML/sprite preview (harmless)** is the default: it
loads original local sprites and basic XML motion/lifetime settings for emitted
entities. Missing graphics use orange labeled entity markers. Cyan diagnostic
cards identify no output, Lua errors or unexercised targets and do not count as
emitted roots. Preset buttons make targeted rendering/trigger tests easy.
**Gameplay demo** mode retains the three existing damage adapters.

Before each case, the tool removes its own previous projectiles and suppresses
cleanup-triggered payloads. Longer lifetimes/timers may therefore be truncated
by the chosen interval. Live JSON reports include attempted recipes, missing
effects, actual root spawn counts, separate diagnostic-card counts, full imported
XML source nodes, per-entity profiles/visual evidence, later collision/trigger events, the loaded
mod version and its file path. Reports are saved under
`<tModLoader save folder>/terrarianoita/debug/`. Auto completion saves after the
last observation interval; Save report also works during or after a run.

Both wand tooltips, the editor heading and the live debug HUD show the loaded
version. **Loaded file** prints the exact active `.tmod` path in chat. The
startup log also records version, build ID and loaded file. If the game still
shows 0.2.0, it is not running this package. Close tModLoader, replace the package
in the folder opened by **Mods → Open Mods Folder**, and restart. Building an
older `ModSources` checkout later recreates its older package; update that
checkout before using Build + Reload.

## What is imported, and what still needs implementation

The host imports original Noita **wand/spell Lua**, complete entity XML source
nodes/attributes and original PNG/sprite-sheet metadata from local extracted
files. The importer resolves bases, component overrides/removal and retains
unmapped data. The debug wand visualizes emitted entities using original sprites
or clearly labeled markers, approximate speed/gravity/drag/lifetime, and
already-built trigger payloads. The ordinary wand also uses original sprites
for its three supported entity paths, keeping its existing gameplay adapter.

The supplied-data audit loaded **197 emitted entity definitions**, **93 with
usable sprite metadata** and **104 marker-only**. This establishes data/frame
coverage, not GPU rendering or native behavior. See [DEBUG_WAND.md](DEBUG_WAND.md)
for useful in-game tests and report meanings. The native engine and most
component behavior are deferred: explosions, terrain/materials, homing,
statuses, particles, physics bodies, audio and entity Lua scripts need further
implementation. Merely loading a definition does not execute those systems.

## Automatic spell audit

**Audit all** runs three isolated casts for every spell without spawning anything
or changing your live wand. **Results** shows the outcomes; click a spell to load
its best tested sequence into a draft and preview it. **Stop audit** retains
partial results, and **Save audit** exports full JSON. Completion saves
automatically. Keep the editor open while the audit runs.

The reference run covered **422 spells / 1266 casts** and found **190 spells with
at least one drawable setup**, all with missing effects. The audit distinguishes
renderable trees with gaps, unsupported entities, native API errors, no-projectile
utility cases and unexercised cards. These outcomes describe prototype coverage;
they do not prove full Noita functionality or visual fidelity. Preview logs now
list ignored configuration and effect fields, rather than checking entity paths
alone. See [SPELL_AUDIT.md](SPELL_AUDIT.md) for counts, limits and reproduction.

Spark and chainsaw drawing now normalizes the pixel texture dimensions, fixing
the oversized red stripe caused by scaling an entire texture as a single pixel.

## What this preserves

The original Lua handles action execution, draw recursion, mana accounting, deck
and discard state, modifiers, always-cast cards and trigger payload construction.
Each wand has its own Lua state. After a complete draw, the bridge snapshots the
shot configuration and builds a tree of projectiles and nested trigger payloads.

This captures the tested script-level quirks: late modifiers affect all projectiles
in a multicast, chainsaw order changes cast delay, insufficient mana skips cards,
negative mana costs add mana, always-cast cards suppress their normal draw, and
trigger payloads use a fresh configuration and consume mana during the initial
cast. Already-built payloads fire once when their Terraria carrier collides,
expires or reaches its timer; impact does not execute the wand again.

The host and editor can run custom decks. Only actions whose
native API requirements are supplied by the bridge will work. Loading Noita's
complete action table does **not** establish that every spell is supported.

## Scope and remaining work

- **Entity visualization:** the debug wand imports full XML data and sprite
  metadata with bounded approximate movement and neutral contact/payload tests.
  Labels distinguish missing graphics from failed/no-output casts. The gameplay
  adapter still supports only spark, blue spark and chainsaw; native components
  and materials/terrain are deferred.
- **Scheduling:** the host records raw reload requests, including fractions and
  negatives. The demo uses an approximate cooldown. Noita's native integer
  conversion and complete reload/frame scheduling are not reproduced.
- **RNG:** shuffle is disabled; actions that require native RNG fail explicitly.
- **Runtime:** upstream LuaJIT 2.0.4 matches the version identified in the supplied
  Noita DLL, but it is a separately built 64-bit runtime. Vendor modifications,
  architecture differences and native engine behavior are not established.
  JIT compilation is disabled so instruction-count limits cover loops.
- **Persistence/networking:** edited definitions and mana are saved with the item.
  The active Lua draw/discard position resets after loading, cloning or applying
  edits. Multiplayer is not implemented.
- **Verification:** 114 original-script, path, editor serialization and diagnostic
  checks pass in the standalone harness. Ten additional checks against the
  packaged mod verify real tModLoader item save/load, cloning, the chainsaw
  damage hitbox, packaged sprite geometry, build identity, debug-item persistence,
  clone isolation, harmless XML visualization and payload suppression during cleanup. The mod builds and packages with tModLoader 2026.08.3.0 with zero
  compilation errors or warnings. The user confirmed the previous runtime loads and fires on Windows. The new
  editor, hitboxes and graphics still need a graphical in-game playtest.

Next priorities are the imported sprite/trigger playtest, choosing per-spell
behavior from debug reports, then component execution and recovered native APIs.

## Standalone checks

With .NET 8 installed, run from the repository root:

```sh
dotnet run --project Tools/AdapterSmoke -- Native/libterrarianoita_lua51.so /absolute/path/to/extracted-noita Compatibility/bridge.lua results.json
```

On Windows, substitute `Native/terrarianoita_lua51.dll` for the library argument.
For packaged item persistence, clone isolation and chainsaw configuration checks:

```sh
dotnet run --project Tools/ModSmoke -- /absolute/path/to/terrarianoita.tmod /absolute/path/to/tModLoader
```
Noita files are read locally. `Compatibility/reference-build.json` identifies the
files used for the reference checks without containing their source.

The checks cover draw/configuration behavior, state isolation, nested triggers,
mana bypass paths, instruction/depth limits, error recovery, trigger callbacks,
root/data/gun/file paths, quoted paths, incomplete extractions, saved editor
definitions, per-action mana diagnostics and the default spell palette.
An error invalidates that wand's Lua state; the mod disposes and recreates it.
The demo validates the entire entity tree before emitting any projectile.

## Implementation

| File | Responsibility |
| --- | --- |
| `Compatibility/bridge.lua` | Adapts native callbacks into completed cast trees |
| `Core/Lua51Runtime.cs` | Loads original local scripts and owns each Lua state |
| `Core/CastPlan.cs` | Deserializes and validates the cast plan |
| `Core/TriggerRunner.cs` | Fires each prebuilt payload once |
| `Common/DemoProjectileAdapter.cs` | Converts three supported entity paths to Terraria projectiles |
| `Core/WandDefinition.cs` | Validated, serializable wand settings and independent drafts |
| `Core/CastDiagnostics.cs` | Draw/mana/tree display and full JSON exports |
| `Common/WandEditorState.cs` | Spell palette, ordered slots, stat editing and log viewer |
| `Content/Items/NoitaWand.cs` | Saved wand definition, hand-anchored aiming and approximate cooldown |

File/process libraries are removed from the embedded Lua environment. Casts have
instruction, projectile-count and trigger-depth limits. These are operational
bounds for the prototype, not a security sandbox for arbitrary Lua code.
Keep original game data outside the repository or under ignored `LocalData/`.

## XML/sprite data audit

After producing the spell-audit JSON, run:

```sh
dotnet run --project Tools/XmlAudit -- /absolute/path/to/extracted-noita spell-audit.json xml-asset-audit.json
```

This audits inherited definitions, PNG headers/frame bounds and deferred
components for emitted entity paths. It does not require a graphics device and
does not prove successful GPU rendering.
