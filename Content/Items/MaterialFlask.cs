#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.Items;

public class MaterialFlask : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.Bottle;
    public FlaskContents Contents { get; private set; } = new();
    public override void SetStaticDefaults() => ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
    public override void SetDefaults()
    {
        Item.width = 20; Item.height = 30; Item.maxStack = 1; Item.rare = ItemRarityID.Blue;
        Item.useStyle = ItemUseStyleID.HoldUp; Item.useTime = Item.useAnimation = 6; Item.autoReuse = true; Item.noMelee = true;
    }
    public override ModItem Clone(Item newEntity) { var clone = (MaterialFlask)base.Clone(newEntity); clone.Contents = Contents.Copy(); return clone; }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) => Main.netMode == NetmodeID.SinglePlayer && !player.mouseInterface;
    protected static Vector2 Target(Player player)
    {
        Vector2 direction = Main.MouseWorld - player.Center;
        if (direction.Length() > 160) direction = Vector2.Normalize(direction) * 160;
        Vector2 target = player.Center + direction;
        for (int i = 0; i < 10 && !Collision.CanHitLine(player.Center, 1, 1, target, 1, 1); i++) target = Vector2.Lerp(target, player.Center, .25f);
        return target;
    }
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        try
        {
            var world = ModContent.GetInstance<MaterialWorld>(); var target = Target(player);
            if (player.altFunctionUse == 2) world.Scoop(target, Contents, 80);
            else
            {
                int remaining = 64;
                foreach (var pair in Contents.Materials.ToArray())
                { int poured = world.Pour(target, pair.Key, Math.Min(remaining, pair.Value)); Contents.Remove(pair.Key, poured); remaining -= poured; if (remaining <= 0) break; }
            }
        }
        catch (Exception e) { Main.NewText(e.Message, Color.OrangeRed); return false; }
        return true;
    }
    public void Fill(string material) { Contents = new(); Contents.Add(material, FlaskContents.Capacity); }
    public override void SaveData(TagCompound tag) => tag["contents"] = Contents.Materials.Select(p => new TagCompound { ["material"] = p.Key, ["amount"] = p.Value }).ToList();
    public override void LoadData(TagCompound tag)
    { Contents = new(); foreach (var entry in tag.GetList<TagCompound>("contents")) Contents.Add(entry.GetString("material"), entry.GetInt("amount")); }
    public override Color? GetAlpha(Color lightColor)
    { try { string? name = Contents.Materials.Keys.FirstOrDefault(); return name == null ? lightColor : MaterialWorld.Tint(ModContent.GetInstance<MaterialWorld>().Catalog.Get(name)); } catch { return lightColor; } }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Add(new(Mod, "Contents", $"{Contents.Total}/{FlaskContents.Capacity} units"));
        foreach (var pair in Contents.Materials.Take(8)) tooltips.Add(new(Mod, "Liquid" + pair.Key, $"{pair.Key.Replace('_', ' ')}: {pair.Value}"));
        tooltips.Add(new(Mod, "Controls", "Hold left-click to pour; hold right-click to scoop nearby liquid"));
        tooltips.Add(new(Mod, "Saves", "Mixtures and remaining contents persist; single-player"));
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).Register();
}

public sealed class DebugMaterialFlask : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.Bottle;
    private static readonly string[] Palette = { "water", "blood", "oil", "alcohol", "acid", "poison", "lava", "slime", "magic_liquid_hp_regeneration", "magic_liquid_berserk", "magic_liquid_movement_faster", "magic_liquid_teleportation", "magic_liquid_polymorph", "magic_liquid_invisibility" };
    public string Selected { get; set; } = "water";
    public override void SetDefaults() { Item.width = 20; Item.height = 30; Item.maxStack = 1; Item.rare = ItemRarityID.Cyan; Item.useStyle = ItemUseStyleID.HoldUp; Item.useTime = Item.useAnimation = 12; Item.noMelee = true; Item.autoReuse = false; }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) => Main.netMode == NetmodeID.SinglePlayer && !player.mouseInterface;
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        if (player.altFunctionUse == 2) { Selected = Palette[(Array.IndexOf(Palette, Selected) + 1) % Palette.Length]; Main.NewText("Material: " + Selected, Color.Cyan); }
        else
        {
            try { var point = Main.MouseWorld; if (Vector2.Distance(player.Center, point) > 480 || !Collision.CanHitLine(player.Center, 1, 1, point, 1, 1)) return false;
                ModContent.GetInstance<MaterialWorld>().Pour(point, Selected, 1000, 3); }
            catch (Exception e) { Main.NewText(e.Message, Color.OrangeRed); }
        }
        return true;
    }
    public override void SaveData(TagCompound tag) => tag["material"] = Selected;
    public override void LoadData(TagCompound tag) => Selected = tag.GetString("material") is { Length: > 0 and <= 128 } text ? text : "water";
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    { tooltips.Add(new(Mod, "Material", "Selected: " + Selected)); tooltips.Add(new(Mod, "Controls", "Left-click creates liquid; right-click changes material")); tooltips.Add(new(Mod, "Command", "Use /noita material NAME to select any imported liquid")); }
    public override Color? GetAlpha(Color lightColor) => Color.Cyan;
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.DirtBlock).AddTile(TileID.WorkBenches).Register();
}
