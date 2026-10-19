using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArenaSetup
{
    private static Texture2D canvas;
    private static int drawingScale = 1;
    private static readonly Color32 Gold = new Color32(218, 177, 67, 255);
    private static void Box(int x, int y, int w, int h, Color32 c)
    {
        x *= drawingScale; y *= drawingScale; w *= drawingScale; h *= drawingScale;
        for (int j = Mathf.Max(0,y); j < Mathf.Min(canvas.height,y+h); j++)
            for (int i = Mathf.Max(0,x); i < Mathf.Min(canvas.width,x+w); i++) canvas.SetPixel(i,j,c);
    }
    private static void Ring(int cx, int cy, int radius, int thickness, bool leftHalf = false, bool rightHalf = false)
    {
        for (int y = cy-radius-1; y <= cy+radius+1; y++)
            for (int x = cx-radius-1; x <= cx+radius+1; x++)
            {
                if (x < 42 || x > 598 || y < 48 || y > 312 || (leftHalf && x > cx) || (rightHalf && x < cx)) continue;
                float d = Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy));
                if (d <= radius && d >= radius-thickness) Box(x,y,1,1,Gold);
            }
    }
    private static Sprite Save(string name)
    {
        canvas.Apply();
        string path = "Assets/Art/Arena/" + name + ".png";
        File.WriteAllBytes(path, canvas.EncodeToPNG());
        Object.DestroyImmediate(canvas);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 50;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (!sprite) throw new System.InvalidOperationException("Sprite import failed: " + path);
        return sprite;
    }
    private static SpriteRenderer Place(string name, Sprite sprite, Vector3 position, int order)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    [MenuItem("Pokemon/Create Charizard Arena")]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/Art/Arena");
        Directory.CreateDirectory("Assets/Scenes");
        drawingScale = 3;
        canvas = new Texture2D(1920,1080,TextureFormat.RGBA32,false);
        Box(0,0,640,360,new Color32(16,19,24,255));
        Box(33,35,576,280,new Color32(7,9,12,255));
        Box(34,40,572,280,new Color32(65,62,56,255));
        Box(39,45,562,270,new Color32(30,32,36,255));
        var random = new System.Random(41);
        for (int row = 0; row < 66; row++)
            for (int col = -1; col < 81; col++)
            {
                int x = 43 + col*7 + (row%2)*3, y = 49+row*4;
                int start = Mathf.Max(43,x), end = Mathf.Min(598,x+6);
                if (end <= start) continue;
                byte shade = (byte)random.Next(66,86);
                Box(start,y,end-start,3,new Color32(shade, (byte)(shade+2),(byte)(shade+5),255));
                Box(start,y+2,end-start,1,new Color32((byte)(shade+14),(byte)(shade+15),(byte)(shade+16),255));
            }
        Box(42,48,557,3,Gold); Box(42,309,557,3,Gold);
        Box(42,48,3,264,Gold); Box(596,48,3,264,Gold);
        Box(319,49,2,262,Gold);
        Ring(320,180,54,2); Ring(320,180,7,2);
        Ring(43,180,33,2,false,true); Ring(597,180,33,2,true,false);
        var floor = Save("CharizardFloor");
        drawingScale = 1;
        var frames = new Sprite[4];
        for (int f=0; f<4; f++)
        {
            canvas = new Texture2D(24,32,TextureFormat.RGBA32,false);
            Box(0,0,24,32,new Color32(0,0,0,0));
            Box(3,1,18,4,new Color32(24,22,24,210));
            Box(6,3,12,3,new Color32(90,46,30,255));
            Box(7,5,11,10,new Color32(205,61,25,255));
            Box(5+f%2,8,12,9,new Color32(235,88,28,255));
            Box(8,10,8,12,new Color32(255,143,39,255));
            Box(11+f%3-1,19,4,7+f,new Color32(255,154,43,255));
            Box(7,16,3,4+f%2*3,new Color32(247,115,31,255));
            Box(10,6,5,10,new Color32(255,208,75,255));
            Box(11,7,3,5,new Color32(255,242,159,255));
            Box(5+f*3,27+f%3,1,2,new Color32(255,183,71,230));
            frames[f] = Save("Flame"+f);
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Arena Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0,0,-10);
        cam.orthographic = true;
        cam.orthographicSize = 3.65f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(16,19,24,255);
        Place("Grey brick court - gold markings",floor,Vector3.zero,0);
        // Sparse decoration, with clear central circle and crossing routes.
        Vector2[] points = { new Vector2(77,76),new Vector2(171,286),new Vector2(262,83),new Vector2(410,283),new Vector2(558,77),new Vector2(565,282),new Vector2(119,237),new Vector2(478,111),new Vector2(205,131),new Vector2(451,221) };
        for (int i=0;i<points.Length;i++)
        {
            var p = points[i];
            var flame = Place("Decorative fire " + (i+1),frames[i%4],new Vector3((p.x-320)*3/50f,(p.y-180)*3/50f,0),2).gameObject.AddComponent<ArenaFlame>();
            flame.frames=frames; flame.phase=i*.173f;
        }
        new GameObject("Arena navigation").AddComponent<ArenaPreview>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CharizardArena.unity");
        var buildScenes=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach(var path in new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/CharizardArena.unity"})
            if(!buildScenes.Exists(x=>x.path==path)) buildScenes.Add(new EditorBuildSettingsScene(path,true));
        EditorBuildSettings.scenes=buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("ARENA_SETUP_OK: floor, markings, 10 animated fires, scene and build settings.");
        // Render the actual scene as a reviewable image when a graphics device is available.
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Directory.CreateDirectory("Docs/Previews");
            var target = new RenderTexture(1280,720,24);
            cam.targetTexture=target; cam.Render();
            var old = RenderTexture.active; RenderTexture.active=target;
            var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);
            shot.ReadPixels(new Rect(0,0,1280,720),0,0); shot.Apply();
            File.WriteAllBytes("Docs/Previews/CharizardArena.png",shot.EncodeToPNG());
            cam.targetTexture=null; RenderTexture.active=old;
            Object.DestroyImmediate(shot); target.Release(); Object.DestroyImmediate(target);
        }
    }
}
