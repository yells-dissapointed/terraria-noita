# Expansion coverage

This is the v0.8.0 spell-cast baseline, retained through the v0.8.1 localization
fix. v0.9.0 changes the material solver and reactions; see LIQUIDS.md and the
separate fluid report for current material coverage. Spell mappings are unchanged.

Build 0.8.0: 1263/1266 reference casts completed; 421/422 cards passed all three Lua contexts.

200/201 emitted entity paths have a live adapter; preview-only paths: 1. 466 material definitions and 102 liquids imported.

Static cast/import coverage, not rendered or combat playtesting. Original Noita entity Lua is not executed. XML subset adapters and modifier approximations are not full native parity. Native liquid chemistry is not ported; custom fluids use bounded tile cells.

## Lua exceptions

- ALL_SPELLS: Noita Lua: terrarianoita-host:199: terrarianoita-host:181: Entity script loader not ported: data/entities/projectiles/deck/all_spells_loader.xml

## Entity adapters

| Entity | Live adapter | Remaining gaps |
| --- | --- | --- |
| data/entities/items/pickup/egg_hollow.xml | XML component subset | 8 recorded;  |
| data/entities/items/pickup/egg_monster.xml | XML component subset | 8 recorded;  |
| data/entities/particles/image_emitters/wand_effect.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/bomb_cart.xml | XML component subset | 7 recorded;  |
| data/entities/projectiles/bomb_holy_giga.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/bomb_holy.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/bomb.xml | Terraria Bomb | 3 recorded;  |
| data/entities/projectiles/chunk_of_soil.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/darkflame.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/acidshot.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/alcohol_blast.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/all_acid.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/all_blackholes.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/all_deathcrosses.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/all_discs.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/all_nukes.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/all_rockets.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/arrow.xml | Terraria WoodenArrowFriendly | 1 recorded;  |
| data/entities/projectiles/deck/ball_lightning.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/berserk_field.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/big_magic_shield_start.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/black_hole_big.xml | Custom BlackHole | 6 recorded;  |
| data/entities/projectiles/deck/black_hole_giga.xml | Custom BlackHole | 5 recorded;  |
| data/entities/projectiles/deck/black_hole.xml | Custom BlackHole | 3 recorded;  |
| data/entities/projectiles/deck/bomb_detonator.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/bouncy_orb.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/bubbleshot.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/buckshot_player.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/bullet_heavy.xml | Terraria Bullet | 1 recorded;  |
| data/entities/projectiles/deck/bullet_slow.xml | Terraria Bullet | 1 recorded;  |
| data/entities/projectiles/deck/bullet.xml | Terraria Bullet | 1 recorded;  |
| data/entities/projectiles/deck/chain_bolt.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/chainsaw.xml | Spark/chainsaw | 1 recorded;  |
| data/entities/projectiles/deck/chaos_polymorph_field.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/circle_acid.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/circle_fire.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/circle_oil.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/circle_water.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/cloud_acid.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/cloud_blood.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/cloud_oil.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/cloud_thunder.xml | XML component subset | 5 recorded;  |
| data/entities/projectiles/deck/cloud_water.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/crumbling_earth.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/death_cross_big.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/death_cross.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/delayed_spell.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/destruction.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/digger.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/disc_bullet_big.xml | Custom Saw | 4 recorded;  |
| data/entities/projectiles/deck/disc_bullet_bigger.xml | Custom Saw | 4 recorded;  |
| data/entities/projectiles/deck/disc_bullet.xml | Custom Saw | 1 recorded;  |
| data/entities/projectiles/deck/duck.xml | XML component subset | 5 recorded;  |
| data/entities/projectiles/deck/electrocution_field.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/exploding_deer.xml | XML component subset | 17 recorded;  |
| data/entities/projectiles/deck/explosion_light.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/explosion.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/fireball.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/fireblast.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/firebomb.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/fireworks/firework_pink.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/fish.xml | XML component subset | 10 recorded;  |
| data/entities/projectiles/deck/flamethrower.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/freeze_field.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/freezing_gaze_beam.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/friend_fly.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/glitter_bomb.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/glowing_bolt.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/glue_shot.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/grenade_anti.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/grenade_large.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/grenade_tier_2.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/grenade_tier_3.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/grenade.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/heal_bullet.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/healhurt.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/hook.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/iceball.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/infestation.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/kantele/kantele_a.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/kantele/kantele_d.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/kantele/kantele_dis.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/kantele/kantele_e.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/kantele/kantele_g.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/lance_holy.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/lance.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/laser.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/levitation_field.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/light_bullet_air.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/light_bullet_blue.xml | Spark/chainsaw | 1 recorded;  |
| data/entities/projectiles/deck/light_bullet.xml | Spark/chainsaw | 1 recorded;  |
| data/entities/projectiles/deck/lightning.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/long_distance_cast.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/luminous_drill.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/machinegun_bullet.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/magic_shield_start.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/mass_polymorph.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/material_acid.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/material_blood.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/material_cement.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/material_oil.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/material_water.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/megalaser_beam.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/megalaser.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/meteor_rain.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/meteor.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/mine.xml | XML component subset | 8 recorded;  |
| data/entities/projectiles/deck/mist_alcohol.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/mist_blood.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/mist_radioactive.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/mist_slime.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/nuke_giga.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/nuke.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/ocarina/ocarina_a.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/ocarina/ocarina_a2.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/ocarina/ocarina_b.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/ocarina/ocarina_c.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/ocarina/ocarina_d.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/ocarina/ocarina_e.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/ocarina/ocarina_f.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/ocarina/ocarina_gsharp.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/orb_laseremitter_cutter.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/orb_laseremitter_four.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/orb_laseremitter.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/pebble_player.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/pipe_bomb.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/poison_blast.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/pollen.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/polymorph_field.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/powerdigger.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/projectile_gravity_field.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/projectile_thunder_field.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/projectile_transmutation_field.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/purple_explosion_field.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/regeneration_field.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/rock.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/rocket_player.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/rocket_tier_2.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/rocket_tier_3.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/rocket.xml | Terraria RocketI | 1 recorded;  |
| data/entities/projectiles/deck/rubber_ball.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/sea_acid_gas.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/sea_acid.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/sea_alcohol.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/sea_lava.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/sea_mimic.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/sea_oil.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/sea_swamp.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/sea_water.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/shield_field.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/slime.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/spiral_shot.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/spitter_tier_2.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/spitter_tier_3.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/spitter.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/spore_pod.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/summon_portal.xml | Preview only | 5 recorded;  |
| data/entities/projectiles/deck/super_teleport_cast.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/swapper.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/swarm_firebug.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/swarm_fly.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/swarm_wasp.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/teleport_cast.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/teleport_projectile_closer.xml | Custom TeleportCloser | 4 recorded;  |
| data/entities/projectiles/deck/teleport_projectile_short.xml | Custom Teleport | 2 recorded;  |
| data/entities/projectiles/deck/teleport_projectile_static.xml | Custom Teleport | 2 recorded;  |
| data/entities/projectiles/deck/teleport_projectile.xml | Custom Teleport | 2 recorded;  |
| data/entities/projectiles/deck/teleportation_field.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/deck/temporary_platform.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/temporary_wall.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/tentacle_portal.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/tentacle.xml | Custom Tentacle | 3 recorded;  |
| data/entities/projectiles/deck/thunder_blast.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/tnt.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/tntbox_big.xml | XML component subset | 5 recorded;  |
| data/entities/projectiles/deck/tntbox.xml | XML component subset | 5 recorded;  |
| data/entities/projectiles/deck/touch_alcohol.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/touch_blood.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/touch_gold.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/touch_grass.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/deck/touch_oil.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/touch_piss.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/touch_smoke.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/touch_water.xml | XML component subset | 2 recorded;  |
| data/entities/projectiles/deck/vacuum_entities.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/vacuum_liquid.xml | XML component subset | 7 recorded;  |
| data/entities/projectiles/deck/vacuum_powder.xml | XML component subset | 7 recorded;  |
| data/entities/projectiles/deck/wall_horizontal.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/wall_square.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/wall_vertical.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/deck/wand_ghost_player.xml | XML component subset | 22 recorded;  |
| data/entities/projectiles/deck/white_hole_big.xml | Custom BlackHole | 6 recorded;  |
| data/entities/projectiles/deck/white_hole_giga.xml | Custom BlackHole | 5 recorded;  |
| data/entities/projectiles/deck/white_hole.xml | Custom BlackHole | 3 recorded;  |
| data/entities/projectiles/deck/worm_rain.xml | XML component subset | 5 recorded;  |
| data/entities/projectiles/deck/worm_shot.xml | XML component subset | 5 recorded;  |
| data/entities/projectiles/deck/xray.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/orb_cursed.xml | XML component subset | 1 recorded;  |
| data/entities/projectiles/orb_expanding.xml | XML component subset | 3 recorded;  |
| data/entities/projectiles/propane_tank.xml | XML component subset | 4 recorded;  |
| data/entities/projectiles/thunderball.xml | XML component subset | 1 recorded;  |

## Modifier entities

| Extra entity | Coverage |
| --- | --- |
| data/entities/misc/accelerating_shot.xml | Movement adapter |
| data/entities/misc/anti_homing.xml | Movement adapter |
| data/entities/misc/arc_electric.xml | Deferred extra entity |
| data/entities/misc/arc_fire.xml | Gameplay augment adapter |
| data/entities/misc/arc_gunpowder.xml | Gameplay augment adapter |
| data/entities/misc/arc_poison.xml | Gameplay augment adapter |
| data/entities/misc/area_damage.xml | Gameplay augment adapter |
| data/entities/misc/autoaim.xml | Deferred extra entity |
| data/entities/misc/avoiding_arc.xml | Deferred extra entity |
| data/entities/misc/blood_to_acid.xml | Gameplay augment adapter |
| data/entities/misc/bounce_explosion.xml | Deferred extra entity |
| data/entities/misc/bounce_hole.xml | Deferred extra entity |
| data/entities/misc/bounce_larpa.xml | Deferred extra entity |
| data/entities/misc/bounce_laser_emitter.xml | Deferred extra entity |
| data/entities/misc/bounce_laser.xml | Deferred extra entity |
| data/entities/misc/bounce_lightning.xml | Deferred extra entity |
| data/entities/misc/bounce_small_explosion.xml | Deferred extra entity |
| data/entities/misc/bounce_spark.xml | Deferred extra entity |
| data/entities/misc/burn.xml | Gameplay augment adapter |
| data/entities/misc/caster_cast.xml | Deferred extra entity |
| data/entities/misc/chain_shot.xml | Deferred extra entity |
| data/entities/misc/chaotic_arc.xml | Movement adapter |
| data/entities/misc/clipping_shot.xml | Gameplay augment adapter |
| data/entities/misc/clusterbomb.xml | Deferred extra entity |
| data/entities/misc/colour_blue.xml | Deferred extra entity |
| data/entities/misc/colour_green.xml | Deferred extra entity |
| data/entities/misc/colour_invis.xml | Deferred extra entity |
| data/entities/misc/colour_orange.xml | Deferred extra entity |
| data/entities/misc/colour_purple.xml | Deferred extra entity |
| data/entities/misc/colour_rainbow.xml | Deferred extra entity |
| data/entities/misc/colour_red.xml | Deferred extra entity |
| data/entities/misc/colour_yellow.xml | Deferred extra entity |
| data/entities/misc/crumbling_earth_projectile.xml | Deferred extra entity |
| data/entities/misc/decelerating_shot.xml | Movement adapter |
| data/entities/misc/effect_meteor_rain.xml | Deferred extra entity |
| data/entities/misc/energy_shield_shot.xml | Gameplay augment adapter |
| data/entities/misc/essence_to_power.xml | Deferred extra entity |
| data/entities/misc/explosion_remove.xml | Gameplay augment adapter |
| data/entities/misc/explosion_tiny.xml | Deferred extra entity |
| data/entities/misc/fireball_ray_line.xml | Deferred extra entity |
| data/entities/misc/fireball_ray.xml | Deferred extra entity |
| data/entities/misc/fizzle.xml | Deferred extra entity |
| data/entities/misc/floating_arc.xml | Gameplay augment adapter |
| data/entities/misc/fly_downwards.xml | Deferred extra entity |
| data/entities/misc/fly_upwards.xml | Deferred extra entity |
| data/entities/misc/hitfx_burning_critical_hit.xml | Deferred extra entity |
| data/entities/misc/hitfx_critical_blood.xml | Deferred extra entity |
| data/entities/misc/hitfx_critical_oil.xml | Deferred extra entity |
| data/entities/misc/hitfx_critical_water.xml | Deferred extra entity |
| data/entities/misc/hitfx_curse_wither_electricity.xml | Deferred extra entity |
| data/entities/misc/hitfx_curse_wither_explosion.xml | Deferred extra entity |
| data/entities/misc/hitfx_curse_wither_melee.xml | Deferred extra entity |
| data/entities/misc/hitfx_curse_wither_projectile.xml | Deferred extra entity |
| data/entities/misc/hitfx_curse.xml | Deferred extra entity |
| data/entities/misc/hitfx_explode_alcohol_giga.xml | Deferred extra entity |
| data/entities/misc/hitfx_explode_alcohol.xml | Deferred extra entity |
| data/entities/misc/hitfx_explode_slime_giga.xml | Deferred extra entity |
| data/entities/misc/hitfx_explode_slime.xml | Deferred extra entity |
| data/entities/misc/hitfx_fireball_ray_enemy.xml | Deferred extra entity |
| data/entities/misc/hitfx_gravity_field_enemy.xml | Deferred extra entity |
| data/entities/misc/hitfx_lightning_ray_enemy.xml | Deferred extra entity |
| data/entities/misc/hitfx_petrify.xml | Deferred extra entity |
| data/entities/misc/hitfx_tentacle_ray_enemy.xml | Deferred extra entity |
| data/entities/misc/hitfx_toxic_charm.xml | Deferred extra entity |
| data/entities/misc/homing_accelerating.xml | Movement adapter |
| data/entities/misc/homing_area.xml | Movement adapter |
| data/entities/misc/homing_cursor.xml | Movement adapter |
| data/entities/misc/homing_rotate.xml | Movement adapter |
| data/entities/misc/homing_shooter.xml | Movement adapter |
| data/entities/misc/homing_short.xml | Movement adapter |
| data/entities/misc/homing_wand.xml | Deferred extra entity |
| data/entities/misc/homing.xml | Movement adapter |
| data/entities/misc/horizontal_arc.xml | Movement adapter |
| data/entities/misc/larpa_chaos_2.xml | Deferred extra entity |
| data/entities/misc/larpa_chaos.xml | Deferred extra entity |
| data/entities/misc/larpa_death.xml | Deferred extra entity |
| data/entities/misc/larpa_downwards.xml | Deferred extra entity |
| data/entities/misc/larpa_upwards.xml | Deferred extra entity |
| data/entities/misc/laser_emitter_ray.xml | Deferred extra entity |
| data/entities/misc/laser_emitter_wider.xml | Deferred extra entity |
| data/entities/misc/lava_to_blood.xml | Gameplay augment adapter |
| data/entities/misc/light.xml | Deferred extra entity |
| data/entities/misc/lightning_ray.xml | Deferred extra entity |
| data/entities/misc/line_arc.xml | Movement adapter |
| data/entities/misc/liquid_to_explosion.xml | Gameplay augment adapter |
| data/entities/misc/matter_eater.xml | Gameplay augment adapter |
| data/entities/misc/nolla.xml | Gameplay augment adapter |
| data/entities/misc/orbit_discs.xml | Deferred extra entity |
| data/entities/misc/orbit_fireballs.xml | Deferred extra entity |
| data/entities/misc/orbit_larpa.xml | Deferred extra entity |
| data/entities/misc/orbit_lasers.xml | Deferred extra entity |
| data/entities/misc/orbit_nukes.xml | Deferred extra entity |
| data/entities/misc/orbit_shot.xml | Movement adapter |
| data/entities/misc/phasing_arc.xml | Gameplay augment adapter |
| data/entities/misc/piercing_shot.xml | Gameplay augment adapter |
| data/entities/misc/pingpong_path.xml | Movement adapter |
| data/entities/misc/quantum_split.xml | Deferred extra entity |
| data/entities/misc/random_explosion.xml | Deferred extra entity |
| data/entities/misc/remove_bounce.xml | Deferred extra entity |
| data/entities/misc/rocket_downwards.xml | Deferred extra entity |
| data/entities/misc/rocket_octagon.xml | Deferred extra entity |
| data/entities/misc/sinewave.xml | Movement adapter |
| data/entities/misc/spells_to_power.xml | Deferred extra entity |
| data/entities/misc/spiraling_shot.xml | Deferred extra entity |
| data/entities/misc/static_to_sand.xml | Gameplay augment adapter |
| data/entities/misc/tentacle_ray.xml | Deferred extra entity |
| data/entities/misc/toxic_to_acid.xml | Gameplay augment adapter |
| data/entities/misc/transmutation.xml | Deferred extra entity |
| data/entities/misc/true_orbit.xml | Deferred extra entity |
| data/entities/misc/water_to_poison.xml | Gameplay augment adapter |
| data/entities/misc/zero_damage.xml | Gameplay augment adapter |
| data/entities/particles/blood_sparks.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/electricity.xml | Gameplay augment adapter |
| data/entities/particles/freeze_charge.xml | Gameplay augment adapter |
| data/entities/particles/heavy_shot.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/light_shot.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_green.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_orange.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_purple_bright.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_purple.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_red.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_white_small.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_white_weak.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_white.xml | Cosmetic extra entity not reproduced |
| data/entities/particles/tinyspark_yellow.xml | Cosmetic extra entity not reproduced |
