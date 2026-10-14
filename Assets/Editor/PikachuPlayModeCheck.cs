using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

// Batch smoke test: real Awake/Update, menu round-trip, scene cleanup, and replay.
[InitializeOnLoad]
public static class PikachuPlayModeCheck
{
    private const string Pending = "PikachuPlayModeCheck.Pending";
    private static int stage;
    private static double deadline;
    private static bool failed;
    private static double captureAt;
    private static System.DateTime captureRequestedAt;
    private static string capturePath;
    private static Vector3 aimCameraPosition;
    private static bool CaptureEnabled => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-pikachuVisualCheck") >= 0;

    static PikachuPlayModeCheck()
    {
        if (SessionState.GetBool(Pending, false)) Attach();
    }

    public static void Run()
    {
        if (!Application.isBatchMode && !CaptureEnabled) throw new System.InvalidOperationException("Run in batch mode or with -pikachuVisualCheck.");
        if(CaptureEnabled)
        {
            var gameViewType=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var gameView=EditorWindow.GetWindow(gameViewType);
            gameView.Show(); gameView.Focus();
        }
        SessionState.SetBool(Pending, true);
        EditorSceneManager.OpenScene("Assets/Scenes/CharizardArena.unity");
        Attach();
        EditorApplication.EnterPlaymode();
    }

    private static void Attach()
    {
        ArenaPreview.TrainingMode = true;
        stage = 0;
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failed = true;
    }

    private static void Tick()
    {
        if (failed || EditorApplication.timeSinceStartup > deadline) { Finish(false, "runtime exception or timeout at stage " + stage); return; }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        var players = Object.FindObjectsByType<PikachuPlayer>(FindObjectsSortMode.None);
        if (stage == 0)
        {
            if (players.Length != 1) { Finish(false, "arena did not spawn exactly one player"); return; }
            if (!players[0].IsEntering) { Finish(false, "intro did not start in Awake"); return; }
            stage = 1;
        }
        else if (stage == 1 && players.Length == 1 && !players[0].IsEntering)
        {
            var player = players[0];
            if (player.transform.position != Vector3.zero || player.FacingDirection != 2)
            { Finish(false, "incorrect runtime arrival"); return; }
            var combat = player.GetComponent<PikachuCombat>();
            if(combat.Dummy is CharizardBoss) { Finish(false,"run legacy training smoke check with training mode enabled"); return; }
            if (!combat || !combat.Dummy) { Finish(false,"training arena not initialized"); return; }
            if(!CaptureEnabled)
            {
                player.TickMovement(Vector2.right,.1f);
                if(!combat.TryCast(PikachuSkill.Thunderbolt,combat.Dummy.transform.position)) { Finish(false,"runtime Thunderbolt cast"); return; }
                stage=10;
                return;
            }
            // Freeze only input for a deterministic screenshot of the real rendered HUD.
            player.enabled = combat.enabled = false;
            combat.BeginAim(PikachuSkill.Thunderbolt,combat.Dummy.transform.position);
            aimCameraPosition=Camera.main.transform.position;
            captureAt=EditorApplication.timeSinceStartup+.5;
            stage=20;
        }
        else if (stage >= 20 && stage <= 23 && players.Length == 1)
        {
            var player=players[0]; var combat=player.GetComponent<PikachuCombat>();
            if ((stage==20 || stage==22) && EditorApplication.timeSinceStartup>=captureAt)
            {
                if (Mathf.Abs(Camera.main.orthographicSize-3.65f)>.001f || Camera.main.transform.position!=aimCameraPosition)
                { Finish(false,"aim/cast changed camera zoom or position"); return; }
                capturePath=Path.GetFullPath("Docs/Previews/"+(stage==20?"Hud-Aim-E.png":"Hud-Auto-C.png"));
                captureRequestedAt=System.DateTime.UtcNow;
                ScreenCapture.CaptureScreenshot(capturePath);
                stage++;
            }
            else if ((stage==21 || stage==23) && File.Exists(capturePath) && File.GetLastWriteTimeUtc(capturePath)>=captureRequestedAt)
            {
                if(stage==21)
                {
                    player.transform.position=new Vector2(1.5f,0);
                    player.CombatPose("Idle",0,Vector2.right);
                    combat.TryCast(PikachuSkill.IronTail,Vector2.left*100);
                    aimCameraPosition=new Vector3(player.transform.position.x,player.transform.position.y,Camera.main.transform.position.z);
                    captureAt=EditorApplication.timeSinceStartup+.5;
                    stage=22;
                }
                else
                {
                    combat.CancelForEntrance(); combat.Tick(100); combat.CancelAim(); player.enabled=combat.enabled=true;
                    player.transform.position=Vector3.zero;
                    player.TickMovement(Vector2.right,.1f);
                    if(player.transform.position.x<=0 || !combat.TryCast(PikachuSkill.Thunderbolt,combat.Dummy.transform.position))
                    { Finish(false,"runtime movement/cast after aiming"); return; }
                    stage=10;
                }
            }
        }
        else if (stage >= 10 && stage <= 14 && players.Length == 1)
        {
            var player = players[0]; var combat = player.GetComponent<PikachuCombat>();
            if (stage == 10 && !combat.IsCasting)
            {
                if (Vector2.Distance(Camera.main.transform.position,player.transform.position)>.001f)
                { Finish(false,"camera did not follow player movement"); return; }
                if (combat.Dummy.LastDamage != 20 || combat.Lightning != 3) { Finish(false,"runtime Thunderbolt damage/charge"); return; }
                if (!combat.TryCast(PikachuSkill.QuickAttack,player.transform.position)) { Finish(false,"runtime Quick Attack cast"); return; }
                stage = 11;
            }
            else if (stage == 11 && !combat.IsCasting)
            {
                if (combat.QuickRemaining <= 0 || combat.Lightning != 5) { Finish(false,"runtime buff/charge"); return; }
                player.transform.position = new Vector2(1.5f,0);
                player.CombatPose("Idle",0,Vector2.left);
                if (!combat.TryCast(PikachuSkill.IronTail,Vector2.left*100,true)) { Finish(false,"runtime Iron Tail cast"); return; }
                stage = 12;
            }
            else if (stage == 12 && !combat.IsCasting)
            {
                if (Vector2.Distance(Camera.main.transform.position,player.transform.position)>.001f)
                { Finish(false,"camera did not follow Iron Tail movement"); return; }
                if (combat.Dummy.LastDamage != 25 || combat.Dummy.SlowRemaining <= 0 || combat.Lightning != 10)
                { Finish(false,"runtime Iron Tail damage/slow/charge"); return; }
                combat.RefillTrainingResources();
                if (!combat.TryCast(PikachuSkill.ElectroBall,combat.Dummy.transform.position)) { Finish(false,"runtime ultimate cast"); return; }
                stage = 13;
            }
            else if (stage == 13 && combat.Charge01 >= .5f)
            { combat.ReleaseElectroBall(combat.Dummy.transform.position); stage = 14; }
            else if (stage == 14 && !combat.IsCasting && combat.Projectiles.Count == 0)
            {
                if (combat.Craters.Count != 1 || combat.Dummy.LastDamage < 60) { Finish(false,"runtime ultimate/crater"); return; }
                SceneManager.LoadScene("MainMenu"); stage = 2;
            }
        }
        else if (stage == 2 && SceneManager.GetActiveScene().name == "MainMenu")
        {
            if (players.Length != 0) { Finish(false, "player leaked into menu"); return; }
            SceneManager.LoadScene("CharizardArena");
            stage = 3;
        }
        else if (stage == 3 && SceneManager.GetActiveScene().name == "CharizardArena")
        {
            if (players.Length != 1 || !players[0].IsEntering)
            { Finish(false, "re-entering arena did not restart intro cleanly"); return; }
            if (players[0].GetComponent<PikachuCombat>().Craters.Count != 0 || players[0].GetComponent<PikachuCombat>().Lightning != 0)
            { Finish(false, "new attempt retained combat state"); return; }
            stage = 4;
        }
        else if (stage == 4 && players.Length == 1 && !players[0].IsEntering)
        {
            players[0].ReplayEntrance();
            if (!players[0].IsEntering || players[0].transform.position.x >= 0)
            { Finish(false, "runtime replay failed"); return; }
            Finish(true, (CaptureEnabled ? "real HUD screenshots of E/C range/target rings; " : "") + "entrance, movement, four shortened casts, damage/charge/buff/slow/projectile/crater, menu cleanup, fresh arena, replay; no game runtime errors.");
        }
    }

    private static void Finish(bool success, string reason)
    {
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        if (success) Debug.Log("PIKACHU_PLAYMODE_OK: " + reason);
        else Debug.LogError("PIKACHU_PLAYMODE_FAILED: " + reason);
        EditorApplication.Exit(success ? 0 : 1);
    }
}
