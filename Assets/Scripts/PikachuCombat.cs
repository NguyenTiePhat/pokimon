using System;
using System.Collections.Generic;
using UnityEngine;

public enum PikachuSkill { Thunderbolt, QuickAttack, IronTail, ElectroBall }

[Serializable]
public sealed class PikachuSkillSettings
{
    public float mana, cooldown, castSeconds, damage, range;
    public int lightning;
    public PikachuSkillSettings(float mana, float cooldown, float cast, float damage, float range, int lightning)
    { this.mana = mana; this.cooldown = cooldown; castSeconds = cast; this.damage = damage; this.range = range; this.lightning = lightning; }
}

[DefaultExecutionOrder(-50)]
public sealed class PikachuCombat : MonoBehaviour
{
    [Header("E / Q / C / X — adjustable per ability")]
    public PikachuSkillSettings thunderbolt = new PikachuSkillSettings(30, 15, .35f, 20, 4, 3);
    public PikachuSkillSettings quickAttack = new PikachuSkillSettings(30, 20, .08f, 0, 0, 2);
    public PikachuSkillSettings ironTail = new PikachuSkillSettings(20, 10, .35f, 25, 1.8f, 5);
    public PikachuSkillSettings electroBall = new PikachuSkillSettings(70, 60, 1.5f, 100, 8, 0);
    [Header("Movement, buff and training balance")]
    public float manaRegen = 8, quickDuration = 5, quickMoveMultiplier = 1.5f, quickCastMultiplier = 1.3f;
    public float slowFraction = .4f, slowSeconds = 2, dashMana = 10, dashSpeed = 11, dashDuration = .16f;
    public float projectileSpeed = 7, minimumBallDamage = 20;
    [Header("Left mouse basic attack")]
    public float basicDamage = 5, basicMana = 5, basicCooldown = 1, basicRange = 6, basicSpeed = 10;
    public float BasicCooldownRemaining { get; private set; }
    public const int RequiredLightning = 15;
    public float Mana { get; private set; } = 100;
    public float Health { get; private set; } = 200;
    [Header("Hit reaction and stagger")]
    public float staggerThreshold = 25, staggerWindow = 2, stunDuration = .8f, stunProtection = 2;
    public float HitRemaining { get; private set; }
    public float LastHitDamage { get; private set; }
    public float StunRemaining { get; private set; }
    public float StunProtectionRemaining { get; private set; }
    private float damageClock;
    private readonly Queue<Vector2> recentHits = new Queue<Vector2>();
    public bool BattleEnded => Health <= 0 || (Dummy is CharizardBoss && !Dummy.IsAlive);
    public void TakeDamage(float damage)
    {
        if (!initialized || Player.IsEntering || BattleEnded || damage <= 0) return;
        LastHitDamage = Mathf.Min(Health,damage); Health -= LastHitDamage;
        HitRemaining = .28f;
        if (Health <= 0) { CancelForEntrance(); HitRemaining = .28f; return; }
        if (StunRemaining > 0 || StunProtectionRemaining > 0) return;
        recentHits.Enqueue(new Vector2(damageClock,LastHitDamage));
        while(recentHits.Count>0 && damageClock-recentHits.Peek().x>staggerWindow) recentHits.Dequeue();
        float total=0; foreach(var hit in recentHits) total+=hit.y;
        if(total < staggerThreshold) return;
        recentHits.Clear();
        if(active>=0) cooldowns[active]=2;
        active=AimingSkill=-1; castTime=0; DashRemaining=0; AimTargetValid=false;
        StunRemaining=stunDuration; StunProtectionRemaining=stunDuration+stunProtection;
        Player.CombatPose("Hurt",0,Player.FacingVector);
    }
    public int Lightning { get; private set; }
    public float QuickRemaining { get; private set; }
    public float DashRemaining { get; private set; }
    public bool MovementLocked => BattleEnded || StunRemaining > 0 || active >= 0 || DashRemaining > 0;
    public bool IsCasting => active >= 0;
    public int ActiveSkill => active;
    public int AimingSkill { get; private set; } = -1;
    public float CastProgress => castTime;
    public float Charge01 => Mathf.Clamp01(castTime / Mathf.Max(.01f, electroBall.castSeconds));
    public float SpeedMultiplier => QuickRemaining > 0 ? quickMoveMultiplier : 1;
    public float CastMultiplier => QuickRemaining > 0 ? quickCastMultiplier : 1;
    public Vector2 AimPoint { get; private set; }
    public int IndicatorSkill => AimingSkill >= 0 ? AimingSkill : active == 0 || active == 2 ? active : -1;
    public float IndicatorRange => IndicatorSkill >= 0 ? Settings((PikachuSkill)IndicatorSkill).range : 0;
    public Vector2 IndicatorOrigin => active == 0 || active == 2 ? castOrigin : Position;
    public Vector2 IndicatorPoint => active == 0 && lockedTarget ? (Vector2)lockedTarget.transform.position : AimPoint;
    public bool AimTargetValid { get; private set; }
    public string Feedback { get; private set; }
    public float FeedbackRemaining { get; private set; }
    public string Message { get; private set; } = "Giữ E để ngắm • C tự chọn địch ở gần • Q tăng tốc • Giữ X để nạp cầu điện";
    public TrainingDummy Dummy { get; private set; }
    public PikachuPlayer Player { get; private set; }
    public float BeamRemaining { get; private set; }
    public Vector2 BeamEnd { get; private set; }
    public readonly List<ElectricProjectile> Projectiles = new List<ElectricProjectile>();
    public readonly List<ElectricImpact> Impacts = new List<ElectricImpact>();
    public readonly List<Vector2> Craters = new List<Vector2>();
    private readonly float[] cooldowns = new float[4];
    private int active = -1;
    private float castTime, regenDelay, dashCooldown;
    private Vector2 castOrigin, lockedPoint, castFacing, dashDirection;
    private TrainingDummy lockedTarget;
    private bool initialized, hasFocus = true;
    private CombatVisuals visuals;
    private static readonly KeyCode[] Keys = { KeyCode.E, KeyCode.Q, KeyCode.C, KeyCode.X };

    public void Initialize(PikachuPlayer player, TrainingDummy dummy)
    {
        Player = player; Dummy = dummy;
        Player.AttachCombat(this);
        visuals = gameObject.AddComponent<CombatVisuals>();
        visuals.Initialize(this);
        initialized = true;
    }

    public PikachuSkillSettings Settings(PikachuSkill skill)
    {
        switch (skill) { case PikachuSkill.Thunderbolt: return thunderbolt; case PikachuSkill.QuickAttack: return quickAttack;
            case PikachuSkill.IronTail: return ironTail; default: return electroBall; }
    }
    public float Cooldown(PikachuSkill skill) => cooldowns[(int)skill];
    public bool CanAct => initialized && !Player.IsEntering && !MovementLocked;
    private Vector2 Position => Player.transform.position;

    private void Update()
    {
        if (!initialized) return;
        Tick(Time.deltaTime);
        if (Player.IsEntering || !hasFocus) return;
        Vector2 pointer = Camera.main ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : Position + Player.FacingVector * 5;
        bool overHud = CombatHud.PointerOverHud(Input.mousePosition);
        if (active == (int)PikachuSkill.ElectroBall)
        {
            AimPoint = ClampAim(pointer, electroBall.range);
            if (Input.GetMouseButtonDown(0) && !overHud) ReleaseElectroBall(pointer);
            else if (Input.GetKeyUp(KeyCode.X)) ReleaseElectroBall(Position + castFacing * electroBall.range);
            return;
        }
        if (!CanAct) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 movement = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            TryDash(movement.sqrMagnitude > 0 ? movement : Player.FacingVector);
            return;
        }
        for (int i = 0; i < Keys.Length; i++)
        {
            if (!Input.GetKeyDown(Keys[i])) continue;
            if (i != 0) { TryCast((PikachuSkill)i, pointer, false); return; }
            BeginAim((PikachuSkill)i, pointer);
        }
        if (AimingSkill < 0)
        {
            if (Input.GetMouseButtonDown(0) && !overHud) TryBasicAttack(pointer);
            return;
        }
        UpdateAim(pointer);
        if (Input.GetMouseButtonDown(0) && !overHud)
        { int skill = AimingSkill; AimingSkill = -1; TryCast((PikachuSkill)skill, pointer, false); }
        else if (Input.GetKeyUp(Keys[AimingSkill]))
        { int skill = AimingSkill; AimingSkill = -1; TryCast((PikachuSkill)skill, Position + Player.FacingVector * Settings((PikachuSkill)skill).range, true); }
    }

    public bool BeginAim(PikachuSkill skill, Vector2 pointer)
    {
        if (!CanAct || skill != PikachuSkill.Thunderbolt) return false;
        AimingSkill = (int)skill;
        UpdateAim(pointer);
        return true;
    }

    public void UpdateAim(Vector2 pointer)
    {
        if (AimingSkill < 0) return;
        float range = Settings((PikachuSkill)AimingSkill).range;
        AimTargetValid = Dummy && Dummy.IsAlive && IsPointOnDummy(pointer) && Vector2.Distance(Position, Dummy.transform.position) <= range;
        // Show the actual ground contact, even when the pointer selects the dummy's head.
        AimPoint = AimTargetValid ? (Vector2)Dummy.transform.position : ClampAim(pointer, range);
    }

    public void CancelAim() { AimingSkill = -1; AimTargetValid = false; }

    public bool TryCast(PikachuSkill skill, Vector2 point, bool facingOnly = false)
    {
        if (!CanAct) return Reject(StunRemaining>0 ? "Pikachu đang bị choáng." : "Đang vận chiêu hoặc lướt.");
        var data = Settings(skill);
        if (Cooldown(skill) > .0001f) return Reject("Chiêu đang hồi.");
        if (Mana + .0001f < data.mana) return Reject("Không đủ năng lượng.");
        if (skill == PikachuSkill.ElectroBall && Lightning < RequiredLightning) return Reject("Cần đủ 15 sét để dùng Electro Ball.");
        TrainingDummy target = null;
        if (skill == PikachuSkill.Thunderbolt || skill == PikachuSkill.IronTail)
        {
            if (!Dummy || !Dummy.IsAlive) return Reject("Không có mục tiêu còn sống.");
            Vector2 offset = (Vector2)Dummy.transform.position - Position;
            if (offset.magnitude > data.range) return Reject(skill == PikachuSkill.IronTail ? "Iron Tail: cần đến gần mục tiêu (1,8 m)." : "Mục tiêu ngoài tầm Thunderbolt.");
            bool selected = skill == PikachuSkill.IronTail || (facingOnly
                ? offset.sqrMagnitude < .01f || Vector2.Dot(offset.normalized, Player.FacingVector) >= .65f
                : IsPointOnDummy(point));
            if (!selected) return Reject("Ngắm vào mục tiêu hoặc quay mặt về phía nó.");
            target = Dummy;
        }
        Mana = Mathf.Max(0, Mana - data.mana);
        regenDelay = 2;
        cooldowns[(int)skill] = data.cooldown;
        if (skill == PikachuSkill.ElectroBall) Lightning -= RequiredLightning;
        active = (int)skill; AimingSkill = -1; castTime = 0;
        castOrigin = Position;
        lockedTarget = target;
        lockedPoint = target ? (Vector2)target.transform.position : ClampAim(point, data.range);
        castFacing = skill == PikachuSkill.ElectroBall ? Player.FacingVector : (lockedPoint - Position).normalized;
        AimPoint = target ? lockedPoint : ClampAim(point, data.range);
        AimTargetValid = target;
        Message = skill == PikachuSkill.ElectroBall ? "Đang nạp: click để ném vào chuột • thả X để ném theo hướng mặt" : "Đang vận " + SkillName(skill);
        Player.CombatPose("Charge", 0, castFacing);
        return true;
    }

    public bool TryBasicAttack(Vector2 pointer)
    {
        if (!CanAct || AimingSkill >= 0 || BasicCooldownRemaining > .0001f) return false;
        if (Mana < basicMana) return Reject("Không đủ năng lượng.");
        // Aim from the visible muzzle, keeping the flight straight through the cursor.
        Vector2 direction = pointer - (Position + Vector2.up * .45f);
        direction = direction.sqrMagnitude > .0001f ? direction.normalized : Player.FacingVector;
        Mana -= basicMana; regenDelay = 2; BasicCooldownRemaining = basicCooldown;
        Projectiles.Add(new ElectricProjectile(Position, Position + direction * basicRange, 0, basicDamage, basicSpeed, true));
        Player.CombatPose("Shoot", .08f, direction);
        return true;
    }

    public bool TryDash(Vector2 direction)
    {
        if (!CanAct || dashCooldown > 0 || Mana < dashMana) return false;
        Mana -= dashMana; regenDelay = 2; dashCooldown = .55f; AimingSkill = -1;
        DashRemaining = dashDuration;
        dashDirection = direction.sqrMagnitude > .001f ? direction.normalized : Player.FacingVector;
        return true;
    }

    public void Tick(float delta)
    {
        if (!initialized) return;
        delta = Mathf.Max(0, delta);
        HitRemaining = Mathf.Max(0,HitRemaining-delta);
        if (BattleEnded) { Projectiles.Clear(); return; }
        damageClock += delta;
        StunRemaining = Mathf.Max(0,StunRemaining-delta);
        StunProtectionRemaining = Mathf.Max(0,StunProtectionRemaining-delta);
        for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Mathf.Max(0, cooldowns[i] - delta);
        QuickRemaining = Mathf.Max(0, QuickRemaining - delta);
        dashCooldown = Mathf.Max(0, dashCooldown - delta);
        BasicCooldownRemaining = Mathf.Max(0, BasicCooldownRemaining - delta);
        BeamRemaining = Mathf.Max(0, BeamRemaining - delta);
        FeedbackRemaining = Mathf.Max(0, FeedbackRemaining - delta);
        float regeneratingTime = Mathf.Max(0, delta - regenDelay);
        regenDelay = Mathf.Max(0, regenDelay - delta);
        if (!IsCasting) Mana = Mathf.Min(100, Mana + manaRegen * regeneratingTime);
        if (!(Dummy is CharizardBoss)) TickProjectiles(delta);
        for (int i = Impacts.Count - 1; i >= 0; i--) { Impacts[i].age += delta; if (Impacts[i].age > .65f) Impacts.RemoveAt(i); }
        if (Player.IsEntering) return;
        if (StunRemaining > 0) { Player.CombatPose("Hurt",stunDuration-StunRemaining,Player.FacingVector); return; }
        if (DashRemaining > 0)
        {
            float step = Mathf.Min(delta, DashRemaining);
            Player.CombatMove(Position + dashDirection * dashSpeed * SpeedMultiplier * step, dashDirection, dashDuration - DashRemaining);
            DashRemaining = Mathf.Max(0, DashRemaining - delta);
            return;
        }
        if (active < 0) return;
        var skill = (PikachuSkill)active;
        var data = Settings(skill);
        castTime += delta * CastMultiplier;
        if (skill == PikachuSkill.IronTail)
        {
            float t = Mathf.Clamp01(castTime / Mathf.Max(.01f, data.castSeconds));
            float leap = Mathf.Clamp01((t - .35f) / .65f);
            Vector2 end = lockedPoint - (lockedPoint - castOrigin).normalized * .45f;
            Player.CombatMove(Vector2.Lerp(castOrigin, end, leap), castFacing, t * .65f, Mathf.Sin(leap * Mathf.PI) * .85f, true);
        }
        else Player.CombatPose("Charge", castTime, castFacing);
        if (castTime < data.castSeconds) return;
        if (skill == PikachuSkill.ElectroBall) { ReleaseElectroBall(AimPoint); return; }
        ResolveRegular(skill);
    }

    private void ResolveRegular(PikachuSkill skill)
    {
        var data = Settings(skill);
        if (skill == PikachuSkill.QuickAttack) QuickRemaining = quickDuration;
        else if (skill == PikachuSkill.Thunderbolt && lockedTarget && lockedTarget.IsAlive)
        {
            // A targeted discharge: position changes do not break a committed lock.
            Vector2 beamTarget = lockedTarget.transform.position;
            float remainingDamage = data.damage;
            if (lockedTarget is CharizardBoss boss)
                remainingDamage = SpellClash.TraceThunderbolt(this,boss,Position,beamTarget,data.damage,out beamTarget);
            BeamEnd = beamTarget + Vector2.up * .5f;
            BeamRemaining = .32f;
            if (remainingDamage > 0) lockedTarget.TakeDamage(remainingDamage);
        }
        else if (skill == PikachuSkill.IronTail)
        {
            Impacts.Add(new ElectricImpact(lockedPoint, .7f, true));
            if (lockedTarget && Vector2.Distance(lockedTarget.transform.position, lockedPoint) <= .85f && lockedTarget.TakeDamage(data.damage))
                lockedTarget.ApplySlow(slowFraction, slowSeconds);
        }
        Lightning = Mathf.Min(RequiredLightning, Lightning + data.lightning);
        Message = SkillName(skill) + " • +" + data.lightning + " sét";
        active = -1;
        Player.CombatPose(skill == PikachuSkill.IronTail ? "Swing" : "Shoot", .08f, castFacing);
    }

    public bool ReleaseElectroBall(Vector2 target)
    {
        if (active != (int)PikachuSkill.ElectroBall) return false;
        float charge = Charge01;
        Vector2 end = ClampAim(target, electroBall.range);
        float damage = Mathf.Lerp(minimumBallDamage, electroBall.damage, charge);
        Projectiles.Add(new ElectricProjectile(Position, end, charge, damage, projectileSpeed));
        Player.CombatPose("Shoot", .1f, end - Position);
        Message = "Electro Ball " + Mathf.RoundToInt(charge * 100) + "% • " + Mathf.RoundToInt(damage) + " sát thương nếu trúng";
        active = -1;
        return true;
    }

    private void TickProjectiles(float delta)
    {
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            var ball = Projectiles[i];
            Vector2 before = ball.Position;
            ball.elapsed = Mathf.Min(ball.duration, ball.elapsed + delta);
            Vector2 after = ball.Position;
            bool hit = Dummy && Dummy.IsAlive && SegmentDistance(Dummy.transform.position, before, after) <= .35f + ball.charge * .3f;
            if (!hit && ball.elapsed < ball.duration) continue;
            Vector2 impact = hit ? ClosestPoint(Dummy.transform.position, before, after) : ball.end;
            ResolveProjectileImpact(ball,impact,hit);
            Projectiles.RemoveAt(i);
        }
    }

    public void ResolveProjectileImpact(ElectricProjectile ball,Vector2 impact,bool hit)
    {
        if(ball.basic)
        {
            if(hit) { Dummy.TakeDamage(ball.damage); Impacts.Add(new ElectricImpact(impact,.25f,false)); }
            return;
        }
        float radius=.7f+ball.charge*.8f;
        if(Dummy && Vector2.Distance(Dummy.transform.position,impact)<=radius) Dummy.TakeDamage(ball.damage);
        Impacts.Add(new ElectricImpact(impact,radius,false)); Craters.Add(impact); visuals.AddCrater(impact,radius);
    }

    public void ClashExplosion(Vector2 point,float power)
    { Impacts.Add(new ElectricImpact(point+Vector2.up*.5f,Mathf.Lerp(.45f,1.5f,Mathf.Clamp01(power/100)),false,true)); }

    private static Vector2 ClosestPoint(Vector2 point, Vector2 from, Vector2 to)
    {
        Vector2 segment = to - from;
        return from + segment * (segment.sqrMagnitude < .000001f ? 0 : Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude));
    }
    private static float SegmentDistance(Vector2 point, Vector2 from, Vector2 to) => Vector2.Distance(point, ClosestPoint(point, from, to));
    private Vector2 ClampAim(Vector2 point, float range)
    {
        Vector2 offset = Vector2.ClampMagnitude(point - Position, range);
        if (offset.sqrMagnitude < .04f) offset = Player.FacingVector * .2f;
        var p = Position + offset;
        return new Vector2(Mathf.Clamp(p.x, -PikachuPlayer.MovementBounds.x, PikachuPlayer.MovementBounds.x), Mathf.Clamp(p.y, -PikachuPlayer.MovementBounds.y, PikachuPlayer.MovementBounds.y));
    }
    private bool Reject(string reason) { Message = Feedback = reason; FeedbackRemaining = 2; return false; }
    public bool IsPointOnDummy(Vector2 point) => Dummy && Vector2.Distance(point, (Vector2)Dummy.transform.position + Vector2.up * .6f) <= 1f;
    public void RefillTrainingResources()
    {
        if (MovementLocked || Player.IsEntering) return;
        Mana = 100; Lightning = RequiredLightning; Array.Clear(cooldowns, 0, cooldowns.Length);
        BasicCooldownRemaining = 0;
        Message = "Đã nạp năng lượng, 15 sét và hồi chiêu để thử. Hố trên sân được giữ nguyên.";
    }
    public void CancelForEntrance()
    {
        active = AimingSkill = -1; DashRemaining = QuickRemaining = BeamRemaining = 0;
        StunRemaining=StunProtectionRemaining=HitRemaining=0; recentHits.Clear();
        Projectiles.Clear(); Impacts.Clear();
    }
    private void OnApplicationFocus(bool focused)
    {
        hasFocus = focused; AimingSkill = -1;
        // Losing focus cannot leave a held ultimate waiting for a missing key-up event.
        if (!focused && active == (int)PikachuSkill.ElectroBall) ReleaseElectroBall(AimPoint);
    }
    public static string SkillName(PikachuSkill skill)
    { switch (skill) { case PikachuSkill.Thunderbolt: return "Thunderbolt"; case PikachuSkill.QuickAttack: return "Quick Attack"; case PikachuSkill.IronTail: return "Iron Tail"; default: return "Electro Ball"; } }
}

public sealed class ElectricProjectile
{
    public readonly Vector2 start, end;
    public readonly float charge, duration;
    public float damage;
    public readonly bool basic;
    public float elapsed;
    public Vector2 Position => Vector2.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
    public float Height => basic ? .45f : .45f + Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI) * (1 + charge);
    public ElectricProjectile(Vector2 start, Vector2 end, float charge, float damage, float speed, bool basic = false)
    { this.basic = basic; this.start = start; this.end = end; this.charge = charge; this.damage = damage; duration = Mathf.Max(.15f, Vector2.Distance(start, end) / Mathf.Max(.1f, speed)); }
}
public sealed class ElectricImpact
{
    public readonly Vector2 position;
    public readonly float radius;
    public readonly bool iron;
    public readonly bool clash;
    public float age;
    public ElectricImpact(Vector2 position, float radius, bool iron, bool clash=false) { this.position = position; this.radius = radius; this.iron = iron; this.clash=clash; }
}
