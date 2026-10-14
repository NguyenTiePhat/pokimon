using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

// Reads original sheets and uses each frame's shadow marker to anchor its feet.
public sealed class PmdSpriteAnimation : IDisposable
{
    private readonly Sprite[,] frames;
    private readonly float[] durations;
    public float Duration { get; }

    public PmdSpriteAnimation(string name, string folder = "Pikachu")
    {
        var metadata = Resources.Load<TextAsset>(folder + "/AnimData");
        if (!metadata) throw new InvalidOperationException("Missing Pikachu AnimData.xml");
        XElement animation = null;
        foreach (var candidate in XDocument.Parse(metadata.text).Root.Element("Anims").Elements("Anim"))
            if ((string)candidate.Element("Name") == name) { animation = candidate; break; }
        if (animation == null) throw new InvalidOperationException("Missing Pikachu animation: " + name);
        int width = (int)animation.Element("FrameWidth"), height = (int)animation.Element("FrameHeight");
        var timing = new List<float>();
        foreach (var value in animation.Element("Durations").Elements("Duration")) timing.Add((int)value / 60f);
        durations = timing.ToArray();
        foreach (float duration in durations) Duration += duration;
        var texture = Resources.Load<Texture2D>(folder + "/" + name + "-Anim");
        var shadow = Resources.Load<Texture2D>(folder + "/" + name + "-Shadow");
        if (!texture || !shadow || texture.width != width * durations.Length || texture.height != height * 8
            || shadow.width != texture.width || shadow.height != texture.height)
            throw new InvalidOperationException("Invalid Pikachu sheet dimensions: " + name);
        frames = new Sprite[8, durations.Length];
        var pixels = shadow.GetPixels32();
        for (int row = 0; row < 8; row++)
            for (int frame = 0; frame < durations.Length; frame++)
            {
                int left = frame * width, bottom = texture.height - (row + 1) * height;
                Vector2 center = Vector2.zero;
                int count = 0;
                // Green marks the center in the multicolored shadow template.
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        Color32 color = pixels[(bottom + y) * shadow.width + left + x];
                        if (color.a > 0 && color.g > 200 && color.r < 80 && color.b < 80)
                        { center += new Vector2(x + .5f, y + .5f); count++; }
                    }
                if (count == 0) throw new InvalidOperationException("Missing shadow anchor: " + name + "/" + row + "/" + frame);
                center /= count;
                var sprite = Sprite.Create(texture, new Rect(left, bottom, width, height), new Vector2(center.x / width, center.y / height), 32);
                sprite.name = name + " direction " + row + " frame " + frame;
                frames[row, frame] = sprite;
            }
    }

    public Sprite Sample(int direction, float seconds)
    {
        float phase = Mathf.Max(0, seconds) % Duration;
        int frame = 0;
        while (frame < durations.Length - 1 && phase >= durations[frame]) phase -= durations[frame++];
        return frames[direction, frame];
    }

    public void Dispose()
    {
        foreach (var sprite in frames) PikachuPlayer.Release(sprite);
    }
}
