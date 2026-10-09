#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace terrarianoita.Core;

/// <summary>Debug choices insert real Noita cards before the target; no changes to the Lua cast output.</summary>
public static class DebugModifiers
{
    public static readonly string[] Movement = { "", "SINEWAVE", "SPIRALING_SHOT", "PINGPONG_PATH" };
    public static readonly string[] Tracking = { "", "HOMING", "HOMING_SHORT", "HOMING_ROTATE", "HOMING_CURSOR", "HOMING_SHOOTER", "HOMING_ACCELERATING", "ANTI_HOMING", "HOMING_AREA" };
    public static readonly string[] Speed = { "", "SPEED", "DECELERATING_SHOT", "ACCELERATING_SHOT" };
    public static readonly string[] Effects = { "", "FREEZE", "ELECTRIC_CHARGE", "PIERCING_SHOT", "EXPLOSIVE_PROJECTILE", "MATTER_EATER", "WATER_TRAIL", "OIL_TRAIL", "ACID_TRAIL" };
    public static string[] Cards(int movement, int tracking, int speed, bool bounce, int effect = 0) =>
        new[] { Movement[Math.Clamp(movement, 0, Movement.Length - 1)], Tracking[Math.Clamp(tracking, 0, Tracking.Length - 1)],
            Speed[Math.Clamp(speed, 0, Speed.Length - 1)], bounce ? "BOUNCE" : "", Effects[Math.Clamp(effect, 0, Effects.Length-1)] }.Where(c => c.Length > 0).ToArray();
}

/// <summary>Bounded movement adapters for specific imported extra_entities. Other effects remain explicit gaps.</summary>
public sealed class SpellMotion
{
    public bool Sine { get; set; }
    public bool Spiral { get; set; }
    public bool PingPong { get; set; }
    public bool Chaotic { get; set; }
    public bool Horizontal { get; set; }
    public bool Octagonal { get; set; }
    public bool AreaTeleport { get; set; }
    public bool AntiHoming { get; set; }
    public string Target { get; set; } = "enemy";
    public double HomingRange { get; set; }
    public double TurnRate { get; set; }
    public double Acceleration { get; set; } = 1;
    public int Bounces { get; set; }
    public List<string> Sources { get; } = new();
    public List<string> Gaps { get; } = new();
    public static SpellMotion Load(ShotPlan shot, NoitaAssetCatalog catalog)
    {
        var result = new SpellMotion { Bounces = (int)Math.Clamp(shot.Number("bounces"), 0, 100) };
        foreach (string path in shot.Text("extra_entities").Split(',', StringSplitOptions.RemoveEmptyEntries).Distinct())
        {
            string file = System.IO.Path.GetFileName(path);
            bool known = path == "data/entities/misc/" + file && file is ("homing.xml" or "homing_short.xml" or "homing_rotate.xml" or "homing_weak.xml" or "homing_accelerating.xml" or "homing_cursor.xml" or "homing_shooter.xml" or "homing_projectile.xml" or "anti_homing.xml" or "homing_area.xml" or "chaotic_arc.xml" or "horizontal_arc.xml" or "line_arc.xml" or "sinewave.xml" or "orbit_shot.xml" or "pingpong_path.xml" or "accelerating_shot.xml" or "decelerating_shot.xml");
            if (!known) { result.Gaps.Add("Deferred extra entity: " + path); continue; }
            var asset = catalog.Entity(path); result.Sources.AddRange(asset.Sources);
            if (asset.Component("HomingComponent") is { } homing)
            {
                result.HomingRange = homing.Number("detect_distance", 240);
                result.TurnRate = homing.Number("max_turn_rate", file == "homing_short.xml" ? .18 : .1);
            }
            if (file is "homing_cursor.xml" or "homing_shooter.xml" or "homing_projectile.xml")
            { result.Target = file == "homing_cursor.xml" ? "cursor" : file == "homing_shooter.xml" ? "shooter" : "projectile"; result.HomingRange = 900; result.TurnRate = .15; }
            if (file == "anti_homing.xml") { result.AntiHoming = true; result.HomingRange = 240; result.TurnRate = .15; }
            result.Octagonal |= file == "line_arc.xml"; result.AreaTeleport |= file == "homing_area.xml";
            result.Chaotic |= file == "chaotic_arc.xml"; result.Horizontal |= file == "horizontal_arc.xml";
            if (file == "homing_accelerating.xml") result.Acceleration *= Math.Exp(1.2 / 60d);
            result.Sine |= file == "sinewave.xml"; result.Spiral |= file == "orbit_shot.xml"; result.PingPong |= file == "pingpong_path.xml";
            if (file == "accelerating_shot.xml") result.Acceleration *= Math.Exp(3 / 60d);
            if (file == "decelerating_shot.xml") result.Acceleration *= Math.Exp(-6 / 60d);
        }
        return result;
    }
    public Vector2 Step(Vector2 velocity, int age, Vector2? targetDirection = null)
    {
        float speed = velocity.Length();
        if (speed < .0001f) return velocity; // Static spells remain static.
        float angle = MathF.Atan2(velocity.Y, velocity.X);
        if (Sine) angle += .6f * (MathF.Sin(age * .18f) - MathF.Sin((age - 1) * .18f));
        if (Spiral) angle += .12f;
        if (Chaotic) angle += MathF.Sin(age * 1.73f) * .23f;
        if (Octagonal) angle = MathF.Round(angle / (MathF.PI/4)) * (MathF.PI/4);
        if (Horizontal) angle = MathF.Cos(angle) < 0 ? MathF.PI : 0;
        if (PingPong && age % (int)Math.Clamp(50 - (speed - 1) * 46 / 9, 4, 50) == 0 && speed >= 1)
        { velocity = -velocity * .85f - new Vector2(0, speed * .1f); angle = MathF.Atan2(velocity.Y, velocity.X); speed = velocity.Length(); }
        if (targetDirection is { } target && target.LengthSquared() > .0001f && HomingRange > 0)
        { float difference = MathF.IEEERemainder(MathF.Atan2(target.Y, target.X) + (AntiHoming ? MathF.PI : 0) - angle, MathF.Tau); angle += Math.Clamp(difference, -(float)TurnRate, (float)TurnRate); }
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Math.Clamp(speed * (float)Acceleration, .05f, 120);
    }
}
