using UnityEngine;

public sealed class ArenaFlame : MonoBehaviour
{
    public Sprite[] frames;
    public float phase;
    public float framesPerSecond = 7;
    private SpriteRenderer spriteRenderer;
    private void Awake() { spriteRenderer = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        if (frames == null || frames.Length == 0) return;
        spriteRenderer.sprite = frames[(int)((Time.time + phase) * framesPerSecond) % frames.Length];
    }
}
