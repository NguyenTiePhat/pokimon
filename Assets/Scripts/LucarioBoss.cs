using System.Collections.Generic;
using UnityEngine;

public enum LucarioSkill { AuraSphere, MetalClaw, ExtremeSpeed, MeteorMash }

// Each attack owns its locked ground geometry for the entire anticipation/action.
public sealed class LucarioBoss : ArenaBoss
{
    public const float ClawRange = 2.15f, ClawHalfAngle = 55, LaneRadius = .65f, MeteorRadius = 2.65f;
    public static readonly float[] Windups = { .64f, .6f, .79f, 1.24f };
    private static readonly float[] Durations = { .5f, .95f, .28f, .5f };
    private static readonly string[] Names = { "AURA SPHERE", "METAL CLAW", "EXTREME SPEED", "METEOR MASH" };
    // Authored Hop foot displacement in pixels relative to frame zero, measured
    // from the original alpha bounds and green shadow anchors (S, SE, E, NE, N, NW, W, SW).
    // Remove this baked-in rise before applying our continuous ground-to-ground arc.
    private static readonly int[,] HopRise = {
        {0,10,16,20,21,22,22,19,13,1}, {0,8,14,18,20,22,22,19,13,1},
        {0,10,16,20,22,22,22,18,12,0}, {0,10,16,20,22,22,20,17,11,0},
        {0,10,16,20,21,22,21,19,13,1}, {0,10,16,20,22,22,20,17,11,0},
        {0,10,16,20,22,22,22,18,12,0}, {0,10,16,20,22,22,22,19,13,1}
    };
    [Header("Lucario balance")]
    public float maximumHealth = 850;
    public float auraDamage = 22, clawDamage = 10, speedDamage = 28, meteorDamage = 48;
    public float auraCooldown = 2.88f, clawCooldown = 4.16f, speedCooldown = 5.6f, meteorCooldown = 12;
    public bool AutoAttack = true;
    public int ActiveSkill { get; private set; } = -1;
    public bool WindingUp { get; private set; }
    public float PhaseTime { get; private set; }
    public Vector2 Target { get; private set; }
    public Vector2 DashEnd { get; private set; }
    public float Height { get; private set; }
    public bool Awakened { get; private set; }
    public float AwakeningRemaining { get; private set; }
    public float RecoveryRemaining => recovery;
    public int ClawsReleased { get; private set; }
    public int AttackCount { get; private set; }
    public string AnimationName { get; private set; }
    public float AnimationTime { get; private set; }
    public SpriteRenderer Body => body;
    public override string DisplayName => "Lucario";
    public override bool SpinActive => false;
    public override void ReduceSpin(float power) { }
    public override string Status => !IsAlive ? "ĐÃ HẠ GỤC" : AwakeningRemaining > 0 ? "LINH KHÍ THỨC TỈNH" :
        ActiveSkill < 0 ? (recovery > 0 && AttackCount > 0 ? "HỞ ĐÒN • PHẢN CÔNG!" : Awakened ? "LUCARIO • LINH KHÍ" : "LUCARIO") :
        (WindingUp ? "CHUẨN BỊ • " : "") + Names[ActiveSkill];
    public string DodgeHint => AwakeningRemaining > 0 ? "Nhịp giao đấu tăng • Dấu báo chiêu vẫn giữ nguyên" :
        ActiveSkill == 0 ? "Cầu đã khóa hướng • Đổi hướng hoặc lướt ngang" :
        ActiveSkill == 1 ? "Ba nhịp vuốt • Lùi ra ngoài cung hoặc vòng ra sau" :
        ActiveSkill == 2 ? "Làn đã khóa • Né sang hai bên" :
        ActiveSkill == 3 ? "Rời toàn bộ vòng vàng trước khi Lucario chạm đất" :
        recovery > 0 && AttackCount > 0 ? "Tận dụng khoảng nghỉ để đánh trả!" : "Võ đường trên mây • Đọc thế võ, tìm nhịp phản công";

    private readonly float[] cooldowns = new float[4];
    private readonly Dictionary<string, PmdSpriteAnimation> clips = new Dictionary<string, PmdSpriteAnimation>();
    private SpriteRenderer body;
    private CombatPixelSurface shadow;
    private LucarioAudio sound;
    private float clock, walkClock, recovery = 1.3f, hitFlash, deathTime;
    private int nextSkill;
    private bool emitted, dashHit, walking;
    public float Cooldown(LucarioSkill skill) => cooldowns[(int)skill];

    public override void InitializeBoss(PikachuCombat opponent, Vector2 position)
    {
        Opponent = opponent;
        transform.position = position;
        ConfigureHealth(maximumHealth);
        foreach (string clip in new[] { "Idle", "Walk", "Charge", "RearUp", "Attack", "QuickStrike", "Hop", "Hurt", "Faint" })
            clips.Add(clip, new PmdSpriteAnimation(clip, "LucarioBoss"));
        var child = new GameObject("Lucario • shadow anchored 8 direction sprite");
        child.transform.SetParent(transform, false);
        body = child.AddComponent<SpriteRenderer>();
        body.sortingOrder = 10;
        child.transform.localScale = Vector3.one * 1.65f;
        shadow = new CombatPixelSurface("Lucario ground shadow", transform, 64, 32, 32, 3);
        shadow.Ellipse(32, 16, 17, 6, new Color32(16,29,42,120));
        shadow.Apply();
        gameObject.AddComponent<LucarioVisuals>().Initialize(this);
        sound = gameObject.AddComponent<LucarioAudio>();
        sound.Initialize();
        Pose();
    }

    public override bool TakeDamage(float damage)
    {
        bool hit = base.TakeDamage(damage);
        if (hit && damage > 0) hitFlash = .14f;
        if (!IsAlive) ClearBattle();
        return hit;
    }

    public bool StartAttack(LucarioSkill skill)
    {
        int index = (int)skill;
        if (index < 0 || index > 3 || !IsAlive || !Opponent || Opponent.Player.IsEntering ||
            Opponent.BattleEnded || ActiveSkill >= 0 || AwakeningRemaining > 0 || cooldowns[index] > 0) return false;
        CastOrigin = transform.position;
        Target = ArenaProgress.Clamp(Opponent.Player.transform.position, gameObject.scene);
        Vector2 delta = Target - CastOrigin;
        LockedDirection = delta.sqrMagnitude > .001f ? delta.normalized : Vector2.left;
        // Clamp the endpoint, never the intermediate samples: the shown lane stays straight at corners.
        DashEnd = ArenaProgress.Clamp(CastOrigin + LockedDirection * Mathf.Min(10, delta.magnitude + 2.1f), gameObject.scene);
        ActiveSkill = index;
        WindingUp = true;
        PhaseTime = Height = 0;
        ClawsReleased = 0;
        emitted = dashHit = walking = false;
        cooldowns[index] = index == 0 ? auraCooldown : index == 1 ? clawCooldown : index == 2 ? speedCooldown : meteorCooldown;
        AttackCount++;
        sound?.Cue(index == 3 ? LucarioCue.Ultimate : LucarioCue.Warning);
        Pose();
        return true;
    }

    protected override void Update() => TickBoss(Time.deltaTime);
    public void TickBoss(float dt)
    {
        if (!Opponent) return;
        // Preserve every strike and swept dash contact even during long frames.
        float remaining = Mathf.Max(0, dt);
        while (remaining > .000001f)
        {
            float step = Mathf.Min(1f / 120, remaining);
            Step(step);
            remaining -= step;
        }
        Pose();
    }

    private void Step(float dt)
    {
        clock += dt;
        hitFlash = Mathf.Max(0, hitFlash - dt);
        base.Tick(dt);
        if (Opponent.Player.IsEntering || Opponent.BattleEnded)
        {
            if (!IsAlive) deathTime += dt;
            ClearBattle();
            return;
        }
        for (int i = 0; i < 4; i++) cooldowns[i] = Mathf.Max(0, cooldowns[i] - dt);
        SpellClash.Step(Opponent, this, dt);
        if (Opponent.BattleEnded) { ClearBattle(); return; }
        if (ActiveSkill < 0)
        {
            walking = false;
            if (!Awakened && Health <= HealthCapacity * .5f)
            {
                Awakened = true;
                AwakeningRemaining = 1.4f;
                recovery = 1.4f;
                sound?.Cue(LucarioCue.Awaken);
            }
            if (AwakeningRemaining > 0)
            {
                AwakeningRemaining = Mathf.Max(0, AwakeningRemaining - dt);
                recovery = Mathf.Max(recovery, .6f);
                return;
            }
            recovery = Mathf.Max(0, recovery - dt);
            Vector2 delta = (Vector2)Opponent.Player.transform.position - (Vector2)transform.position;
            if (delta.sqrMagnitude > .001f) LockedDirection = delta.normalized;
            if (AutoAttack && recovery <= 0)
            {
                if (delta.magnitude > 1.8f)
                {
                    float distance = Mathf.Min(delta.magnitude - 1.8f, dt * (Awakened ? 3.24f : 2.64f) * (SlowRemaining > 0 ? 1 - SlowFraction : 1));
                    transform.position = ArenaProgress.Clamp((Vector2)transform.position + LockedDirection * distance, gameObject.scene);
                    walking = distance > .0001f;
                    walkClock += distance / 2.2f;
                }
                for (int n = 0; n < 4; n++)
                {
                    int candidate = (nextSkill + n) % 4;
                    if (candidate == 1 && delta.magnitude > ClawRange + .2f) continue;
                    if (StartAttack((LucarioSkill)candidate)) { nextSkill = (candidate + 1) % 4; break; }
                }
            }
            return;
        }
        PhaseTime += dt;
        if (WindingUp)
        {
            if (ActiveSkill == 3)
            {
                float flight = Mathf.Clamp01((PhaseTime - .35f) / (Windups[3] - .35f));
                transform.position = Vector2.Lerp(CastOrigin, Target, Mathf.SmoothStep(0, 1, flight));
                Height = Mathf.Sin(flight * Mathf.PI) * .7f;
            }
            if (PhaseTime + .000001f < Windups[ActiveSkill]) return;
            PhaseTime = Mathf.Max(0, PhaseTime - Windups[ActiveSkill]);
            WindingUp = false;
            Height = 0;
        }
        if (ActiveSkill == 0 && !emitted && PhaseTime >= .1f)
        {
            Shots.Add(new BossProjectile(CastOrigin + LockedDirection * .6f, LockedDirection, 6, 22, .34f, auraDamage, true));
            emitted = true;
            sound?.Cue(LucarioCue.Sphere);
        }
        else if (ActiveSkill == 1)
        {
            while (ClawsReleased < 3 && PhaseTime + .000001f >= .06f + ClawsReleased * .3f)
            {
                if (InClaw(Opponent.Player.transform.position)) Opponent.TakeDamage(clawDamage);
                ClawsReleased++;
                sound?.Cue(LucarioCue.Claw);
            }
        }
        else if (ActiveSkill == 2)
        {
            Vector2 before = transform.position;
            Vector2 after = Vector2.Lerp(CastOrigin, DashEnd, Mathf.Clamp01(PhaseTime / Durations[2]));
            if (!emitted) { sound?.Cue(LucarioCue.Dash); emitted = true; }
            transform.position = after;
            if (!dashHit && CharizardBoss.DistanceToSegment(Opponent.Player.transform.position, before, after) <= LaneRadius)
            { Opponent.TakeDamage(speedDamage); dashHit = true; }
        }
        else if (ActiveSkill == 3 && !emitted)
        {
            transform.position = Target;
            if (Vector2.Distance(Opponent.Player.transform.position, Target) <= MeteorRadius) Opponent.TakeDamage(meteorDamage);
            emitted = true;
            sound?.Cue(LucarioCue.Impact);
        }
        if (PhaseTime >= Durations[ActiveSkill])
        {
            recovery = ActiveSkill == 3 ? 1.15f : ActiveSkill == 0 ? .49f : .79f;
            if (Awakened && ActiveSkill != 3) recovery *= .8f;
            ActiveSkill = -1;
            Height = 0;
        }
        if (Opponent.BattleEnded) ClearBattle();
    }

    public bool InClaw(Vector2 position)
    {
        Vector2 offset = position - CastOrigin;
        return offset.magnitude <= ClawRange && (offset.sqrMagnitude < .0001f ||
            Vector2.Dot(offset.normalized, LockedDirection) >= Mathf.Cos(ClawHalfAngle * Mathf.Deg2Rad));
    }

    private void ClearBattle()
    {
        Shots.Clear();
        ActiveSkill = -1;
        WindingUp = walking = false;
        Height = AwakeningRemaining = 0;
        recovery = 1.3f;
        if (body) body.transform.localPosition = Vector3.zero;
    }

    private void Pose()
    {
        if (!body) return;
        string clip = "Idle";
        float time = clock;
        bool once = false;
        if (!IsAlive) { clip = "Faint"; time = deathTime; once = true; }
        else if (AwakeningRemaining > 0) { clip = "Charge"; time = 1.4f - AwakeningRemaining; }
        else if (ActiveSkill >= 0)
        {
            time = PhaseTime;
            if (WindingUp) clip = ActiveSkill == 3 && PhaseTime > .35f ? "Hop" : "Charge";
            else if (ActiveSkill == 0) { clip = "RearUp"; time = PhaseTime * 1.8f; once = true; }
            else if (ActiveSkill == 1) { clip = "Attack"; time = (PhaseTime % .3f) * (clips["Attack"].Duration / .3f); }
            else if (ActiveSkill == 2) { clip = "QuickStrike"; time = PhaseTime * (clips["QuickStrike"].Duration / .28f); once = true; }
            else { clip = "Attack"; time = PhaseTime + .1f; once = true; }
            if (WindingUp && ActiveSkill == 3 && PhaseTime > .35f)
            { time = (PhaseTime - .35f) / (Windups[3] - .35f) * clips["Hop"].Duration; once = true; }
        }
        else if (walking) { clip = "Walk"; time = walkClock; }
        if (WindingUp && clip == "Charge") time *= ChargeAnimationRate;
        AnimationName = clip;
        AnimationTime = once ? Mathf.Min(time, clips[clip].Duration - .001f) : time;
        int direction = PikachuPlayer.DirectionFor(LockedDirection);
        body.sprite = clips[clip].Sample(direction, AnimationTime);
        float authoredRise = clip == "Hop" ? HopRise[direction, Mathf.RoundToInt(body.sprite.rect.x / body.sprite.rect.width)] * 1.65f / 32 : 0;
        body.transform.localPosition = Vector3.up * (Height - authoredRise);
        body.color = !IsAlive ? new Color(.55f,.64f,.7f) : hitFlash > 0 ? new Color(1,.65f,.55f) :
            SlowRemaining > 0 ? new Color(.65f,.85f,1) : Color.white;
        shadow.Renderer.transform.localScale = Vector3.one * Mathf.Lerp(1, .55f, Height / .7f);
    }

    protected override void OnDestroy()
    {
        foreach (var clip in clips.Values) clip.Dispose();
        shadow?.Dispose();
        base.OnDestroy();
    }
}
