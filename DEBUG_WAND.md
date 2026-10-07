# Spell Debug Wand v0.7.0

Verify the HUD/tooltip says **v0.7.0**, build `noita-effects-debug-modifiers-1`.
Close tModLoader before replacing `terrarianoita.tmod` in the folder opened by
**Mods → Open Mods Folder**. The configured extracted Noita data folder is reused.

Original Noita sprites retain **1.75×** drawing. Terraria sprites, including
Bomb and Arrow, now draw at their native **1×** size. Fallback Terraria artwork
also uses 1×. Existing vanilla projectile damage/collision sizes are unchanged.
Custom saw collision follows its scaled Noita disc; tentacle segments use the
same 1.75× spacing as their original sprites. Reports record both draw scales.

Craft the debug wand with one dirt block at a workbench. Right-click opens its
controls. **XML/sprite preview (harmless)** remains the default. Select a preset,
choose **Solo**, then **Selection: Fixed spell** to repeat one test without
advancing. Previous/Next and presets still select manually. Fixed auto repeats
until stopped or switching items; Save report is manual. Cycle spells restores
the finite scan and its final automatic report. Selection position resets after
reload; fixed/cycle and modifier preferences are saved. Auto always loads off.

## Live effect adapters

Select **Mode: Terraria integration (live)** for gameplay. Effects also work in
the ordinary Noita Wand and its expanded editor palette. Unsupported ordinary
wand trees are rejected before emission; unmapped debug entities stay harmless.
The original Noita engine/material simulation is still deferred.

| Preset / spell | Implemented gameplay | Remaining differences |
| --- | --- | --- |
| Bomb / BOMB | Real Terraria fuse, bounce, blast and terrain explosion; native-sized sprite | Terraria blast/physics; XML lifetime and damage conversion |
| Arrow / ARROW | Real Terraria arrow collision; native-sized sprite | Native material penetration deferred |
| Magic arrow/bolt/sphere + trigger/timer variants | Terraria Bullet hit pipeline with Noita sprites | Heavy/slow native blasts and extra effects deferred |
| Rocket / ROCKET | Terraria Rocket I impact blast with Noita sprite | Terraria acceleration/blast rules |
| Teleport / TELEPORT_PROJECTILE | Teleports caster at natural expiry, enemy contact or wall impact | Clear landing within 64 pixels; native particles/audio approximated |
| Short teleport / TELEPORT_PROJECTILE_SHORT | Same, with original 8-frame lifetime and faster XML speed | Same landing rules |
| Return teleport / TELEPORT_PROJECTILE_STATIC | Static launch marker; caster returns after 240 frames | Clearing it cancels the return |
| Enemy teleport / TELEPORT_PROJECTILE_CLOSER | Hit non-boss enemy moves to launch point if it fits | Boss relocation excluded; no damage |
| Black hole / BLACK_HOLE | Original animation, 12-pixel terrain cutter, projectile/item attraction | No direct damage; protected/important tiles and liquids remain intact |
| Super hole / BLACK_HOLE_BIG | Stationary growing terrain cutter (1→64 pixels), attraction and contact damage | Pulls enemies/caster; excludes boss movement; prototype contact damage |
| Tentacle / TENTACLE, TENTACLE_TIMER | Original 15 segment sprites, anchored whipping chain, segment hits, original timer payload | Approximate constrained chain; no native Verlet/material engine |
| Sawblade / DISC_BULLET | Spinning original sprite, XML slice damage, gravity and two wall bounces | Native speed-dependent damage deferred |
| Giant saw / DISC_BULLET_BIG | Original sprite, repeated slice hits, slowing/reversing flight and two wall bounces | Native cell cutting deferred |
| Omega saw / DISC_BULLET_BIGGER | Original sprite, repeated slice hits, attraction back toward caster and ten wall bounces | Native cell cutting deferred |
| Chainsaw / CHAINSAW | Existing visible close-range hit area plus small terrain cuts | 8-frame demo visibility/contact timing and Terraria tile-grid approximation |

Damage converts Noita internal damage at 25 Terraria HP per unit, plus supported
wand damage modifiers and Terraria magic bonuses. Sawblades can hit the caster
after a six-frame grace period. Super Black Hole can pull/hit the caster.
Live Bombs, Black Holes and Chainsaw change terrain. Terrain cutters respect
Terraria explosion restrictions and mod kill hooks, skip important tiles and
consume a shared per-frame work budget. Already-applied world effects persist
when clearing shots; cleanup cancels future effects, teleports and payloads.

Giga Black Hole, portal/tentacle variants, arbitrary attached effects and most
native entity scripts remain unimplemented. They stay preview markers/sprites
or explicit script errors. Importing their XML is not enough to execute them.
The 197-path inventory now has **6 vanilla prototypes, 10 custom effect paths,
29 further candidates, 14 preserved/deferred paths and 138 unmatched paths**.
Full mappings and associated spell IDs are in TERRARIA_MATCHES.md and
spell-matches.json.

## Debug modifiers

Click each control to cycle it. **Reset mods** returns to an unmodified recipe.
Choices combine independently and stay applied while changing spells or using
auto/fixed testing. Their original cards are inserted before the target in the
test deck; the original Lua supplies mana, speed, lifetime, damage and scheduling
changes. Always-cast keeps Noita's original ordering rather than forcing a new
modifier order. Unsupported particle attachments remain explicit report gaps.

| Control | Choices | Live and harmless-preview behavior |
| --- | --- | --- |
| Movement | Normal, Sine wave, Spiral, Ping-pong | Approximate native curve/orbit/reversal paths |
| Tracking | None, Homing, Short homing, Rotate toward target | Nearest chaseable enemy in range with line of sight; short homing uses its XML 60-pixel range |
| Speed | Normal, Faster, Decelerate, Accelerate | Real Lua starting speed; acceleration/deceleration map the native friction changes per frame |
| Bounce | None, +10 | Real BOUNCE card adds wall rebounds alongside built-in saw bounces |

Static effects stay static when velocity is zero. Movement remains bounded at
120 pixels per frame. Exact homing forces, sine frequency, spiral physics and
stacked duplicate effect entities are not the full native simulation. Training
dummies may not be chaseable: use an actual hostile enemy for tracking tests.
Modifiers can affect trigger carriers and their prebuilt payloads according to
the shot configs produced by Lua. Each entity reports MovementSources and
remaining gaps; each test stores its added modifier cards and exact deck.

## Focused in-game checks

Use **Solo**, **Fixed spell**, and manual firing first. Save report after the
effect completes so its later hits, landing and payloads are included.

1. **Size:** compare Bomb and Arrow with Terraria's own equivalents. Their
   native artwork should be normal-sized; Magic arrow and Black Hole should
   retain the larger original Noita artwork.
2. **Teleport:** aim into open space, then at a wall. Check the landing is clear.
   Repeat Short teleport; it should end much sooner. Fire Return teleport,
   walk away, wait 4–5 seconds and check you return. Repeat and Clear shots
   before it ends: you should stay where you are. Enemy teleport should move a
   normal enemy to the launch point without relocating a boss.
3. **Black hole:** aim the regular spell through dirt/stone. Check it visibly
   bores a small passage with its original animation. Fire a slow bolt near it
   from another ordinary wand to inspect attraction. Super hole should stay at
   the launch point, grow its cut radius and pull nearby enemies/items/caster.
4. **Tentacle:** aim beside an enemy at different angles. Check the full chain
   curls, hits along its segments and retracts. Test TENTACLE_TIMER in With
   sparks context: its child payload should fire once after 20 frames.
5. **Saws/chainsaw:** check saw spin and actual enemy hits; walls should rebound
   the ordinary/giant saw twice. Giant should reverse; Omega should return toward
   you. Check caster contact damage. Chainsaw should cut touching ordinary
   terrain without destroying important/protected tiles.
6. **Modifiers:** keep Magic arrow fixed. Compare Normal, Sine wave, Spiral and
   Ping-pong; then add Homing near a hostile enemy. Try Short homing close to
   the enemy and Rotate toward target. Try speed changes and Bounce against a
   wall. Repeat in harmless preview: motion should change but no damage, digging
   or teleport should occur. Reset mods and confirm normal flight returns.
7. **Cleanup:** clear each effect or change modes while it is active. No return
   teleport, Bomb detonation or child payload should occur from removal. A
   5-second interval permits Bomb's 180-frame fuse; Return teleport needs over
   4 seconds and Super hole's 500-frame life needs the 10-second interval.

The supplied v0.5.0 live scan recorded 422 tests, 505 emitted roots and 24
cards, with 389 original-sprite draws and 126 entity markers (including child
payloads). Its known nuke frame mismatch remains a deferred import issue. Plain
sparks in With sparks mode can be deliberate support carriers; an extra entity
still needs an implemented adapter to create its native effect.

## General preview tests

1. **Version and basic rendering:** verify the tooltip/HUD says v0.7.0, build
   `noita-effects-debug-modifiers-1`. Use Spark in Solo context. Aim horizontally, vertically
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
  projectiles plus spark/chainsaw and custom teleport/black-hole/tentacle/saw
  effects. Unmapped paths use harmless Noita visuals. Terrain/location changes
  and caster damage occur only in live mode; use XML preview for graphics.

Reports retain the latest 2048 cases and include recipes, unchanged native
coverage statuses, actual root spawn counts, separate diagnostic-card counts,
per-entity visual evidence, mapped profiles, full imported XML source nodes
(deduplicated), deferred fields/components, later events and loaded version/path.
A drawn sprite does not establish full spell support or visual fidelity.
`TerrariaAdapter` and `GameplayExecuted` identify actual vanilla/custom behavior;
`MovementSources` identifies supported movement attachments and `modifiers`
records the extra debug cards;
`fixed_spell` records whether a test held its selection. Legacy audit statuses
still measure the original three-entity demo, so an adapted Bomb can retain
that audit's Unsupported label while its report records the real Bomb adapter.

## XML import scope and validation

All source XML attributes/nodes are retained as data. Base definitions and
nested component overrides are resolved; tagged overrides and component removal
are supported. Duplicate attributes use the last value and produce warnings;
native precedence is not established. Base-child inclusion follows the preview
mapping, with full original source data retained. Referenced Lua components are
not executed. Full native explosions, materials, statuses, particles, audio,
physics bodies and missing engine APIs remain deferred. The explicit live
adapters described above implement selected terrain, relocation, damage and
movement rules from inspected definitions/scripts; they do not run the native
entity engine. These rules are disabled in harmless preview.

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
406 standalone checks and nineteen packaged-mod checks cover source behavior,
inheritance, frame bounds, scanner selection, report distinctions, item settings,
neutral visualization and cleanup. The tModLoader 2026.08.3.0 package builds with
zero errors/warnings. GPU drawing and in-game UI still require your playtest.

## Install

Close tModLoader. Extract the versioned ZIP and copy its `terrarianoita.tmod`
into the folder opened by Mods → Open Mods Folder, replacing the older package.
Move duplicate older packages outside that folder, then restart. Update an old
ModSources checkout before rebuilding; otherwise it can recreate an old mod.
Keep the configured extracted Noita data folder, including PNG/XML files.
