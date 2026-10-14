using UnityEngine;
using System.Collections.Generic;

public sealed class PikachuPlayer : MonoBehaviour
{
    public const float RunDuration = 2.4f;
    public const float ChargeDuration = .38f;
    public const float EntranceDuration = 3.65f;
    public const float MoveSpeed = 3.2f;
    private const float StrideDistance = 1.15f;
    public bool IsEntering { get; private set; }
    public bool IsMoving { get; private set; }
    public int FacingDirection { get; private set; } = 2;
    public Sprite CurrentSprite => body ? body.sprite : null;
    public Vector2 VisualPosition => body ? (Vector2)body.transform.position : (Vector2)transform.position;
    public static readonly Vector2 MovementBounds = new Vector2(16.1f, 7.4f);

    private SpriteRenderer body, shadow;
    private PmdSpriteAnimation walk, idle, charge;
    private PikachuElectricity electricity;
    private Texture2D shadowTexture;
    private Sprite shadowSprite;
    private float elapsed, distanceTravelled, startX;
    private bool initialized;
    private Camera arenaCamera;
    private PikachuCombat combat;
    private readonly Dictionary<string, PmdSpriteAnimation> combatAnimations = new Dictionary<string, PmdSpriteAnimation>();
    public Vector2 FacingVector => new[] { Vector2.down, new Vector2(1,-1).normalized, Vector2.right, Vector2.one.normalized,
        Vector2.up, new Vector2(-1,1).normalized, Vector2.left, new Vector2(-1,-1).normalized }[FacingDirection];
    public void AttachCombat(PikachuCombat controller) { combat = controller; }

    public void Initialize(Camera camera = null)
    {
        if (initialized) return;
        arenaCamera = camera ? camera : Camera.main;
        walk = new PmdSpriteAnimation("Walk");
        idle = new PmdSpriteAnimation("Idle");
        charge = new PmdSpriteAnimation("Charge");
        body = MakeRenderer("Pikachu sprite", 10);
        shadow = MakeRenderer("Ground shadow", 3);
        shadowTexture = new Texture2D(24, 12, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color32[24 * 12];
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 24; x++)
            {
                float radius = Mathf.Pow((x - 11.5f) / 11, 2) + Mathf.Pow((y - 5.5f) / 5, 2);
                pixels[y * 24 + x] = new Color32(8, 12, 20, radius <= 1 ? (byte)100 : (byte)0);
            }
        shadowTexture.SetPixels32(pixels);
        shadowTexture.Apply();
        shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, 24, 12), new Vector2(.5f, .5f), 32);
        shadow.sprite = shadowSprite;
        electricity = gameObject.AddComponent<PikachuElectricity>();
        electricity.Initialize();
        initialized = true;
        ReplayEntrance();
    }

    private SpriteRenderer MakeRenderer(string label, int order)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(transform, false);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = order;
        return renderer;
    }

    public void ReplayEntrance()
    {
        if (combat) combat.CancelForEntrance();
        elapsed = distanceTravelled = 0;
        FacingDirection = 2;
        IsMoving = false;
        if (arenaCamera)
        {
            arenaCamera.transform.position = new Vector3(0, 0, -10);
            startX = -Mathf.Min(MovementBounds.x, arenaCamera.orthographicSize * arenaCamera.aspect + 1f);
        }
        else startX = -8;
        electricity.Clear();
        SampleEntrance(0);
    }

    public void SampleEntrance(float seconds)
    {
        seconds = Mathf.Max(0, seconds);
        IsEntering = seconds < EntranceDuration;
        // Constant speed, then decelerate over the final 20% of the run.
        float t = Mathf.Clamp01(seconds / RunDuration);
        float progress = t < .8f ? t / .9f : (.8f + (t - .8f) - 2.5f * (t - .8f) * (t - .8f)) / .9f;
        float x = t >= 1 ? 0 : Mathf.Lerp(startX, 0, progress);
        transform.position = new Vector3(x, 0, 0);
        FacingDirection = 2;
        IsMoving = seconds < RunDuration;
        if (IsMoving) body.sprite = walk.Sample(2, (x - startX) / StrideDistance * walk.Duration);
        else if (seconds < RunDuration + ChargeDuration) body.sprite = charge.Sample(2, seconds - RunDuration);
        else body.sprite = idle.Sample(2, 0);
        distanceTravelled = 0;
        SnapVisuals();
        electricity.Sample(seconds);
    }

    private void LateUpdate()
    {
        if(body) body.color=combat && combat.HitRemaining>0
            ? Color.Lerp(new Color(1,.3f,.25f),Color.white,Mathf.PingPong(combat.HitRemaining*22,1)) : Color.white;
    }

    private void Update()
    {
        if (!initialized) return;
        if (IsEntering)
        {
            elapsed += Time.deltaTime;
            SampleEntrance(elapsed);
            return;
        }
        if (combat && combat.MovementLocked) return;
        TickMovement(new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
            (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)), Time.deltaTime);
    }

    // Input and editor checks exercise the same motion code.
    public void TickMovement(Vector2 input, float deltaTime)
    {
        if (!initialized || IsEntering || (combat && combat.MovementLocked)) return;
        Vector2 movement = Vector2.ClampMagnitude(input, 1);
        Vector3 previous = transform.position;
        Vector3 next = previous + (Vector3)movement * (MoveSpeed * (combat ? combat.SpeedMultiplier : 1) * Mathf.Max(0, deltaTime));
        transform.position = new Vector3(Mathf.Clamp(next.x, -MovementBounds.x, MovementBounds.x), Mathf.Clamp(next.y, -MovementBounds.y, MovementBounds.y), 0);
        float distance = Vector3.Distance(previous, transform.position);
        bool moving = distance > .00001f;
        if (movement.sqrMagnitude > .001f) FacingDirection = DirectionFor(movement);
        if (moving != IsMoving) distanceTravelled = 0;
        IsMoving = moving;
        distanceTravelled += distance;
        // Source Idle includes a hop; hold its grounded first pose for combat readiness.
        body.sprite = moving ? walk.Sample(FacingDirection, distanceTravelled / StrideDistance * walk.Duration)
            : idle.Sample(FacingDirection, 0);
        SnapVisuals();
    }

    public static int DirectionFor(Vector2 movement)
    {
        // Visually verified source rows: S, SE, E, NE, N, NW, W, SW.
        // See Docs/Previews/Pikachu-SourceRows.png. No mirroring or rotation.
        int x = movement.x > .01f ? 1 : movement.x < -.01f ? -1 : 0;
        int y = movement.y > .01f ? 1 : movement.y < -.01f ? -1 : 0;
        if (x == 0) return y > 0 ? 4 : 0;
        if (x > 0) return y > 0 ? 3 : y < 0 ? 1 : 2;
        return y > 0 ? 5 : y < 0 ? 7 : 6;
    }

    private void SnapVisuals()
    {
        // Snap artwork to native pixels without quantizing simulation or input.
        var p = transform.position;
        Vector3 snapped = new Vector3(Mathf.Round(p.x * 32) / 32, Mathf.Round(p.y * 32) / 32, 0);
        body.transform.position = snapped;
        shadow.transform.position = snapped;
    }

    public void CombatPose(string animation, float time, Vector2 facing, float height = 0)
    {
        IsMoving = false;
        if (facing.sqrMagnitude > .001f) FacingDirection = DirectionFor(facing);
        if (!combatAnimations.TryGetValue(animation, out var clip))
        {
            clip = new PmdSpriteAnimation(animation);
            combatAnimations.Add(animation, clip);
        }
        body.sprite = clip.Sample(FacingDirection, time);
        SnapVisuals();
        body.transform.position += Vector3.up * height;
    }

    public void CombatMove(Vector2 position, Vector2 facing, float animationTime, float height = 0, bool tail = false)
    {
        transform.position = new Vector3(Mathf.Clamp(position.x, -MovementBounds.x, MovementBounds.x), Mathf.Clamp(position.y, -MovementBounds.y, MovementBounds.y), 0);
        CombatPose(tail ? "Swing" : "Walk", animationTime, facing, height);
        IsMoving = true;
    }

    private void OnDestroy()
    {
        walk?.Dispose(); idle?.Dispose(); charge?.Dispose();
        foreach (var clip in combatAnimations.Values) clip.Dispose();
        Release(shadowSprite); Release(shadowTexture);
    }

    public static void Release(Object obj)
    {
        if (!obj) return;
        if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
    }
}
