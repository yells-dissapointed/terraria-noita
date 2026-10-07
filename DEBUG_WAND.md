# Spell Debug Wand v0.6.0

Spell sprites, orange entity markers and fallback spark/chainsaw visuals now
draw at **1.75×** their previous size, around the same origin. Damage, collision
bounds, speed, lifetime and muzzle placement are unchanged. Diagnostic card
icons retain their existing 2× size. The 256-world-pixel sprite cap still applies.
Live reports include `spell_visual_scale` so comparisons identify the draw size.

The supplied v0.5.0 live scan recorded 422 tests, 505 emitted roots and 24
diagnostic cards. It recorded 389 original-sprite draws, 126 entity markers and
7 pending draws (these include triggered children). Only the known nuke frame
definition mismatch appeared as an import/draw error. 187 cases emitted only
root sparks: the With sparks context deliberately gives modifiers a spark
carrier, and unimplemented trails/attached effects can leave it looking plain.

Craft with one dirt block at a workbench. Left-click tests the selected spell
and advances; right-click opens controls. The default is **XML/sprite preview
(harmless)**. Presets select Spark, Bomb, Trigger, Timer, Chainsaw, Mist, Black
hole or Lua error. Select a preset, close the panel and left-click while aiming.
Test current repeats a selection without advancing.


## Fixed selection and Terraria integration

Right-click the Spell Debug Wand and choose **Selection: Fixed spell**. Left-click
and auto now repeat the same spell **and context**. Previous/Next and presets
still change the selection manually. Choose Cycle spells to resume the finite
scan. Fixed auto repeats until you stop it or switch items; Save report is manual.
The fixed/cycle preference is saved with the item; scan position resets on reload.

**Mode: Terraria integration (live)** replaces the older three-entity gameplay
mode. It reuses actual vanilla projectile types for these exact Noita entities:

| Noita spell family | Terraria behavior | Artwork |
| --- | --- | --- |
| Bomb | Bomb: fuse, bounce, blast and tile explosion | Terraria Bomb |
| Arrow | WoodenArrowFriendly: flight and hit/collision | Terraria Arrow |
| Magic arrow, Magic bolt, Magic sphere (BULLET / HEAVY_BULLET / SLOW_BULLET), with trigger/timer variants | Bullet: flight and hit/collision | Original Noita sprites |
| Rocket | RocketI: flight and impact blast | Original Noita sprite |

These are prototypes, not full equivalents. XML direct/explosion damage is
converted at 25 Terraria HP per Noita unit, XML lifetime becomes a 60 Hz fuse,
and wand speed/lifetime/spread modifiers apply. Vanilla motion, blast size,
status rules and collision replace native Noita rules. Heavy/slow bolt blast
effects and extra modifier entities remain deferred. Terraria explosion
preparation retains the mapped damage. Native Bomb can damage players/terrain.

The ordinary Noita Wand also accepts these six entity paths (12 spell IDs,
including trigger/timer variants) alongside spark/blue-spark/chainsaw. Its normal
editor palette includes the new families. Unmapped ordinary-wand trees still
fail validation; the debug integration mode renders them as harmless previews.
Black Hole and other distinctive spells keep their Noita sprite sheets and
animations in both debug modes; their native gameplay is not replaced.

Debug cleanup directly deactivates bound vanilla test projectiles instead of
calling Kill, which would detonate a real Bomb. Natural deaths and collisions
retain vanilla effects and dispatch prebuilt Noita payloads once. Cleanup of
existing demo/preview projectiles still suppresses payloads.

### Focused in-game checks

1. Select **Bomb**, **Solo**, **Fixed spell**, and **Terraria integration (live)**.
   Fire once and wait at least 3 seconds: check the visible Terraria bomb,
   bounce/fuse and natural blast. A 5-second auto interval lets its 180-frame
   fuse finish; a shorter interval clears it before detonation.
2. Fire another Bomb, then Clear shots or change modes before the fuse finishes.
   It should disappear without a cleanup blast. Repeat manually and confirm
   the selected spell remains Bomb.
3. Try Arrow, Magic arrow and Rocket presets against a target/wall; inspect
   `TerrariaAdapter`, hit and payload events in saved JSON. Test a trigger
   carrying Bomb from the ordinary editor to verify mixed adapter payloads.
4. Select Black Hole in integration mode: check that the original Noita
   animation remains, with a report explaining harmless visual fallback.
5. Switch Cycle spells back on and check that firing advances normally.

The inventory in TERRARIA_MATCHES.md / spell-matches.json covers all 197 emitted
entity paths: 6 implemented prototype paths, 32 candidate paths, 17 explicitly
preserved Noita paths and 142 without an automatic match. Candidate matches
need engineering comparisons and are not silently enabled.

## General preview tests

1. **Version and basic rendering:** verify the tooltip/HUD says v0.6.0, build
   `terraria-adapters-fixed-spell-1`. Use Spark in Solo context. Aim horizontally, vertically
   and diagonally; repeat at another zoom level. Check that it starts near the
   hand and stays small and centered on its yellow outline. Test Bomb for an
   original solid sprite; it should remain harmless in preview mode.
2. **Payloads:** use Trigger with the With sparks context, aiming at a wall or
   enemy. Observe the carrier followed by its payload once at contact. Repeat
   Timer in open space. Use a 5-second interval or manual tests, so cleanup does
   not truncate the payload. Save report after it fires.
3. **Missing graphics versus firing:** use Chainsaw and Mist. An orange labeled
   ENTITY marker means Lua emitted that entity but its particle/native graphics
   are deferred. Black hole imports a sprite but does not remove blocks or
   simulate attraction. These should be visibly different from diagnostic cards.
4. **Error recovery:** use Lua error (DAMAGE_RANDOM), which needs missing native
   RNG. It should show a cyan SCRIPT FAILED — CARD ONLY diagnostic, count zero
   emitted roots, and preserve the error. Select Spark next: it should still
   fire. A utility spell in Solo can show NO PROJECTILE — CARD ONLY legitimately.
5. **Automatic coverage:** choose With sparks, interval 2 seconds, Restart then
   Start auto; close the panel and aim into open space. After 20–30 cases,
   switch items to stop. Check that shots clear between cases, controls work,
   and Save report succeeds. No need to manually inspect all 422 spells. A full
   scan automatically stops/saves after its final observation interval.

For each problem send the saved JSON, spell ID/context, and a short video or
screenshot showing the cursor, player and yellow outline. Note the zoom level,
FPS drops and any client.log exception. Exact loaded file path is available from
Loaded file. JSON is saved under `<tModLoader save folder>/terrarianoita/debug/`;
Save report prints the path in chat. Save after observing the effect, so later
contacts, payloads, GPU fallbacks and deaths are included.

## Controls and report meaning

Previous/Next, Test current, Start/Stop auto, context, interval, Restart, Clear
shots, presets and Save report allow targeted or automatic inspection. Contexts
are With sparks (default), Solo, Always cast and All three. Intervals are 1/2/5/10
seconds. With sparks supplies four follow-up sparks; it can show supporting
sparks even when the tested card is a modifier rather than a projectile.

Every case has a fresh non-shuffle Lua state, unlimited card uses and 10000 test
mana. Switching items stops auto; inventory/chat pauses it. Auto is never
restored after loading/cloning. The run ends rather than wrapping. Clear shots
and automatic cleanup suppress payload callbacks and affect only the latest
case belonging to this item. Short intervals can truncate long effects.

- Original sprites and animation frames are loaded from your local extraction.
  Yellow outlines keep tiny/transparent frames visible; they are preview bounds,
  not native hitboxes. Sprite draw sizes are capped at 256 world pixels.
- Orange ENTITY markers are real emitted cast-plan entities with missing or
  rejected graphics. They count as emitted roots. They are not native effects.
- Cyan diagnostic cards are labeled no-output/error/unexercised cases, using
  the original spell-card icon where available. They never count as emitted
  roots. If the target action was not exercised, support entities can coexist
  with the diagnostic; the audit status remains explicit.
- XML/sprite preview deals no damage and executes only approximate movement,
  lifetime, contact/bounce and already-built timer/contact/death payloads.
  A minimum 12-frame life makes very short entities observable.
- Terraria integration mode runs live mapped Bomb/Arrow/Bullet/Rocket
  projectiles plus the spark/blue-spark/chainsaw demo. Unmapped paths use
  harmless Noita visuals. Live Bombs use vanilla damage and terrain explosion
  rules; use XML preview when you only want to inspect graphics.

Reports retain the latest 2048 cases and include recipes, unchanged native
coverage statuses, actual root spawn counts, separate diagnostic-card counts,
per-entity visual evidence, mapped profiles, full imported XML source nodes
(deduplicated), deferred fields/components, later events and loaded version/path.
A drawn sprite does not establish full spell support or visual fidelity.
`TerrariaAdapter` and `GameplayExecuted` identify actual vanilla behavior;
`fixed_spell` records whether a test held its selection. Legacy audit statuses
still measure the original three-entity demo, so an adapted Bomb can retain
that audit's Unsupported label while its report records the real Bomb adapter.

## XML import scope and validation

All source XML attributes/nodes are retained as data. Base definitions and
nested component overrides are resolved; tagged overrides and component removal
are supported. Duplicate attributes use the last value and produce warnings;
native precedence is not established. Base-child inclusion follows the preview
mapping, with full original source data retained. Referenced Lua components are
not executed. Explosions, terrain, materials, homing, statuses, particles, audio,
physics bodies and missing native APIs remain deferred.

Sprite sheets use declared frame rectangles, stride, timing, looping and
one-pixel cropping. RGB PNG black is treated as transparent; RGBA pixels are
premultiplied for Terraria rendering. These color/timing/offset conventions need
client validation. Random variants use the first file; motion uses mean speed
and approximate conversion to Terraria frames. Original Noita assets are never
bundled in the mod or repository.

The supplied-data audit loaded **197/197 emitted entity definitions**, with
**93 entities having usable sprite metadata** and **104 marker-only**. One nuke
sprite definition requests frames exceeding its PNG; it is rejected/reported.
The 422-spell/1266-cast Lua regression audit is unchanged. Imported sprite/marker
coverage is separate from the conservative three-entity gameplay audit.
123 standalone checks and fifteen packaged-mod checks cover source behavior,
inheritance, frame bounds, scanner selection, report distinctions, item settings,
neutral visualization and cleanup. The tModLoader 2026.08.3.0 package builds with
zero errors/warnings. GPU drawing and in-game UI still require your playtest.

## Install

Close tModLoader. Extract the versioned ZIP and copy its `terrarianoita.tmod`
into the folder opened by Mods → Open Mods Folder, replacing the older package.
Move duplicate older packages outside that folder, then restart. Update an old
ModSources checkout before rebuilding; otherwise it can recreate an old mod.
Keep the configured extracted Noita data folder, including PNG/XML files.
