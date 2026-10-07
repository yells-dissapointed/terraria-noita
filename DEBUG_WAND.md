# Spell Debug Wand v0.5.0

Craft with one dirt block at a workbench. Left-click tests the selected spell
and advances; right-click opens controls. The default is **XML/sprite preview
(harmless)**. Presets select Spark, Bomb, Trigger, Timer, Chainsaw, Mist, Black
hole or Lua error. Select a preset, close the panel and left-click while aiming.
Test current repeats a selection without advancing.

## Five useful in-game tests

1. **Version and basic rendering:** verify the tooltip/HUD says v0.5.0, build
   `xml-sprite-preview-1`. Use Spark in Solo context. Aim horizontally, vertically
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
- Gameplay demo mode retains the existing spark/blue-spark/chainsaw damage
  adapter. Unsupported trees are still skipped in that mode.

Reports retain the latest 2048 cases and include recipes, unchanged native
coverage statuses, actual root spawn counts, separate diagnostic-card counts,
per-entity visual evidence, mapped profiles, full imported XML source nodes
(deduplicated), deferred fields/components, later events and loaded version/path.
A drawn sprite does not establish full spell support or visual fidelity.

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
114 standalone checks and ten packaged-mod checks cover source behavior,
inheritance, frame bounds, scanner selection, report distinctions, item settings,
neutral visualization and cleanup. The tModLoader 2026.08.3.0 package builds with
zero errors/warnings. GPU drawing and in-game UI still require your playtest.

## Install

Close tModLoader. Extract the versioned ZIP and copy its `terrarianoita.tmod`
into the folder opened by Mods → Open Mods Folder, replacing the older package.
Move duplicate older packages outside that folder, then restart. Update an old
ModSources checkout before rebuilding; otherwise it can recreate an old mod.
Keep the configured extracted Noita data folder, including PNG/XML files.
