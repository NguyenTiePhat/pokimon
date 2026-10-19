using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WaterTemplePlayModeCheck
{
    private const string Pending="WaterTemplePlayModeCheck.Pending";
    private static int stage;
    private static double deadline,captureAt;
    private static bool failed;
    private static string capturePath;
    private static System.DateTime captureRequested;
    private static bool Visual=>System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-waterVisualCheck")>=0;
    static WaterTemplePlayModeCheck() { if(SessionState.GetBool(Pending,false)) Attach(); }
    public static void Run()
    {
        if(!Application.isBatchMode && !Visual) throw new System.Exception("Use batch mode or -waterVisualCheck");
        if(Visual) { var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); view.Show(); view.Focus(); }
        SessionState.SetBool(Pending,true); EditorSceneManager.OpenScene("Assets/Scenes/CharizardArena.unity"); Attach(); EditorApplication.EnterPlaymode();
    }
    public static void RunAll()
    {
        PikachuCombatCheck.Run(); CharizardBossCheck.Run(); PikachuHitCheck.Run(); SpellClashCheck.Run(); Run();
    }
    private static void Attach()
    {
        ArenaPreview.TrainingMode=false; ArenaProgress.SuppressSaving=true; stage=0; deadline=EditorApplication.timeSinceStartup+140;
        EditorApplication.update-=Tick; EditorApplication.update+=Tick; Application.logMessageReceived-=OnLog; Application.logMessageReceived+=OnLog;
    }
    private static void OnLog(string message,string stack,LogType type) { if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) failed=true; }
    private static void Tick()
    {
        if(failed || EditorApplication.timeSinceStartup>deadline) { Finish(false,"error/timeout at "+stage); return; }
        if(!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        var combat=Object.FindFirstObjectByType<PikachuCombat>(); if(!combat) return;
        var player=combat.Player;
        if(stage==0 && !player.IsEntering)
        {
            if(!(combat.Dummy is CharizardBoss)) { Finish(false,"stage 1 boss missing"); return; }
            combat.Dummy.TakeDamage(1000);
            if(!ArenaProgress.WaterUnlocked) { Finish(false,"Charizard victory did not unlock stage 2"); return; }
            ArenaProgress.Enter("BlastoiseArena"); stage=1; return;
        }
        if(stage==1 && SceneManager.GetActiveScene().name=="BlastoiseArena")
        {
            if(!(combat.Dummy is BlastoiseBoss) || combat.Health!=200 || combat.Dummy.Health!=650 || !player.IsEntering)
            { Finish(false,"water scene initialization"); return; }
            bool floor=false; foreach(var r in Object.FindObjectsByType<SpriteRenderer>()) if(r.sprite && r.sprite.name=="BlastoiseWaterTemple") floor=true;
            if(!floor) { Finish(false,"water floor missing"); return; }
            stage=2;
        }
        var boss=combat.Dummy as BlastoiseBoss;
        if(stage==2 && !player.IsEntering)
        {
            boss.AutoAttack=false;
            if(!boss.StartAttack(BlastoiseSkill.HydroCannon)) { Finish(false,"cannon start"); return; }
            stage=3;
        }
        else if(stage==3 && boss.PhaseTime>=.9f)
        {
            if(!boss.WindingUp || combat.Health!=200) { Finish(false,"cannon warning damage"); return; }
            if(Visual) BeginCapture(combat,boss,"WaterTemple-Warning-HUD",4);
            else { player.transform.position=new Vector2(0,2.5f); stage=6; }
        }
        else if(stage==4 || stage==8)
        {
            if(EditorApplication.timeSinceStartup<captureAt) return;
            captureRequested=System.DateTime.UtcNow; ScreenCapture.CaptureScreenshot(capturePath); stage++;
        }
        else if((stage==5 || stage==9) && File.Exists(capturePath) && File.GetLastWriteTimeUtc(capturePath)>=captureRequested)
        {
            player.enabled=combat.enabled=boss.enabled=true;
            if(stage==5) { player.transform.position=new Vector2(0,2.5f); stage=6; } else stage=7;
        }
        else if(stage==6 && !boss.WindingUp)
        {
            if(combat.Health!=200 || !boss.SpinActive) { Finish(false,"safe side damaged or cannon missing"); return; }
            if(Visual) BeginCapture(combat,boss,"WaterTemple-Cannon-HUD",8); else stage=7;
        }
        else if(stage==7 && boss.ActiveSkill<0)
        {
            if(combat.Health!=200) { Finish(false,"dodge failed"); return; }
            player.transform.position=new Vector2(1,0);
            if(!combat.TryCast(PikachuSkill.Thunderbolt,boss.transform.position)) { Finish(false,"Pikachu E targeting water boss"); return; }
            stage=10;
        }
        else if(stage==10 && !combat.IsCasting)
        {
            if(boss.Health!=630) { Finish(false,"water boss did not take E damage"); return; }
            combat.TakeDamage(200); stage=11;
        }
        else if(stage==11)
        {
            if(!combat.BattleEnded || boss.StartAttack(BlastoiseSkill.RapidSpin)) { Finish(false,"water defeat not stopped"); return; }
            SceneManager.LoadScene("BlastoiseArena"); stage=12;
        }
        else if(stage==12 && player.IsEntering)
        {
            if(combat.Health!=200 || boss.Health!=650 || boss.Waves.Count!=0 || combat.Craters.Count!=0) { Finish(false,"water retry retained state"); return; }
            stage=13;
        }
        else if(stage==13 && !player.IsEntering)
        { boss.TakeDamage(1000); stage=14; }
        else if(stage==14)
        {
            if(!combat.BattleEnded) { Finish(false,"water victory"); return; }
            ArenaPreview.TrainingMode=true; SceneManager.LoadScene("BlastoiseArena"); stage=15;
        }
        else if(stage==15)
        {
            if(combat.Dummy is ArenaBoss || !ArenaProgress.IsWater) { Finish(false,"water training switch"); return; }
            ArenaProgress.Enter("BlastoiseArena"); stage=16;
        }
        else if(stage==16)
        {
            if(!boss || boss.Health!=650) { Finish(false,"water return from training"); return; }
            Finish(true,"Charizard victory unlock, water scene/floor, warning, safe-side dodge, E targeting, defeat/retry, water victory, training round-trip; no runtime errors");
        }
    }
    private static void BeginCapture(PikachuCombat combat,BlastoiseBoss boss,string name,int next)
    {
        combat.Player.enabled=combat.enabled=boss.enabled=false;
        capturePath=Path.GetFullPath("Docs/Previews/"+name+".png"); captureAt=EditorApplication.timeSinceStartup+.4; stage=next;
    }
    private static void Finish(bool success,string reason)
    {
        SessionState.SetBool(Pending,false); EditorApplication.update-=Tick; Application.logMessageReceived-=OnLog;
        if(success) Debug.Log("WATER_TEMPLE_PLAYMODE_OK: "+reason); else Debug.LogError("WATER_TEMPLE_PLAYMODE_FAILED: "+reason);
        EditorApplication.Exit(success?0:1);
    }
}
