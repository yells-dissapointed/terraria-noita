# Terraria Noita — v0.8.0

This single-player tModLoader mod executes the original wand Lua from your own
extracted Noita installation and translates the resulting casts into Terraria
projectiles, effects and materials. Original Noita scripts and artwork are loaded
locally; they are not bundled or redistributed with the mod.

v0.8.0 adds a shared XML gameplay interpreter, persisted custom materials,
refillable mixed-liquid flasks, randomized/shuffle casts and a larger debug kit.
It is a substantial playable adaptation, not the native Noita engine.

## Install the built mod

1. In tModLoader, choose **Mods → Open Mods Folder**, then close the game.
2. Replace `terrarianoita.tmod` in that folder with the new build. Restart
   tModLoader and enable **Terraria Noita 0.8.0**.
3. Keep **Extracted Noita folder** pointed at your existing extracted files.
   The setting accepts the extracted root, `data`, `scripts`, `gun`, or `gun.lua`.
4. Enter a single-player world and type **`/noita kit`** in chat. This gives the
   normal wand, debug wand, empty material flask and unlimited debug flask.
5. The wand tooltip and debug panel show the loaded version. **Loaded file**
   prints its exact path when diagnosing duplicate or stale mod installations.

The normal wand can also be crafted from 10 dirt at a workbench; the debug wand
and debug flask each cost one dirt there. An empty material flask uses one Bottle.
Windows x64 and Linux x64 LuaJIT runtimes are included. macOS needs a native build;
see [Native/README.md](Native/README.md).

## What works in this build

- **Wands:** all 422 cards in the editor, ordered decks, always-cast cards, shuffle,
  mana, draw recursion, late modifiers, multicast spread, timers and impact/death
  payloads. Each ordinary wand keeps its own Lua deck/discard state while loaded.
- **Projectiles:** the shared interpreter reads XML movement, contact damage,
  damage types, healing, bounces, lifetimes, homing, explosions, plasma/lightning
  rays, digging, material emitters, conversion and area statuses. Imported entity
  Lua remains unexecuted; selected script behaviors have explicit C# adapters.
- **Special effects:** teleport variants, swapper, black/white holes including
  large and giga variants, tentacles/portals, saws, death crosses, charged beams,
  projectile conversion spells, destruction, mass polymorph and worm rain.
- **Modifiers:** speed, gravity, crit chance, knockback, wave/spiral/ping-pong and
  angular paths, cursor/enemy/caster homing, piercing, phasing, freezing,
  electricity, material trails, matter eating, damage fields and selected
  conversion/shield/lifetime effects. See coverage data for deferred extras.
- **Materials:** 466 imported definitions, including 102 scoopable liquids with
  inherited names, colors and status metadata. Native water/lava/honey/shimmer
  use Terraria liquid simulation. Other fluids use saved, flowing tile cells.
  Selected reactions include ignition, water extinguishing and acid corrosion.
  Material identity is imported more broadly than chemical behavior.
- **Flasks:** 1,000-unit capacity, mixed contents, hold right-click to scoop and
  hold left-click to pour near the cursor. Only accepted amounts transfer;
  full or blocked pours retain the rest. Contents survive saving and cloning.
- **Resource cards:** blood magic and blood-to-power spend actual health;
  money magic spends inventory coins; conditionals read player health and nearby
  entities; Zeta reads other carried Noita wands. Cessation grants a short
  invulnerable/invisible interval with item use blocked.

Native Terraria sprites remain **1×**; original Noita sprites remain **1.75×**.
Beam and fallback geometry uses explicit pixel dimensions to avoid screen-sized
stretching. The sprite cache accommodates a full scan while retaining its pixel
memory limit. Effects with native particle-only artwork use adapted particles or
rings when no usable original sprite exists.

## In-game editing and debugging

Right-click the normal wand to edit a draft. Reorder cards, choose always-casts,
adjust mana/timing/draw stats and toggle shuffle, then **Apply**. **Test draft**
records a fresh cast without changing world resources. Edited definitions and
mana are saved; Lua deck/discard position resets on reload/clone/edit.

Right-click the debug wand for controls. **XML/sprite preview is harmless by
default.** Switch to **Terraria integration (live)** to execute effects. Live
spells can damage the caster, spend resources, change terrain and leave liquids.
Use **Fixed spell** to repeat one selection; Previous/Next and **Presets >** choose
cards. Movement, tracking, speed, bounce and effect choices insert real Noita
modifier cards before the selected spell.

The debug kit supports these chat commands:

| Command | Action |
| --- | --- |
| `/noita kit` | Give both wands and both flasks |
| `/noita spell FIREBALL` | Select a spell while holding the debug wand |
| `/noita liquids acid` | List matching imported liquids, up to 40 at once |
| `/noita material oil` | Select any imported liquid in the debug flask |
| `/noita fill acid` | Give the held normal flask test contents |
| `/noita status` | Show build, material counts and active custom cells |
| `/noita clearliquids` | Clear custom cells; native Terraria liquids remain |

The debug flask creates liquid on left-click and cycles a common material list
on right-click. The normal flask scoops existing custom or native liquid.
**Clear shots** cancels the current debug case's projectiles and spawned test
creatures. It cannot reverse damage, resource costs, converted enemies, terrain
changes or liquids already deposited. Auto scans clean up the previous case;
short intervals can cut off long effects. See [DEBUG_WAND.md](DEBUG_WAND.md).

## Coverage and known limits

The v0.8 audit runs **1,266 reference casts: 422 cards × three contexts**.
**1,263 casts pass**, and **421 cards pass all three Lua contexts**. The remaining
`ALL_SPELLS` card requires Noita's multi-stage world ritual and reports an explicit
unsupported entity-loader error. It is not silently treated as a working spell.

**200 of 201 emitted entity paths** have live adapters in those casts.
`SUMMON_PORTAL` remains preview-only because its destination relies on Noita's
biome/world portal rules. See [EXPANSION_COVERAGE.md](EXPANSION_COVERAGE.md) for
entity and modifier classifications. The downloadable coverage JSON includes
per-card results, XML component gaps and material metadata.

These are static cast/import coverage counts, not proof that 421 cards or 200
entities reproduce every native effect. A follow-up spark can fire even when a
modifier's extra entity is deferred. Entity scripts, modded callbacks, native
physics joints, detailed chemistry, native audio and Noita creature AI remain
partial. Some ordinary projectiles have working motion/damage but missing
secondary scripted behavior. Reports retain these gaps explicitly.

The adapters use 25 Terraria HP per Noita damage unit. They preserve Terraria's
protected/important tiles. Custom materials use 16-pixel cells with 255 units per
cell, at most 32,768 cells, and bounded update work. They do not implement Noita's
pixel chemistry or seamless mixing with native liquids. Statuses use Terraria
buffs where possible. NPC polymorph becomes a temporary bunny; player polymorph
is temporary curse/weakness rather than a sheep body. Egg creatures use Terraria
slimes. Worm body segments are visual, with head contact damage. Shields and
special fields are approximations. Infinite lifetime is capped at 3,600 frames.
All-projectile conversion affects up to 128 existing shots per cast.

The host PRNG is deterministic but **not Noita's bit-identical RNG**. Unlocks and
card uses remain permissive for development; loot/progression/balance and
multiplayer are not implemented. Mana/reload timing and hit immunity follow the
Terraria adapter, not a recovered native scheduling engine.

## Build and checks

Clone branch `feature/noita-lua-adapter` into a ModSources folder named exactly
`terrarianoita`, then build from **Workshop → Develop Mods**. The folder name is
the mod's internal name. The current build compiles with tModLoader
2026.08.3.0 / .NET 8.

From the repository root with .NET 8 installed:

```powershell
dotnet run --project Tools/AdapterSmoke -- Native/terrarianoita_lua51.dll C:/Noita-extracted Compatibility/bridge.lua results.json
dotnet run --project Tools/SpellAudit -- Native/terrarianoita_lua51.dll C:/Noita-extracted Compatibility/bridge.lua spell-audit.json
dotnet run --project Tools/ExpansionAudit -- C:/Noita-extracted spell-audit.json expansion-coverage.json
dotnet run --project Tools/ModSmoke -- C:/path/to/terrarianoita.tmod C:/path/to/tModLoader
```

On Linux substitute `Native/libterrarianoita_lua51.so`. The standalone checks
exercise original Lua ordering/payloads, modifiers, resource snapshots, material
inheritance and conservation. Packaged reflection checks exercise actual
`tModLoader` item serialization, clone isolation, effect geometry, harmless
preview and cleanup. There is no graphical Terraria client in the build
environment: this release still needs in-game visual/combat/terrain validation.

Release validation: **1,041 standalone checks** and **25 packaged-mod checks**
passed, in addition to the 1,266-case coverage audit above. The packaged version
is checked against its compiled build identity to catch stale installations.
