#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Core;

namespace terrarianoita.Common;

public static class SpellStatus
{
    public static int Buff(string name) => name.ToLowerInvariant() switch {
        "poison" or "poisoned" => BuffID.Poisoned,
        "radioactive_liquid" or "acid" or "acid_gas" => BuffID.Venom,
        "fire" or "lava" or "on_fire" or "burning" => BuffID.OnFire,
        "oil" or "oiled" => BuffID.Oiled, "alcohol" => BuffID.Tipsy,
        "frozen" or "freeze" => BuffID.Frozen,
        "electrocuted" or "electricity" => BuffID.Electrified,
        "slime" or "movement_slower" => BuffID.Slow,
        "hp_regeneration" or "regeneration" or "magic_liquid_hp_regeneration" => BuffID.Regeneration,
        "berserk" or "magic_liquid_berserk" => BuffID.Wrath,
        "movement_faster" or "magic_liquid_movement_faster" => BuffID.Swiftness,
        "invisibility" or "magic_liquid_invisibility" => BuffID.Invisibility,
        "protection_all" => BuffID.Ironskin,
        "confusion" or "magic_liquid_confusion" => BuffID.Confused,
        "levitation" or "magic_liquid_faster_levitation" => BuffID.Featherfall,
        "mana_regeneration" or "magic_liquid_mana_regeneration" => BuffID.ManaRegeneration,
        "water" or "water_salt" or "water_ice" => BuffID.Wet,
        _ => 0 };
    public static void Apply(Player player, NoitaMaterial material, int duration)
    {
        Apply(player, material.Name, duration);
        foreach (string status in material.Status.Split(',', StringSplitOptions.RemoveEmptyEntries)) Apply(player, status, duration);
        if (material.Name.StartsWith("water", StringComparison.Ordinal)) { player.ClearBuff(BuffID.OnFire); player.ClearBuff(BuffID.Oiled); }
    }
    public static void Apply(NPC npc, NoitaMaterial material, int duration)
    {
        Apply(npc, material.Name, duration);
        foreach (string status in material.Status.Split(',', StringSplitOptions.RemoveEmptyEntries)) Apply(npc, status, duration);
    }
    public static string EffectName(string path) => System.IO.Path.GetFileNameWithoutExtension(path).Replace("effect_", "").Replace("apply_", "");
    public static void Apply(Player player, string status, int duration)
    {
        status = status.ToLowerInvariant(); int buff = Buff(status); if (buff > 0) player.AddBuff(buff, duration);
        if (status.Contains("polymorph", StringComparison.Ordinal)) { player.AddBuff(BuffID.Cursed, duration); player.AddBuff(BuffID.Weak, duration); }
        if (status.Contains("teleportation", StringComparison.Ordinal) && player.GetModPlayer<SpellStatusPlayer>().TeleportCooldown == 0)
        {
            var target = player.Center + new Vector2(Main.rand.Next(-320, 321), Main.rand.Next(-160, 161));
            var landing = SpellLanding.Find(new System.Numerics.Vector2(target.X, target.Y), p => p.X > 32 && p.Y > 32 && p.X < Main.maxTilesX * 16 - 32 && p.Y < Main.maxTilesY * 16 - 64 &&
                !Collision.SolidCollision(new Vector2(p.X - player.width / 2f, p.Y - player.height / 2f), player.width, player.height));
            if (landing is { } point) player.Teleport(new Vector2(point.X - player.width / 2f, point.Y - player.height / 2f));
            player.GetModPlayer<SpellStatusPlayer>().TeleportCooldown = 180;
        }
    }
    public static void Apply(NPC npc, string status, int duration)
    {
        if (!npc.active) return; status = status.ToLowerInvariant(); int buff = Buff(status); if (buff > 0) npc.AddBuff(buff, duration);
        var state = npc.GetGlobalNPC<SpellStatusNPC>();
        if (status.Contains("polymorph", StringComparison.Ordinal) && !npc.boss && !npc.friendly && npc.realLife < 0 && npc.lifeMax < 10000 && state.OriginalType == 0)
        { int originalType=npc.type; float ratio=npc.life/(float)npc.lifeMax; npc.Transform(NPCID.Bunny); state=npc.GetGlobalNPC<SpellStatusNPC>(); state.OriginalType=originalType; state.OriginalLifeRatio=ratio; state.PolymorphTime=Math.Max(120,duration); }
        if (status is "charm" or "charm_cloud") state.Charm(npc,duration);
        if (status is "frozen" or "freeze") state.FrozenTime = Math.Max(state.FrozenTime, Math.Min(duration, 180));
        if (status is "regeneration" or "hp_regeneration") { if (Main.GameUpdateCount % 30 == 0) npc.life = Math.Min(npc.lifeMax, npc.life + 1); }
    }
}
public sealed class SpellStatusPlayer : ModPlayer
{
    public int TeleportCooldown, CessationTime;
    public override void PostUpdate()
    {
        if (TeleportCooldown > 0) TeleportCooldown--;
        if (CessationTime > 0) { CessationTime--; Player.immune = true; Player.immuneTime = Math.Max(Player.immuneTime, 2); Player.invis = true; Player.controlUseItem = false; }
    }
}
public sealed class SpellStatusNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public int OriginalType, PolymorphTime, CharmTime, FrozenTime;
    public float OriginalLifeRatio;
    private bool wasFriendly;
    public void Charm(NPC npc,int duration) {if(CharmTime<=0)wasFriendly=npc.friendly;CharmTime=Math.Max(CharmTime,duration);}
    public override bool PreAI(NPC npc)
    {
        if (PolymorphTime > 0 && --PolymorphTime == 0 && OriginalType > 0)
        { int type = OriginalType; float ratio=OriginalLifeRatio; OriginalType = 0; npc.Transform(type); npc.life = Math.Clamp((int)(npc.lifeMax * ratio), 1, npc.lifeMax); }
        if (CharmTime > 0) { npc.friendly = true; if (--CharmTime == 0) npc.friendly = wasFriendly; }
        if (FrozenTime > 0 && !npc.boss) { FrozenTime--; npc.velocity *= .5f; return false; }
        return true;
    }
}

public sealed class CessationItems : GlobalItem
{
    public override bool CanUseItem(Item item, Player player) => player.GetModPlayer<SpellStatusPlayer>().CessationTime <= 0;
}

public sealed class SpellSpawnedNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public CastDiagnostics? Diagnostics;
}
