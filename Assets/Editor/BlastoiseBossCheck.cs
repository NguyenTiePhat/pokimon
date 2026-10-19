using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BlastoiseBossCheck
{
    private static Scene scene;
    private static Camera camera;
    private static PikachuCombat combat;
    private static BlastoiseBoss boss;
    private static int count;
    public static void Run()
    {
        scene=EditorSceneManager.OpenScene("Assets/Scenes/BlastoiseArena.unity",OpenSceneMode.Additive);
        try
        {
            foreach(var root in scene.GetRootGameObjects()) if(root.TryGetComponent<Camera>(out var cam)) camera=cam;
            Fresh(); Check(boss.Health==650 && combat.Health==200,"starting health");
            combat.Player.ReplayEntrance(); Check(!boss.StartAttack(BlastoiseSkill.HydroPump),"intro allowed attack");
            Fresh(); boss.StartAttack(BlastoiseSkill.HydroPump); Advance(.4f); Check(combat.Health==200,"pump damaged during warning"); Render("Pump-Warning");
            Advance(.1f); Check(combat.Health==180,"pump damage"); Render("HydroPump"); Advance(.8f); Check(combat.Health==180,"pump repeated damage");
            Fresh(); boss.StartAttack(BlastoiseSkill.HydroPump); combat.Player.transform.position=new Vector2(3,3); Advance(.45f);
            Check(Vector2.Angle(boss.LockedDirection,Vector2.right)<.1f,"pump warning must hold initial direction");
            Advance(.2f); Check(combat.Health==200 && Vector2.Angle(boss.LockedDirection,Vector2.right)<=8.1f,"pump turn must be limited and initially dodgeable");
            Advance(1.2f); Check(combat.Health==180 && boss.SpinActive,"pump must track and reach stationary target"); Render("HydroPump-Tracking");
            Advance(1.2f); Check(combat.Health==180 && !boss.SpinActive,"pump must end without repeated damage");
            Fresh(); boss.StartAttack(BlastoiseSkill.HydroPump); Advance(.46f);
            for(int side=-1;side<=1;side+=2)
                Check(boss.StreamContact(Vector2.Lerp(boss.CannonSocket(side),boss.StreamJoin,.4f),Vector2.zero,0,0,out _),"cannon branch missing collision");
            Check(!boss.StreamContact(new Vector2(.1f,1.2f),Vector2.zero,0,0,out _),"space outside branches must be safe");
            Fresh(); boss.StartAttack(BlastoiseSkill.WaterPulse); Advance(.8f); Render("WaterPulse"); Advance(1.5f); Check(combat.Health==176,"pulse direct and wave damage must total 24");
            Fresh(); boss.StartAttack(BlastoiseSkill.WaterPulse); Vector2 target=boss.LockedPoint; combat.Player.transform.position=new Vector2(3,3); Advance(1.25f); Render("Pulse-Wave"); Advance(1);
            Check(combat.Health==200 && boss.LockedPoint==target && boss.Shots.Count==0 && boss.Waves.Count==0,"pulse must miss, land and expire");
            Fresh(); boss.StartAttack(BlastoiseSkill.RapidSpin); Advance(.4f); Check(combat.Health==200 && boss.transform.position==Vector3.zero,"spin warning");
            Advance(.5f); Render("RapidSpin"); Advance(.7f); Check(combat.Health==175 && Mathf.Abs(boss.transform.position.x-4)<.01f,"spin must move 4 and hit once");
            Fresh(); boss.StartAttack(BlastoiseSkill.RapidSpin); combat.Player.transform.position=new Vector2(3,2); Advance(2); Check(combat.Health==200,"spin must allow sideways dodge");
            Fresh(); boss.StartAttack(BlastoiseSkill.HydroCannon); Advance(1.05f); Check(combat.Health==200 && boss.WindingUp,"cannon shortened warning"); Render("Cannon-Warning");
            Advance(.1f); Check(combat.Health==140,"cannon damage"); Render("HydroCannon"); Advance(1); Check(combat.Health==140,"cannon repeated damage");
            Fresh(); boss.StartAttack(BlastoiseSkill.HydroCannon); combat.Player.transform.position=new Vector2(-3,2); Advance(3); Check(combat.Health==200,"cannon turn limit leaves safe side");
            Fresh(); boss.StartAttack(BlastoiseSkill.HydroCannon); combat.Player.transform.position=new Vector2(3,1); Advance(1.11f);
            Check(combat.Health==200 && boss.LaneHalfWidth==.65f,"narrow cannon must leave old lane edge safe");
            Check(!boss.StreamContact(Vector2.zero,Vector2.zero,0,0,out _),"cannon must not originate behind mouth");
            Check(boss.StreamContact(boss.CannonPoint(.4f),Vector2.zero,0,0,out _),"curved cannon core must collide");
            Advance(.3f);
            Check(Vector2.Angle(Vector2.right,boss.LockedDirection)>5 && Vector2.Angle(Vector2.right,boss.LockedDirection)<21,"cannon must track at limited speed");
            Vector2 tipDelta=boss.CannonPoint(1)-boss.MouthOrigin;
            Check(Vector2.Angle(tipDelta,boss.LockedDirection)>1,"turning cannon must bend behind mouth heading");
            Check(boss.StreamContact(boss.CannonPoint(.6f),Vector2.zero,0,0,out _),"turning curve must retain collision"); Render("HydroCannon-Tracking");
            Advance(.55f); Check(!boss.SpinActive,"tracking cannon must expire");

            Fresh(); boss.StartAttack(BlastoiseSkill.HydroPump); Advance(.43f);
            combat.Projectiles.Add(new ElectricProjectile(new Vector2(2,0),new Vector2(2,0),1,100,7));
            Advance(.04f); Check(!boss.SpinActive && combat.Projectiles.Count==1 && Mathf.Abs(combat.Projectiles[0].damage-80)<.01f && combat.Health==200,"orb must block pump before player damage");
            Fresh(); boss.StartAttack(BlastoiseSkill.HydroCannon); combat.Player.transform.position=new Vector2(3,2); Advance(1.11f);
            float power=SpellClash.TraceThunderbolt(combat,boss,new Vector2(3,0),Vector2.zero,20,out var endpoint);
            Check(power==0 && Mathf.Abs(boss.SpinPower-40)<.01f,"cannon and Thunderbolt clash");
            Fresh(); boss.StartAttack(BlastoiseSkill.WaterPulse); Advance(.7f);
            var shot=boss.Shots[0]; combat.Projectiles.Add(new ElectricProjectile(shot.position,shot.position,0,24,1,true)); SpellClash.Step(combat,boss,.01f);
            Check(boss.Shots.Count==0 && boss.Waves.Count==0,"canceled pulse must not create damaging wave");

            Fresh(); Check(combat.TryCast(PikachuSkill.Thunderbolt,boss.transform.position),"E selects water boss"); Advance(.4f); Check(boss.Health==630,"E damages water boss");
            combat.Tick(2); combat.Player.transform.position=new Vector2(1,0); Check(combat.TryCast(PikachuSkill.IronTail,Vector2.right*100),"C auto selects water boss"); Advance(.4f);
            Check(boss.Health==605 && boss.SlowRemaining>0,"C damage and slow");
            boss.TakeDamage(1000); Check(combat.BattleEnded && !combat.TryBasicAttack(Vector2.zero),"water boss victory");
            Fresh(); combat.TakeDamage(200); boss.TickBoss(.1f); Check(combat.BattleEnded && boss.Shots.Count==0 && boss.Waves.Count==0,"defeat cleanup");
            Fresh(); combat.Player.transform.position=new Vector2(8.3f,0); combat.Player.TickMovement(Vector2.right,1);
            Check(((Vector2)combat.Player.transform.position).magnitude<=8.401f,"walking escaped circular arena");
            combat.Player.CombatMove(new Vector2(15,15),Vector2.one,0);
            Check(((Vector2)combat.Player.transform.position).magnitude<=8.401f,"dash escaped circular arena");
            Fresh(); boss.AutoAttack=true; combat.Player.transform.position=new Vector2(0,7);
            var seen=new bool[4]; for(int i=0;i<2000 && !combat.BattleEnded;i++) { Advance(.02f); if(boss.ActiveSkill>=0) seen[boss.ActiveSkill]=true; }
            for(int i=0;i<4;i++) Check(seen[i],"AI missed skill "+i);
            Debug.Log("BLASTOISE_BOSS_OK: "+count+" assertions; locks, warnings, four attacks/dodges, waves, clashes, Pikachu targeting, win/lose, circular bounds, AI; eight previews.");
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }
    private static void Fresh()
    {
        if(combat) Object.DestroyImmediate(combat.gameObject); if(boss) Object.DestroyImmediate(boss.gameObject);
        var p=new GameObject("Water check Pikachu"); SceneManager.MoveGameObjectToScene(p,scene);
        var player=p.AddComponent<PikachuPlayer>(); player.Initialize(camera); player.SampleEntrance(PikachuPlayer.EntranceDuration); player.transform.position=new Vector2(3,0); player.CombatPose("Idle",0,Vector2.left);
        var b=new GameObject("Water check Blastoise"); SceneManager.MoveGameObjectToScene(b,scene); boss=b.AddComponent<BlastoiseBoss>();
        combat=p.AddComponent<PikachuCombat>(); combat.Initialize(player,boss); boss.InitializeBoss(combat,Vector2.zero); boss.AutoAttack=false;
        camera.transform.position=new Vector3(2,0,-10); camera.orthographicSize=4;
    }
    private static void Advance(float seconds) { while(seconds>.00001f) { float dt=Mathf.Min(.02f,seconds); boss.TickBoss(dt); combat.Tick(dt); seconds-=dt; } }
    private static void Check(bool ok,string message) { count++; if(!ok) throw new System.Exception("Blastoise regression: "+message); }
    private static void Render(string name)
    {
        combat.Player.CombatPose(combat.StunRemaining>0?"Hurt":"Idle",0,Vector2.left);
        boss.GetComponent<BlastoiseVisuals>().Render(count+boss.PhaseTime); combat.GetComponent<CombatVisuals>().RenderFrame(count+boss.PhaseTime);
        WaterTempleSetup.Capture(camera,"Blastoise-"+name);
    }
}
