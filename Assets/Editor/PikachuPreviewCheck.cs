using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PikachuPreviewCheck
{
    [MenuItem("Pokemon/Preview Pikachu Entrance")]
    public static void Run()
    {
        // Use an additive scene so previewing never replaces or saves the user's open scene.
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CharizardArena.unity", OpenSceneMode.Additive);
        try
        {
            Camera camera = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<Camera>(out var found)) camera = found;
            var obj = new GameObject("Pikachu preview");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, scene);
            var player = obj.AddComponent<PikachuPlayer>();
            player.Initialize(camera);
            CheckMovement(player);
            player.ReplayEntrance();
            Directory.CreateDirectory("Docs/Previews");
            float[] moments = { 1.15f, 2.58f, 2.84f, 3.7f };
            string[] names = { "Pikachu-Running", "Pikachu-Charging", "Pikachu-Lightning", "Pikachu-Ready" };
            for (int i = 0; i < moments.Length; i++)
            {
                player.SampleEntrance(moments[i]);
                if (i == 0 && player.transform.position.x >= 0) throw new System.Exception("Entrance must approach from the left.");
                if (i > 0 && player.transform.position != Vector3.zero) throw new System.Exception("Pikachu must reach the center.");
                if (i == 3 && player.IsEntering) throw new System.Exception("Entrance must release controls.");
                var target = new RenderTexture(1280, 720, 24);
                var previous = RenderTexture.active;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var shot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                shot.Apply();
                File.WriteAllBytes("Docs/Previews/" + names[i] + ".png", shot.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(shot);
                target.Release();
                Object.DestroyImmediate(target);
            }
            player.ReplayEntrance();
            if (!player.IsEntering || player.transform.position.x >= 0) throw new System.Exception("Replay did not restart entrance.");
            // A short image sequence makes timing and direction reviewable, not just a single pose.
            Directory.CreateDirectory("Docs/Previews/PikachuSequence");
            for (int frame = 0; frame < 96; frame++)
            {
                float seconds = frame / 24f;
                player.SampleEntrance(seconds);
                Capture(camera, "Docs/Previews/PikachuSequence/" + frame.ToString("D3") + ".png", 640, 360);
            }
            Debug.Log("PIKACHU_CHECK_OK: eight directions, equal diagonal speed, frame-rate independence, stop/facing, boundary, blocked intro input, monotonic entrance, centered arrival, effect cleanup, replay, four previews and animation sequence.");
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.Exception("Pikachu regression: " + message);
    }

    private static void CheckMovement(PikachuPlayer player)
    {
        Vector3 start = player.transform.position;
        player.TickMovement(Vector2.right, 1);
        Require(player.transform.position == start, "movement allowed during intro");
        float previousX = start.x;
        for (int frame = 0; frame <= 240; frame++)
        {
            player.SampleEntrance(frame / 100f);
            Require(player.transform.position.x >= previousX - .00001f && player.transform.position.x <= 0, "entrance reverses or overshoots");
            previousX = player.transform.position.x;
        }
        Require(player.transform.position == Vector3.zero, "arrival misses center");
        player.SampleEntrance(PikachuPlayer.EntranceDuration);
        Require(!player.IsEntering && player.FacingDirection == 2, "intro changed facing on handoff");
        Require(!player.GetComponent<PikachuElectricity>().HasVisibleEffect, "electricity persists after intro");
        // Ground truth from the enlarged source sheet, not the movement implementation.
        Vector2[] inputs = { Vector2.down, new Vector2(1, -1), Vector2.right, Vector2.one, Vector2.up, new Vector2(-1, 1), Vector2.left, new Vector2(-1, -1) };
        for (int i = 0; i < inputs.Length; i++)
        {
            player.transform.position = Vector3.zero;
            player.TickMovement(inputs[i], .25f);
            Require(player.FacingDirection == i, "wrong facing for " + inputs[i]);
            Require(Vector3.Distance(player.transform.position, (Vector3)inputs[i].normalized * .8f) < .0001f, "wrong direction/speed");
            Require(player.CurrentSprite.name.StartsWith("Walk direction " + i + " "), "wrong sheet row");
            Vector3 stopped = player.transform.position;
            player.TickMovement(Vector2.zero, 1);
            Require(player.transform.position == stopped && !player.IsMoving && player.FacingDirection == i, "release must stop and retain facing");
            Require(player.CurrentSprite.name == "Idle direction " + i + " frame 0", "idle is not grounded");
        }
        player.transform.position = Vector3.zero;
        for (int i = 0; i < 30; i++) player.TickMovement(Vector2.right, 1f / 30);
        var at30 = player.transform.position;
        player.transform.position = Vector3.zero;
        for (int i = 0; i < 144; i++) player.TickMovement(Vector2.right, 1f / 144);
        Require(Vector3.Distance(at30, player.transform.position) < .0001f, "frame-rate-dependent movement");
        player.transform.position = (Vector3)PikachuPlayer.MovementBounds;
        player.TickMovement(Vector2.one, .2f);
        Require(player.transform.position == (Vector3)PikachuPlayer.MovementBounds && !player.IsMoving, "walking through or in place at boundary");
        player.ReplayEntrance();
        Require(!player.GetComponent<PikachuElectricity>().HasVisibleEffect, "replay retains stale effects");
    }

    private static void Capture(Camera camera, string path, int width, int height)
    {
        var target = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active;
        var previousTarget = camera.targetTexture;
        var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();
            File.WriteAllBytes(path, shot.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            Object.DestroyImmediate(shot);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }
}
