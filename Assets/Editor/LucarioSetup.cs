using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class LucarioSetup
{
    private static Camera camera;
    private static PikachuCombat combat;
    private static LucarioBoss boss;
    private static int checks;
    public static void RunAll()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Create();
        Check();
    }
    [MenuItem("Pokemon/Create Lucario Mountain Dojo")]
    public static void Create()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var cam=new GameObject("Mountain dojo camera").AddComponent<Camera>();
            cam.tag="MainCamera";
            cam.transform.position=new Vector3(0,0,-10);
            cam.orthographic=true;
            cam.orthographicSize=3.65f;
            cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color32(91,139,167,255);
            cam.gameObject.AddComponent<AudioListener>();
            var dojo=new GameObject("Mountain dojo • open stone court, banners and distant snow").AddComponent<MountainDojo>();
            new GameObject("Lucario arena controller").AddComponent<ArenaPreview>();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/LucarioArena.unity");
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index=scenes.FindIndex(x=>x.path=="Assets/Scenes/LucarioArena.unity");
            if(index<0) scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/LucarioArena.unity",true));
            else scenes[index].enabled=true;
            EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
            dojo.Build();
            cam.orthographicSize=13.3f;
            cam.aspect=16f/9;
            WaterTempleSetup.Capture(cam,"Lucario-Dojo-Overview");
        }
        finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
    }
    [MenuItem("Pokemon/Check Lucario Boss")]
    public static void Check()
    {
        checks=0;
        bool saving=ArenaProgress.SuppressSaving;
        ArenaProgress.SuppressSaving=true;
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/LucarioArena.unity",OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            foreach(var root in scene.GetRootGameObjects())
            {
                if(root.TryGetComponent<Camera>(out var cam))camera=cam;
                if(root.TryGetComponent<MountainDojo>(out var dojo))dojo.Build();
            }
            camera.transform.position=new Vector3(1,0,-10);
            camera.orthographicSize=4;
            camera.aspect=16f/9;
            Assert(ArenaProgress.IsDojo,"scene routing");
            Assert(Resources.Load<TextAsset>("BossCatalog").text.Contains("\"LucarioArena\""),"menu catalog");
            Assert(Array.Exists(EditorBuildSettings.scenes,x=>x.enabled&&x.path.EndsWith("LucarioArena.unity")),"build scene registered");
            foreach(string clip in new[]{"Idle","Walk","Charge","RearUp","Attack","QuickStrike","Hop","Hurt","Faint"})
                using(var anim=new PmdSpriteAnimation(clip,"LucarioBoss"))
                    for(int dir=0;dir<8;dir++)
                        for(float t=0;t<anim.Duration;t+=.025f)
                            Assert(anim.Sample(dir,t)!=null,"valid animation "+clip+" / "+dir);

            Fresh();
            Assert(boss.Health==850&&combat.Health==200,"initial health");
            Assert(!boss.StartAttack((LucarioSkill)9),"invalid skill rejected");
            combat.Player.ReplayEntrance();
            Assert(!boss.StartAttack(LucarioSkill.AuraSphere),"intro prevents attack");
            Fresh();
            Assert(boss.StartAttack(LucarioSkill.AuraSphere),"start sphere");
            Assert(!boss.StartAttack(LucarioSkill.ExtremeSpeed),"no overlapping attack");
            Advance(LucarioBoss.Windups[0]-.01f);
            Assert(boss.WindingUp&&combat.Health==200&&boss.Shots.Count==0,"sphere anticipation harmless");
            Capture("Aura-Warning");
            Advance(.13f);
            Assert(boss.Shots.Count==1&&boss.AnimationName=="RearUp","release sphere from attack pose");
            Capture("Aura-Sphere");
            Advance(.6f);
            Assert(combat.Health==178,"sphere exactly 22 damage");
            Assert(!boss.StartAttack(LucarioSkill.AuraSphere),"independent cooldown");
            Advance(3);
            Assert(boss.StartAttack(LucarioSkill.AuraSphere),"sphere ready again");
            Fresh(); boss.StartAttack(LucarioSkill.AuraSphere);
            combat.Player.transform.position=new Vector2(3,2);
            Advance(4.8f);
            Assert(combat.Health==200&&boss.Shots.Count==0&&boss.LockedDirection!=Vector2.zero,"sphere dodge and expiry");

            Fresh(new Vector2(1.4f,0)); boss.StartAttack(LucarioSkill.MetalClaw);
            Advance(LucarioBoss.Windups[1]-.01f); Assert(combat.Health==200&&boss.ClawsReleased==0,"claw windup harmless"); Capture("Claw-Warning");
            Advance(.09f); Assert(combat.Health==190&&boss.ClawsReleased==1,"first claw"); Capture("Metal-Claw");
            Advance(.3f); Assert(combat.Health==180&&boss.ClawsReleased==2,"second claw");
            Advance(.3f); Assert(combat.Health==170&&boss.ClawsReleased==3&&combat.StunRemaining>0,"third claw and existing stagger");
            Advance(1); Assert(combat.Health==170,"no repeat claw hits");
            Fresh(new Vector2(1.4f,0)); boss.StartAttack(LucarioSkill.MetalClaw); Advance(LucarioBoss.Windups[1]+.09f);
            combat.Player.transform.position=new Vector2(1.4f,2);
            Advance(1); Assert(combat.Health==190,"escape between claw beats");
            foreach(Vector2 p in new[]{new Vector2(-1,0),new Vector2(1,2),new Vector2(2.16f,0)})
            {
                Fresh(new Vector2(1.4f,0)); boss.StartAttack(LucarioSkill.MetalClaw);
                combat.Player.transform.position=p; Advance(2);
                Assert(combat.Health==200,"claw back/side/outside dodge "+p);
            }
            Fresh(new Vector2(1.4f,0)); boss.StartAttack(LucarioSkill.MetalClaw);
            Assert(boss.InClaw(new Vector2(2.14f,0))&&!boss.InClaw(new Vector2(2.16f,0)),"claw range boundary");
            Assert(boss.InClaw(Quaternion.Euler(0,0,54)*Vector2.right*2)&&!boss.InClaw(Quaternion.Euler(0,0,56)*Vector2.right*2),"claw angle boundary");

            Fresh(); boss.StartAttack(LucarioSkill.ExtremeSpeed);
            Vector2 end=boss.DashEnd;
            Advance(LucarioBoss.Windups[2]-.01f); Assert(combat.Health==200&&(Vector2)boss.transform.position==Vector2.zero,"dash holds during warning"); Capture("Speed-Warning");
            Advance(.17f); Assert(combat.Health==172,"swept dash hit"); Capture("Extreme-Speed");
            Advance(.8f); Assert(combat.Health==172&&(Vector2)boss.transform.position==end,"dash hits once and reaches endpoint");
            foreach(float y in new[]{.66f,1.8f,-1.8f})
            {
                Fresh(); boss.StartAttack(LucarioSkill.ExtremeSpeed);
                combat.Player.transform.position=new Vector2(3,y); Advance(2);
                Assert(combat.Health==200,"dash outside lane safe "+y);
            }
            Fresh(); boss.StartAttack(LucarioSkill.ExtremeSpeed); combat.Player.transform.position=new Vector2(3,.64f); Advance(2);
            Assert(combat.Health==172,"inside dash lane hit");
            Fresh(); boss.StartAttack(LucarioSkill.ExtremeSpeed); Advance(LucarioBoss.Windups[2]-.35f);
            Assert(combat.TryDash(Vector2.up),"player can dash out");
            Advance(1); Assert(combat.Health==200,"real dash counterplay");

            Fresh(); boss.StartAttack(LucarioSkill.MeteorMash);
            Advance(.35f+(LucarioBoss.Windups[3]-.35f)*.5f);
            Assert(boss.Height>.65f&&boss.AnimationName=="Hop"&&combat.Health==200,"meteor leap pose and no early damage");
            Capture("Meteor-Leap");
            combat.Player.transform.position=new Vector2(6,0);
            Advance((LucarioBoss.Windups[3]-.35f)*.5f-.05f);
            Assert(boss.Target==new Vector2(3,0)&&combat.Health==200,"meteor locked target"); Capture("Meteor-Warning");
            Advance(.1f); Assert(combat.Health==200,"outside full meteor circle safe"); Capture("Meteor-Impact");
            Fresh(); boss.StartAttack(LucarioSkill.MeteorMash); Advance(LucarioBoss.Windups[3]-.01f);
            Assert(combat.Health==200,"meteor full anticipation");
            Advance(.04f); Assert(combat.Health==152&&boss.Height==0,"meteor impact 48");
            Advance(.6f); Assert(combat.Health==152&&boss.RecoveryRemaining>1,"shockwave no duplicate damage and punish window");
            foreach(float radius in new[]{2.64f,2.66f})
            {
                Fresh(); boss.StartAttack(LucarioSkill.MeteorMash);
                combat.Player.transform.position=new Vector2(3,radius); Advance(2);
                Assert(combat.Health==(radius<2.65f?152:200),"meteor exact advertised boundary "+radius);
            }
            Fresh(); boss.StartAttack(LucarioSkill.MeteorMash);
            for(int i=0;i<100;i++) { combat.Player.TickMovement(Vector2.up,.02f); boss.TickBoss(.02f); combat.Tick(.02f); }
            Assert(combat.Health==200,"ordinary walking escapes meteor");

            // Verify eight directions and edge/corner clamping never bend the locked lane.
            for(int dir=0;dir<8;dir++)
            {
                Vector2 aim=Quaternion.Euler(0,0,dir*45)*Vector2.right;
                Fresh(aim*3); boss.StartAttack(LucarioSkill.ExtremeSpeed); Advance(2);
                Assert(combat.Health==172,"dash all directions "+dir);
                Fresh(aim*3); boss.StartAttack(LucarioSkill.ExtremeSpeed);
                combat.Player.transform.position=aim*3+new Vector2(-aim.y,aim.x);
                Advance(2); Assert(combat.Health==200,"dash side dodge all directions "+dir);
            }
            Fresh(new Vector2(8.3f,6.3f));
            boss.transform.position=new Vector2(7,5); boss.StartAttack(LucarioSkill.ExtremeSpeed);
            end=boss.DashEnd; Assert(end.x<=8.4f&&end.y<=6.4f,"dash endpoint clipped at corner");
            Advance(2); Assert((Vector2)boss.transform.position==end,"corner dash endpoint exact");
            combat.Player.CombatMove(new Vector2(100,-100),Vector2.one,0);
            Assert((Vector2)combat.Player.transform.position==new Vector2(8.4f,-6.4f),"player court bounds");

            // Large dt cannot skip claw strikes, dash contacts or the meteor landing.
            for(int skill=0;skill<4;skill++)
            {
                Vector2 p=skill==1?new Vector2(1.4f,0):new Vector2(3,0);
                Fresh(p); boss.StartAttack((LucarioSkill)skill); Advance(2.6f);
                float hp=combat.Health; Vector2 position=boss.transform.position;
                Fresh(p); boss.StartAttack((LucarioSkill)skill); boss.TickBoss(2.6f);
                Assert(combat.Health==hp&&Vector2.Distance(boss.transform.position,position)<.001f,"long frame consistency "+skill);
            }
            Fresh(); boss.StartAttack(LucarioSkill.AuraSphere); Advance(LucarioBoss.Windups[0]+.12f);
            combat.Projectiles.Add(new ElectricProjectile(new Vector2(3,0),new Vector2(0,0),0,22,10,true));
            Advance(.3f);
            Assert(boss.Shots.Count==0&&combat.Projectiles.Count==0&&combat.Health==200&&boss.Health==850,"equal Aura Sphere clash");
            Fresh(); boss.StartAttack(LucarioSkill.AuraSphere); Advance(LucarioBoss.Windups[0]+.12f);
            combat.Projectiles.Add(new ElectricProjectile(new Vector2(3,0),new Vector2(0,0),0,5,10,true));
            Advance(.6f); Assert(combat.Health==183,"weaker electric attack reduces aura damage");
            Fresh(new Vector2(2,0));
            Assert(combat.TryCast(PikachuSkill.Thunderbolt,Vector2.zero),"E can target Lucario"); Advance(.4f);
            Assert(boss.Health==830,"E damages Lucario");
            Fresh(new Vector2(1.4f,0));
            Assert(combat.TryCast(PikachuSkill.IronTail,Vector2.zero),"C can target Lucario"); Advance(.4f);
            Assert(boss.Health==825,"C damages Lucario");

            Fresh(); boss.StartAttack(LucarioSkill.ExtremeSpeed); boss.TakeDamage(430); Advance(LucarioBoss.Windups[2]-.25f);
            Assert(!boss.Awakened&&boss.WindingUp,"health threshold cannot interrupt current warning");
            Advance(.6f); Assert(boss.Awakened&&boss.AwakeningRemaining>0,"phase transition after action");
            Assert(!boss.StartAttack(LucarioSkill.MeteorMash),"phase announcement prevents cast");
            Capture("Aura-Awakening");
            Advance(2); Assert(boss.StartAttack(LucarioSkill.MeteorMash),"phase two cast");
            Advance(LucarioBoss.Windups[3]-.01f); Assert(boss.WindingUp,"phase two keeps full warning");

            for(int skill=0;skill<4;skill++)
            {
                Fresh(); boss.StartAttack((LucarioSkill)skill); Advance(.4f);
                boss.TakeDamage(10000); boss.TickBoss(.02f);
                Assert(combat.BattleEnded&&boss.ActiveSkill==-1&&boss.Shots.Count==0&&boss.Height==0,"victory cancels "+skill);
                Assert(!boss.StartAttack((LucarioSkill)skill),"dead boss cannot cast");
                Fresh(); boss.StartAttack((LucarioSkill)skill); Advance(.4f);
                combat.TakeDamage(1000); Advance(.1f);
                Assert(combat.BattleEnded&&boss.ActiveSkill==-1&&boss.Shots.Count==0&&boss.Height==0,"defeat cancels "+skill);
            }
            Fresh(); boss.StartAttack(LucarioSkill.MeteorMash); Advance(.9f);
            combat.Player.ReplayEntrance(); boss.TickBoss(.1f);
            Assert(boss.ActiveSkill==-1&&boss.Height==0&&boss.Shots.Count==0,"entrance replay cancels attack");
            Fresh(); boss.AutoAttack=true;
            var skills=new HashSet<int>();
            for(int i=0;i<3000&&!combat.BattleEnded;i++)
            {
                // Exercise the rotation in melee range, then position outside the locked hazards.
                // Actual movement counterplay is tested separately; this bot isolates AI scheduling.
                Vector2 center=(Vector2)boss.transform.position;
                Vector2 inward=center.sqrMagnitude>.01f?-center.normalized:Vector2.up;
                Vector2 point=ArenaProgress.Clamp(center+inward*2,scene);
                if(boss.ActiveSkill>=0)
                {
                    float best=float.MaxValue;
                    for(int x=-7;x<=7;x++)for(int y=-5;y<=5;y++)
                    {
                        Vector2 candidate=new Vector2(x,y);
                        if(boss.ActiveSkill==1&&boss.InClaw(candidate))continue;
                        if(boss.ActiveSkill==2&&CharizardBoss.DistanceToSegment(candidate,boss.CastOrigin,boss.DashEnd)<=LucarioBoss.LaneRadius+.2f)continue;
                        if(boss.ActiveSkill==3&&Vector2.Distance(candidate,boss.Target)<=LucarioBoss.MeteorRadius+.2f)continue;
                        bool projectile=false;
                        foreach(var shot in boss.Shots)
                            if(CharizardBoss.DistanceToSegment(candidate,shot.position,shot.position+shot.direction*shot.remaining)<=shot.radius+.2f)projectile=true;
                        if(projectile)continue;
                        float distance=Vector2.SqrMagnitude(candidate-(Vector2)combat.Player.transform.position);
                        if(distance<best) {best=distance;point=candidate;}
                    }
                }
                combat.Player.transform.position=ArenaProgress.Clamp(point,scene);
                boss.TickBoss(.02f); combat.Tick(.02f);
                if(boss.ActiveSkill>=0)skills.Add(boss.ActiveSkill);
            }
            Assert(skills.Count==4&&boss.AttackCount>=10,"sustained AI uses all four attacks: skills="+skills.Count+", casts="+boss.AttackCount+", HP="+combat.Health);
            Assert(!combat.BattleEnded,"active dodging sustains 60s combat");
            Debug.Log("LUCARIO_CHECK_OK: "+checks+" assertions; animation frames, warnings, locks, all directions, hit/dodge boundaries, real dash/walk, clashes, phase two, timestep consistency, lifecycle and 60s AI.");
        }
        finally { ArenaProgress.SuppressSaving=saving; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
    }
    private static void Fresh(Vector2? position=null)
    {
        if(combat)Object.DestroyImmediate(combat.gameObject);
        if(boss)Object.DestroyImmediate(boss.gameObject);
        var player=new GameObject("Test Pikachu").AddComponent<PikachuPlayer>();
        player.Initialize(camera);
        player.SampleEntrance(PikachuPlayer.EntranceDuration);
        player.transform.position=position??new Vector2(3,0);
        boss=new GameObject("Test Lucario").AddComponent<LucarioBoss>();
        combat=player.gameObject.AddComponent<PikachuCombat>(); combat.Initialize(player,boss);
        boss.InitializeBoss(combat,Vector2.zero); boss.AutoAttack=false;
    }
    private static void Advance(float duration)
    {
        while(duration>.000001f) { float dt=Mathf.Min(.02f,duration); combat.Tick(dt); boss.TickBoss(dt); duration-=dt; }
    }
    private static void Assert(bool value,string message) { checks++; if(!value)throw new Exception("Lucario check: "+message); }
    private static void Capture(string name)
    {
        combat.Player.CombatPose("Idle",0,((Vector2)boss.transform.position-(Vector2)combat.Player.transform.position).normalized);
        boss.GetComponent<LucarioVisuals>().Render(boss.PhaseTime);
        WaterTempleSetup.Capture(camera,"Lucario-"+name);
    }
}
