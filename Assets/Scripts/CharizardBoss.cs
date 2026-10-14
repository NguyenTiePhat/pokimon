using System.Collections.Generic;
using UnityEngine;

public enum CharizardSkill { Flamethrower, DragonClaw, AirSlash, FireSpin }

public sealed class CharizardBoss : TrainingDummy
{
    public float maximumHealth = 500;
    public float flameDamage = 5, clawDamage = 25, airDamage = 12, spinDamage = 8;
    public float flameCooldown = 1, clawCooldown = 8, airCooldown = 5, spinCooldown = 15;
    public bool AutoAttack = true;
    public int ActiveSkill { get; private set; } = -1;
    public bool WindingUp { get; private set; }
    public Vector2 LockedDirection { get; private set; } = Vector2.left;
    public Vector2 CastOrigin { get; private set; }
    public float PhaseTime { get; private set; }
    public string Status => !IsAlive ? "ĐÃ HẠ GỤC" : ActiveSkill < 0 ? "CHARIZARD" :
        (WindingUp ? "CHUẨN BỊ • " : "") + new[] { "FLAMETHROWER", "DRAGON CLAW", "AIR SLASH", "FIRE SPIN" }[ActiveSkill];
    public readonly List<BossProjectile> Shots = new List<BossProjectile>();
    public PikachuCombat Opponent { get; private set; }
    public float Height { get; private set; }
    private readonly float[] cooldowns = new float[4];
    private readonly Dictionary<string,PmdSpriteAnimation> clips = new Dictionary<string,PmdSpriteAnimation>();
    private SpriteRenderer body;
    private CombatPixelSurface shadow;
    private CharizardVisuals effects;
    private float clock, recovery = 1.2f;
    private int emitted, nextSkill;
    private bool clawHit;
    private static readonly float[] Windups = { .4f, .8f, .65f, 1.1f };
    private static readonly float[] Durations = { .4f, .5f, .25f, 3f };

    public void InitializeBoss(PikachuCombat opponent, Vector2 position)
    {
        Opponent = opponent; transform.position = position; ConfigureHealth(maximumHealth);
        foreach (string clip in new[] { "Idle", "Walk", "Charge", "Shoot", "Attack", "Faint" })
            clips.Add(clip,new PmdSpriteAnimation(clip,"CharizardBoss"));
        var child = new GameObject("Charizard PMDCollab sprite"); child.transform.SetParent(transform,false);
        body = child.AddComponent<SpriteRenderer>(); body.sortingOrder = 10;
        body.transform.localScale = Vector3.one * 1.7f;
        shadow = new CombatPixelSurface("Charizard ground shadow",transform,96,48,32,3);
        shadow.Clear(); shadow.Ellipse(48,24,29,10,new Color32(0,0,0,105)); shadow.Apply();
        effects = gameObject.AddComponent<CharizardVisuals>(); effects.Initialize(this);
        UpdatePose();
    }
    protected override void Update() { TickBoss(Time.deltaTime); }
    public float Cooldown(CharizardSkill skill) => cooldowns[(int)skill];
    public bool StartAttack(CharizardSkill skill)
    {
        if (!Opponent || Opponent.Player.IsEntering || Opponent.BattleEnded || ActiveSkill >= 0 || cooldowns[(int)skill] > 0) return false;
        CastOrigin = transform.position;
        Vector2 offset = (Vector2)Opponent.Player.transform.position - CastOrigin;
        LockedDirection = offset.sqrMagnitude > .001f ? offset.normalized : Vector2.left;
        ActiveSkill = (int)skill; WindingUp = true; PhaseTime = 0; emitted = 0; clawHit = false;
        cooldowns[ActiveSkill] = new[] { flameCooldown, clawCooldown, airCooldown, spinCooldown }[ActiveSkill];
        return true;
    }
    public void TickBoss(float dt)
    {
        if (!Opponent) return;
        dt = Mathf.Max(0,dt); clock += dt;
        base.Tick(dt);
        if (Opponent.Player.IsEntering || Opponent.BattleEnded)
        {
            Shots.Clear(); ActiveSkill = -1; Height = 0; PhaseTime = 0; recovery = 1.2f;
            UpdatePose(); return;
        }
        for(int i=0;i<4;i++) cooldowns[i]=Mathf.Max(0,cooldowns[i]-dt);
        TickShots(dt);
        if (ActiveSkill < 0)
        {
            recovery -= dt;
            Vector2 delta = (Vector2)Opponent.Player.transform.position-(Vector2)transform.position;
            if (delta.sqrMagnitude > .001f) LockedDirection=delta.normalized;
            if(AutoAttack && delta.magnitude>4.5f) transform.position=Clamp((Vector2)transform.position+LockedDirection*dt*1.6f*(SlowRemaining>0?1-SlowFraction:1));
            if(AutoAttack && recovery<=0)
                for(int n=0;n<4;n++)
                {
                    int candidate=(nextSkill+n)%4;
                    if(StartAttack((CharizardSkill)candidate)) { nextSkill=(candidate+1)%4; break; }
                }
            UpdatePose(); return;
        }
        PhaseTime += dt;
        if(WindingUp)
        {
            Height=ActiveSkill==1?Mathf.Clamp01(PhaseTime/Windups[1])*1.6f:0;
            if(PhaseTime<Windups[ActiveSkill]) { UpdatePose(); return; }
            PhaseTime-=Windups[ActiveSkill]; WindingUp=false;
        }
        if(ActiveSkill==0)
        {
            while(emitted<3 && PhaseTime>=emitted*.12f)
            { Shots.Add(new BossProjectile(CastOrigin+LockedDirection*.65f,LockedDirection,6,7,.3f,flameDamage,false)); emitted++; }
        }
        else if(ActiveSkill==1)
        {
            Vector2 before=transform.position;
            Vector2 after=Clamp(CastOrigin+LockedDirection*(7*Mathf.Clamp01(PhaseTime/.5f)));
            transform.position=after; Height=1.6f*(1-Mathf.Clamp01(PhaseTime/.5f));
            if(!clawHit && DistanceToSegment(Opponent.Player.transform.position,before,after)<.85f)
            { Opponent.TakeDamage(clawDamage); clawHit=true; }
        }
        else if(ActiveSkill==2 && emitted==0)
        {
            for(int i=-1;i<=1;i++)
            {
                Vector2 dir=Quaternion.Euler(0,0,i*20)*LockedDirection;
                Shots.Add(new BossProjectile(CastOrigin+dir*.65f,dir,5,8,.48f,airDamage,true));
            }
            emitted=1;
        }
        else if(ActiveSkill==3)
        {
            while(emitted<6 && PhaseTime>=(emitted+1)*.5f)
            {
                Vector2 offset=(Vector2)Opponent.Player.transform.position-CastOrigin;
                float along=Vector2.Dot(offset,LockedDirection);
                float across=Mathf.Abs(offset.x*LockedDirection.y-offset.y*LockedDirection.x);
                if(along>=.4f && along<=7 && across<=.25f+along*.18f) Opponent.TakeDamage(spinDamage);
                emitted++;
            }
        }
        if(PhaseTime>=Durations[ActiveSkill]) { ActiveSkill=-1; Height=0; recovery=.65f; }
        UpdatePose();
    }
    private void TickShots(float dt)
    {
        for(int i=Shots.Count-1;i>=0;i--)
        {
            var shot=Shots[i]; Vector2 before=shot.position;
            float step=Mathf.Min(shot.remaining,shot.speed*dt);
            shot.position+=shot.direction*step; shot.remaining-=step;
            if(DistanceToSegment(Opponent.Player.transform.position,before,shot.position)<=shot.radius+.22f)
            { Opponent.TakeDamage(shot.damage); Shots.RemoveAt(i); }
            else if(shot.remaining<=0) Shots.RemoveAt(i);
        }
    }
    public static float DistanceToSegment(Vector2 point,Vector2 a,Vector2 b)
    { Vector2 d=b-a; return Vector2.Distance(point,a+d*(d.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector2.Dot(point-a,d)/d.sqrMagnitude))); }
    private static Vector2 Clamp(Vector2 p) => new Vector2(Mathf.Clamp(p.x,-16.1f,16.1f),Mathf.Clamp(p.y,-7.4f,7.4f));
    private void UpdatePose()
    {
        if(!body) return;
        string clip=!IsAlive?"Faint":ActiveSkill<0?"Idle":WindingUp?"Charge":ActiveSkill==1?"Attack":"Shoot";
        body.sprite=clips[clip].Sample(PikachuPlayer.DirectionFor(LockedDirection),!IsAlive?0:clock);
        body.transform.localPosition=Vector3.up*Height;
        body.color=!IsAlive?new Color(.5f,.5f,.5f):SlowRemaining>0?new Color(.65f,.85f,1):Color.white;
    }
    protected override void OnDestroy()
    { foreach(var clip in clips.Values) clip.Dispose(); shadow?.Dispose(); base.OnDestroy(); }
}

public sealed class BossProjectile
{
    public Vector2 position;
    public readonly Vector2 direction;
    public readonly float speed, radius, damage;
    public float remaining;
    public readonly bool air;
    public BossProjectile(Vector2 p,Vector2 d,float speed,float range,float radius,float damage,bool air)
    { position=p; direction=d; this.speed=speed; remaining=range; this.radius=radius; this.damage=damage; this.air=air; }
}
