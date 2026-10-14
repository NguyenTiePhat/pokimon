using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SpellClashCheck
{
    private static Scene scene;
    private static Camera camera;
    private static PikachuCombat combat;
    private static CharizardBoss boss;
    private static int checks;
    public static void RunRegressionAndPlayMode()
    {
        PikachuCombatCheck.Run();
        CharizardBossCheck.Run();
        PikachuHitCheck.Run();
        CharizardPlayModeCheck.Run();
    }
    public static void Run()
    {
        scene=EditorSceneManager.OpenScene("Assets/Scenes/CharizardArena.unity",OpenSceneMode.Additive);
        try
        {
            foreach(var root in scene.GetRootGameObjects()) if(root.TryGetComponent<Camera>(out var cam)) camera=cam;
            Fresh(); Ball(5); Shot(5); SpellClash.Step(combat,boss,.4f);
            Check(combat.Projectiles.Count==0 && boss.Shots.Count==0,"equal shots must cancel");
            Check(combat.Health==200 && boss.Health==500 && combat.Craters.Count==0,"clash must not hurt characters or crater");
            Check(combat.Impacts.Count==1 && combat.Impacts[0].clash,"one clash explosion"); Render();
            Fresh(); Ball(100,false); Shot(12); SpellClash.Step(combat,boss,.4f);
            Check(combat.Projectiles.Count==1 && Near(combat.Projectiles[0].damage,88) && boss.Shots.Count==0,"strong ball must retain 88");
            SpellClash.Step(combat,boss,1);
            Check(Near(boss.Health,412) && combat.Craters.Count==1,"survivor must deal reduced damage and crater only on landing");
            Fresh(); Ball(5); Shot(12); SpellClash.Step(combat,boss,1);
            Check(Near(combat.Health,193) && combat.Projectiles.Count==0 && boss.Shots.Count==0,"enemy survivor must deal only 7");
            Fresh(); Ball(20); Shot(5); Shot(5,3); SpellClash.Step(combat,boss,.4f);
            Check(combat.Projectiles.Count==1 && Near(combat.Projectiles[0].damage,10) && combat.Impacts.Count==2,"multiple sequential contacts");
            Fresh(); Ball(5); Shot(5); SpellClash.Step(combat,boss,1);
            float largeHP=combat.Health; int largeCount=combat.Impacts.Count;
            Fresh(); Ball(5); Shot(5); for(int i=0;i<100;i++) SpellClash.Step(combat,boss,.01f);
            Check(combat.Health==largeHP && combat.Impacts.Count==largeCount && combat.Projectiles.Count==0,"frame subdivision changed collision outcome");
            Fresh(); Ball(5); boss.Shots.Add(new BossProjectile(new Vector2(2,2),Vector2.left,10,10,.3f,5,false)); SpellClash.Step(combat,boss,.4f);
            Check(combat.Impacts.Count==0 && combat.Projectiles.Count==1 && boss.Shots.Count==1,"separate lanes must not clash");
            Fresh(); Ball(5); Ball(5); SpellClash.Step(combat,boss,.1f);
            Check(combat.Projectiles.Count==2 && combat.Impacts.Count==0,"same side projectiles collided");
            Fresh(); Ball(20); boss.Shots.Add(new BossProjectile(new Vector2(5,0),Vector2.right,1,10,.3f,5,false)); SpellClash.Step(combat,boss,1);
            Check(Near(boss.Health,480) && boss.Shots.Count==1 && combat.Impacts.TrueForAll(x=>!x.clash),"target hit must occur before a later clash");

            Fresh(); ActivateSpin(); Ball(5); SpellClash.Step(combat,boss,.05f);
            Check(combat.Projectiles.Count==0 && Near(boss.SpinPower,43) && boss.SpinActive,"weak shot must weaken but not erase stream");
            Fresh(); ActivateSpin(); Ball(100,false); SpellClash.Step(combat,boss,.05f);
            Check(!boss.SpinActive && combat.Projectiles.Count==1 && Near(combat.Projectiles[0].damage,52),"full orb must overpower 48-strength stream");
            Check(combat.Craters.Count==0,"stream clash made a crater");
            Fresh(); boss.StartAttack(CharizardSkill.FireSpin); Ball(5); SpellClash.Step(combat,boss,.05f);
            Check(combat.Projectiles.Count==1 && combat.Impacts.Count==0,"warning must not collide");

            Fresh(); Shot(5,1); Shot(5,-1);
            float power=SpellClash.TraceThunderbolt(combat,boss,new Vector2(-4,0),new Vector2(4,0),20,out var end);
            Check(Near(power,10) && boss.Shots.Count==0 && end==new Vector2(4,0),"beam should pierce two weak shots with 10 power");
            Fresh(); ActivateSpin(); power=SpellClash.TraceThunderbolt(combat,boss,new Vector2(-4,0),new Vector2(4,0),20,out end);
            Check(power==0 && Near(boss.SpinPower,28) && end.x<0,"strong stream must intercept Thunderbolt before target");
            Fresh(); ActivateSpin(); combat.Player.transform.position=new Vector2(0,0); combat.Player.CombatPose("Idle",0,Vector2.right);
            Check(combat.TryCast(PikachuSkill.Thunderbolt,boss.transform.position),"E cast for live interception"); combat.Tick(.36f);
            Check(boss.Health==500 && Near(boss.SpinPower,28) && combat.BeamEnd.x<4,"E must use clash result instead of full damage");
            Fresh(); Ball(5); Shot(5); combat.CancelForEntrance();
            Check(combat.Projectiles.Count==0 && combat.Impacts.Count==0,"reset left projectiles/effects");
            Debug.Log("SPELL_CLASH_OK: "+checks+" assertions; equal/strong/weak, fast crossing, timestep independence, contact ordering, stream and Thunderbolt, harmless explosion, crater and reset rules.");
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }
    private static bool Near(float a,float b)=>Mathf.Abs(a-b)<.001f;
    private static void Check(bool ok,string why) { checks++; if(!ok) throw new System.Exception("Clash regression: "+why); }
    private static void Fresh()
    {
        if(combat) Object.DestroyImmediate(combat.gameObject); if(boss) Object.DestroyImmediate(boss.gameObject);
        var p=new GameObject("Clash Pikachu"); SceneManager.MoveGameObjectToScene(p,scene);
        var player=p.AddComponent<PikachuPlayer>(); player.Initialize(camera); player.SampleEntrance(PikachuPlayer.EntranceDuration);
        player.transform.position=new Vector2(-4,0); player.CombatPose("Idle",0,Vector2.right);
        var b=new GameObject("Clash Charizard"); SceneManager.MoveGameObjectToScene(b,scene);
        boss=b.AddComponent<CharizardBoss>(); combat=p.AddComponent<PikachuCombat>(); combat.Initialize(player,boss);
        boss.InitializeBoss(combat,new Vector2(4,0)); boss.AutoAttack=false;
    }
    private static void Ball(float power,bool basic=true)
    { combat.Projectiles.Add(new ElectricProjectile(new Vector2(-2,0),new Vector2(6,0),basic?0:1,power,10,basic)); }
    private static void Shot(float power,float x=2)
    { boss.Shots.Add(new BossProjectile(new Vector2(x,0),Vector2.left,10,10,.3f,power,false)); }
    private static void ActivateSpin() { boss.StartAttack(CharizardSkill.FireSpin); boss.TickBoss(1.2f); }
    private static void Render()
    {
        combat.Impacts[0].age=.14f; combat.GetComponent<CombatVisuals>().RenderFrame(5);
        camera.transform.position=new Vector3(0,0,-10); camera.orthographicSize=3;
        var rt=new RenderTexture(1280,720,24); var old=RenderTexture.active; var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply(); File.WriteAllBytes("Docs/Previews/Spell-Clash.png",image.EncodeToPNG()); }
        finally { camera.targetTexture=null; RenderTexture.active=old; Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
