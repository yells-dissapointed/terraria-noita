#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.Projectiles;

public sealed class NoitaComponentProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagicMissile;
    public CastDiagnostics? Diagnostics => trace;
    public string EntityPath => settings?.Entity ?? "";
    private ComponentSpellProfile? settings;
    private TriggerRunner? triggers;
    private CastDiagnostics? trace;
    private SpellLiveCase? live;
    private VisualEntityEvidence? evidence;
    private ShotPlan shot = new();
    private Vector2 aim, launchVelocity;
    private int age, bounces, seaCursor;
    private bool cancelled, contacted;
    private readonly List<(Vector2 End, float Width)> rays = new();
    private readonly Dictionary<string, int> vacuum = new();
    private readonly HashSet<string> drawErrors = new();
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10; Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1; Projectile.ignoreWater = true; Projectile.timeLeft = 60;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 10;
    }
    public void Configure(ComponentSpellProfile profile, ProjectilePlan node, ShotPlan config, Vector2 direction, CastDiagnostics? diagnostics, SpellLiveCase? result, VisualEntityEvidence record)
    {
        launchVelocity = Projectile.velocity; settings = profile; shot = config; aim = direction; trace = diagnostics; live = result; evidence = record;
        triggers = new(node.Triggers); bounces = profile.Visual.Bounces; Projectile.tileCollide = profile.Visual.TileCollide;
        Projectile.localNPCHitCooldown = profile.HitInterval;
        if (profile.AreaRadius > 0) { var center = Projectile.Center; Projectile.width = Projectile.height = (int)Math.Clamp(profile.AreaRadius * 2, 8, 320); Projectile.Center = center; }
        if (profile.Beams.Count > 0) Projectile.damage = Math.Max(Projectile.damage, (int)Math.Round(profile.Beams.Max(b => b.Damage) * 25));
        if (profile.AreaDamage > 0) Projectile.damage = Math.Max(Projectile.damage, (int)Math.Round(profile.AreaDamage * 25));
        Projectile.friendly = Projectile.damage > 0;
    }
    public void RemoveForDebug() { cancelled = true; triggers = null; Projectile.active = false; trace?.Event("Component cleanup; future effects cancelled"); }
    private Player Owner => Main.player[Math.Clamp(Projectile.owner, 0, Main.maxPlayers - 1)];
    private void Emit(ShotPlan payload)
    {
        if (cancelled || Main.netMode != NetmodeID.SinglePlayer || !Owner.active) return;
        trace?.Event($"Component payload #{Projectile.whoAmI} age {age}");
        GameplayProjectileAdapter.Emit(payload, Projectile.GetSource_FromThis(), Owner, Projectile.Center, Projectile.velocity.LengthSquared() > .001f ? Vector2.Normalize(Projectile.velocity) : aim, trace, false, live);
    }
    public override bool? CanDamage() => settings != null && Projectile.damage > 0;
    public override void AI()
    {
        if (settings == null || Main.netMode != NetmodeID.SinglePlayer) { Projectile.active = false; return; }
        age++; triggers?.Observe(TriggerSignal.Tick, age, Emit);
        if (settings.ChargeFrames > 0 && age <= settings.ChargeFrames) { Projectile.velocity=Vector2.Zero; Lighting.AddLight(Projectile.Center,.2f,1,.4f); return; }
        if (settings.ChargeFrames > 0 && age == settings.ChargeFrames+1) Projectile.velocity=launchVelocity;
        ScriptAdapters();
        if (!Projectile.active) return;
        Projectile.velocity *= (float)settings.Visual.DragPerFrame; Projectile.velocity.Y += (float)settings.Visual.GravityPerFrame;
        Projectile.velocity = Vector2.Clamp(Projectile.velocity, new Vector2(-120), new Vector2(120));
        if (settings.HomingRange > 0)
        {
            var target = Main.npc.Where(n => n.active && n.CanBeChasedBy(Projectile) && Vector2.DistanceSquared(n.Center, Projectile.Center) < settings.HomingRange * settings.HomingRange)
                .OrderBy(n => Vector2.DistanceSquared(n.Center, Projectile.Center)).FirstOrDefault();
            if (target != null)
            {
                Vector2 desired = target.Center - Projectile.Center;
                if (desired.LengthSquared() > 1)
                { float speed = Math.Max(Projectile.velocity.Length(), settings.Visual.SpeedPerFrame == 0 ? .8f : .1f); float angle = Projectile.velocity.LengthSquared() > .001f ? Projectile.velocity.ToRotation() : aim.ToRotation();
                    float delta = MathHelper.WrapAngle(desired.ToRotation() - angle); Projectile.velocity = (angle + Math.Clamp(delta, -(float)settings.HomingTurn, (float)settings.HomingTurn)).ToRotationVector2() * speed; }
            }
        }
        if (settings.Visual.Rotate && Projectile.velocity.LengthSquared() > .001f) Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, (float)settings.Visual.LightR, (float)settings.Visual.LightG, (float)settings.Visual.LightB);
        if (settings.DigRadius > 0)
        {
            Vector2 original = Projectile.Center;
            int samples = Math.Clamp((int)Math.Ceiling(Projectile.velocity.Length() / 8), 1, 16);
            int cut = 0;
            for (int i = 0; i <= samples; i++) { Projectile.Center = original + Projectile.velocity * (i / (float)samples); cut += SpellTerrain.Eat(Projectile, (float)settings.DigRadius); }
            Projectile.Center = original; if (cut > 0) trace?.Event("Digging removed " + cut + " tiles");
        }
        BuildRays(); EmitMaterials(); ContactEffects();
        if (age % 6 == 0) { ConvertMaterials(); FieldEffects(); }
        if (age % 3 == 0 && settings.Visual.Sprites.Count == 0 && settings.Beams.Count == 0)
        {
            var color = EffectColor(); int id = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.MagicMirror, 0, 0, 100, color, .9f);
            Main.dust[id].noGravity = true; Main.dust[id].velocity *= .15f;
        }
    }
    private void ScriptAdapters()
    {
        if (settings == null) return;
        string name=settings.Name;
        if (name == "tentacle_portal" && age % 20 == 0 && Main.rand.Next(4)>0)
        {
            var target=Main.npc.Where(n=>n.active&&!n.friendly&&Vector2.DistanceSquared(n.Center,Projectile.Center)<72*72).FirstOrDefault();
            Vector2 direction=target==null ? Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() : (target.Center-Projectile.Center).SafeNormalize(aim);
            SpawnEntity("tentacle",Projectile.Center,direction);
        }
        if(name=="worm_rain" && age%40==0 && NPC.CountNPCS(ModContent.NPCType<global::terrarianoita.Content.NPCs.NoitaRainWorm>())<10)
        {
            var point=Projectile.Center+new Vector2(Main.rand.Next(-200,201),Main.rand.Next(-300,-159));
            if(point.X>32 && point.Y>32 && point.X<Main.maxTilesX*16-32 && point.Y<Main.maxTilesY*16-32)
            {
                int npcId=NPC.NewNPC(Projectile.GetSource_FromThis(),(int)point.X,(int)point.Y,ModContent.NPCType<global::terrarianoita.Content.NPCs.NoitaRainWorm>());
                if(npcId>=0 && npcId<Main.maxNPCs) Main.npc[npcId].GetGlobalNPC<SpellSpawnedNPC>().Diagnostics=trace;
            }
        }
        if(age != 1) return;
        string replacement=name switch { "all_acid"=>"acidburst", "all_blackholes"=>"black_hole", "all_deathcrosses"=>"death_cross", "all_discs"=>"disc_bullet_big", "all_nukes"=>"nuke", "all_rockets"=>"rocket", _=>"" };
        if(replacement.Length>0)
        {
            int count=0;
            foreach(var other in Main.projectile.Where(p=>p.active && p.whoAmI!=Projectile.whoAmI && p.ModProjectile is not NoitaBlast).ToArray())
            {
                string path=other.ModProjectile is NoitaComponentProjectile component ? component.EntityPath : other.ModProjectile is NoitaEffectProjectile effect ? effect.EntityPath : "";
                if(path.EndsWith("/"+replacement+".xml",StringComparison.Ordinal) || path.Contains("/all_",StringComparison.Ordinal)) continue;
                if(count>=128) break;
                Vector2 point=other.Center, direction=other.velocity.SafeNormalize(aim);
                string target=replacement=="death_cross" && Main.rand.NextBool() ? "death_cross_big" : replacement=="rocket" ? new[]{"rocket","rocket_tier_2","rocket_tier_3"}[Main.rand.Next(3)] : replacement;
                if(SpawnEntity(target,point,direction)>0) {other.active=false;count++;}
            }
            trace?.Event("Converted "+count+" existing projectiles to "+replacement);
        }
        if(name is "destruction" or "mass_polymorph")
        {
            foreach(var npc in Main.npc) if(npc.active && !npc.friendly && !npc.boss && npc.realLife<0 && Vector2.DistanceSquared(npc.Center,Projectile.Center)<200*200)
            { if(name=="destruction") npc.StrikeInstantKill(); else SpellStatus.Apply(npc,"polymorph",600); }
            if(name=="destruction") Owner.statLife=Math.Max(1,Owner.statLife-Math.Max(13,(int)Math.Ceiling(Owner.statLife*.075)));
            else if(Vector2.DistanceSquared(Owner.Center,Projectile.Center)<200*200) SpellStatus.Apply(Owner,"polymorph",300);
            trace?.Event(name+" applied; bosses excluded");
        }
    }
    private int SpawnEntity(string name,Vector2 point,Vector2 direction)
    {
        var payload=new ShotPlan{Committed=true,Config=new(shot.Config),Projectiles=new(){new(){Entity="data/entities/projectiles/deck/"+name+".xml"}}};
        // Generated child entities use the parent configuration without spawning additional modifiers recursively.
        payload.Config.Remove("extra_entities");
        return GameplayProjectileAdapter.Emit(payload,Projectile.GetSource_FromThis(),Owner,point,direction,trace,false,live);
    }
    private void BuildRays()
    {
        rays.Clear(); if (settings == null) return;
        foreach (var beam in settings.Beams)
        {
            Vector2 direction = (Projectile.rotation + (float)beam.Angle).ToRotationVector2();
            if (settings.Lightning) direction = aim;
            Vector2 end = Projectile.Center;
            for (float distance = 4; distance <= beam.Length; distance += 4)
            {
                end = Projectile.Center + direction * distance;
                if (!Collision.SolidCollision(end - new Vector2(2), 4, 4)) continue;
                if (beam.Cuts && age % 3 == 0) { var original = Projectile.Center; Projectile.Center = end; SpellTerrain.Eat(Projectile, (float)beam.Width + 2); Projectile.Center = original; }
                break;
            }
            rays.Add((end, (float)beam.Width * 2));
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (rays.Count > 0)
        {
            foreach (var ray in rays) { float collision = 0; if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, ray.End, ray.Width, ref collision)) return true; }
            return false;
        }
        if (settings?.AreaRadius > 0)
        { Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight()); return Vector2.DistanceSquared(closest, Projectile.Center) <= settings.AreaRadius * settings.AreaRadius; }
        return null;
    }
    private void EmitMaterials()
    {
        if (settings == null) return; var world = ModContent.GetInstance<MaterialWorld>();
        foreach (var emitter in settings.Emissions)
        {
            if (age % emitter.Interval != 0) continue;
            Vector2 position = Projectile.Center;
            if (emitter.Shape == "sea")
            {
                int columns = Math.Max(1, (int)(emitter.Radius * 2 / 16));
                if (seaCursor >= columns * 16) continue;
                position += new Vector2((seaCursor % columns - columns / 2) * 16, 32 + seaCursor / columns * 16); seaCursor++;
            }
            else if (emitter.Shape == "circle") position += (age * .42f).ToRotationVector2() * (float)emitter.Radius;
            else if (emitter.Shape == "rain") position += new Vector2(Main.rand.NextFloat(-(float)emitter.Radius, (float)emitter.Radius), 16);
            int added = world.Pour(position, emitter.Material, emitter.Amount, 1);
            if (added > 0 && age % 60 == 0) trace?.Event("Material emitter: " + emitter.Material + ", " + added + " units this tick");
        }
    }
    private void ContactEffects()
    {
        if (settings == null) return;
        bool pulse = age % settings.HitInterval == 0;
        foreach (var player in Main.player)
        {
            if (!player.active || player.dead || age <= settings.ShooterGrace && player.whoAmI == Projectile.owner) continue;
            bool touches = Colliding(Projectile.Hitbox, player.Hitbox) ?? Projectile.Hitbox.Intersects(player.Hitbox);
            if (!touches) continue;
            if (settings.Healing > 0 && (pulse || settings.AreaRadius == 0))
            { int hp = Math.Min(player.statLifeMax2 - player.statLife, Math.Max(1, (int)Math.Round(settings.Healing * 25))); if (hp > 0) { player.statLife += hp; player.HealEffect(hp); trace?.Event("Healing: " + hp + " HP"); }
                if (settings.AreaRadius == 0) { Projectile.Kill(); return; } }
            if (pulse && (settings.FriendlyFire || settings.AreaRadius > 0))
            {
                foreach (string status in settings.Statuses) SpellStatus.Apply(player, SpellStatus.EffectName(status), 90);
                if (Projectile.damage > 0 && settings.FriendlyFire) player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + " was caught in a Noita spell.")), Projectile.damage, 0);
            }
        }
        if (Projectile.damage == 0 || settings.AreaRadius > 0)
            foreach (var npc in Main.npc)
            {
                if (!npc.active || !(Colliding(Projectile.Hitbox, npc.Hitbox) ?? Projectile.Hitbox.Intersects(npc.Hitbox))) continue;
                if (pulse || settings.AreaRadius == 0)
                {
                    foreach (string status in settings.Statuses) SpellStatus.Apply(npc, SpellStatus.EffectName(status), 90);
                    if (settings.Healing > 0) npc.life = Math.Min(npc.lifeMax, npc.life + Math.Max(1, (int)Math.Round(settings.Healing * 25)));
                    if (settings.Name == "swapper" && !npc.boss && !npc.friendly && !contacted) Swap(npc);
                }
                if (settings.Visual.EntityCollide && !contacted && settings.AreaRadius == 0)
                { contacted = true; triggers?.Observe(TriggerSignal.Impact, age, Emit); if (settings.Visual.DieOnCollision && !settings.Penetrates && !SpellAugmentBinding.IsPiercing(Projectile)) { Projectile.Kill(); return; } }
            }
    }
    private void Swap(NPC npc)
    {
        Vector2 old = Owner.Center, next = npc.Center;
        if (Collision.SolidCollision(next - Owner.Size / 2, Owner.width, Owner.height) || Collision.SolidCollision(old - npc.Size / 2, npc.width, npc.height)) return;
        Owner.Teleport(next - Owner.Size / 2); npc.Center = old; npc.netUpdate = true; trace?.Event("Swapped caster and " + npc.FullName);
    }
    private void ConvertMaterials()
    {
        if (settings == null) return; var world = ModContent.GetInstance<MaterialWorld>();
        foreach (var conversion in settings.Conversions)
        {
            world.Convert(Projectile.Center, (float)conversion.Radius, conversion.To, conversion.Any ? "" : conversion.From);
            if (conversion.Any)
            {
                int cut = SpellTerrain.Eat(Projectile, (float)conversion.Radius);
                if (cut > 0) world.Pour(Projectile.Center, conversion.To, cut * 255, 4);
            }
            if (conversion.Entities)
                foreach (var npc in Main.npc)
                    if (npc.active && !npc.boss && !npc.friendly && npc.realLife < 0 && Vector2.Distance(npc.Center, Projectile.Center) < conversion.Radius)
                    { world.Pour(npc.Center, conversion.To, Math.Clamp(npc.width * npc.height, 32, 1000), 2); npc.StrikeInstantKill(); }
        }
    }
    private void FieldEffects()
    {
        if (settings == null) return; string name = settings.Name; var world = ModContent.GetInstance<MaterialWorld>();
        if (name is "vacuum_liquid" or "vacuum_powder")
        {
            int capacity = 10000 - vacuum.Values.Sum();
            foreach (var cell in world.Grid.Cells.Where(c => Vector2.DistanceSquared(Projectile.Center, new Vector2(c.X * 16 + 8, c.Y * 16 + 8)) < 64 * 64).Take(16).ToArray())
            {
                var meta = world.Catalog.Get(cell.Material); if (name == "vacuum_liquid" ? !meta.Scoopable : !meta.Sand) continue;
                int amount = world.Grid.Take(cell.X, cell.Y, Math.Min(capacity, 128),cell.Material); if (amount == 0) continue;
                vacuum[cell.Material] = vacuum.GetValueOrDefault(cell.Material) + amount; capacity -= amount;
            }
        }
        if (name is "vacuum_entities" or "projectile_gravity_field")
            foreach (var item in Main.item) if (item.active && Vector2.DistanceSquared(item.Center, Projectile.Center) < 256 * 256) item.velocity += (Projectile.Center - item.Center).SafeNormalize(Vector2.Zero) * 2;
        if (!name.StartsWith("projectile_", StringComparison.Ordinal) || !name.EndsWith("_field", StringComparison.Ordinal)) return;
        foreach (var other in Main.projectile)
        {
            if (!other.active || other.whoAmI == Projectile.whoAmI || Vector2.DistanceSquared(other.Center, Projectile.Center) > 80 * 80) continue;
            if (name == "projectile_gravity_field") other.velocity += (Projectile.Center - other.Center).SafeNormalize(Vector2.Zero) * .8f;
            else if (name == "projectile_transmutation_field") { world.Pour(other.Center, "blood", 64); other.active = false; }
            else if (name == "projectile_thunder_field" && other.hostile) { SpawnBlast(other.Center, 28, 25, false); other.active = false; }
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (settings == null) return;
        foreach (string status in settings.Statuses) SpellStatus.Apply(target, SpellStatus.EffectName(status), 180);
        triggers?.Observe(TriggerSignal.Impact, age, Emit);
        if (settings.Visual.DieOnCollision && !settings.Penetrates && !SpellAugmentBinding.IsPiercing(Projectile) && settings.AreaRadius == 0 && rays.Count == 0) Projectile.Kill();
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        triggers?.Observe(TriggerSignal.Impact, age, Emit);
        if (SpellMotionBinding.TryBounce(Projectile, oldVelocity)) return false;
        if (bounces-- > 0)
        { if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X; if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y; return false; }
        return settings?.Visual.DieOnCollision ?? true;
    }
    private void SpawnBlast(Vector2 center, float radius, int damage, bool hurtCaster)
    {
        int id = Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero, ModContent.ProjectileType<NoitaBlast>(), damage, 4, Projectile.owner);
        if (id >= 0 && id < Main.maxProjectiles) ((NoitaBlast)Main.projectile[id].ModProjectile).Configure(radius, hurtCaster, trace);
    }
    public override void OnKill(int timeLeft)
    {
        if (cancelled || settings == null || Main.netMode != NetmodeID.SinglePlayer) return;
        triggers?.Observe(TriggerSignal.Death, age, Emit); var world = ModContent.GetInstance<MaterialWorld>();
        bool explode = (timeLeft <= 0 ? settings.ExplodeOnExpiry : settings.ExplodeOnDeath) || shot.Number("explosion_radius") > 0;
        explode &= !Projectile.GetGlobalProjectile<SpellAugmentBinding>().ExplosionsDisabled;
        if (explode && settings.BlastRadius + shot.Number("explosion_radius") > 0)
        {
            int damage = (int)Math.Clamp(Owner.GetTotalDamage(DamageClass.Magic).ApplyTo((float)((settings.BlastDamage + shot.Number("damage_explosion_add")) * 25)), 0, 100000);
            float radius = (float)Math.Clamp(settings.BlastRadius + shot.Number("explosion_radius"), 1, 160);
            SpawnBlast(Projectile.Center, radius, damage, settings.BlastHurtsCaster);
            if (settings.BlastCuts) SpellTerrain.Eat(Projectile, Math.Min(radius, 64));
            if (settings.BlastMaterial.Length > 0) world.Pour(Projectile.Center, settings.BlastMaterial, 128, 2);
            trace?.Event($"Blast: {damage} HP / {radius:0.#} px");
        }
        if (settings.Name.StartsWith("egg_",StringComparison.Ordinal) && Main.npc.Count(n=>n.active && n.type==NPCID.BlueSlime)<20)
        {
            int type=settings.Name.Contains("fire") ? NPCID.LavaSlime : settings.Name.Contains("red") ? NPCID.RedSlime : NPCID.BlueSlime;
            int npcId=NPC.NewNPC(Projectile.GetSource_FromThis(),(int)Projectile.Center.X,(int)Projectile.Center.Y,type);
            if(npcId>=0 && npcId<Main.maxNPCs) Main.npc[npcId].GetGlobalNPC<SpellSpawnedNPC>().Diagnostics=trace;
        }
        if (settings.DeathMaterial.Length > 0) world.Pour(Projectile.Center, settings.DeathMaterial, 80, 2);
        foreach (var pair in vacuum) world.Pour(Projectile.Center, pair.Key, pair.Value, 8);
    }
    private Color EffectColor() => settings?.Name switch {
        string n when n.Contains("acid") => Color.GreenYellow,
        string n when n.Contains("fire") || n.Contains("lava") => Color.OrangeRed,
        string n when n.Contains("blood") => Color.Crimson,
        string n when n.Contains("heal") || n.Contains("regeneration") => Color.LimeGreen,
        string n when n.Contains("poison") || n.Contains("polymorph") => Color.Violet,
        string n when n.Contains("water") => Color.DodgerBlue,
        _ => Color.Cyan };
    public override bool PreDraw(ref Color lightColor)
    {
        if (settings == null) return false; bool drawn = false;
        foreach (var sprite in settings.Visual.Sprites)
        {
            bool ok = ModContent.GetInstance<AdapterSystem>().Assets.Draw(sprite, Projectile.Center, Projectile.rotation, age, SpellVisuals.Scale, out string error); drawn |= ok;
            if (!ok && drawErrors.Add(error)) evidence?.Gaps.Add(error);
        }
        var pixel = TextureAssets.MagicPixel.Value; Color color = EffectColor();
        foreach (var ray in rays)
        {
            Vector2 delta = ray.End - Projectile.Center; var quad = PixelQuad.Fit(pixel.Width,pixel.Height,Math.Max(1,delta.Length()),ray.Width);
            Main.spriteBatch.Draw(pixel, (Projectile.Center+ray.End)/2-Main.screenPosition, pixel.Bounds, color*.7f,delta.ToRotation(),new Vector2(quad.OriginX,quad.OriginY),new Vector2(quad.ScaleX,quad.ScaleY),SpriteEffects.None,0);
            drawn=true;
        }
        if (!drawn || settings.AreaRadius > 0 || settings.Emissions.Count > 0)
        {
            float radius = (float)Math.Clamp(settings.AreaRadius > 0 ? settings.AreaRadius : settings.Emissions.Count > 0 ? 24 : 5, 4, 160);
            for (int i = 0; i < 24; i++)
            { Vector2 p = Projectile.Center + (i * MathHelper.TwoPi / 24).ToRotationVector2() * radius; Main.spriteBatch.Draw(pixel, new Rectangle((int)(p.X-Main.screenPosition.X)-1,(int)(p.Y-Main.screenPosition.Y)-1,2,2), color*.65f); }
        }
        if (evidence != null) evidence.Appearance = drawn ? "Original sprite / raycast beam drawn" : "Component particle/ring effect";
        return false;
    }
}

public sealed class NoitaBlast : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.MagicMissile;
    private float radius = 16;
    private bool hurtCaster;
    private CastDiagnostics? trace;
    private int age;
    public CastDiagnostics? Diagnostics => trace;
    public override void SetDefaults() { Projectile.width = Projectile.height = 32; Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = -1; Projectile.timeLeft = 12; Projectile.tileCollide = false; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1; }
    public void Configure(float size, bool selfDamage, CastDiagnostics? diagnostics)
    { radius = Math.Clamp(size, 1, 160); hurtCaster = selfDamage; trace = diagnostics; var center = Projectile.Center; Projectile.width = Projectile.height = (int)Math.Ceiling(radius * 2); Projectile.Center = center; }
    public override bool? CanDamage() => age <= 2 && Projectile.damage > 0;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Vector2.DistanceSquared(Projectile.Center, Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight())) <= radius * radius;
    public override void AI()
    {
        age++; Lighting.AddLight(Projectile.Center, 1, .5f, .15f);
        if (age == 1 && hurtCaster && Projectile.damage > 0 && Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
        { var player = Main.player[Projectile.owner]; if (player.active && Colliding(Projectile.Hitbox, player.Hitbox) == true) player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(player.name + " was caught in a spell explosion.")), Projectile.damage, 0); }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float progress = age / 12f; Color color = Color.Lerp(Color.White, Color.OrangeRed, progress) * (1 - progress);
        for (int i = 0; i < 48; i++) { Vector2 p = Projectile.Center + (i * MathHelper.TwoPi / 48).ToRotationVector2() * radius * (.4f + .6f * progress); Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)(p.X-Main.screenPosition.X)-1,(int)(p.Y-Main.screenPosition.Y)-1,3,3),color); }
        return false;
    }
}
