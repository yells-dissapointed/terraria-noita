#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.Items;

public class NoitaWand : ModItem
{
    public WandDefinition Definition { get; private set; } = new();
    public bool DebugVisible { get; set; } = true;
    public double Mana => mana;
    public int Cooldown => cooldown;
    public List<CastDiagnostics> History { get; private set; } = new();
    public CastDiagnostics? Diagnostics => History.FirstOrDefault();
    private int cooldown, castCount;
    private double mana = 100;
    private Lua51Runtime? runtime;
    private string? lastError;

    public override ModItem Clone(Item newEntity)
    {
        var copy = (NoitaWand)base.Clone(newEntity);
        copy.Definition = Definition.Copy();
        copy.History = new(); copy.runtime = null; copy.lastError = null;
        return copy;
    }
    public override void SetDefaults()
    {
        Item.damage = 3; Item.DamageType = DamageClass.Magic;
        Item.width = Item.height = 40; Item.useTime = Item.useAnimation = 1;
        Item.useStyle = ItemUseStyleID.Shoot; Item.noMelee = true;
        Item.value = Item.buyPrice(silver: 1); Item.rare = ItemRarityID.Blue;
        Item.UseSound = SoundID.Item20; Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<Projectiles.NoitaSpark>(); Item.shootSpeed = 12;
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player)
    {
        if (Main.netMode != NetmodeID.SinglePlayer) { Report("This wand supports single-player worlds."); return false; }
        return cooldown == 0 && !ModContent.GetInstance<WandEditorSystem>().IsOpen;
    }
    public override bool? UseItem(Player player)
    {
        if (player.altFunctionUse == 2)
        {
            ModContent.GetInstance<WandEditorSystem>().Open(this);
            cooldown = 10;
        }
        return true;
    }
    private Lua51Runtime Runtime()
    {
        if (runtime == null || runtime.IsDisposed)
        {
            runtime = ModContent.GetInstance<AdapterSystem>().CreateRuntime();
            runtime.Configure(Definition.Configuration());
        }
        return runtime;
    }
    private CastDiagnostics BeginTrace(string label, WandDefinition definition, double manaBefore)
    {
        var trace = new CastDiagnostics(); trace.Begin(label, definition, manaBefore);
        History.Insert(0, trace); if (History.Count > 8) History.RemoveAt(8);
        return trace;
    }
    public string[] SpellIds() => Runtime().SpellIds();
    public void Apply(WandDefinition definition)
    {
        definition.Validate(); Definition = definition.Copy();
        mana = Math.Clamp(mana, 0, Definition.ManaMax); Release(); cooldown = 10; lastError = null;
    }
    public void Preview(WandDefinition draft)
    {
        var trace = BeginTrace("Test draft: fresh deck and full mana, no live changes", draft, draft.ManaMax);
        Lua51Runtime? test = null;
        try
        {
            draft.Validate(); test = ModContent.GetInstance<AdapterSystem>().CreateRuntime();
            test.Configure(draft.Configuration());
            var plan = test.Cast(draft.ManaMax, draft.AlwaysCast);
            trace.Capture(plan); GameplayProjectileAdapter.Validate(plan.Root);
            var inspection = DemoCapabilities.Inspect(plan, test.DefaultConfiguration());
            foreach (string missing in inspection.Missing) trace.Event(GameplayProjectileAdapter.DescribeGap(missing));
            trace.Event(inspection.ProjectileCount == 0 ? "No projectile in this cast; try a follow-up spell." :
                "Gameplay prototype tree accepted. Terraria adapters and Noita demo effects require in-game checks; native fidelity remains partial.");
        }
        catch (Exception e) { trace.Fail(e.Message); }
        finally { if (test != null) ModContent.GetInstance<AdapterSystem>().Release(test); }
    }
    public string SaveLog(CastDiagnostics trace)
    {
        string directory = Path.Combine(Main.SavePath, "terrarianoita", "debug"); Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "wand-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json");
        File.WriteAllText(path, trace.Json()); return path;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2) return false;
        var trace = BeginTrace("Live cast #" + ++castCount, Definition, mana);
        try
        {
            var plan = Runtime().Cast(mana, Definition.AlwaysCast); trace.Capture(plan);
            GameplayProjectileAdapter.Validate(plan.Root);
            mana = Math.Clamp(plan.Mana, 0, Definition.ManaMax);
            cooldown = Math.Max(1, (int)Math.Ceiling(Math.Max(plan.Root.Number("fire_rate_wait"), plan.ReloadRequest ?? 0)));
            Vector2 hand = player.RotatedRelativePoint(player.MountedCenter);
            Vector2 aim = Main.MouseWorld - hand;
            if (aim.LengthSquared() < .001f) aim = new Vector2(player.direction, 0);
            aim.Normalize();
            // Ignore Terraria's pre-adjusted Shoot position; anchor the muzzle near the player's hand.
            Vector2 muzzle = hand + aim * 16;
            if (!Collision.CanHitLine(hand, 1, 1, muzzle, 1, 1)) muzzle = hand;
            trace.Event($"Muzzle {Vector2.Distance(hand, muzzle):0.#} px from hand; cooldown {cooldown} frames (approximate)");
            GameplayProjectileAdapter.Emit(plan.Root, source, player, muzzle, aim, trace, DebugVisible); lastError = null;
        }
        catch (Exception e) { trace.Fail(e.Message); Report(e.Message); Release(); cooldown = 60; }
        return false;
    }
    public override void UpdateInventory(Player player)
    {
        if (cooldown > 0) cooldown--;
        mana = Math.Min(Definition.ManaMax, mana + Definition.ManaRecharge / 60);
    }
    private void Release()
    {
        if (runtime != null) ModContent.GetInstance<AdapterSystem>().Release(runtime);
        runtime = null;
    }
    private void Report(string message)
    {
        if (lastError == message) return;
        lastError = message; Main.NewText(message, Color.OrangeRed);
    }
    public override void SaveData(TagCompound tag)
    {
        tag["definition"] = JsonSerializer.Serialize(Definition); tag["mana"] = mana; tag["debug"] = (byte)(DebugVisible ? 1 : 0);
    }
    public override void LoadData(TagCompound tag)
    {
        try
        {
            if (tag.ContainsKey("definition"))
            {
                var loaded = JsonSerializer.Deserialize<WandDefinition>(tag.GetString("definition")) ?? new();
                loaded.Validate(); Definition = loaded;
            }
        }
        catch (Exception e) { Mod.Logger.Warn("Invalid saved wand; restored default deck: " + e.Message); Definition = new(); }
        double savedMana = tag.ContainsKey("mana") ? tag.GetDouble("mana") : Definition.ManaMax;
        mana = double.IsFinite(savedMana) ? Math.Clamp(savedMana, 0, Definition.ManaMax) : Definition.ManaMax;
        DebugVisible = !tag.ContainsKey("debug") || tag.GetByte("debug") != 0;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Add(new TooltipLine(Mod, "LoadedVersion", BuildIdentity.Label(Mod)));
        tooltips.Add(new TooltipLine(Mod, "WandMana", $"Wand mana: {mana:0}/{Definition.ManaMax:0}"));
        tooltips.Add(new TooltipLine(Mod, "SpellDeck", string.Join(" → ", Definition.Deck)));
        if (Definition.AlwaysCast.Count > 0) tooltips.Add(new TooltipLine(Mod, "AlwaysCast", "Always cast: " + string.Join(", ", Definition.AlwaysCast)));
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.DirtBlock, 10).AddTile(TileID.WorkBenches).Register();
}
