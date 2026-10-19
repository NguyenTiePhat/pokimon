using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CharizardPlayModeCheck
{
    private const string Pending="CharizardPlayModeCheck.Pending";
    private static int stage;
    private static double deadline, captureAt;
    private static bool failed;
    private static bool clashChecked;
    private static System.DateTime requested;
    private static string path;
    private static bool Visual => System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-charizardVisualCheck")>=0;
    static CharizardPlayModeCheck() { if(SessionState.GetBool(Pending,false)) Attach(); }
    public static void Run()
    {
        if(!Application.isBatchMode && !Visual) throw new System.Exception("Use batch mode or -charizardVisualCheck");
        if(Visual) { var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); view.Show(); view.Focus(); }
        SessionState.SetBool(Pending,true); ArenaPreview.TrainingMode=false;
        EditorSceneManager.OpenScene("Assets/Scenes/CharizardArena.unity"); Attach(); EditorApplication.EnterPlaymode();
    }
    private static void Attach()
    {
        ArenaProgress.SuppressSaving=true;
        ArenaPreview.TrainingMode=false; stage=0; clashChecked=false; deadline=EditorApplication.timeSinceStartup+100;
        EditorApplication.update-=Tick; EditorApplication.update+=Tick;
        Application.logMessageReceived-=OnLog; Application.logMessageReceived+=OnLog;
    }
    private static void OnLog(string message,string stack,LogType type)
    { if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) failed=true; }
    private static void Tick()
    {
        if(failed || EditorApplication.timeSinceStartup>deadline) { Finish(false,"runtime error or timeout at "+stage); return; }
        if(!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        var boss=Object.FindFirstObjectByType<CharizardBoss>();
        if(stage==10)
        {
            var training=Object.FindFirstObjectByType<PikachuCombat>();
            if(boss || !training || !training.Dummy || training.Health!=200)
            { Finish(false,"training mode switch or boss cleanup"); return; }
            ArenaPreview.TrainingMode=false; SceneManager.LoadScene("CharizardArena"); stage=11; return;
        }
        if(!boss) { Finish(false,"default arena did not spawn Charizard"); return; }
        var combat=boss.Opponent; var player=combat.Player;
        if(stage==11)
        {
            if(boss.Health!=500 || !player.IsEntering) { Finish(false,"return from training"); return; }
            Finish(true,"default boss spawn, intro, player E damage, Fire Spin damage, victory, fresh retry, defeat, training round-trip; no runtime errors"); return;
        }
        if(stage==0)
        {
            if(!player.IsEntering || boss.Health!=500 || combat.Health!=200) { Finish(false,"initial health/entrance"); return; }
            stage=1;
        }
        else if(stage==1 && !player.IsEntering)
        {
            boss.AutoAttack=false;
            if(!combat.TryCast(PikachuSkill.Thunderbolt,boss.transform.position)) { Finish(false,"runtime E target boss"); return; }
            stage=2;
        }
        else if(stage==2 && !combat.IsCasting)
        {
            if(boss.Health!=480) { Finish(false,"runtime E boss damage"); return; }
            if(!clashChecked)
            {
                combat.Projectiles.Add(new ElectricProjectile(new Vector2(.8f,0),new Vector2(3.2f,0),0,5,10,true));
                boss.Shots.Add(new BossProjectile(new Vector2(3.2f,0),Vector2.left,10,2.4f,.3f,5,false));
                stage=15; return;
            }
            if(!boss.StartAttack(CharizardSkill.FireSpin)) { Finish(false,"runtime Fire Spin start"); return; }
            if(Visual)
            {
                player.enabled=combat.enabled=boss.enabled=false;
                boss.TickBoss(1.35f);
                captureAt=EditorApplication.timeSinceStartup+.5; stage=3;
            }
            else stage=5;
        }
        else if(stage==15 && combat.Projectiles.Count==0 && boss.Shots.Count==0)
        {
            if(combat.Health!=200 || boss.Health!=480 || !combat.Impacts.Exists(x=>x.clash))
            { Finish(false,"runtime equal spell clash failed or damaged a character"); return; }
            clashChecked=true; stage=2;
        }
        else if(stage==3 && EditorApplication.timeSinceStartup>=captureAt)
        {
            path=Path.GetFullPath("Docs/Previews/Charizard-Battle-HUD.png"); requested=System.DateTime.UtcNow;
            ScreenCapture.CaptureScreenshot(path); stage=4;
        }
        else if(stage==4 && File.Exists(path) && File.GetLastWriteTimeUtc(path)>=requested)
        { player.enabled=combat.enabled=boss.enabled=true; stage=5; }
        else if(stage==5 && combat.Health<200)
        {
            if(combat.Health!=192) { Finish(false,"runtime Fire Spin first tick"); return; }
            boss.TakeDamage(1000); stage=6;
        }
        else if(stage==6)
        {
            if(!combat.BattleEnded || boss.Shots.Count!=0) { Finish(false,"runtime victory"); return; }
            SceneManager.LoadScene("CharizardArena"); stage=7;
        }
        else if(stage==7 && player.IsEntering)
        {
            if(boss.Health!=500 || combat.Health!=200 || combat.Craters.Count!=0) { Finish(false,"retry state"); return; }
            stage=8;
        }
        else if(stage==8 && !player.IsEntering)
        {
            boss.AutoAttack=false;
            if(!boss.StartAttack(CharizardSkill.DragonClaw)) { Finish(false,"runtime claw start"); return; }
            stage=12;
        }
        else if(stage==12 && combat.StunRemaining>0)
        {
            if(combat.Health!=175 || combat.CanAct || combat.TryDash(Vector2.up))
            { Finish(false,"Dragon Claw failed to stun and lock Pikachu"); return; }
            stage=13;
        }
        else if(stage==13)
        {
            if(!combat.GetComponent<CombatVisuals>().StunVisible) { Finish(false,"runtime stun stars missing"); return; }
            stage=14;
        }
        else if(stage==14 && combat.StunRemaining<=0)
        {
            if(!combat.CanAct || combat.StunProtectionRemaining<=0) { Finish(false,"stun recovery/protection"); return; }
            combat.TakeDamage(200); stage=9;
        }
        else if(stage==9)
        {
            if(!combat.BattleEnded || boss.StartAttack(CharizardSkill.DragonClaw)) { Finish(false,"runtime defeat"); return; }
            ArenaPreview.TrainingMode=true; SceneManager.LoadScene("CharizardArena"); stage=10;
        }
    }
    private static void Finish(bool success,string reason)
    {
        SessionState.SetBool(Pending,false); EditorApplication.update-=Tick; Application.logMessageReceived-=OnLog;
        if(success) Debug.Log("CHARIZARD_PLAYMODE_OK: "+reason); else Debug.LogError("CHARIZARD_PLAYMODE_FAILED: "+reason);
        EditorApplication.Exit(success?0:1);
    }
}
