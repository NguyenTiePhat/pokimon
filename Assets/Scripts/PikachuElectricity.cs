using System;
using UnityEngine;

// Small point-filtered pixel canvas: stepped white cores, gold rims, branching bolts.
// Deterministic local noise never changes Unity's global random state.
public sealed class PikachuElectricity : MonoBehaviour
{
    private const int Width = 256, Height = 192, OriginX = 128, OriginY = 32;
    private readonly Color32[] pixels = new Color32[Width * Height];
    private Texture2D texture;
    private Sprite sprite;
    private SpriteRenderer display;
    private int lastFrame = -1;
    public bool HasVisibleEffect => display && display.enabled;

    public void Initialize()
    {
        texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(.5f, OriginY / (float)Height), 32);
        var obj = new GameObject("Pixel lightning");
        obj.transform.SetParent(transform, false);
        display = obj.AddComponent<SpriteRenderer>();
        display.sprite = sprite;
        // The player's silhouette stays readable in front of the discharge.
        display.sortingOrder = 8;
        Clear();
    }

    public void Clear()
    {
        if (display) display.enabled = false;
        lastFrame = -1;
    }

    public void Sample(float seconds)
    {
        if (seconds < .12f || seconds >= PikachuPlayer.EntranceDuration) { Clear(); return; }
        display.enabled = true;
        Vector3 world = transform.position;
        display.transform.position = new Vector3(Mathf.Round(world.x * 32) / 32, Mathf.Round(world.y * 32) / 32, 0);
        int frame = Mathf.FloorToInt(seconds * 24);
        if (frame == lastFrame) return;
        lastFrame = frame;
        Array.Clear(pixels, 0, pixels.Length);
        float arrival = seconds - PikachuPlayer.RunDuration;
        if (arrival < 0) DrawRunning(frame);
        else if (arrival < PikachuPlayer.ChargeDuration) DrawCharging(arrival / PikachuPlayer.ChargeDuration, frame);
        else DrawStrike(arrival - PikachuPlayer.ChargeDuration, frame);
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    private void DrawRunning(int frame)
    {
        // Broken, short electrical wakes; no rigid lines attached to the body.
        for (int i = 0; i < 3; i++)
        {
            int phase = (frame + i * 3) % 9;
            int x = OriginX - 15 - phase * 4;
            int y = OriginY + 5 + i * 5;
            Bolt(new[] { P(x + 10, y), P(x + 3, y + 3), P(x + 5, y - 2), P(x - 9, y - 1) }, .8f - phase * .07f, false);
        }
        if (frame % 3 != 0)
            Bolt(new[] { P(OriginX + 9, OriginY + 14), P(OriginX + 14, OriginY + 17), P(OriginX + 12, OriginY + 21) }, .9f, false);
    }

    private void DrawCharging(float progress, int frame)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI / 2 + frame * .3f;
            int radius = Mathf.RoundToInt(26 - progress * 8);
            Vector2Int a = P(OriginX + Mathf.RoundToInt(Mathf.Cos(angle) * radius), OriginY + 11 + Mathf.RoundToInt(Mathf.Sin(angle) * radius * .7f));
            Bolt(new[] { a, a + P(4, 4), a + P(1, 7), a + P(6, 9) }, .5f + progress * .5f, false);
        }
        GroundArc(19, .55f, frame);
    }

    private void DrawStrike(float age, int frame)
    {
        // Two short contacts, followed by a readable, rapidly decaying afterglow.
        bool contact = age < .14f || (age > .22f && age < .31f);
        if (contact)
        {
            int shift = (frame % 3 - 1) * 3;
            Vector2Int[] trunk = {
                P(OriginX + 20 + shift, 188), P(OriginX + 5, 159), P(OriginX + 17, 147),
                P(OriginX - 11 + shift, 116), P(OriginX + 1, 109), P(OriginX - 17, 78),
                P(OriginX - 6 + shift, 68), P(OriginX - 13, OriginY + 4)
            };
            Bolt(trunk, 1, true);
            Bolt(new[] { trunk[3], P(OriginX - 38, 104), P(OriginX - 32, 92), P(OriginX - 52, 72) }, .85f, false);
            Bolt(new[] { trunk[2], P(OriginX + 39, 132), P(OriginX + 31, 119), P(OriginX + 58, 98) }, .75f, false);
            Bolt(new[] { trunk[5], P(OriginX + 23, 65), P(OriginX + 18, 55), P(OriginX + 35, 44) }, .8f, false);
        }
        float fade = Mathf.Clamp01(1 - age / .78f);
        GroundArc(Mathf.RoundToInt(16 + age * 65), fade, frame);
        for (int i = 0; i < 9; i++)
        {
            float angle = i * 2.39996f;
            float radius = 14 + age * (22 + i * 2);
            int x = OriginX + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
            int y = OriginY + 6 + Mathf.RoundToInt(Mathf.Sin(angle) * radius * .48f + age * 17);
            if ((frame + i) % 4 == 0) continue;
            if (i % 3 == 0) Bolt(new[] { P(x - 3, y), P(x + 1, y + 4), P(x - 1, y + 6), P(x + 4, y + 8) }, fade, false);
            else
            {
                Dot(x, y, 1, new Color32(255, 189, 32, (byte)(fade * 210)));
                Dot(x, y, 0, new Color32(255, 255, 209, (byte)(fade * 255)));
            }
        }
    }

    private void GroundArc(int radius, float alpha, int frame)
    {
        for (int arc = 0; arc < 5; arc++)
        {
            var points = new Vector2Int[6];
            for (int p = 0; p < points.Length; p++)
            {
                float a = arc * Mathf.PI * 2 / 5 + p * .16f + .15f;
                int jitter = ((p + frame + arc) % 3) - 1;
                points[p] = P(OriginX + Mathf.RoundToInt(Mathf.Cos(a) * (radius + jitter)),
                    OriginY + Mathf.RoundToInt(Mathf.Sin(a) * radius * .38f));
            }
            Bolt(points, alpha * .8f, false);
        }
    }

    private static Vector2Int P(int x, int y) => new Vector2Int(x, y);

    private void Bolt(Vector2Int[] points, float alpha, bool thick)
    {
        alpha = Mathf.Clamp01(alpha);
        for (int pass = 0; pass < 3; pass++)
        {
            int radius = pass == 0 ? (thick ? 4 : 2) : pass == 1 ? (thick ? 2 : 1) : 0;
            Color32 color = pass == 0 ? new Color32(255, 165, 12, (byte)(alpha * 40))
                : pass == 1 ? new Color32(255, 205, 42, (byte)(alpha * 230))
                : new Color32(255, 255, 224, (byte)(alpha * 255));
            for (int i = 1; i < points.Length; i++) Line(points[i - 1], points[i], radius, color);
        }
    }

    private void Line(Vector2Int a, Vector2Int b, int radius, Color32 color)
    {
        int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0 : i / (float)steps;
            Dot(Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t)), Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t)), radius, color);
        }
    }

    private void Dot(int x, int y, int radius, Color32 color)
    {
        for (int py = Mathf.Max(0, y - radius); py <= Mathf.Min(Height - 1, y + radius); py++)
            for (int px = Mathf.Max(0, x - radius); px <= Mathf.Min(Width - 1, x + radius); px++)
            {
                int index = py * Width + px;
                if (color.a >= pixels[index].a) pixels[index] = color;
            }
    }

    private void OnDestroy()
    {
        PikachuPlayer.Release(sprite);
        PikachuPlayer.Release(texture);
    }
}
