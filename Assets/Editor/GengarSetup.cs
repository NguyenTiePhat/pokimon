using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GengarSetup
{
    private const string Request="Temp/GengarSetup.request";
    static GengarSetup(){EditorApplication.update+=Poll;EditorApplication.update+=PlayPoll;}
    public static void RunAll(){EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Single);Create();Check();}
    public static void RunPlay()
    {
        SessionState.SetBool("Gengar.PlayCheck",true);SessionState.SetInt("Gengar.PlayStage",0);
        SessionState.SetFloat("Gengar.PlayDeadline",(float)EditorApplication.timeSinceStartup+100);
        ArenaPreview.TrainingMode=false;
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Single);EditorApplication.isPlaying=true;
    }
    private static void PlayPoll()
    {
        if(!SessionState.GetBool("Gengar.PlayCheck",false))return;
        try
        {
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat("Gengar.PlayDeadline",0))throw new Exception("Gengar play timeout");
            if(!EditorApplication.isPlaying)return;
            int stage=SessionState.GetInt("Gengar.PlayStage",0);
            if(stage==0)
            {
                if(!UnityEngine.Object.FindAnyObjectByType<MainMenu>())return;
                ArenaProgress.Enter("GengarArena");SessionState.SetInt("Gengar.PlayStage",1);return;
            }
            var player=UnityEngine.Object.FindAnyObjectByType<PikachuCombat>();
            var enemy=UnityEngine.Object.FindAnyObjectByType<GengarBoss>();
            if(!player||!enemy||player.Player.IsEntering)return;
            enemy.AutoAttack=false;
            if(stage==1)
            {
                if(enemy.Health!=800||player.Health!=200||!UnityEngine.Object.FindAnyObjectByType<HauntedRuins>())throw new Exception("Haunted play spawn failed");
                if(!enemy.StartAttack(GengarSkill.PhantomForce))throw new Exception("Haunted play cast failed");
                SessionState.SetInt("Gengar.PlayStage",2);
            }
            else if(stage==2&&enemy.WindingUp&&enemy.PhaseTime>.7f)
            {
                ScreenCapture.CaptureScreenshot("Docs/Previews/Gengar-PlayMode-HUD.png");
                SessionState.SetInt("Gengar.PlayStage",3);
            }
            else if(stage==3&&(!enemy.WindingUp && enemy.ActiveSkill==3))
            {
                enemy.TakeDamage(1000);if(!player.BattleEnded)throw new Exception("Haunted victory failed");
                ArenaProgress.Enter("GengarArena");SessionState.SetInt("Gengar.PlayStage",4);
            }
            else if(stage==4)
            {
                if(enemy.Health!=800||player.Health!=200||enemy.Patches.Count!=0)throw new Exception("Haunted retry failed");
                Debug.Log("GENGAR_PLAYMODE_OK: menu entry, scene spawn, intro, cast, victory and retry");
                SessionState.SetBool("Gengar.PlayCheck",false);EditorApplication.Exit(0);
            }
        }
        catch(Exception e){Debug.LogException(e);SessionState.SetBool("Gengar.PlayCheck",false);EditorApplication.Exit(1);}
    }
    private static void Poll()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(Request);
        try { Create(); Check(); File.WriteAllText("Docs/Previews/GengarSetup.result","GENGAR_OK"); }
        catch(Exception e){ Debug.LogException(e); File.WriteAllText("Docs/Previews/GengarSetup.result",e.ToString()); }
    }
    [MenuItem("Pokemon/Create Gengar Haunted")]
    public static void Create()
    {
        foreach(string path in Directory.GetFiles("Assets/Resources/GengarBoss","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable=true; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.npotScale=TextureImporterNPOTScale.None; importer.SaveAndReimport();
        }
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
        try
        {
            var camera=new GameObject("Haunted Camera").AddComponent<Camera>(); camera.tag="MainCamera";
            camera.orthographic=true; camera.orthographicSize=3.65f; camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(20,23,43,255);
            var ruins=new GameObject("Moonlit haunted ruins arches and tombs").AddComponent<HauntedRuins>();
            new GameObject("Haunted arena controller").AddComponent<ArenaPreview>();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/GengarArena.unity");
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!scenes.Exists(x=>x.path=="Assets/Scenes/GengarArena.unity")) scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/GengarArena.unity",true));
            EditorBuildSettings.scenes=scenes.ToArray();
            ruins.Build(); camera.orthographicSize=13.5f; camera.aspect=16f/9;
            WaterTempleSetup.Capture(camera,"Gengar-Haunted-Overview");
        }
        finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
    }
    private static PikachuCombat combat;
    private static GengarBoss boss;
    private static Camera cam;
    private static Scene testScene;
    private static int assertions;
    [MenuItem("Pokemon/Check Gengar Boss")]
    public static void Check()
    {
        var previous=SceneManager.GetActiveScene(); bool suppress=ArenaProgress.SuppressSaving; ArenaProgress.SuppressSaving=true;
        testScene=EditorSceneManager.OpenScene("Assets/Scenes/GengarArena.unity",OpenSceneMode.Additive); SceneManager.SetActiveScene(testScene);
        try
        {
            foreach(var go in testScene.GetRootGameObjects()) { if(go.TryGetComponent<Camera>(out var c))cam=c; if(go.TryGetComponent<HauntedRuins>(out var g))g.Build(); }
            cam.transform.position=new Vector3(1,0,-10); cam.orthographicSize=4; cam.aspect=16f/9;
            Fresh(); Assert(boss.Health==800,"health");
            combat.Player.ReplayEntrance(); Assert(!boss.StartAttack(GengarSkill.ShadowBall),"intro blocks cast");
            Fresh(); boss.StartAttack(GengarSkill.ShadowBall); Advance(GengarBoss.Windups[0]-.05f); Assert(combat.Health==200&&boss.Shots.Count==0,"ball warning"); Capture("Ball-Warning");
            Advance(.08f); Assert(boss.Shots.Count==1,"one ball"); Capture("ShadowBall"); Advance(.5f); Assert(combat.Health==178,"ball hit");
            Fresh(); boss.StartAttack(GengarSkill.ShadowBall); combat.Player.transform.position=new Vector2(3,2); Advance(2); Assert(combat.Health==200&&boss.Target==new Vector2(3,0),"ball locks direction and can be dodged");
            Fresh(); boss.StartAttack(GengarSkill.SludgeBomb); Advance(GengarBoss.Windups[1]-.1f); Assert(combat.Health==200,"bomb warning"); Capture("Bomb-Warning"); Advance(.15f); Assert(combat.Health==182&&boss.Patches.Count==1,"bomb impact"); Capture("Poison");
            combat.Player.transform.position=new Vector2(0,3); Advance(3.5f); Assert(combat.Health==182&&boss.Patches.Count==0,"leaving poison and expiry");
            Fresh(); boss.StartAttack(GengarSkill.SludgeBomb); combat.Player.transform.position=Vector2.up*3; Advance(4.5f); Assert(combat.Health==200,"bomb locked target dodge");
            Fresh(); boss.StartAttack(GengarSkill.SludgeBomb); Advance(GengarBoss.Windups[1]+.65f); Assert(combat.Health==176,"poison tick");
            Fresh(); boss.StartAttack(GengarSkill.ShadowPunch); Advance(.4f); Assert(boss.Hidden&&!boss.TakeDamage(20),"vanish immunity"); Capture("Punch-Warning");
            Advance(GengarBoss.Windups[2]-.4f+.05f); Assert(!boss.Hidden&&combat.Health==172&&(Vector2)boss.transform.position==new Vector2(3,0),"punch reappears at marker"); Capture("ShadowPunch"); Advance(.8f); Assert(combat.Health==172,"punch hits once");
            Fresh(); boss.StartAttack(GengarSkill.ShadowPunch); combat.Player.transform.position=new Vector2(3,2); Advance(1.1f); Assert(combat.Health==200,"punch dodge");
            Fresh(); boss.StartAttack(GengarSkill.PhantomForce); Advance(GengarBoss.Windups[3]-.1f); Assert(boss.Hidden&&combat.Health==200,"ulti warning throughout wait"); Capture("Phantom-Warning"); Advance(.15f); Assert(combat.Health==145&&!boss.Hidden,"ulti hit"); Capture("PhantomForce"); Advance(.8f); Assert(combat.Health==145,"ulti hits once");
            Fresh(); boss.StartAttack(GengarSkill.PhantomForce); combat.Player.transform.position=new Vector2(3,3.5f); Advance(2); Assert(combat.Health==200,"ulti dodge");
            Fresh(); boss.StartAttack(GengarSkill.PhantomForce); Advance(.1f); boss.TakeDamage(1000); boss.TickBoss(.1f); Assert(combat.BattleEnded&&boss.ActiveSkill==-1&&boss.Patches.Count==0&&boss.Shots.Count==0,"death cancels attacks");
            Fresh(); boss.StartAttack(GengarSkill.ShadowPunch); Advance(.4f); combat.TakeDamage(1000); boss.TickBoss(.1f); Assert(!boss.Hidden&&boss.ActiveSkill==-1,"defeat clears hidden state");
            Fresh(); combat.Player.CombatMove(new Vector2(20,20),Vector2.one,0); Assert(((Vector2)combat.Player.transform.position).magnitude<=8.401f,"ruins bounds");
            Debug.Log("GENGAR_CHECK_OK: "+assertions+" assertions");
        }
        finally { ArenaProgress.SuppressSaving=suppress; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(testScene,true); }
    }
    private static void Fresh()
    {
        if(combat)UnityEngine.Object.DestroyImmediate(combat.gameObject); if(boss)UnityEngine.Object.DestroyImmediate(boss.gameObject);
        var p=new GameObject("Test Pikachu").AddComponent<PikachuPlayer>(); p.Initialize(cam); p.SampleEntrance(PikachuPlayer.EntranceDuration); p.transform.position=new Vector2(3,0);
        boss=new GameObject("Test Gengar").AddComponent<GengarBoss>(); combat=p.gameObject.AddComponent<PikachuCombat>(); combat.Initialize(p,boss); boss.InitializeBoss(combat,Vector2.zero); boss.AutoAttack=false;
    }
    private static void Advance(float seconds){while(seconds>.00001f){float dt=Mathf.Min(.02f,seconds);boss.TickBoss(dt);combat.Tick(dt);seconds-=dt;}}
    private static void Assert(bool value,string message){assertions++;if(!value)throw new Exception("Gengar check: "+message);}
    private static void Capture(string name){boss.GetComponent<GengarVisuals>().Render(boss.PhaseTime);WaterTempleSetup.Capture(cam,"Gengar-"+name);}
}


