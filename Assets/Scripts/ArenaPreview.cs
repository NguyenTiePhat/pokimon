using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ArenaPreview : MonoBehaviour
{
    public static bool TrainingMode;
    [SerializeField] private Vector2 mapHalfSize = new Vector2(19.2f, 10.8f);
    [SerializeField] private float viewHeight = 3.65f;
    private bool dragging;
    private PikachuPlayer player;
    private PikachuCombat combat;

    private void Awake()
    {
        var cam = Camera.main;
        if (cam) cam.orthographicSize = viewHeight;
        player = new GameObject("Player - Pikachu").AddComponent<PikachuPlayer>();
        player.Initialize(cam);
        TrainingDummy dummy;
        if (TrainingMode)
        {
            dummy = new GameObject("Training dummy").AddComponent<TrainingDummy>();
            dummy.Initialize(new Vector2(3, 0));
        }
        else if(ArenaProgress.IsDojo) dummy = new GameObject("Boss - Lucario").AddComponent<LucarioBoss>();
        else if(ArenaProgress.IsHaunted) dummy = new GameObject("Boss - Gengar").AddComponent<GengarBoss>();
        else if(ArenaProgress.IsGarden) dummy = new GameObject("Boss - Venusaur").AddComponent<VenusaurBoss>();
        else if(ArenaProgress.IsWater) dummy = new GameObject("Boss - Blastoise").AddComponent<BlastoiseBoss>();
        else dummy = new GameObject("Boss - Charizard").AddComponent<CharizardBoss>();
        combat = player.gameObject.AddComponent<PikachuCombat>();
        combat.Initialize(player, dummy);
        if (dummy is ArenaBoss boss) boss.InitializeBoss(combat, new Vector2(4, 0));
        gameObject.AddComponent<CombatHud>().Initialize(combat);
    }

    private void OnApplicationFocus(bool focused) { if (!focused) dragging = false; }
    private void OnDisable() { dragging = false; }

    private void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam) return;
        cam.orthographicSize = viewHeight;
        // Follow the ground position every frame, including dashes and Iron Tail leaps.
        // Holding middle mouse temporarily lets the player look around.
        if (player && !player.IsEntering && !dragging)
        {
            var p = cam.transform.position;
            var target = player.transform.position;
            p.x = target.x;
            p.y = target.y;
            cam.transform.position = p;
        }
        ClampCamera(cam);
    }

    private void ClampCamera(Camera cam)
    {
        var limitX = Mathf.Max(0, mapHalfSize.x - cam.orthographicSize * cam.aspect);
        var limitY = Mathf.Max(0, mapHalfSize.y - cam.orthographicSize);
        var p = cam.transform.position;
        cam.transform.position = new Vector3(Mathf.Clamp(p.x, -limitX, limitX), Mathf.Clamp(p.y, -limitY, limitY), p.z);
    }

    private void OnGUI()
    {
        var e = Event.current;
        bool entering = player && player.IsEntering;
        if (entering) dragging = false;
        if (!entering && e.type == EventType.MouseDown && e.button == 2) { dragging = true; e.Use(); }
        if (e.type == EventType.MouseUp && e.button == 2) { dragging = false; e.Use(); }
        if (e.type == EventType.MouseDrag && e.button == 2 && dragging)
        {
            var cam = Camera.main;
            if (cam)
            {
                // GUI Y points down. Move the camera opposite the drag so the floor follows the cursor.
                float unitsPerPixel = cam.orthographicSize * 2 / cam.pixelHeight;
                cam.transform.position += new Vector3(-e.delta.x, e.delta.y, 0) * unitsPerPixel;
                ClampCamera(cam);
            }
            e.Use();
        }
        var label = new GUIStyle(GUI.skin.label) { fontSize = 18 };
        label.normal.textColor = new Color32(241, 196, 90, 255);
        GUI.Label(new Rect(24, 16, 500, 30), ArenaProgress.IsDojo?"MÀN 05 / VÕ ĐƯỜNG TRÊN NÚI":ArenaProgress.IsHaunted?"MÀN 04 / SÂN PHẾ TÍCH MA ÁM":ArenaProgress.IsGarden?"MÀN 03 / KHU VƯỜN CỔ":ArenaProgress.IsWater?"MÀN 02 / ĐỀN NƯỚC NGẬP":"MÀN 01 / ĐẤU TRƯỜNG CHARIZARD", label);
        GUI.enabled = !entering && (!combat || !combat.MovementLocked);
        if (GUI.Button(new Rect(Screen.width - 310, 16, 140, 32), "Xem lại xuất hiện") && player) player.ReplayEntrance();
        GUI.enabled = true;
        if (GUI.Button(new Rect(Screen.width - 158, 16, 134, 32), "Về màn hình chính"))
            SceneManager.LoadScene("MainMenu");
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            SceneManager.LoadScene("MainMenu");
    }
}
