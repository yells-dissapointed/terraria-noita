#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using terrarianoita.Common;
using terrarianoita.Core;
using terrarianoita.Content.Projectiles;

namespace terrarianoita.Content.Items;

public sealed class SpellDebugWand : ModItem
{
    public override string Texture => "terrarianoita/Content/Items/NoitaWand";
    public override Color? GetAlpha(Color lightColor) => Color.Cyan;
    public SpellDebugSequence? Sequence { get; private set; }
    public SpellLiveReport Report { get; private set; } = new();
    public CastDiagnostics? LastTrace { get; private set; }
    public bool Automatic { get; private set; }
    public int Interval { get; private set; } = 120;
    public string Status { get; private set; } = "Left-click: test next spell. Right-click: scan controls.";
    public SpellDebugMode Mode { get; private set; } = SpellDebugMode.FollowedBySparks;
    public bool VisualPreview { get; private set; } = true;
    public bool FixedSpell { get; private set; }
    private Dictionary<string, JsonElement>? defaults;
    private Dictionary<string, SpellCard>? cards;
    private int countdown, manualCooldown;
    public override ModItem Clone(Item newEntity)
    {
        var clone = (SpellDebugWand)base.Clone(newEntity);
        clone.Sequence = null; clone.Report = new(); clone.LastTrace = null; clone.defaults = null; clone.cards = null;
        clone.Automatic = false; clone.countdown = clone.manualCooldown = 0;
        return clone;
    }
    public override void SetDefaults()
    {
        Item.damage = 3; Item.DamageType = DamageClass.Magic; Item.width = Item.height = 40;
        Item.useTime = Item.useAnimation = 15; Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true; Item.rare = ItemRarityID.Cyan; Item.autoReuse = false;
        Item.shoot = ModContent.ProjectileType<NoitaSpark>(); Item.shootSpeed = 12;
    }
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) => Main.netMode == NetmodeID.SinglePlayer && manualCooldown == 0 &&
        !ModContent.GetInstance<SpellDebugSystem>().IsOpen && !ModContent.GetInstance<WandEditorSystem>().IsOpen;
    public override bool? UseItem(Player player)
    {
        if (player.altFunctionUse == 2) ModContent.GetInstance<SpellDebugSystem>().Open(this);
        return true;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse != 2) { Stop(); TestNext(player, source); }
        return false;
    }
    private void Initialize()
    {
        if (Sequence != null) return;
        var manager = ModContent.GetInstance<AdapterSystem>(); var runtime = manager.CreateRuntime();
        try
        {
            defaults = runtime.DefaultConfiguration(); Sequence = new(runtime.SpellIds()); Sequence.SetMode(Mode);
            cards = new(); foreach (var card in runtime.SpellCards()) cards[card.Id] = card;
        }
        finally { manager.Release(runtime); }
    }
    public void Stop() => Automatic = false;
    public void ToggleAutomatic()
    {
        try
        {
            Initialize();
            if (Sequence!.Complete) { Status = "Scan finished. Restart to scan again."; return; }
            Automatic = !Automatic; countdown = Interval;
            Status = Automatic ? (FixedSpell ? "Auto repeats this spell/context until stopped; aim the cursor." : "Auto scan starts after the interval. Aim the cursor; switching items stops it.") : "Auto scan stopped.";
        }
        catch (Exception e) { Stop(); Status = e.Message; }
    }
    public void Move(int delta)
    {
        try { Stop(); Initialize(); Sequence!.Move(delta); Status = "Selected " + Sequence.Spell; }
        catch (Exception e) { Status = e.Message; }
    }
    public void Select(string spell)
    {
        try { Stop(); Initialize(); Sequence!.Select(spell); Status = "Selected " + spell + ". Close controls and left-click, or Test current."; }
        catch (Exception e) { Status = e.Message; }
    }
    public void ChangeContext()
    {
        Stop(); Mode = (SpellDebugMode)(((int)Mode + 1) % 4); Sequence?.SetMode(Mode);
        Status = "Context changed; scan restarted. Earlier results remain in the report.";
    }
    public void ChangeInterval() => Interval = Interval switch { 60 => 120, 120 => 300, 300 => 600, _ => 60 };
    public void ToggleFixedSpell()
    {
        Stop(); FixedSpell = !FixedSpell;
        if (FixedSpell && Sequence?.Complete == true) Sequence.Move(-1);
        Status = FixedSpell ? "Fixed spell: left-click and auto repeat the selected spell/context. Previous/Next still select manually." : "Cycle spells: each test advances to the next case.";
    }
    public void ChangeVisualization()
    {
        Stop(); CleanupCurrent(); VisualPreview = !VisualPreview;
        Status = VisualPreview ? "XML/sprite preview: harmless entities; deferred behavior remains logged." : "Terraria integration: mapped spells deal damage; Bomb uses a real terrain explosion. Unmapped spells stay visual.";
    }
    public void Restart()
    {
        Stop(); Sequence?.Restart(); Status = "Scan restarted; earlier results remain in the report.";
    }
    public void CleanupCurrent()
    {
        if (LastTrace == null) return;
        foreach (var projectile in Main.projectile)
        {
            if (projectile is { active: true, ModProjectile: NoitaSpark spark } && ReferenceEquals(spark.Diagnostics, LastTrace))
            { spark.CancelDebugPayloads(); projectile.Kill(); }
            else if (projectile is { active: true, ModProjectile: NoitaVisualProjectile visual } && ReferenceEquals(visual.Diagnostics, LastTrace))
            { visual.CancelDebugPayloads(); projectile.Kill(); }
            else if (projectile is { active: true } && projectile.TryGetGlobalProjectile<TerrariaProjectileBinding>(out var binding) &&
                binding.Bound && ReferenceEquals(binding.Diagnostics, LastTrace)) binding.RemoveForDebug(projectile);
        }
    }
    public void TestNext(Player player, IEntitySource? source = null, bool advance = true)
    {
        if (Main.netMode != NetmodeID.SinglePlayer || player.whoAmI != Main.myPlayer || player.dead) return;
        try
        {
            Initialize();
            if (Sequence!.Complete) { Stop(); Status = "All selected cases finished. Save report or Restart."; return; }
            CleanupCurrent();
            var manager = ModContent.GetInstance<AdapterSystem>();
            var test = SpellAudit.RunCase(Sequence.Spell, Sequence.ContextIndex, manager.CreateRuntime, defaults!, manager.Release);
            var trace = new CastDiagnostics(); LastTrace = trace;
            var definition = new WandDefinition { Deck = test.Deck, AlwaysCast = test.AlwaysCast, ManaMax = 10000 };
            trace.Begin($"Live debug: {test.Spell} / {test.Context}", definition, 10000);
            var result = new SpellLiveCase { Test = test, Trace = trace, VisualPreview = VisualPreview, FixedSpell = FixedSpell }; Report.Add(result);
            if (test.Plan != null) trace.Capture(test.Plan);
            foreach (string missing in test.Inspection?.Missing ?? new List<string>())
                trace.Event(VisualPreview ? "NOT IMPLEMENTED: " + missing : GameplayProjectileAdapter.DescribeGap(missing));
            Vector2 hand = player.RotatedRelativePoint(player.MountedCenter), aim = Main.MouseWorld - hand;
            if (aim.LengthSquared() < .001f) aim = new Vector2(player.direction, 0);
            aim.Normalize(); Vector2 muzzle = hand + aim * 16;
            if (!Collision.CanHitLine(hand, 1, 1, muzzle, 1, 1)) muzzle = hand;
            var castSource = source ?? player.GetSource_ItemUse(Item);
            if (VisualPreview)
            {
                if (test.Plan != null) result.RootProjectilesSpawned = VisualProjectileAdapter.Emit(test.Plan.Root, castSource, player, muzzle, aim, result);
                string message = test.Status == SpellAuditStatus.ScriptError ? "SCRIPT FAILED — CARD ONLY" :
                    test.Status == SpellAuditStatus.NotExercised ? "TARGET NOT EXERCISED — CARD" :
                    result.RootProjectilesSpawned == 0 ? "NO PROJECTILE — CARD ONLY" : "";
                if (message.Length > 0)
                {
                    cards!.TryGetValue(test.Spell, out var card);
                    result.DiagnosticCardsSpawned = VisualProjectileAdapter.Card(castSource, player, hand + aim * 36, card, message, result);
                }
                if (test.Error.Length > 0) trace.Fail(test.Error);
                trace.Event($"XML visual test: {result.RootProjectilesSpawned} emitted root entities; {result.DiagnosticCardsSpawned} diagnostic cards. Audit status {test.Status} remains unchanged.");
                Status = $"{test.Spell}: {result.RootProjectilesSpawned} visual root(s), {result.DiagnosticCardsSpawned} card(s); {test.Status}.";
            }
            else if (test.Status is SpellAuditStatus.ScriptError or SpellAuditStatus.NotExercised)
            {
                trace.Fail(test.Error.Length > 0 ? test.Error : "No live emission: unsupported entity tree or unexercised action.");
                Status = $"Skipped {test.Spell}: {test.Status}. Full reason is in the report.";
            }
            else
            {
                result.RootProjectilesSpawned = GameplayProjectileAdapter.Emit(test.Plan!.Root, castSource, player, muzzle, aim, trace, true, result);
                trace.Event($"Terraria integration: {result.RootProjectilesSpawned} root(s); unmapped entities use harmless Noita visuals. Legacy audit status {test.Status} is unchanged.");
                Status = $"Tested {test.Spell}: {result.RootProjectilesSpawned} root(s); Terraria adapters or Noita visual fallback.";
            }
            if (advance) Sequence.Advance(FixedSpell);
            countdown = Interval; manualCooldown = 15;
        }
        catch (Exception e) { Stop(); Status = "Debug test stopped: " + e.Message; LastTrace?.Fail(e.Message); }
    }
    public string SaveReport()
    {
        string directory = Path.Combine(Main.SavePath, "terrarianoita", "debug"); Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "live-spell-debug-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json");
        File.WriteAllText(path, Report.Json(Mod.Version.ToString(), BuildIdentity.LoadedPath(Mod))); return path;
    }
    public override void HoldItem(Player player)
    {
        if (!Automatic || player.whoAmI != Main.myPlayer || Main.netMode != NetmodeID.SinglePlayer || player.dead || Main.gameMenu) return;
        if (Main.playerInventory || Main.drawingPlayerChat || Main.gamePaused) return;
        if (--countdown > 0) return;
        if (Sequence?.Complete == true)
        {
            Stop(); CleanupCurrent();
            try { string path = SaveReport(); Main.NewText("Spell scan finished: " + path, Color.Cyan); Status = "Scan complete; report saved. Path printed in chat."; }
            catch (Exception e) { Status = "Scan complete; report save failed: " + e.Message; }
            return;
        }
        TestNext(player);
    }
    public override void UpdateInventory(Player player)
    {
        if (manualCooldown > 0) manualCooldown--;
        if (player.dead || !ReferenceEquals(player.HeldItem.ModItem, this)) Stop();
    }
    public override void SaveData(TagCompound tag) { tag["mode"] = (int)Mode; tag["interval"] = Interval; tag["visualPreview"] = (byte)(VisualPreview ? 1 : 0); tag["fixedSpell"] = (byte)(FixedSpell ? 1 : 0); }
    public override void LoadData(TagCompound tag)
    {
        int mode = tag.GetInt("mode"); Mode = Enum.IsDefined(typeof(SpellDebugMode), mode) ? (SpellDebugMode)mode : SpellDebugMode.FollowedBySparks;
        int interval = tag.GetInt("interval"); Interval = interval is 60 or 120 or 300 or 600 ? interval : 120;
        VisualPreview = !tag.ContainsKey("visualPreview") || tag.GetByte("visualPreview") != 0;
        FixedSpell = tag.ContainsKey("fixedSpell") && tag.GetByte("fixedSpell") != 0;
        Stop(); Sequence = null; defaults = null; cards = null; Report = new(); LastTrace = null;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Add(new TooltipLine(Mod, "LoadedVersion", BuildIdentity.Label(Mod)));
        tooltips.Add(new TooltipLine(Mod, "DebugUse", FixedSpell ? "Left-click repeats the fixed spell; right-click for controls" : "Left-click tests the next spell; right-click for fixed/cycle controls"));
        tooltips.Add(new TooltipLine(Mod, "DebugLimits", "XML/sprite preview is harmless; orange entity markers and cyan diagnostic cards label gaps"));
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.DirtBlock).AddTile(TileID.WorkBenches).Register();
}
