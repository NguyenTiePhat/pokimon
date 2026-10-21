using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VenusaurSetup
{
    private const string Request="Temp/VenusaurSetup.request";
    static VenusaurSetup(){EditorApplication.update+=Poll;EditorApplication.update+=PlayPoll;}
    public static void RunAll(){EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Single);Create();Check();}
    public static void RunPlay()
    {
        SessionState.SetBool("Venusaur.PlayCheck",true);SessionState.SetInt("Venusaur.PlayStage",0);
        SessionState.SetFloat("Venusaur.PlayDeadline",(float)EditorApplication.timeSinceStartup+100);
        ArenaPreview.TrainingMode=false;
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;
    }
    private static void PlayPoll()
    {
        if(!SessionState.GetBool("Venusaur.PlayCheck",false))return;
        try
        {
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat("Venusaur.PlayDeadline",0))throw new Exception("Venusaur play timeout");
            if(!EditorApplication.isPlaying)return;
            int stage=SessionState.GetInt("Venusaur.PlayStage",0);
            if(stage==0)
            {
                if(!UnityEngine.Object.FindAnyObjectByType<MainMenu>())return;
                ArenaProgress.Enter("VenusaurArena");SessionState.SetInt("Venusaur.PlayStage",1);return;
            }
            var player=UnityEngine.Object.FindAnyObjectByType<PikachuCombat>();
            var enemy=UnityEngine.Object.FindAnyObjectByType<VenusaurBoss>();
            if(!player||!enemy||player.Player.IsEntering)return;
            enemy.AutoAttack=false;
            if(stage==1)
            {
                if(enemy.Health!=750||player.Health!=200||!UnityEngine.Object.FindAnyObjectByType<AncientGarden>())throw new Exception("Garden play spawn failed");
                if(!enemy.StartAttack(VenusaurSkill.SolarBeam))throw new Exception("Garden play cast failed");
                SessionState.SetInt("Venusaur.PlayStage",2);
            }
            else if(stage==2&&enemy.WindingUp&&enemy.PhaseTime>.7f)
            {
                ScreenCapture.CaptureScreenshot("Docs/Previews/Venusaur-PlayMode-HUD.png");
                SessionState.SetInt("Venusaur.PlayStage",3);
            }
            else if(stage==3&&enemy.SpinActive)
            {
                enemy.TakeDamage(1000);if(!player.BattleEnded)throw new Exception("Garden victory failed");
                ArenaProgress.Enter("VenusaurArena");SessionState.SetInt("Venusaur.PlayStage",4);
            }
            else if(stage==4)
            {
                if(enemy.Health!=750||player.Health!=200||enemy.Patches.Count!=0)throw new Exception("Garden retry failed");
                Debug.Log("VENUSAUR_PLAYMODE_OK: menu entry, scene spawn, intro, cast, victory and retry");
                SessionState.SetBool("Venusaur.PlayCheck",false);EditorApplication.Exit(0);
            }
        }
        catch(Exception e){Debug.LogException(e);SessionState.SetBool("Venusaur.PlayCheck",false);EditorApplication.Exit(1);}
    }
    private static void Poll()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(Request);
        try { Create(); Check(); File.WriteAllText("Docs/Previews/VenusaurSetup.result","VENUSAUR_OK"); }
        catch(Exception e){ Debug.LogException(e); File.WriteAllText("Docs/Previews/VenusaurSetup.result",e.ToString()); }
    }
    [MenuItem("Pokemon/Create Venusaur Garden")]
    public static void Create()
    {
        foreach(string path in Directory.GetFiles("Assets/Resources/VenusaurBoss","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable=true; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.npotScale=TextureImporterNPOTScale.None; importer.SaveAndReimport();
        }
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
        try
        {
            var camera=new GameObject("Garden Camera").AddComponent<Camera>(); camera.tag="MainCamera";
            camera.orthographic=true; camera.orthographicSize=3.65f; camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(28,55,34,255);
            var garden=new GameObject("Ancient garden roots ruins flowers").AddComponent<AncientGarden>();
            new GameObject("Garden arena controller").AddComponent<ArenaPreview>();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/VenusaurArena.unity");
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!scenes.Exists(x=>x.path=="Assets/Scenes/VenusaurArena.unity")) scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/VenusaurArena.unity",true));
            EditorBuildSettings.scenes=scenes.ToArray();
            garden.Build(); camera.orthographicSize=13.5f; camera.aspect=16f/9;
            WaterTempleSetup.Capture(camera,"Venusaur-Garden-Overview");
        }
        finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
    }
    private static PikachuCombat combat;
    private static VenusaurBoss boss;
    private static Camera cam;
    private static Scene testScene;
    private static int assertions;
    [MenuItem("Pokemon/Check Venusaur Boss")]
    public static void Check()
    {
        var previous=SceneManager.GetActiveScene(); bool suppress=ArenaProgress.SuppressSaving; ArenaProgress.SuppressSaving=true;
        testScene=EditorSceneManager.OpenScene("Assets/Scenes/VenusaurArena.unity",OpenSceneMode.Additive); SceneManager.SetActiveScene(testScene);
        try
        {
            foreach(var go in testScene.GetRootGameObjects()) { if(go.TryGetComponent<Camera>(out var c))cam=c; if(go.TryGetComponent<AncientGarden>(out var g))g.Build(); }
            cam.transform.position=new Vector3(1,0,-10); cam.orthographicSize=4; cam.aspect=16f/9;
            Fresh(); Assert(boss.Health==750,"health");
            combat.Player.ReplayEntrance(); Assert(!boss.StartAttack(VenusaurSkill.RazorLeaf),"intro blocks cast");
            Fresh(); boss.StartAttack(VenusaurSkill.RazorLeaf); Advance(VenusaurBoss.Windups[0]-.05f); Assert(combat.Health==200&&boss.Shots.Count==0,"leaf warning"); Capture("Leaf-Warning");
            Advance(.08f); Assert(boss.Shots.Count==5,"five fan leaves"); Capture("RazorLeaf"); Advance(1); Assert(combat.Health==190,"central leaf hit");
            Fresh(); combat.Player.transform.position=new Vector2(6,0); boss.StartAttack(VenusaurSkill.RazorLeaf); combat.Player.transform.position=new Vector2(6,1.06f); Advance(2); Assert(combat.Health==200,"fan gap is dodgeable");
            Fresh(); boss.StartAttack(VenusaurSkill.LeechSeed); Advance(VenusaurBoss.Windups[1]-.05f); Assert(combat.Health==200,"seed warning"); Capture("Seed-Warning"); Advance(.4f); Assert(combat.Health==194,"seed first tick"); Capture("LeechSeed");
            combat.Player.transform.position=new Vector2(0,3); Advance(3.5f); Assert(combat.Health==194&&boss.Patches.Count==0,"leaving seed and expiry");
            Fresh(); boss.StartAttack(VenusaurSkill.LeechSeed); combat.Player.transform.position=Vector2.up*3; Advance(4); Assert(combat.Health==200,"seed target locked");
            Fresh(); boss.StartAttack(VenusaurSkill.VineWhip); Advance(VenusaurBoss.Windups[2]-.05f); Assert(combat.Health==200,"whip warning"); Advance(.35f); Assert(combat.Health==175,"whip sweep hit"); Capture("VineWhip"); Advance(1); Assert(combat.Health==175,"whip hits once");
            Fresh(); boss.StartAttack(VenusaurSkill.VineWhip); combat.Player.transform.position=Vector2.left*2; Advance(2); Assert(combat.Health==200,"behind whip safe");
            Fresh(); boss.StartAttack(VenusaurSkill.SolarBeam); Advance(VenusaurBoss.Windups[3]-.05f); Assert(combat.Health==200,"solar warning"); Capture("Solar-Warning"); Advance(.1f); Assert(combat.Health==140,"solar damage"); Capture("SolarBeam"); Advance(1); Assert(combat.Health==140,"solar hits once");
            Fresh(); boss.StartAttack(VenusaurSkill.SolarBeam); combat.Player.transform.position=new Vector2(3,2); Advance(VenusaurBoss.Windups[3]+.35f); Assert(combat.Health==200&&boss.LockedDirection==Vector2.right,"solar locked safe side");
            float power=SpellClash.TraceThunderbolt(combat,boss,new Vector2(3,0),Vector2.zero,20,out _); Assert(power==0&&boss.SpinPower==40,"solar strength clash");
            Fresh(); boss.StartAttack(VenusaurSkill.SolarBeam); Advance(VenusaurBoss.Windups[3]-.03f); combat.Projectiles.Add(new ElectricProjectile(new Vector2(2,0),new Vector2(2,0),1,100,7)); Advance(.06f); Assert(combat.Health==200&&!boss.SpinActive,"orb intercepts solar");
            Fresh(); boss.TakeDamage(750); Assert(combat.BattleEnded,"victory"); boss.TickBoss(.1f); Assert(boss.Patches.Count==0&&boss.Shots.Count==0,"battle cleanup");
            Fresh(); combat.Player.CombatMove(new Vector2(20,20),Vector2.one,0); Assert(((Vector2)combat.Player.transform.position).magnitude<=8.401f,"garden bounds");
            Debug.Log("VENUSAUR_CHECK_OK: "+assertions+" assertions");
        }
        finally { ArenaProgress.SuppressSaving=suppress; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(testScene,true); }
    }
    private static void Fresh()
    {
        if(combat)UnityEngine.Object.DestroyImmediate(combat.gameObject); if(boss)UnityEngine.Object.DestroyImmediate(boss.gameObject);
        var p=new GameObject("Test Pikachu").AddComponent<PikachuPlayer>(); p.Initialize(cam); p.SampleEntrance(PikachuPlayer.EntranceDuration); p.transform.position=new Vector2(3,0);
        boss=new GameObject("Test Venusaur").AddComponent<VenusaurBoss>(); combat=p.gameObject.AddComponent<PikachuCombat>(); combat.Initialize(p,boss); boss.InitializeBoss(combat,Vector2.zero); boss.AutoAttack=false;
    }
    private static void Advance(float seconds){while(seconds>.00001f){float dt=Mathf.Min(.02f,seconds);boss.TickBoss(dt);combat.Tick(dt);seconds-=dt;}}
    private static void Assert(bool value,string message){assertions++;if(!value)throw new Exception("Venusaur check: "+message);}
    private static void Capture(string name){boss.GetComponent<VenusaurVisuals>().Render(boss.PhaseTime);WaterTempleSetup.Capture(cam,"Venusaur-"+name);}
}
