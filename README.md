# Terraria Noita wand prototype

This prototype executes the original wand Lua scripts from your own extracted
Noita installation inside tModLoader. The C# bridge receives a completed cast
plan and renders a small demonstration in Terraria. Original Noita code and
assets are not included in this repository.

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

## Editor and debugging (v0.2.0)

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

- **Projectile demo:** only spark bolt, blue spark bolt and chainsaw entity paths are rendered.
  Speed, gravity, spread, lifetime, collision and damage conversion are prototype
  values. Noita XML components, materials, terrain, explosions, statuses, perks
  and native physics have not been ported.
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
- **Verification:** 68 original-script, path, editor serialization and diagnostic
  checks pass in the standalone harness. Three additional checks against the
  packaged mod verify real tModLoader item save/load, cloning, and the chainsaw
  damage hitbox. The mod builds and packages with tModLoader 2026.08.3.0 with zero
  compilation errors or warnings. The user confirmed the previous runtime loads and fires on Windows. The new
  editor, hitboxes and graphics still need a graphical in-game playtest.

Next priorities are the editor/gameplay playtest, recovered native RNG and reload
scheduling, then a broader entity/component adapter.

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
| `Common/DemoProjectileAdapter.cs` | Converts two supported entity paths to Terraria projectiles |
| `Core/WandDefinition.cs` | Validated, serializable wand settings and independent drafts |
| `Core/CastDiagnostics.cs` | Draw/mana/tree display and full JSON exports |
| `Common/WandEditorState.cs` | Spell palette, ordered slots, stat editing and log viewer |
| `Content/Items/NoitaWand.cs` | Saved wand definition, hand-anchored aiming and approximate cooldown |

File/process libraries are removed from the embedded Lua environment. Casts have
instruction, projectile-count and trigger-depth limits. These are operational
bounds for the prototype, not a security sandbox for arbitrary Lua code.
Keep original game data outside the repository or under ignored `LocalData/`.
