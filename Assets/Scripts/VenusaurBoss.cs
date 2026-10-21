using System.Collections.Generic;
using UnityEngine;

public enum VenusaurSkill { RazorLeaf, LeechSeed, VineWhip, SolarBeam }
public sealed class VenusaurBoss : ArenaBoss
{
    public override string DisplayName=>"Venusaur";
    public override string Status=>!IsAlive?"ĐÃ HẠ GỤC":ActiveSkill<0?"VENUSAUR":(WindingUp?"CHUẨN BỊ • ":"")+new[]{"RAZOR LEAF","LEECH SEED","VINE WHIP","SOLAR BEAM"}[ActiveSkill];
    public int ActiveSkill { get; private set; }=-1;
    public bool WindingUp { get; private set; }
    public float PhaseTime { get; private set; }
    public Vector2 SeedTarget { get; private set; }
    public bool AutoAttack=true;
    public static readonly float[] Windups={.42f,.6f,.49f,1.24f}, Cooldowns={2.8f,5.6f,4,12.8f};
    private static readonly float[] Durations={.18f,.2f,.55f,.8f};
    public readonly List<SeedPatch> Patches=new List<SeedPatch>();
    public override bool SpinActive=>ActiveSkill==3&&!WindingUp&&SpinPower>0;
    public Vector2 BeamStart=>CastOrigin+LockedDirection*.55f;
    public Vector2 BeamEnd=>CastOrigin+LockedDirection*16;
    public float WhipAngle=>-65+130*Mathf.Clamp01(PhaseTime/.55f);
    public Vector2 WhipDirection=>Quaternion.Euler(0,0,WhipAngle)*LockedDirection;
    private readonly float[] cooldowns=new float[4];
    private readonly Dictionary<string,PmdSpriteAnimation> clips=new Dictionary<string,PmdSpriteAnimation>();
    private SpriteRenderer body;
    private CombatPixelSurface shadow;
    private float clock,recovery=1.3f;
    private int next;
    private bool hit,emitted;
    public override void InitializeBoss(PikachuCombat opponent,Vector2 position)
    {
        Opponent=opponent; transform.position=position; ConfigureHealth(750);
        foreach(string clip in new[]{"Idle","Walk","Charge","Shoot","Attack"}) clips.Add(clip,new PmdSpriteAnimation(clip,"VenusaurBoss"));
        var go=new GameObject("Venusaur PMDCollab"); go.transform.SetParent(transform,false);
        body=go.AddComponent<SpriteRenderer>(); body.sortingOrder=10; go.transform.localScale=Vector3.one*2.2f;
        shadow=new CombatPixelSurface("Venusaur shadow",transform,80,40,32,3);
        shadow.Ellipse(40,20,29,10,new Color32(5,22,9,120)); shadow.Apply();
        gameObject.AddComponent<VenusaurVisuals>().Initialize(this); Pose();
    }
    public bool StartAttack(VenusaurSkill skill)
    {
        if(!Opponent||Opponent.Player.IsEntering||Opponent.BattleEnded||ActiveSkill>=0||cooldowns[(int)skill]>0) return false;
        CastOrigin=transform.position; Vector2 delta=(Vector2)Opponent.Player.transform.position-CastOrigin;
        LockedDirection=delta.sqrMagnitude>.001f?delta.normalized:Vector2.left;
        SeedTarget=ArenaProgress.Clamp(CastOrigin+Vector2.ClampMagnitude(delta,7),gameObject.scene);
        ActiveSkill=(int)skill; PhaseTime=0; WindingUp=true; hit=emitted=false;
        SpinPower=skill==VenusaurSkill.SolarBeam?60:0; cooldowns[ActiveSkill]=Cooldowns[ActiveSkill]; return true;
    }
    protected override void Update()=>TickBoss(Time.deltaTime);
    public void TickBoss(float dt)
    {
        if(!Opponent) return;
        dt=Mathf.Max(0,dt); clock+=dt; base.Tick(dt);
        if(Opponent.Player.IsEntering||Opponent.BattleEnded) { Shots.Clear(); Patches.Clear(); ActiveSkill=-1; SpinPower=0; Pose(); return; }
        for(int i=0;i<4;i++) cooldowns[i]=Mathf.Max(0,cooldowns[i]-dt);
        for(int i=Patches.Count-1;i>=0;i--)
        {
            var patch=Patches[i]; patch.age+=dt;
            while(patch.nextTick<=3 && patch.age>=patch.nextTick)
            {
                if(Vector2.Distance(Opponent.Player.transform.position,patch.position)<=1.3f) Opponent.TakeDamage(6);
                patch.nextTick+=.6f;
            }
            if(patch.age>=3.2f) Patches.RemoveAt(i);
        }
        SpellClash.Step(Opponent,this,dt);
        if(Opponent.BattleEnded) { Shots.Clear(); Patches.Clear(); ActiveSkill=-1; SpinPower=0; Pose(); return; }
        if(ActiveSkill<0)
        {
            recovery-=dt; Vector2 delta=(Vector2)Opponent.Player.transform.position-(Vector2)transform.position;
            if(delta.sqrMagnitude>.001f) LockedDirection=delta.normalized;
            if(AutoAttack && delta.magnitude>4.5f) transform.position=ArenaProgress.Clamp((Vector2)transform.position+LockedDirection*dt*1.2f*(SlowRemaining>0?1-SlowFraction:1),gameObject.scene);
            if(AutoAttack&&recovery<=0) for(int i=0;i<4;i++) if(StartAttack((VenusaurSkill)((next+i)%4))) { next=(ActiveSkill+1)%4; break; }
            Pose(); return;
        }
        float previous=PhaseTime; PhaseTime+=dt;
        if(WindingUp)
        {
            if(PhaseTime<Windups[ActiveSkill]) { Pose(); return; }
            PhaseTime-=Windups[ActiveSkill]; previous=0; WindingUp=false;
            if(SpinActive) SpellClash.ResolveStreamOverlaps(Opponent,this);
            if(ActiveSkill<0) { Pose(); return; }
        }
        if(ActiveSkill==0&&!emitted)
        {
            for(int i=-2;i<=2;i++)
            {
                Vector2 dir=Quaternion.Euler(0,0,i*20)*LockedDirection;
                Shots.Add(new BossProjectile(CastOrigin+dir*.75f,dir,5.8f,12,.13f,10,true));
            }
            emitted=true;
        }
        if(ActiveSkill==1&&!emitted) { Patches.Add(new SeedPatch(SeedTarget)); emitted=true; }
        if(ActiveSkill==2&&!hit)
        {
            Vector2 delta=(Vector2)Opponent.Player.transform.position-CastOrigin;
            float angle=Vector2.SignedAngle(LockedDirection,delta),before=-65+130*Mathf.Clamp01(previous/.55f);
            if(delta.magnitude<=3.2f && angle>=before-8 && angle<=WhipAngle+8)
            { Opponent.TakeDamage(25); hit=true; }
        }
        if(SpinActive&&!hit&&StreamContact(Opponent.Player.transform.position,Vector2.zero,0,0,out _)) { Opponent.TakeDamage(SpinPower); hit=true; }
        if(PhaseTime>=Durations[ActiveSkill]) { ActiveSkill=-1; SpinPower=0; recovery=.6f; }
        Pose();
    }
    public override void ReduceSpin(float power) { SpinPower=Mathf.Max(0,SpinPower-power); if(SpinPower<=.001f) { ActiveSkill=-1; recovery=.6f; } }
    public override bool StreamContact(Vector2 origin,Vector2 velocity,float radius,float horizon,out float time)
    {
        Vector2 d=origin-BeamStart,n=new Vector2(-LockedDirection.y,LockedDirection.x);
        float along=Vector2.Dot(d,LockedDirection),across=Vector2.Dot(d,n),vx=Vector2.Dot(velocity,LockedDirection),vy=Vector2.Dot(velocity,n);
        float enter=0,exit=horizon;
        bool result=Clip(along+radius,vx,ref enter,ref exit)&&Clip(15.45f+radius-along,-vx,ref enter,ref exit)
            &&Clip(.8f+radius-across,-vy,ref enter,ref exit)&&Clip(.8f+radius+across,vy,ref enter,ref exit);
        time=enter; return result;
    }
    private static bool Clip(float d,float v,ref float enter,ref float exit)
    { if(Mathf.Abs(v)<.00001f) return d>=0; float t=-d/v; if(v>0) enter=Mathf.Max(enter,t); else exit=Mathf.Min(exit,t); return enter<=exit; }
    private void Pose()
    {
        if(!body)return;
        string clip=ActiveSkill<0?"Idle":WindingUp?"Charge":ActiveSkill==2?"Attack":"Shoot";
        body.sprite=clips[clip].Sample(PikachuPlayer.DirectionFor(LockedDirection),ActiveSkill<0?clock:WindingUp?PhaseTime*ChargeAnimationRate:PhaseTime);
        body.color=!IsAlive?new Color(.45f,.5f,.4f):SlowRemaining>0?new Color(.6f,.85f,1):Color.white;
    }
    protected override void OnDestroy() { foreach(var clip in clips.Values) clip.Dispose(); shadow?.Dispose(); base.OnDestroy(); }
}
public sealed class SeedPatch
{
    public readonly Vector2 position;
    public float age,nextTick=.3f;
    public SeedPatch(Vector2 p) { position=p; }
}
