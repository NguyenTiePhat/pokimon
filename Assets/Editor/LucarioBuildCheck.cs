using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

public static class LucarioBuildCheck
{
    public static void Run()
    {
        LucarioSetup.RunAll();
        // Isolate legacy checks because some create their test actors in the active scene.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        PikachuCombatCheck.Run();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        PikachuHitCheck.Run();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SpellClashCheck.Run();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        CharizardBossCheck.Run();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        BlastoiseBossCheck.Run();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        VenusaurSetup.Check();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        GengarSetup.Check();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        BossTempoCheck.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        BuildOnly();
    }
    public static void BuildOnly()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray(),
            locationPathName="Builds/LucarioQA/PokemonBossArena.exe",
            target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.Development,
            extraScriptingDefines=new[]{"LUCARIO_CAPTURE"}
        });
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Lucario Windows build failed: "+report.summary.result);
        UnityEngine.Debug.Log("LUCARIO_BUILD_OK: Windows x64, all six scenes, "+report.summary.totalSize+" bytes.");
    }
    public static void CheckLucarioAndBuild()
    {
        LucarioSetup.RunAll();
        BuildOnly();
    }
}
