using System;
using UnityEngine;

// Small reusable pixel-art drawing surface for game-native effects and the training dummy.
public sealed class CombatPixelSurface : IDisposable
{
    public readonly SpriteRenderer Renderer;
    public readonly int Width, Height;
    private readonly Texture2D texture;
    private readonly Sprite sprite;
    private readonly Color32[] pixels;
    public CombatPixelSurface(string name, Transform parent, int width, int height, float ppu, int order, Vector2? pivot = null)
    {
        Width = width; Height = height;
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        pixels = new Color32[width * height];
        sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot ?? new Vector2(.5f, .5f), ppu);
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Renderer = obj.AddComponent<SpriteRenderer>();
        Renderer.sprite = sprite; Renderer.sortingOrder = order;
    }
    public void Clear() { Array.Clear(pixels, 0, pixels.Length); }
    public void Apply() { texture.SetPixels32(pixels); texture.Apply(false); }
    public void Dot(int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        pixels[y * Width + x] = color;
    }
    public void Box(int x, int y, int width, int height, Color32 color)
    {
        for (int py = Mathf.Max(0, y); py < Mathf.Min(Height, y + height); py++)
            for (int px = Mathf.Max(0, x); px < Mathf.Min(Width, x + width); px++) Dot(px, py, color);
    }
    public void Line(Vector2 a, Vector2 b, Color32 color, int thickness = 1)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y))));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            Box(Mathf.RoundToInt(p.x) - thickness / 2, Mathf.RoundToInt(p.y) - thickness / 2, thickness, thickness, color);
        }
    }
    public void Ellipse(int cx, int cy, int rx, int ry, Color32 color, bool fill = true, int thickness = 1)
    {
        rx = Mathf.Max(1, rx); ry = Mathf.Max(1, ry);
        for (int y = -ry; y <= ry; y++)
            for (int x = -rx; x <= rx; x++)
            {
                float d = (float)(x * x) / (rx * rx) + (float)(y * y) / (ry * ry);
                float inner = (float)(x * x) / Mathf.Max(1, (rx - thickness) * (rx - thickness)) + (float)(y * y) / Mathf.Max(1, (ry - thickness) * (ry - thickness));
                if (d <= 1 && (fill || inner >= 1)) Dot(cx + x, cy + y, color);
            }
    }
    public void Bolt(Vector2 a, Vector2 b, int seed, Color32 rim, float width = 1)
    {
        Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized;
        var points = new Vector2[9];
        for (int i = 0; i < points.Length; i++)
            points[i] = Vector2.Lerp(a, b, i / 8f) + (i == 0 || i == 8 ? Vector2.zero : normal * Mathf.Sin(i * 7.31f + seed * 2.13f) * Mathf.Min(7, Vector2.Distance(a,b) * .13f));
        for (int pass = 0; pass < 3; pass++)
        {
            Color32 color = pass == 0 ? new Color32(rim.r, rim.g, rim.b, (byte)(rim.a / 5)) : pass == 1 ? rim : new Color32(255,255,235,rim.a);
            int thickness = pass == 0 ? Mathf.CeilToInt(5 * width) : pass == 1 ? Mathf.CeilToInt(3 * width) : Mathf.Max(1, Mathf.FloorToInt(width));
            for (int i = 1; i < points.Length; i++) Line(points[i-1], points[i], color, thickness);
        }
    }
    public void Dispose()
    {
        PikachuPlayer.Release(sprite); PikachuPlayer.Release(texture);
        if (Renderer) PikachuPlayer.Release(Renderer.gameObject);
    }
}
