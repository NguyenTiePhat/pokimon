using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WaterTempleSetup
{
    public static void CreateAndCheck()
    {
        Create();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        BlastoiseBossCheck.Run();
    }
    [MenuItem("Pokemon/Create Blastoise Water Temple")]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/Art/Arena");
        var root=new GameObject("Water temple art generator");
        var s=new CombatPixelSurface("Generated floor",root.transform,1024,640,24,0);
        var dark=new Color32(27,59,75,255); var pale=new Color32(144,179,180,255);
        s.Clear();
        for(int y=0;y<640;y++) for(int x=0;x<1024;x++)
        {
            float r=Vector2.Distance(new Vector2(x,y),new Vector2(512,320));
            int noise=((x/4)*13+(y/4)*7)%11;
            Color32 c=new Color32((byte)(20+noise),(byte)(70+noise),(byte)(96+noise*2),255);
            if(r<224)
            {
                int tile=((x/25)*17+(y/19)*13)%17;
                c=new Color32((byte)(87+tile),(byte)(119+tile),(byte)(126+tile),255);
                if(x%25<2 || y%19<2) c=new Color32(53,83,96,255);
                else if(y%19==2) c=new Color32(137,166,170,255);
            }
            s.Dot(x,y,c);
        }
        s.Ellipse(512,320,231,231,new Color32(16,45,65,255),false,7);
        s.Ellipse(512,320,225,225,pale,false,5);
        s.Ellipse(512,320,216,216,new Color32(47,89,100,255),false,3);
        s.Ellipse(512,320,209,209,new Color32(82,189,193,255),false,2);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6; Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            Vector2 start=new Vector2(512,320)+d*170,end=new Vector2(512,320)+d*222;
            s.Line(start,end,new Color32(31,77,98,255),7);
            s.Line(start,end,new Color32(77,176,197,255),3);
            Vector2 bridge=new Vector2(512,320)+d*190,side=new Vector2(-d.y,d.x);
            s.Line(bridge-side*9,bridge+side*9,pale,6);
        }
        s.Ellipse(512,320,47,47,new Color32(68,112,127,255),false,2);
        s.Ellipse(512,320,42,42,new Color32(125,170,176,255),false,1);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4; Vector2 p=new Vector2(512,320)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*37;
            s.Line(p,p+new Vector2(Mathf.Cos(a+.9f),Mathf.Sin(a+.9f))*8,new Color32(61,143,161,255),2);
        }
        // Ancient shrine and stairs stay beyond the playable northern rim.
        for(int i=0;i<5;i++) { s.Box(422+i*5,535+i*9,180-i*10,9,dark); s.Box(424+i*5,538+i*9,176-i*10,5,pale); }
        s.Box(445,580,134,43,new Color32(37,70,82,255));
        s.Box(457,582,110,35,new Color32(75,110,123,255));
        s.Box(494,582,36,35,new Color32(11,37,56,255));
        for(int i=0;i<4;i++) { int x=450+i*39; s.Box(x,579,12,43,pale); s.Box(x+3,581,3,39,new Color32(75,124,137,255)); }
        for(int y=0;y<17;y++) s.Box(432+y*3,622+y,160-y*6,1,new Color32(69,132,145,255));
        s.Box(430,619,164,5,new Color32(159,191,183,255));
        // Broken columns in the pools, never in the central dodge lanes.
        for(int i=0;i<6;i++)
        {
            float a=(30+i*60)*Mathf.Deg2Rad; int x=512+Mathf.RoundToInt(Mathf.Cos(a)*252),y=320+Mathf.RoundToInt(Mathf.Sin(a)*252);
            s.Ellipse(x,y-4,28,10,new Color32(12,42,61,170));
            s.Box(x-20,y-1,40,9,dark); s.Box(x-17,y+2,34,6,pale);
            int h=22+(i%3)*13; s.Box(x-11,y+8,22,h,new Color32(99,145,156,255));
            s.Box(x-8,y+9,4,h-2,pale); s.Box(x+4,y+8,3,h-1,new Color32(49,96,116,255));
            s.Line(new Vector2(x-11,y+h+8),new Vector2(x+10,y+h+1),pale,4);
            s.Box(x+20,y+5,9,5,new Color32(108,151,160,255));
        }
        s.Apply(); string path="Assets/Art/Arena/BlastoiseWaterTemple.png";
        File.WriteAllBytes(path,s.Renderer.sprite.texture.EncodeToPNG()); s.Dispose(); Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.spritePixelsPerUnit=24; importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false; importer.npotScale=TextureImporterNPOTScale.None; importer.SaveAndReimport();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Water Temple Camera").AddComponent<Camera>(); camera.tag="MainCamera";
        camera.transform.position=new Vector3(0,0,-10); camera.orthographic=true; camera.orthographicSize=3.65f; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(20,64,91,255);
        var floor=new GameObject("Circular stone court, shallow channels, temple and ruined columns").AddComponent<SpriteRenderer>(); floor.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(!floor.sprite) throw new System.InvalidOperationException("Water temple floor sprite import failed");
        new GameObject("Water reflections and marked fountains").AddComponent<WaterTempleEffects>();
        new GameObject("Water arena controller").AddComponent<ArenaPreview>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/BlastoiseArena.unity");
        var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if(!scenes.Exists(x=>x.path=="Assets/Scenes/BlastoiseArena.unity")) scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/BlastoiseArena.unity",true));
        EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets();
        camera.orthographicSize=13.5f; camera.aspect=16f/9; Capture(camera,"Blastoise-Temple-Overview");
        camera.orthographicSize=3.65f;
        Debug.Log("WATER_TEMPLE_SETUP_OK: circular arena, shrine, channels, columns, scene and build registration.");
    }
    public static void Capture(Camera camera,string name)
    {
        Directory.CreateDirectory("Docs/Previews"); var rt=new RenderTexture(1280,720,24); var prior=RenderTexture.active; var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply(); File.WriteAllBytes("Docs/Previews/"+name+".png",image.EncodeToPNG()); }
        finally { camera.targetTexture=null; RenderTexture.active=prior; Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
