#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using terrarianoita.Core;

namespace terrarianoita.Common;

/// <summary>Client-only lazy GPU cache. Reads originals from the configured game data, never the mod package.</summary>
public sealed class ImportedSpriteCache : IDisposable
{
    public NoitaAssetCatalog Catalog { get; }
    private readonly Dictionary<string, Texture2D> textures = new();
    private readonly Dictionary<string, string> failures = new();
    private long pixels;
    public ImportedSpriteCache(string root) => Catalog = new(root);
    private Texture2D Texture(NoitaSpriteAsset sprite)
    {
        string key = sprite.ImagePath + (sprite.BlackKey ? ":black" : ":alpha");
        if (failures.TryGetValue(key, out string? error)) throw new InvalidOperationException(error);
        if (textures.TryGetValue(key, out var cached)) return cached;
        Texture2D? texture = null;
        try
        {
            if (textures.Count >= 1024 || pixels + (long)sprite.TextureWidth * sprite.TextureHeight > 16_777_216)
                throw new InvalidOperationException("Sprite cache limit reached; marker used");
            using var file = File.OpenRead(Catalog.LocalPath(sprite.ImagePath));
            texture = Texture2D.FromStream(Main.graphics.GraphicsDevice, file);
            if (texture.Width != sprite.TextureWidth || texture.Height != sprite.TextureHeight) throw new InvalidOperationException("Decoded PNG size differs from header");
            var colors = new Color[texture.Width * texture.Height]; texture.GetData(colors);
            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                if (sprite.BlackKey && c.R == 0 && c.G == 0 && c.B == 0) colors[i] = Color.Transparent;
                else colors[i] = new Color(c.R * c.A / 255, c.G * c.A / 255, c.B * c.A / 255, c.A);
            }
            texture.SetData(colors); textures.Add(key, texture); pixels += colors.Length; return texture;
        }
        catch (Exception e) { texture?.Dispose(); failures[key] = e.Message; throw; }
    }
    public bool Draw(NoitaSpriteAsset sprite, Vector2 center, float rotation, int age, float extraScale, out string error)
    {
        error = "";
        try
        {
            var frame = sprite.Frame(age); var texture = Texture(sprite);
            var scale = new Vector2((float)sprite.ScaleX, (float)sprite.ScaleY) * extraScale;
            float largest = Math.Max(frame.Width * scale.X, frame.Height * scale.Y);
            if (largest > 256) scale *= 256 / largest;
            var color = Color.White * (float)sprite.Alpha;
            // Premultiplied additive color under Terraria's existing AlphaBlend batch.
            if (sprite.Additive) color.A = 0;
            Main.EntitySpriteDraw(texture, center - Main.screenPosition, new Rectangle(frame.X, frame.Y, frame.Width, frame.Height),
                color, sprite.Rotate ? rotation : 0, new Vector2((float)sprite.OffsetX, (float)sprite.OffsetY), scale, SpriteEffects.None);
            return true;
        }
        catch (Exception e) { error = sprite.ImagePath + ": " + e.Message; return false; }
    }
    public void Dispose()
    {
        if (textures.Count > 0) Main.RunOnMainThread(() => { foreach (var texture in textures.Values) texture.Dispose(); textures.Clear(); });
        failures.Clear(); pixels = 0;
    }
}
