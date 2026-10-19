using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class MenuSetup
{
    [MenuItem("Pokemon/Create Main Menu Scene")]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(10, 19, 29, 255);
        new GameObject("Main Menu").AddComponent<MainMenu>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        var buildScenes=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if(!buildScenes.Exists(x=>x.path=="Assets/Scenes/MainMenu.unity")) buildScenes.Insert(0,new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",true));
        EditorBuildSettings.scenes=buildScenes.ToArray();
        PlayerSettings.productName = "Pokemon Boss Arena";
        PlayerSettings.companyName = "PokemonPrototype";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        AssetDatabase.SaveAssets();
        Debug.Log("MENU_SETUP_OK");
    }
}
