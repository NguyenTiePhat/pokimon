using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CharizardBossCheck
{
    private static PikachuPlayer player;
    private static PikachuCombat combat;
    private static CharizardBoss boss;
    private static Camera camera;
    private static Scene scene;
    private static int assertions;
    public static void Run()
    {
        scene=EditorSceneManager.OpenScene("Assets/Scenes/CharizardArena.unity",OpenSceneMode.Additive);
        try
        {
            foreach(var root in scene.GetRootGameObjects()) if(root.TryGetComponent<Camera>(out var c)) camera=c;
            Fresh();
            Require(boss.Health==500 && combat.Health==200,"health defaults");
            player.ReplayEntrance();
            Require(!boss.StartAttack(CharizardSkill.Flamethrower),"boss attacked during intro");
            combat.TakeDamage(100); Require(combat.Health==200,"intro damage");
            player.SampleEntrance(PikachuPlayer.EntranceDuration); player.transform.position=new Vector2(3,0);
            Require(boss.StartAttack(CharizardSkill.Flamethrower) && boss.Cooldown(CharizardSkill.Flamethrower)==1,"flame cooldown");
            Advance(.39f); Require(combat.Health==200 && boss.Shots.Count==0,"flame must warn before damage");
            Advance(.18f); Render("Flamethrower"); Advance(.8f);
            Require(combat.Health==185,"three flames must deal 5 each");
            Require(boss.Cooldown(CharizardSkill.Flamethrower)==0,"flame cooldown must expire in 1 second");

            Fresh(); boss.StartAttack(CharizardSkill.DragonClaw); Advance(.4f);
            Require(boss.Height>0 && combat.Health==200,"claw lift and warning"); Render("DragonClaw-Warning");
            Advance(.62f); Render("DragonClaw"); Advance(.5f);
            Require(combat.Health==175 && boss.Height==0,"claw one hit and landing");
            Fresh(); boss.StartAttack(CharizardSkill.DragonClaw); player.transform.position=new Vector2(3,3); Advance(1.5f);
            Require(combat.Health==200 && Mathf.Abs(boss.transform.position.y)<.001f,"claw must lock direction and be dodgeable");

            Fresh(); boss.StartAttack(CharizardSkill.AirSlash); Advance(.7f);
            Require(boss.Shots.Count==3,"air slash three arcs"); Render("AirSlash"); Advance(.9f);
            Require(combat.Health==188,"one central air blade should hit for 12");

            Fresh(); boss.StartAttack(CharizardSkill.FireSpin); Advance(1.05f);
            Require(combat.Health==200,"spin warning must not damage"); Render("FireSpin-Warning");
            Advance(.8f); Render("FireSpin"); Advance(2.4f);
            Require(combat.Health==152,"spin six ticks of 8 damage");
            Fresh(); boss.StartAttack(CharizardSkill.FireSpin); player.transform.position=new Vector2(3,3); Advance(4.2f);
            Require(combat.Health==200,"spin must be dodgeable outside locked cone");

            Fresh();
            Require(combat.TryCast(PikachuSkill.Thunderbolt,boss.transform.position),"Pikachu E targets boss");
            combat.Tick(.36f); Require(boss.Health==480,"boss receives E damage");
            combat.Tick(2); player.transform.position=new Vector2(1,0);
            Require(combat.TryCast(PikachuSkill.IronTail,Vector2.right*100),"C auto targets boss");
            combat.Tick(.36f); Require(boss.Health==455 && boss.SlowRemaining>0,"boss takes tail damage and slow");
            combat.Tick(2); Require(combat.TryBasicAttack((Vector2)boss.transform.position+Vector2.up*.45f),"basic shot targets boss");
            combat.Tick(1); Require(boss.Health==450,"boss takes basic projectile damage");
            boss.TakeDamage(9999); Require(combat.BattleEnded && !combat.TryBasicAttack(Vector2.zero),"victory must stop combat");
            boss.TickBoss(.1f); Require(boss.Shots.Count==0,"victory clears enemy shots");
            Fresh(); combat.TakeDamage(200);
            Require(combat.BattleEnded && !boss.StartAttack(CharizardSkill.FireSpin),"defeat must stop boss");
            var old=player.transform.position; player.TickMovement(Vector2.right,1); Require(player.transform.position==old,"defeat locks movement");
            Fresh(); boss.AutoAttack=true; player.transform.position=new Vector2(0,6);
            bool[] seen=new bool[4];
            for(int i=0;i<2400 && !combat.BattleEnded;i++) { boss.TickBoss(.02f); if(boss.ActiveSkill>=0) seen[boss.ActiveSkill]=true; }
            for(int i=0;i<4;i++) Require(seen[i],"AI never selected skill "+i);
            Debug.Log("CHARIZARD_BOSS_OK: "+assertions+" assertions; telegraphs, four attacks, lock/dodge, health, Pikachu hits/slow, victory/defeat, AI rotation; six rendered previews.");
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }
    private static void Fresh()
    {
        if(player) Object.DestroyImmediate(player.gameObject);
        if(boss) Object.DestroyImmediate(boss.gameObject);
        var p=new GameObject("Boss check Pikachu"); SceneManager.MoveGameObjectToScene(p,scene);
        player=p.AddComponent<PikachuPlayer>(); player.Initialize(camera); player.SampleEntrance(PikachuPlayer.EntranceDuration); player.transform.position=new Vector2(3,0);
        player.CombatPose("Idle",0,Vector2.left);
        var b=new GameObject("Boss check Charizard"); SceneManager.MoveGameObjectToScene(b,scene);
        boss=b.AddComponent<CharizardBoss>(); combat=p.AddComponent<PikachuCombat>(); combat.Initialize(player,boss);
        boss.InitializeBoss(combat,Vector2.zero); boss.AutoAttack=false;
        camera.transform.position=new Vector3(2,0,-10); camera.orthographicSize=4;
    }
    private static void Advance(float seconds)
    { while(seconds>.00001f) { float step=Mathf.Min(.02f,seconds); boss.TickBoss(step); seconds-=step; } }
    private static void Require(bool yes,string message)
    { assertions++; if(!yes) throw new System.Exception("Boss regression: "+message); }
    private static void Render(string name)
    {
        player.CombatPose("Idle",0,Vector2.left);
        boss.GetComponent<CharizardVisuals>().Render(assertions+boss.PhaseTime);
        var rt=new RenderTexture(1280,720,24); var old=RenderTexture.active;
        var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            shot.ReadPixels(new Rect(0,0,1280,720),0,0); shot.Apply();
            Directory.CreateDirectory("Docs/Previews"); File.WriteAllBytes("Docs/Previews/Charizard-"+name+".png",shot.EncodeToPNG());
        }
        finally { camera.targetTexture=null; RenderTexture.active=old; Object.DestroyImmediate(shot); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
