#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.Items;

public class NoitaWand : ModItem
{
    private static readonly string[][] Presets = {
        new[] { "DAMAGE", "LIGHT_BULLET" },
        new[] { "LIGHT_BULLET_TRIGGER", "DAMAGE", "LIGHT_BULLET" },
        new[] { "BURST_2", "LIGHT_BULLET", "DAMAGE", "LIGHT_BULLET" },
        new[] { "LIGHT_BULLET_TIMER", "LIGHT_BULLET" },
        new[] { "BURST_2", "LIGHT_BULLET", "CHAINSAW" }
    };
    private int preset, cooldown;
    private double mana = 100;
    private Lua51Runtime? runtime;
    private string? lastError;

    public override ModItem Clone(Item newEntity)
    {
        var copy = (NoitaWand)base.Clone(newEntity);
        copy.runtime = null;
        copy.lastError = null;
        return copy;
    }
    public override void SetDefaults()
    {
        Item.damage = 3;
        Item.DamageType = DamageClass.Magic;
        Item.width = 40;
        Item.height = 40;
        Item.useTime = Item.useAnimation = 1;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 0;
        Item.value = Item.buyPrice(silver: 1);
        Item.rare = ItemRarityID.Blue;
        Item.UseSound = SoundID.Item20;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<Projectiles.NoitaSpark>();
        Item.shootSpeed = 12;
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player)
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
        {
            Report("This prototype wand supports single-player worlds.");
            return false;
        }
        return cooldown == 0;
    }
    public override bool? UseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            preset = (preset + 1) % Presets.Length;
            Release();
            cooldown = 20;
            Main.NewText("Wand: " + string.Join(" → ", Presets[preset]), Color.LightPink);
        }
        return true;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2) return false;
        try
        {
            if (runtime == null || runtime.IsDisposed)
            {
                runtime = ModContent.GetInstance<AdapterSystem>().CreateRuntime();
                var slots = Array.ConvertAll(Presets[preset], id => new SpellSlot(id));
                runtime.Configure(new WandConfiguration(slots));
            }
            var plan = runtime.Cast(mana);
            DemoProjectileAdapter.Validate(plan.Root);
            mana = Math.Clamp(plan.Mana, 0, 100);
            // Demo scheduler; actual native frame scheduling remains to be recovered.
            cooldown = Math.Max(1, (int)Math.Ceiling(Math.Max(plan.Root.Number("fire_rate_wait"), plan.ReloadRequest ?? 0)));
            DemoProjectileAdapter.Emit(plan.Root, source, player, position, velocity);
            lastError = null;
        }
        catch (Exception e)
        {
            Report(e.Message);
            Release();
            cooldown = 60;
        }
        return false;
    }
    public override void UpdateInventory(Player player)
    {
        if (cooldown > 0) cooldown--;
        mana = Math.Min(100, mana + 50.0 / 60);
    }
    private void Release()
    {
        if (runtime != null) ModContent.GetInstance<AdapterSystem>().Release(runtime);
        runtime = null;
    }
    private void Report(string message)
    {
        if (lastError == message) return;
        lastError = message;
        Main.NewText(message, Color.OrangeRed);
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Add(new TooltipLine(Mod, "WandMana", $"Wand mana: {mana:0}/100"));
        tooltips.Add(new TooltipLine(Mod, "SpellDeck", string.Join(" → ", Presets[preset])));
    }
    public override void AddRecipes()
    {
        CreateRecipe().AddIngredient(ItemID.DirtBlock, 10).AddTile(TileID.WorkBenches).Register();
    }
}
