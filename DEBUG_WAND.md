# v0.8.0 playtest and reporting

Install `terrarianoita.tmod`, confirm **0.8.0** in its tooltip, and type
`/noita kit`. Keep the existing extracted Noita folder setting.

Use a test world for the live effect pass: the requested live mechanics include
terrain cutting, large explosions, harmful liquids and caster damage.

## Fast test sequence

1. **Baseline graphics:** hold the debug wand, right-click, select **Fixed spell**,
   then test Spark, Bomb, Arrow, Black Hole and one plasma emitter. Native Bomb/
   Arrow sprites should remain normal size; original Noita sprites use 1.75×.
   Rays and rings should have finite, local extents.
2. **Elemental projectiles:** select Fireball, Healing Bolt, Death Cross and Plasma
   Beam using **Presets >** or `/noita spell ID`. Switch to live mode. Check
   collision/damage, delayed explosions, caster damage and healing on return.
3. **Modifier combinations:** keep Bullet or Spark fixed, cycle tracking through
   enemy/cursor/caster homing, and add Freeze, Electric, Piercing or Matter Eater
   with the **Effect** control. Test moving targets and a wall; reset modifiers
   between comparisons. These controls insert original cards into the deck.
4. **Liquids:** create a small basin. Hold the debug flask and use
   `/noita material oil`, then left-click. Take the normal flask, hold right-click
   over the pool to scoop, and left-click to pour elsewhere. Compare its tooltip
   before/after. Repeat with native water and with a two-liquid mixture.
5. **Persistence:** save and reload with a partly filled mixed flask and some
   custom fluid remaining in the basin. Verify the material names and amounts.
   A cloned flask must have independent contents.
6. **Emitters:** test `CLOUD_WATER`, `SEA_WATER`, `CIRCLE_ACID`, `REGENERATION_FIELD`
   and `VACUUM_LIQUID` in live mode. Use a 10-second interval or fixed manual
   casts for long effects; the automatic scan cleans up its previous case.
7. **Special effects:** test tentacles, saws, black/white holes, swapper and a
   projectile-conversion card. Worm rain can spawn dangerous worms; Clear shots
   removes test creatures created by the current case.
8. **Resource cards:** put Blood Magic or Money Magic before a projectile in a
   normal wand. Confirm health/coins decrease with the recorded bonus. Try Zeta
   with a second Noita wand in inventory. Shuffle should persist with the item.

## Useful controls

- **XML/sprite preview:** draws and steers harmless entities; does not apply
  explosions, terrain changes, fluids, statuses or health/coin costs.
- **Terraria integration:** executes bound live adapters. Unsupported entities
  retain harmless previews. The current Noita biome portal is one such case.
- **Fixed spell:** repeats the current spell and context without advancing.
- **Presets >:** rotates four pages of spell families. Chat selection also accepts
  any catalog ID: `/noita spell FIREBALL` while holding the debug wand.
- **Context:** Solo, With sparks, Always cast or all three. A modifier often needs
  a follow-up projectile; a successful spark alone does not validate its effect.
- **Clear shots:** cancels future effects and payloads from the current case;
  cannot undo existing world/resource changes. Previously deposited liquids
  continue to simulate. `/noita clearliquids` clears custom material cells only.
- **Save report:** writes JSON and prints its exact path in chat. Reports include
  the loaded build/path, decks, original cast plans, XML sources, adapter names,
  selected modifiers, visual evidence, runtime traces and deferred behaviors.

## What to send back

For a failure, save the report immediately after repeating one fixed spell.
Include the spell ID, mode, context, selected modifiers and whether the problem
was visual, contact/damage, movement, resource cost or persistence. A short
screen recording is useful for aiming and animation issues. Report the loaded
file path if the tooltip does not show 0.8.0.

The bundled coverage file is an automated import/cast inventory. It does not
substitute for in-game checks. `ALL_SPELLS` explicitly reports its unported world
ritual; `SUMMON_PORTAL` requires Noita biome rules. Other live adapters may still
list deferred components or scripts, especially special secondary effects.
