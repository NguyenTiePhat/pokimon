#if LUCARIO_CAPTURE
// Compiled only into the QA build; activates solely with an explicit local capture argument.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LucarioPlayerCapture : MonoBehaviour
{
    private string output;
    private bool error;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var args=Environment.GetCommandLineArgs();
        int index=Array.IndexOf(args,"-lucarioCapture");
        if(index<0||index+1>=args.Length)return;
        var harness=new GameObject("Lucario QA capture").AddComponent<LucarioPlayerCapture>();
        harness.output=args[index+1];
        DontDestroyOnLoad(harness.gameObject);
    }
    private void Awake() { Application.runInBackground=true; Application.logMessageReceived+=OnLog; }
    private void OnLog(string message,string stack,LogType type)
    { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)error=true; }
    private IEnumerator Start()
    {
        Directory.CreateDirectory(output);
        ArenaProgress.SuppressSaving=true;
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
        // Recreate the swap chain after a hidden Windows launch, before requesting readbacks.
        Screen.SetResolution(960,540,FullScreenMode.Windowed);
        yield return new WaitForSecondsRealtime(.3f);
        Screen.SetResolution(1280,720,FullScreenMode.Windowed);
        yield return new WaitForSecondsRealtime(.5f);
        var menu=FindAnyObjectByType<MainMenu>();
        if(!menu) { Application.Quit(1); yield break; }
        typeof(MainMenu).GetField("page",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(menu,1);
        yield return Capture("Lucario-Menu-Page2");
        ArenaProgress.Enter("LucarioArena");
        yield return null;
        var boss=FindAnyObjectByType<LucarioBoss>();
        if(!boss) { Application.Quit(1); yield break; }
        boss.AutoAttack=false;
        var combat=boss.Opponent;
        while(combat.Player.IsEntering)yield return null;
        // Freeze simulation only for reproducible visual samples; render and IMGUI remain live.
        boss.enabled=false;
        combat.enabled=false;
        combat.Player.enabled=false;
        foreach(LucarioSkill skill in Enum.GetValues(typeof(LucarioSkill)))
        {
            boss.transform.position=new Vector2(3,0);
            combat.Player.transform.position=Vector2.zero;
            boss.TickBoss(20); // Independent cooldowns expire, without driving gameplay input.
            if(!boss.StartAttack(skill)) { error=true; break; }
            boss.TickBoss(LucarioBoss.Windups[(int)skill]*.6f);
            boss.GetComponent<LucarioVisuals>().Render(Time.time);
            yield return Capture("Lucario-HUD-"+skill);
            boss.TickBoss(LucarioBoss.Windups[(int)skill]*.4f+.12f);
            boss.GetComponent<LucarioVisuals>().Render(Time.time);
            yield return Capture("Lucario-Action-"+skill);
            combat.Player.transform.position=new Vector2(-6,-4);
            boss.TickBoss(3);
        }
        boss.TakeDamage(10000);
        boss.TickBoss(.05f);
        yield return Capture("Lucario-Victory-HUD");
        Screen.SetResolution(960,540,FullScreenMode.Windowed);
        yield return null;
        yield return Capture("Lucario-Victory-960x540");
        SceneManager.LoadScene("MainMenu");
        yield return null;
        menu=FindAnyObjectByType<MainMenu>();
        typeof(MainMenu).GetField("page",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(menu,1);
        yield return Capture("Lucario-Menu-960x540");
        File.WriteAllText(Path.Combine(output,"LucarioPlayerCapture.result.txt"),error?"LUCARIO_PLAYER_FAILED":"LUCARIO_PLAYER_OK: Windows standalone, page-two portrait, four warning/action HUDs, victory, 1280x720 and 960x540, no runtime errors.");
        Application.Quit(error?1:0);
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var screenshot=ScreenCapture.CaptureScreenshotAsTexture();
        if(!screenshot) { error=true; Debug.LogError("Missing screenshot: "+name); yield break; }
        var pixels=screenshot.GetPixels32();
        bool varied=false;
        Color32 first=pixels[0];
        for(int i=0;i<pixels.Length;i+=Mathf.Max(1,pixels.Length/512))
            if(Mathf.Abs(pixels[i].r-first.r)+Mathf.Abs(pixels[i].g-first.g)+Mathf.Abs(pixels[i].b-first.b)>50) { varied=true; break; }
        if(!varied) { error=true; Debug.LogError("Blank screenshot: "+name); }
        else File.WriteAllBytes(Path.Combine(output,name+".png"),screenshot.EncodeToPNG());
        Destroy(screenshot);
        yield return new WaitForSecondsRealtime(.15f);
    }
    private void OnDestroy() { Application.logMessageReceived-=OnLog; }
}
#endif
