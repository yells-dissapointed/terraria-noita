# Noita → Terraria projectile inventory

Inspected 197 emitted entity paths from 422 spells / 1266 casts. Complete details are in the companion JSON.

- No automatic match: 142 entity paths
- Candidate only: 32 entity paths
- Preserve Noita: 17 entity paths
- Implemented prototype: 6 entity paths

The implemented prototypes reuse real Terraria projectile types. Other matches are candidates requiring custom rules. Black holes and other distinctive Noita effects retain their original artwork/data. Candidate names are suggestions, not claimed equivalents.

| Noita spells | Status | Terraria reuse | Visual policy | Entity |
| --- | --- | --- | --- | --- |
| SUMMON_HOLLOW_EGG | No automatic match |  | Retain Noita visual | data/entities/items/pickup/egg_hollow.xml |
| SUMMON_WANDGHOST | No automatic match |  | Retain Noita visual | data/entities/particles/image_emitters/wand_effect.xml |
| BOMB_CART | Candidate only | Bomb / Dynamite | Retain Noita visual | data/entities/projectiles/bomb_cart.xml |
| BOMB_HOLY_GIGA | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/bomb_holy_giga.xml |
| BOMB_HOLY | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/bomb_holy.xml |
| BOMB | Implemented prototype | Bomb | Use Terraria sprite | data/entities/projectiles/bomb.xml |
| SOILBALL | No automatic match |  | Retain Noita visual | data/entities/projectiles/chunk_of_soil.xml |
| DARKFLAME | No automatic match |  | Retain Noita visual | data/entities/projectiles/darkflame.xml |
| ACIDSHOT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/acidshot.xml |
| ALCOHOL_BLAST | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/alcohol_blast.xml |
| ALL_ACID | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/all_acid.xml |
| ALL_BLACKHOLES | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/all_blackholes.xml |
| ALL_DEATHCROSSES | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/all_deathcrosses.xml |
| ALL_DISCS | Candidate only | LightDisc / DeathSickle | Retain Noita visual | data/entities/projectiles/deck/all_discs.xml |
| ALL_NUKES | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/all_nukes.xml |
| ALL_ROCKETS | Candidate only | RocketI / RocketIII | Retain Noita visual | data/entities/projectiles/deck/all_rockets.xml |
| ARROW | Implemented prototype | WoodenArrowFriendly | Use Terraria sprite | data/entities/projectiles/deck/arrow.xml |
| BALL_LIGHTNING | Candidate only | CultistBossLightningOrbArc | Retain Noita visual | data/entities/projectiles/deck/ball_lightning.xml |
| BERSERK_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/berserk_field.xml |
| BIG_MAGIC_SHIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/big_magic_shield_start.xml |
| BLACK_HOLE_BIG | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/black_hole_big.xml |
| BLACK_HOLE, BLACK_HOLE_DEATH_TRIGGER | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/black_hole.xml |
| BOMB_DETONATOR | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/bomb_detonator.xml |
| BOUNCY_ORB, BOUNCY_ORB_TIMER | Candidate only | WaterBolt | Retain Noita visual | data/entities/projectiles/deck/bouncy_orb.xml |
| BUBBLESHOT, BUBBLESHOT_TRIGGER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/bubbleshot.xml |
| BUCKSHOT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/buckshot_player.xml |
| HEAVY_BULLET, HEAVY_BULLET_TIMER, HEAVY_BULLET_TRIGGER | Implemented prototype | Bullet | Retain Noita visual | data/entities/projectiles/deck/bullet_heavy.xml |
| SLOW_BULLET, SLOW_BULLET_TIMER, SLOW_BULLET_TRIGGER | Implemented prototype | Bullet | Retain Noita visual | data/entities/projectiles/deck/bullet_slow.xml |
| BULLET, BULLET_TIMER, BULLET_TRIGGER | Implemented prototype | Bullet | Retain Noita visual | data/entities/projectiles/deck/bullet.xml |
| CHAIN_BOLT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/chain_bolt.xml |
| CHAINSAW | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/chainsaw.xml |
| CHAOS_POLYMORPH_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/chaos_polymorph_field.xml |
| CIRCLE_ACID | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/circle_acid.xml |
| CIRCLE_FIRE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/circle_fire.xml |
| CIRCLE_OIL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/circle_oil.xml |
| CIRCLE_WATER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/circle_water.xml |
| CLOUD_ACID | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/cloud_acid.xml |
| CLOUD_BLOOD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/cloud_blood.xml |
| CLOUD_OIL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/cloud_oil.xml |
| CLOUD_THUNDER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/cloud_thunder.xml |
| CLOUD_WATER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/cloud_water.xml |
| CRUMBLING_EARTH | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/crumbling_earth.xml |
| DEATH_CROSS_BIG | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/death_cross_big.xml |
| DEATH_CROSS | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/death_cross.xml |
| DELAYED_SPELL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/delayed_spell.xml |
| DESTRUCTION | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/destruction.xml |
| DIGGER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/digger.xml |
| DISC_BULLET_BIG | Candidate only | LightDisc / DeathSickle | Retain Noita visual | data/entities/projectiles/deck/disc_bullet_big.xml |
| DISC_BULLET_BIGGER | Candidate only | LightDisc / DeathSickle | Retain Noita visual | data/entities/projectiles/deck/disc_bullet_bigger.xml |
| DISC_BULLET | Candidate only | LightDisc / DeathSickle | Retain Noita visual | data/entities/projectiles/deck/disc_bullet.xml |
| EXPLODING_DUCKS | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/duck.xml |
| ELECTROCUTION_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/electrocution_field.xml |
| EXPLODING_DEER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/exploding_deer.xml |
| EXPLOSION_LIGHT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/explosion_light.xml |
| EXPLOSION | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/explosion.xml |
| FIREBALL | Candidate only | BallofFire / Flames | Retain Noita visual | data/entities/projectiles/deck/fireball.xml |
| FIRE_BLAST | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/fireblast.xml |
| FIREBOMB | Candidate only | BallofFire / Flames | Retain Noita visual | data/entities/projectiles/deck/firebomb.xml |
| FISH | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/fish.xml |
| FLAMETHROWER | Candidate only | BallofFire / Flames | Retain Noita visual | data/entities/projectiles/deck/flamethrower.xml |
| FREEZE_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/freeze_field.xml |
| FREEZING_GAZE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/freezing_gaze_beam.xml |
| FRIEND_FLY | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/friend_fly.xml |
| GLITTER_BOMB | Candidate only | Grenade | Retain Noita visual | data/entities/projectiles/deck/glitter_bomb.xml |
| GLOWING_BOLT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/glowing_bolt.xml |
| GLUE_SHOT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/glue_shot.xml |
| GRENADE_ANTI | Candidate only | Grenade | Retain Noita visual | data/entities/projectiles/deck/grenade_anti.xml |
| GRENADE_LARGE | Candidate only | Grenade | Retain Noita visual | data/entities/projectiles/deck/grenade_large.xml |
| GRENADE_TIER_2 | Candidate only | Grenade | Retain Noita visual | data/entities/projectiles/deck/grenade_tier_2.xml |
| GRENADE_TIER_3 | Candidate only | Grenade | Retain Noita visual | data/entities/projectiles/deck/grenade_tier_3.xml |
| GRENADE, GRENADE_TRIGGER | Candidate only | Grenade | Retain Noita visual | data/entities/projectiles/deck/grenade.xml |
| HEAL_BULLET | Candidate only | Healing helpers | Retain Noita visual | data/entities/projectiles/deck/heal_bullet.xml |
| ANTIHEAL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/healhurt.xml |
| HOOK | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/hook.xml |
| ICEBALL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/iceball.xml |
| INFESTATION | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/infestation.xml |
| KANTELE_A | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/kantele/kantele_a.xml |
| KANTELE_D | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/kantele/kantele_d.xml |
| KANTELE_DIS | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/kantele/kantele_dis.xml |
| KANTELE_E | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/kantele/kantele_e.xml |
| KANTELE_G | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/kantele/kantele_g.xml |
| LANCE_HOLY | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/lance_holy.xml |
| LANCE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/lance.xml |
| LASER | Candidate only | PurpleLaser / LaserMachinegunLaser | Retain Noita visual | data/entities/projectiles/deck/laser.xml |
| LEVITATION_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/levitation_field.xml |
| AIR_BULLET | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/light_bullet_air.xml |
| LIGHT_BULLET_TRIGGER_2 | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/light_bullet_blue.xml |
| ACCELERATING_SHOT, ACID_TRAIL, ACIDSHOT, ADD_DEATH_TRIGGER, ADD_TIMER, ADD_TRIGGER, AIR_BULLET, ALCOHOL_BLAST, ALL_ACID, ALL_BLACKHOLES, ALL_DEATHCROSSES, ALL_DISCS, ALL_NUKES, ALL_ROCKETS, ALPHA, ANTI_HOMING, ANTIHEAL, ARC_ELECTRIC, ARC_FIRE, ARC_GUNPOWDER, ARC_POISON, AREA_DAMAGE, ARROW, AUTOAIM, AVOIDING_ARC, BALL_LIGHTNING, BERSERK_FIELD, BIG_MAGIC_SHIELD, BLACK_HOLE, BLACK_HOLE_BIG, BLACK_HOLE_DEATH_TRIGGER, BLOOD_TO_ACID, BLOODLUST, BOMB, BOMB_CART, BOMB_DETONATOR, BOMB_HOLY, BOMB_HOLY_GIGA, BOUNCE, BOUNCE_EXPLOSION, BOUNCE_HOLE, BOUNCE_LARPA, BOUNCE_LASER, BOUNCE_LASER_EMITTER, BOUNCE_LIGHTNING, BOUNCE_SMALL_EXPLOSION, BOUNCE_SPARK, BOUNCY_ORB, BOUNCY_ORB_TIMER, BUBBLESHOT, BUBBLESHOT_TRIGGER, BUCKSHOT, BULLET, BULLET_TIMER, BULLET_TRIGGER, BURN_TRAIL, BURST_2, BURST_3, BURST_4, BURST_8, BURST_X, CASTER_CAST, CHAIN_BOLT, CHAIN_SHOT, CHAINSAW, CHAOS_POLYMORPH_FIELD, CHAOTIC_ARC, CIRCLE_ACID, CIRCLE_FIRE, CIRCLE_OIL, CIRCLE_SHAPE, CIRCLE_WATER, CLIPPING_SHOT, CLOUD_ACID, CLOUD_BLOOD, CLOUD_OIL, CLOUD_THUNDER, CLOUD_WATER, CLUSTERMOD, COLOUR_BLUE, COLOUR_GREEN, COLOUR_INVIS, COLOUR_ORANGE, COLOUR_PURPLE, COLOUR_RAINBOW, COLOUR_RED, COLOUR_YELLOW, CRITICAL_HIT, CRUMBLING_EARTH, CRUMBLING_EARTH_PROJECTILE, CURSE, CURSE_WITHER_ELECTRICITY, CURSE_WITHER_EXPLOSION, CURSE_WITHER_MELEE, CURSE_WITHER_PROJECTILE, CURSED_ORB, DAMAGE, DAMAGE_FOREVER, DARKFLAME, DEATH_CROSS, DEATH_CROSS_BIG, DECELERATING_SHOT, DELAYED_SPELL, DESTRUCTION, DIGGER, DISC_BULLET, DISC_BULLET_BIG, DISC_BULLET_BIGGER, DIVIDE_10, DIVIDE_2, DIVIDE_3, DIVIDE_4, DUPLICATE, DYNAMITE, ELECTRIC_CHARGE, ELECTROCUTION_FIELD, ENERGY_SHIELD, ENERGY_SHIELD_SECTOR, ENERGY_SHIELD_SHOT, ESSENCE_TO_POWER, EXPANDING_ORB, EXPLODING_DEER, EXPLODING_DUCKS, EXPLOSION, EXPLOSION_LIGHT, EXPLOSION_REMOVE, EXPLOSION_TINY, EXPLOSIVE_PROJECTILE, FIRE_BLAST, FIRE_TRAIL, FIREBALL, FIREBALL_RAY, FIREBALL_RAY_ENEMY, FIREBALL_RAY_LINE, FIREBOMB, FISH, FIZZLE, FLAMETHROWER, FLOATING_ARC, FLY_DOWNWARDS, FLY_UPWARDS, FREEZE, FREEZE_FIELD, FREEZING_GAZE, FRIEND_FLY, FUNKY_SPELL, GAMMA, GLITTER_BOMB, GLOWING_BOLT, GLUE_SHOT, GRAVITY, GRAVITY_ANTI, GRAVITY_FIELD_ENEMY, GRENADE, GRENADE_ANTI, GRENADE_LARGE, GRENADE_TIER_2, GRENADE_TIER_3, GRENADE_TRIGGER, GUNPOWDER_TRAIL, HEAL_BULLET, HEAVY_BULLET, HEAVY_BULLET_TIMER, HEAVY_BULLET_TRIGGER, HEAVY_SHOT, HEAVY_SPREAD, HEXA_SHOT, HITFX_BURNING_CRITICAL_HIT, HITFX_CRITICAL_BLOOD, HITFX_CRITICAL_OIL, HITFX_CRITICAL_WATER, HITFX_EXPLOSION_ALCOHOL, HITFX_EXPLOSION_ALCOHOL_GIGA, HITFX_EXPLOSION_SLIME, HITFX_EXPLOSION_SLIME_GIGA, HITFX_PETRIFY, HITFX_TOXIC_CHARM, HOMING, HOMING_ACCELERATING, HOMING_AREA, HOMING_CURSOR, HOMING_ROTATE, HOMING_SHOOTER, HOMING_SHORT, HOMING_WAND, HOOK, HORIZONTAL_ARC, I_SHAPE, I_SHOT, ICEBALL, IF_ELSE, IF_END, INFESTATION, KANTELE_A, KANTELE_D, KANTELE_DIS, KANTELE_E, KANTELE_G, KNOCKBACK, LANCE, LANCE_HOLY, LARPA_CHAOS, LARPA_CHAOS_2, LARPA_DEATH, LARPA_DOWNWARDS, LARPA_UPWARDS, LASER, LASER_EMITTER, LASER_EMITTER_CUTTER, LASER_EMITTER_FOUR, LASER_EMITTER_RAY, LASER_EMITTER_WIDER, LASER_LUMINOUS_DRILL, LAVA_TO_BLOOD, LEVITATION_FIELD, LIFETIME, LIFETIME_DOWN, LIGHT, LIGHT_BULLET, LIGHT_BULLET_TIMER, LIGHT_BULLET_TRIGGER, LIGHT_BULLET_TRIGGER_2, LIGHT_SHOT, LIGHTNING, LIGHTNING_RAY, LIGHTNING_RAY_ENEMY, LINE_ARC, LIQUID_TO_EXPLOSION, LONG_DISTANCE_CAST, LUMINOUS_DRILL, MAGIC_SHIELD, MANA_REDUCE, MASS_POLYMORPH, MATERIAL_ACID, MATERIAL_BLOOD, MATERIAL_CEMENT, MATERIAL_OIL, MATERIAL_WATER, MATTER_EATER, MEGALASER, METEOR, METEOR_RAIN, MINE, MINE_DEATH_TRIGGER, MISSILE, MIST_ALCOHOL, MIST_BLOOD, MIST_RADIOACTIVE, MIST_SLIME, MU, NECROMANCY, NOLLA, NUKE, NUKE_GIGA, OCARINA_A, OCARINA_A2, OCARINA_B, OCARINA_C, OCARINA_D, OCARINA_E, OCARINA_F, OCARINA_GSHARP, OIL_TRAIL, OMEGA, ORBIT_DISCS, ORBIT_FIREBALLS, ORBIT_LARPA, ORBIT_LASERS, ORBIT_NUKES, ORBIT_SHOT, PEBBLE, PENTA_SHOT, PENTAGRAM_SHAPE, PHASING_ARC, PHI, PIERCING_SHOT, PINGPONG_PATH, PIPE_BOMB, PIPE_BOMB_DEATH_TRIGGER, POISON_BLAST, POISON_TRAIL, POLLEN, POLYMORPH_FIELD, POWERDIGGER, PROJECTILE_GRAVITY_FIELD, PROJECTILE_THUNDER_FIELD, PROJECTILE_TRANSMUTATION_FIELD, PROPANE_TANK, PURPLE_EXPLOSION_FIELD, QUAD_SHOT, QUANTUM_SPLIT, RAINBOW_TRAIL, RANDOM_EXPLOSION, RECHARGE, RECOIL, RECOIL_DAMPER, REGENERATION_FIELD, REMOVE_BOUNCE, ROCKET, ROCKET_DOWNWARDS, ROCKET_OCTAGON, ROCKET_TIER_2, ROCKET_TIER_3, RUBBER_BALL, SCATTER_2, SCATTER_3, SCATTER_4, SEA_ACID, SEA_ACID_GAS, SEA_ALCOHOL, SEA_LAVA, SEA_MIMIC, SEA_OIL, SEA_SWAMP, SEA_WATER, SHIELD_FIELD, SIGMA, SINEWAVE, SLIMEBALL, SLOW_BULLET, SLOW_BULLET_TIMER, SLOW_BULLET_TRIGGER, SLOW_BUT_STEADY, SOILBALL, SPEED, SPELLS_TO_POWER, SPIRAL_SHOT, SPIRALING_SHOT, SPITTER, SPITTER_TIER_2, SPITTER_TIER_2_TIMER, SPITTER_TIER_3, SPITTER_TIER_3_TIMER, SPITTER_TIMER, SPORE_POD, SPREAD_REDUCE, STATIC_TO_SAND, SUMMON_HOLLOW_EGG, SUMMON_PORTAL, SUMMON_ROCK, SUMMON_WANDGHOST, SUPER_TELEPORT_CAST, SWAPPER_PROJECTILE, SWARM_FIREBUG, SWARM_FLY, SWARM_WASP, T_SHAPE, T_SHOT, TAU, TELEPORT_CAST, TELEPORT_PROJECTILE, TELEPORT_PROJECTILE_CLOSER, TELEPORT_PROJECTILE_SHORT, TELEPORT_PROJECTILE_STATIC, TELEPORTATION_FIELD, TEMPORARY_PLATFORM, TEMPORARY_WALL, TENTACLE, TENTACLE_PORTAL, TENTACLE_RAY, TENTACLE_RAY_ENEMY, TENTACLE_TIMER, THUNDER_BLAST, THUNDERBALL, TINY_GHOST, TNTBOX, TNTBOX_BIG, TORCH, TORCH_ELECTRIC, TOUCH_ALCOHOL, TOUCH_BLOOD, TOUCH_GOLD, TOUCH_GRASS, TOUCH_OIL, TOUCH_PISS, TOUCH_SMOKE, TOUCH_WATER, TOXIC_TO_ACID, TRANSMUTATION, TRUE_ORBIT, UNSTABLE_GUNPOWDER, VACUUM_ENTITIES, VACUUM_LIQUID, VACUUM_POWDER, W_SHAPE, W_SHOT, WALL_HORIZONTAL, WALL_SQUARE, WALL_VERTICAL, WATER_TO_POISON, WATER_TRAIL, WHITE_HOLE, WHITE_HOLE_BIG, WORM_RAIN, WORM_SHOT, X_RAY, Y_SHAPE, Y_SHOT, ZERO_DAMAGE | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/light_bullet.xml |
| LIGHTNING | Candidate only | CultistBossLightningOrbArc | Retain Noita visual | data/entities/projectiles/deck/lightning.xml |
| LONG_DISTANCE_CAST | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/long_distance_cast.xml |
| LASER_LUMINOUS_DRILL, LUMINOUS_DRILL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/luminous_drill.xml |
| FUNKY_SPELL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/machinegun_bullet.xml |
| MAGIC_SHIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/magic_shield_start.xml |
| MASS_POLYMORPH | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/mass_polymorph.xml |
| MATERIAL_ACID | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/material_acid.xml |
| MATERIAL_BLOOD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/material_blood.xml |
| MATERIAL_CEMENT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/material_cement.xml |
| MATERIAL_OIL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/material_oil.xml |
| MATERIAL_WATER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/material_water.xml |
| MEGALASER | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/megalaser_beam.xml |
| MEGALASER | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/megalaser.xml |
| METEOR_RAIN | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/meteor_rain.xml |
| METEOR | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/meteor.xml |
| MINE, MINE_DEATH_TRIGGER | Candidate only | ProximityMineI | Retain Noita visual | data/entities/projectiles/deck/mine.xml |
| MIST_ALCOHOL | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/mist_alcohol.xml |
| MIST_BLOOD | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/mist_blood.xml |
| MIST_RADIOACTIVE | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/mist_radioactive.xml |
| MIST_SLIME | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/mist_slime.xml |
| NUKE_GIGA | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/nuke_giga.xml |
| NUKE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/nuke.xml |
| OCARINA_A | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/ocarina/ocarina_a.xml |
| OCARINA_A2 | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/ocarina/ocarina_a2.xml |
| OCARINA_B | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/ocarina/ocarina_b.xml |
| OCARINA_C | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/ocarina/ocarina_c.xml |
| OCARINA_D | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/ocarina/ocarina_d.xml |
| OCARINA_E | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/ocarina/ocarina_e.xml |
| OCARINA_F | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/ocarina/ocarina_f.xml |
| OCARINA_GSHARP | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/ocarina/ocarina_gsharp.xml |
| LASER_EMITTER_CUTTER | Candidate only | PurpleLaser / LaserMachinegunLaser | Retain Noita visual | data/entities/projectiles/deck/orb_laseremitter_cutter.xml |
| LASER_EMITTER_FOUR | Candidate only | PurpleLaser / LaserMachinegunLaser | Retain Noita visual | data/entities/projectiles/deck/orb_laseremitter_four.xml |
| LASER_EMITTER | Candidate only | PurpleLaser / LaserMachinegunLaser | Retain Noita visual | data/entities/projectiles/deck/orb_laseremitter.xml |
| PEBBLE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/pebble_player.xml |
| PIPE_BOMB, PIPE_BOMB_DEATH_TRIGGER | Candidate only | Grenade | Retain Noita visual | data/entities/projectiles/deck/pipe_bomb.xml |
| POISON_BLAST | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/poison_blast.xml |
| POLLEN | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/pollen.xml |
| POLYMORPH_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/polymorph_field.xml |
| POWERDIGGER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/powerdigger.xml |
| PROJECTILE_GRAVITY_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/projectile_gravity_field.xml |
| PROJECTILE_THUNDER_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/projectile_thunder_field.xml |
| PROJECTILE_TRANSMUTATION_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/projectile_transmutation_field.xml |
| PURPLE_EXPLOSION_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/purple_explosion_field.xml |
| REGENERATION_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/regeneration_field.xml |
| SUMMON_ROCK | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/rock.xml |
| MISSILE | Candidate only | RocketI / RocketIII | Retain Noita visual | data/entities/projectiles/deck/rocket_player.xml |
| ROCKET_TIER_2 | Candidate only | RocketI / RocketIII | Retain Noita visual | data/entities/projectiles/deck/rocket_tier_2.xml |
| ROCKET_TIER_3 | Candidate only | RocketI / RocketIII | Retain Noita visual | data/entities/projectiles/deck/rocket_tier_3.xml |
| ROCKET | Implemented prototype | RocketI | Retain Noita visual | data/entities/projectiles/deck/rocket.xml |
| RUBBER_BALL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/rubber_ball.xml |
| SEA_ACID_GAS | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/sea_acid_gas.xml |
| SEA_ACID | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/sea_acid.xml |
| SEA_ALCOHOL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/sea_alcohol.xml |
| SEA_LAVA | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/sea_lava.xml |
| SEA_MIMIC | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/sea_mimic.xml |
| SEA_OIL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/sea_oil.xml |
| SEA_SWAMP | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/sea_swamp.xml |
| SEA_WATER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/sea_water.xml |
| SHIELD_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/shield_field.xml |
| SLIMEBALL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/slime.xml |
| SPIRAL_SHOT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/spiral_shot.xml |
| SPITTER_TIER_2, SPITTER_TIER_2_TIMER | Candidate only | Bullet | Retain Noita visual | data/entities/projectiles/deck/spitter_tier_2.xml |
| SPITTER_TIER_3, SPITTER_TIER_3_TIMER | Candidate only | Bullet | Retain Noita visual | data/entities/projectiles/deck/spitter_tier_3.xml |
| SPITTER, SPITTER_TIMER | Candidate only | Bullet | Retain Noita visual | data/entities/projectiles/deck/spitter.xml |
| SPORE_POD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/spore_pod.xml |
| SUMMON_PORTAL | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/summon_portal.xml |
| SUPER_TELEPORT_CAST | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/super_teleport_cast.xml |
| SWAPPER_PROJECTILE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/swapper.xml |
| SWARM_FIREBUG | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/swarm_firebug.xml |
| SWARM_FLY | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/swarm_fly.xml |
| SWARM_WASP | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/swarm_wasp.xml |
| TELEPORT_CAST | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/teleport_cast.xml |
| TELEPORT_PROJECTILE_CLOSER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/teleport_projectile_closer.xml |
| TELEPORT_PROJECTILE_SHORT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/teleport_projectile_short.xml |
| TELEPORT_PROJECTILE_STATIC | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/teleport_projectile_static.xml |
| TELEPORT_PROJECTILE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/teleport_projectile.xml |
| TELEPORTATION_FIELD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/teleportation_field.xml |
| TEMPORARY_PLATFORM | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/temporary_platform.xml |
| TEMPORARY_WALL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/temporary_wall.xml |
| TENTACLE_PORTAL | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/tentacle_portal.xml |
| TENTACLE, TENTACLE_TIMER | Preserve Noita |  | Retain Noita visual | data/entities/projectiles/deck/tentacle.xml |
| THUNDER_BLAST | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/thunder_blast.xml |
| DYNAMITE | Candidate only | Bomb / Dynamite | Retain Noita visual | data/entities/projectiles/deck/tnt.xml |
| TNTBOX_BIG | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/tntbox_big.xml |
| TNTBOX | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/tntbox.xml |
| TOUCH_ALCOHOL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/touch_alcohol.xml |
| TOUCH_BLOOD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/touch_blood.xml |
| TOUCH_GOLD | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/touch_gold.xml |
| TOUCH_GRASS | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/touch_grass.xml |
| TOUCH_OIL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/touch_oil.xml |
| TOUCH_PISS | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/touch_piss.xml |
| TOUCH_SMOKE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/touch_smoke.xml |
| TOUCH_WATER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/touch_water.xml |
| VACUUM_ENTITIES | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/vacuum_entities.xml |
| VACUUM_LIQUID | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/vacuum_liquid.xml |
| VACUUM_POWDER | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/vacuum_powder.xml |
| WALL_HORIZONTAL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/wall_horizontal.xml |
| WALL_SQUARE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/wall_square.xml |
| WALL_VERTICAL | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/wall_vertical.xml |
| SUMMON_WANDGHOST | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/wand_ghost_player.xml |
| WHITE_HOLE_BIG | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/white_hole_big.xml |
| WHITE_HOLE | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/white_hole.xml |
| WORM_RAIN | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/worm_rain.xml |
| WORM_SHOT | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/worm_shot.xml |
| X_RAY | No automatic match |  | Retain Noita visual | data/entities/projectiles/deck/xray.xml |
| CURSED_ORB | No automatic match |  | Retain Noita visual | data/entities/projectiles/orb_cursed.xml |
| EXPANDING_ORB | No automatic match |  | Retain Noita visual | data/entities/projectiles/orb_expanding.xml |
| PROPANE_TANK | No automatic match |  | Retain Noita visual | data/entities/projectiles/propane_tank.xml |
| THUNDERBALL | No automatic match |  | Retain Noita visual | data/entities/projectiles/thunderball.xml |

Primary references: [Projectile IDs](https://docs.tmodloader.net/docs/stable/class_projectile_i_d.html), [projectile API](https://docs.tmodloader.net/docs/stable/class_projectile.html), [global projectile hooks](https://docs.tmodloader.net/docs/stable/class_global_projectile.html). Noita definitions inspected locally; original code/assets are not bundled.

