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
   path. It must contain `data/scripts/gun/gun.lua`. Save and reload the mod.
5. Enter a **single-player** world. Craft the Noita Wand using **10 dirt blocks at
   a workbench**. Left-click to fire and right-click to cycle the five demo decks.

Windows x64 and Linux x64 LuaJIT libraries are included. macOS requires building
an appropriate native library; it has not been validated. See [Native/README.md](Native/README.md).
The wand has its own 100 mana pool, displayed in its tooltip.

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

The standalone host can run decks beyond the five presets. Only actions whose
native API requirements are supplied by the bridge will work. Loading Noita's
complete action table does **not** establish that every spell is supported.

## Scope and remaining work

- **Projectile demo:** only spark bolt and chainsaw entity paths are rendered.
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
- **Persistence/networking:** presets and mana reset when the item is reloaded;
  Lua deck state is retained only during the active item session. Multiplayer,
  a wand editor and saved spell decks are not implemented.
- **Verification:** the adapter has passed 31 checks using the extracted original
  scripts. The mod builds and packages with tModLoader 2026.08.3.0 with zero
  compilation errors or warnings. A graphical in-game playtest and Windows
  runtime execution are still required.

Next priorities are the in-game playtest, recovered native RNG and reload
scheduling, then an entity/component adapter and persistent wand editing.

## Standalone checks

With .NET 8 installed, run from the repository root:

```sh
dotnet run --project Tools/AdapterSmoke -- Native/libterrarianoita_lua51.so /absolute/path/to/extracted-noita Compatibility/bridge.lua results.json
```

On Windows, substitute `Native/terrarianoita_lua51.dll` for the library argument.
Noita files are read locally. `Compatibility/reference-build.json` identifies the
files used for the reference checks without containing their source.

The checks cover draw/configuration behavior, state isolation, nested triggers,
mana bypass paths, instruction/depth limits, error recovery and trigger callbacks.
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
| `Content/Items/NoitaWand.cs` | Demo deck selection, mana and approximate cooldown |

File/process libraries are removed from the embedded Lua environment. Casts have
instruction, projectile-count and trigger-depth limits. These are operational
bounds for the prototype, not a security sandbox for arbitrary Lua code.
Keep original game data outside the repository or under ignored `LocalData/`.
