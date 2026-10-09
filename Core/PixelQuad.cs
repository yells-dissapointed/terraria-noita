using System;

namespace terrarianoita.Core;

/// <summary>Convert a requested size in world pixels to texture-relative draw units.</summary>
public readonly record struct PixelQuad(float OriginX, float OriginY, float ScaleX, float ScaleY)
{
    public static PixelQuad Fit(int textureWidth, int textureHeight, float width, float height)
    {
        if (textureWidth < 1 || textureHeight < 1 || !float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        return new(textureWidth / 2f, textureHeight / 2f, width / textureWidth, height / textureHeight);
    }
}
