using System.Collections.Generic;
using UnityEngine;

public enum BlastoiseSkill { HydroPump, WaterPulse, RapidSpin, HydroCannon }

public sealed class BlastoiseBoss : ArenaBoss
{
    public float maximumHealth=650, pumpDamage=20, pulseDamage=24, spinDamage=25, cannonDamage=60;
    public bool AutoAttack=true;
    public int ActiveSkill { get; private set; }=-1;
    public bool WindingUp { get; private set; }
    public float PhaseTime { get; private set; }
    public Vector2 LockedPoint { get; private set; }
    public readonly List<WaterWave> Waves=new List<WaterWave>();
    public override string DisplayName=>"Blastoise";
    public override string Status=>!IsAlive?"ĐÃ HẠ GỤC":ActiveSkill<0?"BLASTOISE":
        (WindingUp?"CHUẨN BỊ • ":"")+new[]{"HYDRO PUMP","WATER PULSE","RAPID SPIN","HYDRO CANNON"}[ActiveSkill];
    public override bool SpinActive=>!WindingUp && (ActiveSkill==0 || ActiveSkill==3) && SpinPower>0;
    public float LaneLength=>ActiveSkill==3?18:ActiveSkill==2?4:10;
    public float LaneHalfWidth=>ActiveSkill==3?.65f:ActiveSkill==2?.95f:.55f;
    public static readonly float[] Windups={.45f,.35f,.45f,1.1f};
    public static readonly float[] Cooldowns={4,6,8,18};
    public const float PumpTurnSpeed=40f;
    public const float CannonTurnSpeed=65f;
    private Vector2 cannonTrailingDirection;
    private static readonly float[] Durations={2.4f,.15f,.65f,.8f};
    private Vector2 SpriteFacing=>Quaternion.Euler(0,0,Mathf.Round(Mathf.Atan2(LockedDirection.y,LockedDirection.x)*Mathf.Rad2Deg/45)*45)*Vector2.right;
    // Ground-projected cannon sockets; visuals add the same height to every stream segment.
    public Vector2 CannonSocket(int side)
    {
        Vector2 facing=SpriteFacing;
        return CastOrigin-facing*.15f+new Vector2(-facing.y,facing.x)*(.43f*side);
    }
    public Vector2 StreamJoin=>CastOrigin+LockedDirection*1.35f;
    public Vector2 StreamEnd=>CastOrigin+LockedDirection*LaneLength;
    public Vector2 MouthOrigin=>CastOrigin+LockedDirection*.6f;
    public const int CannonSegments=48;
    public Vector2 CannonPoint(float t)
    {
        t=Mathf.Clamp01(t);
        Vector2 normal=new Vector2(-LockedDirection.y,LockedDirection.x);
        float envelope=Mathf.Sin(t*Mathf.PI);
        // Integrate a progressively delayed heading: water farther from the mouth
        // keeps its previous direction, creating a bend when the boss turns.
        Vector2 point=MouthOrigin;
        float turn=Vector2.SignedAngle(LockedDirection,cannonTrailingDirection);
        const int steps=12;
        for(int i=0;i<steps;i++)
        {
            float along=t*(i+.5f)/steps;
            Vector2 direction=Quaternion.Euler(0,0,turn*along)*LockedDirection;
            point+=direction*((LaneLength-.6f)*t/steps);
        }
        return point+normal*(Mathf.Sin(t*14-PhaseTime*12)*.12f*envelope*envelope);
    }
    public float CannonRadius(float t)=>Mathf.Lerp(.13f,.5f,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.12f)));
    private readonly float[] cooldowns=new float[4];
    private readonly Dictionary<string,PmdSpriteAnimation> clips=new Dictionary<string,PmdSpriteAnimation>();
    private SpriteRenderer body;
    private CombatPixelSurface shadow;
    private float recovery=1.4f,clock;
    private bool hit,emitted,moving;
    private int nextSkill;
    public override void InitializeBoss(PikachuCombat opponent,Vector2 position)
    {
        Opponent=opponent; transform.position=position; ConfigureHealth(maximumHealth);
        foreach(var name in new[]{"Idle","Walk","Charge","Shoot","Attack","Rotate"}) clips.Add(name,new PmdSpriteAnimation(name,"BlastoiseBoss"));
        var go=new GameObject("Blastoise PMDCollab sprite"); go.transform.SetParent(transform,false);
        body=go.AddComponent<SpriteRenderer>(); body.sortingOrder=10; body.transform.localScale=Vector3.one*1.8f;
        shadow=new CombatPixelSurface("Blastoise ground shadow",transform,80,40,32,3);
        shadow.Clear(); shadow.Ellipse(40,20,25,8,new Color32(0,12,25,115)); shadow.Apply();
        gameObject.AddComponent<BlastoiseVisuals>().Initialize(this); Pose();
    }
    protected override void Update() { TickBoss(Time.deltaTime); }
    public float Cooldown(BlastoiseSkill skill)=>cooldowns[(int)skill];
    public bool StartAttack(BlastoiseSkill skill)
    {
        if(!Opponent || Opponent.Player.IsEntering || Opponent.BattleEnded || ActiveSkill>=0 || cooldowns[(int)skill]>0) return false;
        CastOrigin=transform.position; Vector2 delta=(Vector2)Opponent.Player.transform.position-CastOrigin;
        LockedDirection=delta.sqrMagnitude>.001f?delta.normalized:Vector2.left;
        cannonTrailingDirection=LockedDirection;
        LockedPoint=ArenaProgress.Clamp(CastOrigin+Vector2.ClampMagnitude(delta,8),gameObject.scene);
        ActiveSkill=(int)skill; PhaseTime=0; WindingUp=true; hit=emitted=false;
        SpinPower=skill==BlastoiseSkill.HydroPump?pumpDamage:skill==BlastoiseSkill.HydroCannon?cannonDamage:0;
        cooldowns[ActiveSkill]=Cooldowns[ActiveSkill]; return true;
    }
    public void TickBoss(float dt)
    {
        if(!Opponent) return;
        dt=Mathf.Max(0,dt); clock+=dt; base.Tick(dt); moving=false;
        if(Opponent.Player.IsEntering || Opponent.BattleEnded)
        { Shots.Clear(); Waves.Clear(); ActiveSkill=-1; SpinPower=0; recovery=1.4f; Pose(); return; }
        for(int i=0;i<4;i++) cooldowns[i]=Mathf.Max(0,cooldowns[i]-dt);
        TickWaves(dt);
        if((ActiveSkill==0 || ActiveSkill==3) && !WindingUp)
        {
            Vector2 target=(Vector2)Opponent.Player.transform.position-CastOrigin;
            if(target.sqrMagnitude>.001f)
            {
                float angle=Vector2.SignedAngle(LockedDirection,target);
                float speed=ActiveSkill==3?CannonTurnSpeed:PumpTurnSpeed;
                LockedDirection=Quaternion.Euler(0,0,Mathf.Clamp(angle,-speed*dt,speed*dt))*LockedDirection;
                if(ActiveSkill==3)
                {
                    float lag=Vector2.SignedAngle(cannonTrailingDirection,LockedDirection);
                    cannonTrailingDirection=Quaternion.Euler(0,0,lag*(1-Mathf.Exp(-dt/.32f)))*cannonTrailingDirection;
                }
            }
        }
        SpellClash.Step(Opponent,this,dt);
        if(Opponent.BattleEnded) { Shots.Clear(); Waves.Clear(); ActiveSkill=-1; Pose(); return; }
        if(ActiveSkill<0)
        {
            recovery-=dt;
            Vector2 delta=(Vector2)Opponent.Player.transform.position-(Vector2)transform.position;
            if(delta.sqrMagnitude>.001f) LockedDirection=delta.normalized;
            if(AutoAttack && delta.magnitude>4)
            { transform.position=ArenaProgress.Clamp((Vector2)transform.position+LockedDirection*dt*1.3f*(SlowRemaining>0?1-SlowFraction:1),gameObject.scene); moving=true; }
            if(AutoAttack && recovery<=0)
                for(int i=0;i<4;i++) if(StartAttack((BlastoiseSkill)((nextSkill+i)%4))) { nextSkill=(ActiveSkill+1)%4; break; }
            Pose(); return;
        }
        PhaseTime+=dt;
        if(WindingUp)
        {
            if(PhaseTime<Windups[ActiveSkill]) { Pose(); return; }
            PhaseTime-=Windups[ActiveSkill]; WindingUp=false;
            if(SpinActive) SpellClash.ResolveStreamOverlaps(Opponent,this);
            if(ActiveSkill<0) { Pose(); return; }
        }
        if(ActiveSkill==0 || ActiveSkill==3)
        {
            if(!hit && SpinActive && StreamContact(Opponent.Player.transform.position,Vector2.zero,0,0,out _))
            { Opponent.TakeDamage(SpinPower); hit=true; }
        }
        else if(ActiveSkill==1 && !emitted)
        {
            Vector2 start=CastOrigin+LockedDirection*.6f, delta=LockedPoint-start;
            Shots.Add(new BossProjectile(start,delta.sqrMagnitude>.001f?delta.normalized:LockedDirection,4.5f,Mathf.Max(.01f,delta.magnitude),.35f,pulseDamage,false)); emitted=true;
        }
        else if(ActiveSkill==2)
        {
            Vector2 before=transform.position, after=ArenaProgress.Clamp(CastOrigin+LockedDirection*(4*Mathf.Clamp01(PhaseTime/.65f)),gameObject.scene);
            transform.position=after;
            if(!hit && CharizardBoss.DistanceToSegment(Opponent.Player.transform.position,before,after)<=.95f)
            { Opponent.TakeDamage(spinDamage); hit=true; }
        }
        if(PhaseTime>=Durations[ActiveSkill]) { ActiveSkill=-1; SpinPower=0; recovery=.9f; }
        Pose();
    }
    public override void ProjectileImpact(BossProjectile shot,bool direct)
    {
        if(direct) Opponent.TakeDamage(shot.damage*.5f);
        if(!Opponent.BattleEnded) Waves.Add(new WaterWave(shot.position,shot.damage*.5f));
    }
    private void TickWaves(float dt)
    {
        for(int i=Waves.Count-1;i>=0;i--)
        {
            var wave=Waves[i]; float before=wave.Radius; wave.age+=dt;
            float distance=Vector2.Distance(Opponent.Player.transform.position,wave.position);
            if(!wave.hit && distance>=before-.25f && distance<=wave.Radius+.25f)
            { Opponent.TakeDamage(wave.damage); wave.hit=true; }
            if(wave.age>=.65f) Waves.RemoveAt(i);
        }
    }
    public override void ReduceSpin(float power)
    { SpinPower=Mathf.Max(0,SpinPower-power); if(SpinPower<=.0001f) { ActiveSkill=-1; recovery=.9f; } }
    public override bool StreamContact(Vector2 origin,Vector2 velocity,float radius,float horizon,out float time)
    {
        if(ActiveSkill==3)
        {
            bool contact=false; time=0;
            for(int i=0;i<CannonSegments;i++)
            {
                float t=i/(float)CannonSegments,next=(i+1)/(float)CannonSegments;
                if(SegmentContact(CannonPoint(t),CannonPoint(next),CannonRadius(next),origin,velocity,radius,horizon,out float at))
                { time=contact?Mathf.Min(time,at):at; contact=true; }
            }
            return contact;
        }
        bool found=SegmentContact(StreamJoin,StreamEnd,LaneHalfWidth,origin,velocity,radius,horizon,out time);
        for(int side=-1;side<=1;side+=2)
            if(SegmentContact(CannonSocket(side),StreamJoin,.16f,origin,velocity,radius,horizon,out float branchTime))
            { time=found?Mathf.Min(time,branchTime):branchTime; found=true; }
        return found;
    }
    private static bool SegmentContact(Vector2 start,Vector2 end,float width,Vector2 origin,Vector2 velocity,float radius,float horizon,out float time)
    {
        Vector2 direction=(end-start).normalized, d=origin-start, side=new Vector2(-direction.y,direction.x);
        float x=Vector2.Dot(d,direction), y=Vector2.Dot(d,side), vx=Vector2.Dot(velocity,direction), vy=Vector2.Dot(velocity,side);
        float enter=0,exit=horizon;
        bool result=Clip(x+radius,vx,ref enter,ref exit)&&Clip(Vector2.Distance(start,end)+radius-x,-vx,ref enter,ref exit)
            &&Clip(width+radius-y,-vy,ref enter,ref exit)&&Clip(width+radius+y,vy,ref enter,ref exit);
        time=enter; return result && enter<=exit;
    }
    private static bool Clip(float d,float v,ref float enter,ref float exit)
    { if(Mathf.Abs(v)<.00001f) return d>=0; float t=-d/v; if(v>0) enter=Mathf.Max(enter,t); else exit=Mathf.Min(exit,t); return enter<=exit; }
    private void Pose()
    {
        if(!body) return;
        string clip=ActiveSkill==2&&!WindingUp?"Rotate":ActiveSkill<0?(moving?"Walk":"Idle"):WindingUp?"Charge":"Shoot";
        body.sprite=clips[clip].Sample(PikachuPlayer.DirectionFor(SpriteFacing),clock);
        body.color=!IsAlive?new Color(.4f,.5f,.6f):SlowRemaining>0?new Color(.65f,.85f,1):Color.white;
    }
    protected override void OnDestroy() { foreach(var clip in clips.Values) clip.Dispose(); shadow?.Dispose(); base.OnDestroy(); }
}
public sealed class WaterWave
{
    public readonly Vector2 position;
    public readonly float damage;
    public float age;
    public bool hit;
    public float Radius=>Mathf.Lerp(.2f,1.8f,Mathf.Clamp01(age/.65f));
    public WaterWave(Vector2 position,float damage) { this.position=position; this.damage=damage; }
}
