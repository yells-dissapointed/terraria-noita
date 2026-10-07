#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace terrarianoita.Core;

public sealed class NoitaEntityAsset
{
    public string Path { get; set; } = "";
    public NoitaXmlNode Definition { get; set; } = new();
    public NoitaXmlNode SourceDefinition { get; set; } = new();
    public List<string> Sources { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public NoitaXmlNode? Component(string name) => Definition.Children.LastOrDefault(n => n.Name == name && n.Enabled);
    public List<string> DeferredComponents => Definition.Children.Where(n => n.Enabled && n.Name.EndsWith("Component") &&
        n.Name is not ("ProjectileComponent" or "VelocityComponent" or "SpriteComponent" or "LifetimeComponent" or "LightComponent" or "PhysicsImageShapeComponent" or "HitboxComponent" or "VariableStorageComponent"))
        .Select(n => n.Name).Distinct().OrderBy(x => x).ToList();
}

public readonly record struct SpriteFrame(int X, int Y, int Width, int Height);
public sealed class NoitaSpriteAsset
{
    public string ImagePath { get; set; } = "";
    public string DefinitionPath { get; set; } = "";
    public int TextureWidth { get; set; }
    public int TextureHeight { get; set; }
    public bool BlackKey { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double ScaleX { get; set; } = 1;
    public double ScaleY { get; set; } = 1;
    public double Alpha { get; set; } = 1;
    public bool Additive { get; set; }
    public bool Rotate { get; set; } = true;
    public int FrameCount { get; set; } = 1;
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public int FramesPerRow { get; set; } = 1;
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public bool Shrink { get; set; }
    public bool Loop { get; set; } = true;
    public double FrameSeconds { get; set; } = .2;
    public SpriteFrame Frame(int age)
    {
        int frame = (int)Math.Min(int.MaxValue, Math.Max(0, age) / Math.Max(1, FrameSeconds * 60));
        frame = Loop ? frame % FrameCount : Math.Min(frame, FrameCount - 1);
        int x = PositionX + frame % FramesPerRow * FrameWidth, y = PositionY + frame / FramesPerRow * FrameHeight;
        int width = FrameWidth - (Shrink ? 1 : 0), height = FrameHeight - (Shrink ? 1 : 0);
        if (x < 0 || y < 0 || width < 1 || height < 1 || x + width > TextureWidth || y + height > TextureHeight)
            throw new InvalidOperationException("Animation frame outside PNG: " + ImagePath);
        return new(x, y, width, height);
    }
}

public sealed class NoitaVisualProfile
{
    public double SpeedPerFrame { get; set; } = 12;
    public double GravityPerFrame { get; set; }
    public double DragPerFrame { get; set; } = 1;
    public int Lifetime { get; set; } = 60;
    public int Bounces { get; set; }
    public bool TileCollide { get; set; } = true;
    public bool EntityCollide { get; set; } = true;
    public bool DieOnCollision { get; set; } = true;
    public bool Rotate { get; set; } = true;
    public double LightR { get; set; }
    public double LightG { get; set; }
    public double LightB { get; set; }
    public List<NoitaSpriteAsset> Sprites { get; set; } = new();
    public List<string> Gaps { get; set; } = new();
}

/// <summary>Loads local definitions and original sprite metadata; does not execute component scripts.</summary>
public sealed class NoitaAssetCatalog
{
    public string Root { get; }
    private readonly Dictionary<string, NoitaEntityAsset> entities = new(StringComparer.Ordinal);
    private static readonly Regex Variant = new(@"\$\[(\d+)-(\d+)\]", RegexOptions.None, TimeSpan.FromSeconds(1));
    public NoitaAssetCatalog(string extractedRoot) => Root = NoitaDataPaths.ResolveRoot(extractedRoot);
    public string LocalPath(string relative)
    {
        relative = relative.Replace('\\', '/');
        if (!relative.StartsWith("data/", StringComparison.Ordinal) || relative.Split('/').Any(p => p is ".." or "." or "") || relative.Contains(':'))
            throw new InvalidOperationException("Invalid Noita asset path: " + relative);
        return System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
    }
    private NoitaXmlNode Read(string path, List<string> warnings)
    {
        string file = LocalPath(path);
        if (new FileInfo(file).Length > 4_000_000) throw new InvalidOperationException("XML file exceeds import size limit");
        return NoitaXml.Parse(File.ReadAllText(file), warnings);
    }
    public NoitaEntityAsset Entity(string path) => Entity(path, new HashSet<string>(StringComparer.Ordinal));
    private NoitaEntityAsset Entity(string path, HashSet<string> visiting)
    {
        if (entities.TryGetValue(path, out var cached)) return cached;
        if (visiting.Count >= 16 || !visiting.Add(path)) throw new InvalidOperationException("XML base inheritance cycle/depth: " + path);
        if (entities.Count >= 4096) throw new InvalidOperationException("Entity import cache full");
        try
        {
            var result = new NoitaEntityAsset { Path = path }; var source = Read(path, result.Warnings);
            result.SourceDefinition = source;
            if (source.Name != "Entity") throw new InvalidOperationException("Expected Entity XML: " + path);
            result.Definition = new() { Name = "Entity", Attributes = new(source.Attributes), Text = source.Text };
            foreach (var node in source.Children)
            {
                if (node.Name != "Base") { result.Definition.Children.Add(node.Copy()); continue; }
                var basis = Entity(node.Get("file"), visiting);
                foreach (var field in basis.Definition.Attributes) if (!result.Definition.Attributes.ContainsKey(field.Key)) result.Definition.Attributes[field.Key] = field.Value;
                var inherited = basis.Definition.Children.Where(n => n.Name != "Entity" || node.Flag("include_children")).Select(n => n.Copy()).ToList();
                foreach (var change in node.Children)
                {
                    var target = inherited.FirstOrDefault(n => n.Name == change.Name && (!change.Attributes.ContainsKey("_tags") || n.Get("_tags") == change.Get("_tags")));
                    if (change.Flag("_remove_from_base")) { if (target != null) inherited.Remove(target); continue; }
                    if (target == null) inherited.Add(change.Copy()); else Overlay(target, change);
                }
                result.Definition.Children.AddRange(inherited); result.Sources.AddRange(basis.Sources); result.Warnings.AddRange(basis.Warnings);
            }
            result.Sources.Add(path); result.Sources = result.Sources.Distinct().ToList();
            entities[path] = result; return result;
        }
        finally { visiting.Remove(path); }
    }
    private static void Overlay(NoitaXmlNode target, NoitaXmlNode change)
    {
        foreach (var field in change.Attributes) target.Attributes[field.Key] = field.Value;
        foreach (var child in change.Children)
        {
            var match = target.Children.FirstOrDefault(n => n.Name == child.Name);
            if (match == null) target.Children.Add(child.Copy()); else Overlay(match, child);
        }
    }
    public NoitaVisualProfile Profile(NoitaEntityAsset asset)
    {
        var result = new NoitaVisualProfile();
        var projectile = asset.Component("ProjectileComponent");
        var velocity = asset.Component("VelocityComponent");
        result.SpeedPerFrame = Math.Clamp(((projectile?.Number("speed_min", 720) ?? 720) + (projectile?.Number("speed_max", 720) ?? 720)) / 120, 0, 120);
        result.GravityPerFrame = Math.Clamp(velocity?.Number("gravity_y") ?? 0, -3600, 3600) / 3600;
        result.DragPerFrame = Math.Clamp(Math.Exp(-(velocity?.Number("air_friction") ?? 0) / 60), .8, 1.2);
        result.Lifetime = (int)Math.Clamp(projectile?.Number("lifetime", asset.Component("LifetimeComponent")?.Number("lifetime", 60) ?? 60) ?? 60, 1, 3600);
        result.Bounces = (int)Math.Clamp(projectile?.Number("bounces_left") ?? 0, 0, 100);
        result.TileCollide = projectile?.Flag("collide_with_world", true) ?? true;
        result.EntityCollide = projectile?.Flag("collide_with_entities", true) ?? true;
        result.DieOnCollision = projectile?.Flag("on_collision_die", true) ?? true;
        result.Rotate = projectile?.Flag("velocity_sets_rotation", true) ?? true;
        var light = asset.Component("LightComponent");
        result.LightR = Math.Clamp(light?.Number("r") ?? 0, 0, 255) / 255;
        result.LightG = Math.Clamp(light?.Number("g") ?? 0, 0, 255) / 255;
        result.LightB = Math.Clamp(light?.Number("b") ?? 0, 0, 255) / 255;
        result.Gaps.AddRange(asset.Warnings); result.Gaps.AddRange(asset.DeferredComponents.Select(c => "Deferred component: " + c));
        if (projectile?.Children.Any(c => c.Name is "config_explosion" or "damage_by_type") == true) result.Gaps.Add("Explosion/damage configuration imported as data; gameplay not simulated in visual mode");
        foreach (var component in asset.Definition.Children.Where(n => n.Name == "SpriteComponent" && n.Enabled))
        {
            try { if (component.Get("image_file").Length > 0) result.Sprites.Add(Sprite(component.Get("image_file"), component)); }
            catch (Exception e) { result.Gaps.Add("Sprite: " + e.Message); }
        }
        if (result.Sprites.Count == 0 && asset.Component("PhysicsImageShapeComponent") is { } shape && shape.Get("image_file").Length > 0)
        {
            try { result.Sprites.Add(Sprite(shape.Get("image_file"))); }
            catch (Exception e) { result.Gaps.Add("Physics image: " + e.Message); }
        }
        if (result.Sprites.Count == 0) result.Gaps.Add("No usable sprite; show labeled entity marker");
        if (asset.Definition.Children.Any(n => n.Name == "Entity")) result.Gaps.Add("Child entity data retained; child entity gameplay not simulated");
        result.Gaps.Add("Visual mapping uses mean speed, deterministic variants and bounded physics; component/native fidelity unverified");
        result.Gaps.Add("Sprite timing/offsets, RGB black transparency and approximate 8x8 contact geometry need client validation");
        return result;
    }
    public NoitaSpriteAsset Sprite(string path, NoitaXmlNode? component = null)
    {
        path = Variant.Replace(path, m => m.Groups[1].Value); var warnings = new List<string>();
        NoitaXmlNode? xml = null, animation = null;
        if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            xml = Read(path, warnings); if (xml.Name != "Sprite") throw new InvalidOperationException("Expected Sprite XML: " + path);
            string requested = component?.Get("rect_animation") ?? "";
            if (requested.Length == 0) requested = xml.Get("default_animation", "default");
            animation = xml.Children.FirstOrDefault(n => n.Name == "RectAnimation" && n.Get("name") == requested) ?? xml.Children.FirstOrDefault(n => n.Name == "RectAnimation");
        }
        string image = xml?.Get("filename") ?? path;
        if (!image.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsupported sprite image: " + image);
        using var stream = File.OpenRead(LocalPath(image));
        if (stream.Length > 32 * 1024 * 1024) throw new InvalidOperationException("PNG exceeds import limit: " + image);
        var header = new byte[26]; if (stream.Read(header, 0, header.Length) != header.Length || !header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) throw new InvalidOperationException("Invalid PNG: " + image);
        int width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4)), height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4));
        if (width < 1 || height < 1 || width > 4096 || height > 4096 || (long)width * height > 4_194_304) throw new InvalidOperationException("PNG dimensions exceed import limit: " + image);
        var result = new NoitaSpriteAsset {
            ImagePath = image, DefinitionPath = path, TextureWidth = width, TextureHeight = height, BlackKey = header[25] is 0 or 2,
            OffsetX = (xml?.Number("offset_x", width / 2d) ?? width / 2d) + (component?.Number("offset_x") ?? 0),
            OffsetY = (xml?.Number("offset_y", height / 2d) ?? height / 2d) + (component?.Number("offset_y") ?? 0),
            Alpha = Math.Clamp(component?.Number("alpha", 1) ?? 1, 0, 1), Additive = component?.Flag("additive") ?? false,
            Rotate = component?.Flag("update_transform_rotation", true) ?? true,
            ScaleX = Math.Clamp(component?.Number("special_scale_x", 1) ?? 1, .05, 4), ScaleY = Math.Clamp(component?.Number("special_scale_y", 1) ?? 1, .05, 4),
            FrameCount = (int)Math.Clamp(animation?.Number("frame_count", 1) ?? 1, 1, 4096), FramesPerRow = (int)Math.Clamp(animation?.Number("frames_per_row", 1) ?? 1, 1, 4096),
            FrameWidth = (int)Math.Clamp(animation?.Number("frame_width", width) ?? width, 1, 4096), FrameHeight = (int)Math.Clamp(animation?.Number("frame_height", height) ?? height, 1, 4096),
            PositionX = (int)(animation?.Number("pos_x") ?? 0), PositionY = (int)(animation?.Number("pos_y") ?? 0), Shrink = animation?.Flag("shrink_by_one_pixel") ?? false,
            Loop = animation?.Flag("loop", true) ?? true, FrameSeconds = Math.Clamp(animation?.Number("frame_wait", .2) ?? .2, 1 / 60d, 60)
        };
        for (int i = 0; i < result.FrameCount; i++) result.Frame((int)Math.Ceiling(i * result.FrameSeconds * 60));
        return result;
    }
}
