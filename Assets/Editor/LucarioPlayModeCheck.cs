using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class LucarioPlayModeCheck
{
    private const string Pending="Lucario.PlayCheck";
    private static int stage, frames, captures, lastFrame;
    private static double deadline;
    private static bool failed;
    private static float minFps=1000, sumFrame;
    private static int frameCount;
    static LucarioPlayModeCheck() { if(SessionState.GetBool(Pending,false))Attach(); }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new Exception("Run the Lucario Play Mode check with -batchmode.");
        SessionState.SetBool(Pending,true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Attach();
        EditorApplication.EnterPlaymode();
    }
    private static void Attach()
    {
        stage=frames=captures=frameCount=0; sumFrame=0; minFps=1000; failed=false; lastFrame=-1;
        deadline=EditorApplication.timeSinceStartup+180;
        EditorApplication.update-=Tick; EditorApplication.update+=Tick;
        Application.logMessageReceived-=OnLog; Application.logMessageReceived+=OnLog;
    }
    private static void OnLog(string message,string stack,LogType type)
    { if(type==LogType.Exception||type==LogType.Assert||type==LogType.Error)failed=true; }
    private static void Verify(bool value,string description) { if(!value)throw new Exception(description); }
    private static void Load(string scene,int next,bool training=false)
    {
        stage=next; frames=0;
        ArenaPreview.TrainingMode=training;
        SceneManager.LoadScene(scene);
    }
    private static void Tick()
    {
        if(failed||EditorApplication.timeSinceStartup>deadline) { Finish(false,"runtime error or timeout at stage "+stage); return; }
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(Time.frameCount==lastFrame)return;
        lastFrame=Time.frameCount;
        try
        {
            ArenaProgress.SuppressSaving=true;
            frames++;
            if(stage==0)
            {
                var menu=Object.FindAnyObjectByType<MainMenu>();
                if(!menu)return;
                // The fifth card must be reachable on page two with the existing four-card menu.
                var field=typeof(MainMenu).GetField("bosses",BindingFlags.Instance|BindingFlags.NonPublic);
                var entries=(Array)field.GetValue(menu);
                Verify(entries.Length==5,"five catalog entries");
                object entry=entries.GetValue(4);
                var type=entry.GetType();
                Verify((string)type.GetField("scene").GetValue(entry)=="LucarioArena","page two Lucario route");
                Verify(type.GetField("texture").GetValue(entry)!=null,"Lucario menu portrait");
                typeof(MainMenu).GetField("page",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(menu,1);
                stage=1; frames=0; return;
            }
            if(stage==1)
            {
                if(frames<5)return;
                Load("LucarioArena",2); return;
            }
            var combat=Object.FindAnyObjectByType<PikachuCombat>();
            if(!combat)return;
            var boss=combat.Dummy as LucarioBoss;
            if(stage==14)
            {
                Verify(!boss&&combat.Dummy&&!(combat.Dummy is ArenaBoss)&&combat.Health==200,"training dummy spawn");
                Load("LucarioArena",15); return;
            }
            if(stage==16)
            {
                var ghost=combat.Dummy as GengarBoss;
                Verify(ghost!=null,"Gengar scene still routes correctly");
                if(combat.Player.IsEntering)return;
                ghost.AutoAttack=false;
                ghost.TakeDamage(10000);
                Verify(combat.BattleEnded,"Gengar victory");
                Load("LucarioArena",17); return;
            }
            Verify(boss!=null,"Lucario scene must spawn Lucario");
            boss.AutoAttack=false;
            if(stage==2)
            {
                Verify(boss.Health==850&&combat.Health==200&&Object.FindAnyObjectByType<MountainDojo>(),"spawn health and environment");
                Verify(combat.Player.IsEntering,"real entrance started");
                Verify(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"one audio listener");
                Verify(!boss.StartAttack(LucarioSkill.AuraSphere),"intro attack blocked");
                stage=3; return;
            }
            if(combat.Player.IsEntering)return;
            if(stage==3)
            {
                Verify(combat.TryCast(PikachuSkill.Thunderbolt,boss.transform.position),"real E cast");
                stage=4; return;
            }
            if(stage==4)
            {
                if(combat.IsCasting)return;
                Verify(boss.Health==830,"E damage in Play Mode");
                Verify(boss.StartAttack(LucarioSkill.MeteorMash),"real meteor start");
                stage=5; return;
            }
            if(stage==5)
            {
                if(boss.WindingUp&&boss.PhaseTime>.85f&&captures==0)
                {
                    // Camera render is reliable in batch mode; HUD is validated in a standalone capture below.
                    boss.GetComponent<LucarioVisuals>().Render(Time.time);
                    WaterTempleSetup.Capture(Camera.main,"Lucario-PlayMode-Leap");
                    captures++;
                }
                if(boss.WindingUp)return;
                Verify(combat.Health==152,"real meteor damage");
                stage=6; return;
            }
            if(stage==6)
            {
                if(boss.ActiveSkill>=0||combat.StunRemaining>0)return;
                boss.transform.position=new Vector2(-3,0);
                combat.Player.transform.position=Vector2.zero;
                Verify(boss.StartAttack(LucarioSkill.ExtremeSpeed),"real speed start");
                stage=7; return;
            }
            if(stage==7)
            {
                if(boss.PhaseTime<.55f&&boss.WindingUp)return;
                Verify(combat.TryDash(Vector2.up),"real player dash");
                stage=8; return;
            }
            if(stage==8)
            {
                if(boss.ActiveSkill>=0)return;
                Verify(combat.Health==152,"real player dash avoids speed");
                boss.TakeDamage(10000);
                Verify(combat.BattleEnded,"victory state");
                stage=9; frames=0; return;
            }
            if(stage==9)
            {
                if(frames<3)return;
                Verify(boss.ActiveSkill==-1&&boss.Shots.Count==0,"victory cleanup");
                Load("LucarioArena",10); return;
            }
            if(stage==10)
            {
                Verify(combat.Health==200&&boss.Health==850&&boss.Shots.Count==0&&!boss.Awakened,"fresh retry resets");
                combat.TakeDamage(10000);
                Verify(combat.BattleEnded&&combat.Health==0,"defeat");
                stage=11; frames=0; return;
            }
            if(stage==11)
            {
                if(frames<3)return;
                Verify(boss.ActiveSkill==-1&&boss.Shots.Count==0,"defeat cleanup");
                Load("LucarioArena",12); return;
            }
            if(stage==12)
            {
                Verify(combat.Health==200&&boss.Health==850,"retry after defeat");
                Verify(boss.StartAttack(LucarioSkill.AuraSphere),"sphere before replay");
                combat.Player.ReplayEntrance();
                stage=13; return;
            }
            if(stage==13)
            {
                Verify(boss.ActiveSkill==-1&&boss.Shots.Count==0,"entrance replay clears attack");
                Load("LucarioArena",14,true); return;
            }
            if(stage==15)
            {
                Verify(boss.Health==850&&combat.Health==200,"training return to Lucario");
                Load("GengarArena",16); return;
            }
            if(stage==17)
            {
                Verify(combat.Health==200&&boss.Health==850,"Gengar to Lucario transition");
                boss.AutoAttack=true;
                stage=18; frames=0; return;
            }
            if(stage==18)
            {
                boss.AutoAttack=true;
                // Measure the actual runtime with rendering active for several attacks.
                if(Time.deltaTime>0)
                { sumFrame+=Time.deltaTime; frameCount++; minFps=Mathf.Min(minFps,1/Time.deltaTime); }
                if(sumFrame<10&&!combat.BattleEnded)return;
                Verify(boss.AttackCount>=2,"live AI attacks");
                Finish(true,"menu page two, actual scene spawn/intro, E, meteor damage, dash dodge, victory/defeat/retry, entrance replay, training round trip, Gengar transition, live AI. Runtime samples="+frameCount+", average frame="+(sumFrame/Mathf.Max(1,frameCount)*1000).ToString("0.0")+" ms, slowest="+(1000/minFps).ToString("0.0")+" ms.");
            }
        }
        catch(Exception ex) { Finish(false,"stage "+stage+": "+ex); }
    }
    private static void Finish(bool success,string message)
    {
        SessionState.SetBool(Pending,false);
        EditorApplication.update-=Tick;
        Application.logMessageReceived-=OnLog;
        string result=(success?"LUCARIO_PLAYMODE_OK: ":"LUCARIO_PLAYMODE_FAILED: ")+message;
        File.WriteAllText("Docs/Previews/LucarioPlayMode.result.txt",result);
        if(success)Debug.Log(result); else Debug.LogError(result);
        EditorApplication.Exit(success?0:1);
    }
}
