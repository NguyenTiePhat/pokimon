using System.Collections.Generic;
using UnityEngine;

public enum GengarSkill { ShadowBall, SludgeBomb, ShadowPunch, PhantomForce }
public sealed class GengarBoss : ArenaBoss
{
    public override string DisplayName=>"Gengar";
    public override string Status=>!IsAlive?"ĐÃ HẠ GỤC":ActiveSkill<0?"GENGAR":(WindingUp?"CHUẨN BỊ • ":"")+Names[ActiveSkill];
    private static readonly string[] Names={"SHADOW BALL","SLUDGE BOMB","SHADOW PUNCH","PHANTOM FORCE"};
    public static readonly float[] Windups={.53f,.9f,.75f,1.35f}, Cooldowns={2.8f,5.6f,4.8f,13.6f};
    public int ActiveSkill { get; private set; }=-1;
    public bool WindingUp { get; private set; }
    public float PhaseTime { get; private set; }
    public Vector2 Target { get; private set; }
    public bool Hidden=>WindingUp&&ActiveSkill>=2&&PhaseTime>=.25f;
    public float Radius=>ActiveSkill==3?3.2f:ActiveSkill==2?1.25f:1.45f;
    public bool AutoAttack=true;
    public readonly List<GengarPoison> Patches=new List<GengarPoison>();
    public override bool SpinActive=>false;
    public override void ReduceSpin(float power) {}
    private readonly float[] cooldowns=new float[4];
    private readonly Dictionary<string,PmdSpriteAnimation> clips=new Dictionary<string,PmdSpriteAnimation>();
    private SpriteRenderer body;
    private CombatPixelSurface shadow;
    private float clock,recovery=1.3f;
    private int next;
    public override void InitializeBoss(PikachuCombat opponent,Vector2 position)
    {
        Opponent=opponent; transform.position=position; ConfigureHealth(800);
        foreach(string clip in new[]{"Idle","Charge","Shoot","Attack"}) clips.Add(clip,new PmdSpriteAnimation(clip,"GengarBoss"));
        var go=new GameObject("Gengar PMDCollab"); go.transform.SetParent(transform,false);
        body=go.AddComponent<SpriteRenderer>(); body.sortingOrder=10; go.transform.localScale=Vector3.one*2;
        shadow=new CombatPixelSurface("Gengar shadow",transform,80,40,32,3);
        shadow.Ellipse(40,20,24,9,new Color32(14,6,30,135)); shadow.Apply();
        gameObject.AddComponent<GengarVisuals>().Initialize(this); Pose();
    }
    public override bool TakeDamage(float damage)=>!Hidden&&base.TakeDamage(damage);
    public bool StartAttack(GengarSkill skill)
    {
        int index=(int)skill;
        if(index<0||index>=4||!IsAlive||!Opponent||Opponent.Player.IsEntering||Opponent.BattleEnded||ActiveSkill>=0||cooldowns[index]>0)return false;
        CastOrigin=transform.position;
        Vector2 delta=(Vector2)Opponent.Player.transform.position-CastOrigin;
        LockedDirection=delta.sqrMagnitude>.001f?delta.normalized:Vector2.left;
        Target=ArenaProgress.Clamp(Opponent.Player.transform.position,gameObject.scene);
        ActiveSkill=index; WindingUp=true; PhaseTime=0; cooldowns[index]=Cooldowns[index]; return true;
    }
    protected override void Update()=>TickBoss(Time.deltaTime);
    public void TickBoss(float dt)
    {
        if(!Opponent)return;
        dt=Mathf.Max(0,dt); clock+=dt; base.Tick(dt);
        if(Opponent.Player.IsEntering||Opponent.BattleEnded) { ClearBattle(); return; }
        for(int i=0;i<4;i++)cooldowns[i]=Mathf.Max(0,cooldowns[i]-dt);
        for(int i=Patches.Count-1;i>=0;i--)
        {
            var patch=Patches[i]; patch.age+=dt;
            while(patch.nextTick<=3&&patch.age>=patch.nextTick)
            {
                if(Vector2.Distance(Opponent.Player.transform.position,patch.position)<=1.45f)Opponent.TakeDamage(6);
                patch.nextTick+=.6f;
            }
            if(patch.age>=3.2f)Patches.RemoveAt(i);
        }
        SpellClash.Step(Opponent,this,dt);
        if(Opponent.BattleEnded) { ClearBattle(); return; }
        if(ActiveSkill<0)
        {
            recovery-=dt;
            Vector2 delta=(Vector2)Opponent.Player.transform.position-(Vector2)transform.position;
            if(delta.sqrMagnitude>.001f)LockedDirection=delta.normalized;
            if(AutoAttack&&delta.magnitude>4.5f)transform.position=ArenaProgress.Clamp((Vector2)transform.position+LockedDirection*dt*1.2f*(SlowRemaining>0?1-SlowFraction:1),gameObject.scene);
            if(AutoAttack&&recovery<=0)for(int i=0;i<4;i++)if(StartAttack((GengarSkill)((next+i)%4))) { next=(ActiveSkill+1)%4; break; }
            Pose(); return;
        }
        PhaseTime+=dt;
        if(WindingUp&&PhaseTime>=Windups[ActiveSkill])
        {
            PhaseTime-=Windups[ActiveSkill]; WindingUp=false;
            if(ActiveSkill==0)Shots.Add(new BossProjectile(CastOrigin+LockedDirection*.65f,LockedDirection,6.5f,18,.32f,22,true));
            else
            {
                if(ActiveSkill==1)Patches.Add(new GengarPoison(Target));
                else transform.position=Target;
                if(Vector2.Distance(Opponent.Player.transform.position,Target)<=Radius)Opponent.TakeDamage(ActiveSkill==3?55:ActiveSkill==2?28:18);
            }
        }
        if(!WindingUp&&PhaseTime>=.45f) { ActiveSkill=-1; recovery=.68f; }
        if(Opponent.BattleEnded)ClearBattle(); else Pose();
    }
    private void ClearBattle() { Shots.Clear(); Patches.Clear(); ActiveSkill=-1; WindingUp=false; Pose(); }
    private void Pose()
    {
        if(!body)return;
        body.enabled=!Hidden; shadow.Renderer.enabled=!Hidden;
        string clip=ActiveSkill<0?"Idle":WindingUp?"Charge":ActiveSkill>=2?"Attack":"Shoot";
        body.sprite=clips[clip].Sample(PikachuPlayer.DirectionFor(LockedDirection),ActiveSkill<0?clock:WindingUp?PhaseTime*ChargeAnimationRate:PhaseTime);
        body.color=!IsAlive?new Color(.4f,.4f,.5f):SlowRemaining>0?new Color(.6f,.85f,1):Color.white;
    }
    protected override void OnDestroy() { foreach(var clip in clips.Values)clip.Dispose(); shadow?.Dispose(); base.OnDestroy(); }
}
public sealed class GengarPoison
{
    public readonly Vector2 position;
    public float age,nextTick=.6f;
    public GengarPoison(Vector2 p) { position=p; }
}
