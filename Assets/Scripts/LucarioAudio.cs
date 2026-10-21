using UnityEngine;

public enum LucarioCue { Warning, Sphere, Claw, Dash, Ultimate, Impact, Awaken }

// Short original synthesized cues; respects the menu volume and requires no remote audio.
public sealed class LucarioAudio : MonoBehaviour
{
    private AudioSource source;
    private readonly AudioClip[] clips = new AudioClip[7];
    public void Initialize()
    {
        if (!Application.isPlaying) return;
        if (!FindAnyObjectByType<AudioListener>() && Camera.main) Camera.main.gameObject.AddComponent<AudioListener>();
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.volume = .22f;
        AudioListener.volume = PlayerPrefs.GetFloat("Menu.Volume", .8f);
        for (int i = 0; i < clips.Length; i++)
        {
            float duration = i == 4 || i == 6 ? .6f : i == 5 ? .38f : .16f;
            int count = Mathf.CeilToInt(22050 * duration);
            var data = new float[count];
            uint noise = 173;
            float phase = 0;
            for (int n = 0; n < count; n++)
            {
                float t = n / 22050f, u = t / duration;
                float frequency = i == 0 ? 620 : i == 1 ? 370 + u * 600 : i == 4 ? 240 + u * 700 :
                    i == 5 ? 100 - u * 55 : i == 6 ? 440 * (u < .33f ? 1 : u < .66f ? 1.25f : 1.5f) : 600 - u * 420;
                phase += frequency * Mathf.PI * 2 / 22050;
                noise = noise * 1664525u + 1013904223u;
                float hiss = ((noise >> 8) / 16777215f - .5f) * 2;
                float envelope = Mathf.Min(1, t / .008f) * Mathf.Pow(1 - u, 1.6f);
                data[n] = envelope * (Mathf.Sin(phase) * .65f + hiss * (i == 2 || i == 3 || i == 5 ? .35f : .05f));
            }
            clips[i] = AudioClip.Create("Lucario " + (LucarioCue)i, count, 1, 22050, false);
            clips[i].SetData(data, 0);
        }
    }
    public void Cue(LucarioCue cue) { if (source && clips[(int)cue]) source.PlayOneShot(clips[(int)cue]); }
    private void OnDestroy() { foreach (var clip in clips) if (clip) PikachuPlayer.Release(clip); }
}
