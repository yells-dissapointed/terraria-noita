# Liquid mixtures and reactions — v0.9.0

The previous adapter treated a different liquid as a wall and never used its
imported density. A partly filled water cell could therefore support a partly
filled oil cell one full tile above it. v0.9 gives those liquids shared space.

Each tile holds up to 255 ordinary units across all its ingredients. Flow keeps
individual material quantities, spreads mixtures, and exchanges equal amounts
when heavy fluid is above light fluid. Water settles below oil; gas rises through
liquid. Drawing divides the occupied height between the ingredients, ordered
by density, without drawing a separate full-height block for every ingredient.
Scooping samples the liquid layers into a mixed flask.

## Native fluid contact

Native water, lava and honey enter the shared solver where a custom material
touches them. Unrelated pools continue using Terraria. Isolated pure native
fluid is released back to Terraria. A native simulation step can push fluid into
an already full custom cell: that excess is held as pressure and spilled into
available space, rather than deleted or rendered as overlapping full blocks.
The native bridge and reactions remain an adaptation; mixed-pool contact effects
are mapped to Terraria statuses rather than reproducing every native liquid rule.
Shimmer is excluded to retain its native transmutation mechanics.

World saves retain every ingredient and pending overflow. Old v0.8 records load
as one-ingredient cells. Ordinary capacity is 255 units, with up to 16 ingredients
per cell and 32,768 cells; native overflow storage is capped at 65,535 units per
cell. Transfers beyond a capacity limit are refused without removing the source.

## Chemistry

The importer reads 325 reaction definitions from the supplied materials.xml.
228 have a supported binary shape, and 203 have a supported ingredient/product
match among the imported mobile materials and supported environmental catalysts.
This is recipe coverage, not proof of pixel-exact timing or complete alchemy.
The companion JSON lists all definitions and deferred requirements.

Recipes needing three inputs, lifetime conditions, direction, blob regions,
entity spawns or changes to native solid terrain are deferred. Air is not treated
as an unlimited source of mass for new products. Rates run on bounded contact
checks and small aliquots; they do not reproduce Noita's per-pixel scheduling.
Names, tag selectors and explicitly inherited reactions are resolved from the
local original data. Unbound output selectors are not guessed.

| Pair | Result | Origin |
| --- | --- | --- |
| Water + radioactive liquid | Water | Noita XML |
| Water + mana regeneration potion | Mana regeneration potion | Noita XML |
| Water + invisibility potion | Water | Noita XML |
| Blood + poison | Slime + smoke | Noita XML |
| Steam against a solid wall | Water | Noita XML, wall catalyst adapter |
| Acid + water | Diluted acid | Adapted recipe |
| Water + fire | Steam; some volume becomes air | Adapted recipe |
| Flammable fluid + heat without a supported XML recipe | Fire | Adapted fallback |

The additional acid liquid is `noita_diluted_acid`. It has weaker poisoning and
does not corrode tiles. It is a gameplay adaptation, not a claim about real-world
chemistry. Disable **Adapted liquid reactions** in mod settings to keep only
the supported original recipes. Shared capacity and density settling stay active.

## Focused playtest

1. Make a small basin. Pour a little oil, then water; repeat in the opposite order.
   They should settle into the same occupied volume with oil above water.
2. Repeat beside an existing Terraria water pool. Use `/noita probe` under the
   cursor to see each ingredient's amount and density. Brief pressure should
   disperse when space is available.
3. Mix water and `radioactive_liquid`; it should become water. Mix water and
   `magic_liquid_mana_regeneration`; it should become mana potion.
4. Mix acid and water with adapted reactions enabled. Confirm
   `noita_diluted_acid` appears. `/noita reactions acid water` explains the rule.
5. Scoop a mixed pool with a partly filled flask. Its contents and the remaining
   pool should agree. Save/reload and repeat; old filled flasks should still work.
6. `/noita clearliquids` removes custom materials while retaining captured native
   fluid. Native pools remain. Include `/noita probe` and liquid names in reports.

## Engine direction

The 16-pixel solver is a mod implementation choice, not the smallest grid that
could be drawn or simulated inside Terraria. A finer active-region solver could
be developed while leaving Terraria terrain and progression intact, at the cost
of more simulation and collision integration work.

Noita already supplies its pixel physics, native wand behavior and alchemy.
Using it as the host would be attractive if those are the project's main goal.
Recreating Terraria's building, crafting, progression, inventory, enemies and
world rules there would be a different major project. Neither direction makes
the other game's engine directly transferable through its data files.

References: [Noita's engine description](https://noitagame.com/) and
[tModLoader's tile/liquid data](https://docs.tmodloader.net/docs/stable/struct_tile.html).
