using UnityEngine;

// Deliberately self-contained menu prototype. No combat or progression is implemented.
public sealed class MainMenu : MonoBehaviour
{
    private enum Panel { None, Start, Settings, Quit }
    private Panel panel;
    private Texture2D fire, water;
    private float volume = 0.8f;
    private readonly Color ink = new Color32(12, 23, 34, 255);
    private readonly Color gold = new Color32(255, 207, 106, 255);
    private readonly Color muted = new Color32(151, 171, 183, 255);

    private void Awake()
    {
        fire = Resources.Load<Texture2D>("Charizard");
        water = Resources.Load<Texture2D>("Blastoise");
        if (fire) fire.filterMode = FilterMode.Point;
        if (water) water.filterMode = FilterMode.Point;
        volume = PlayerPrefs.GetFloat("Menu.Volume", 0.8f);
        AudioListener.volume = volume;
    }

    private void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void Text(Rect r, string value, int size, Color color, TextAnchor align = TextAnchor.UpperLeft)
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = align, wordWrap = true };
        style.normal.textColor = color;
        GUI.Label(r, value, style);
    }

    private bool Button(Rect r, string label, bool primary = false)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        Fill(new Rect(r.x, r.y + 5, r.width, r.height), new Color(0, 0, 0, 0.35f));
        Fill(r, primary ? (hover ? Color.white : gold) : (hover ? new Color32(49, 72, 88, 255) : new Color32(29, 46, 61, 255)));
        if (!primary) Fill(new Rect(r.x, r.y, 3, r.height), muted);
        Text(r, label, 19, primary ? ink : Color.white, TextAnchor.MiddleCenter);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    private void OnGUI()
    {
        var previous = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2), Quaternion.identity, new Vector3(scale, scale, 1));
        Fill(new Rect(0, 0, 1280, 720), ink);
        for (int y = 0; y < 720; y += 48)
            for (int x = 0; x < 1280; x += 64)
                Fill(new Rect(x + 2, y + 2, 60, 44), new Color(0.08f, 0.13f, 0.17f, ((x + y) % 3 == 0) ? .65f : .3f));
        Fill(new Rect(48, 40, 26, 26), gold);
        Fill(new Rect(56, 48, 10, 10), ink);
        Text(new Rect(90, 42, 400, 28), "POKÉMON  /  BOSS ARENA", 17, Color.white);
        Text(new Rect(1050, 44, 180, 25), "OFFLINE • SOLO", 13, muted, TextAnchor.UpperRight);
        Text(new Rect(76, 159, 470, 25), "HAI THỬ THÁCH. MỘT HÀNH TRÌNH.", 14, gold);
        Text(new Rect(72, 197, 510, 90), "BOSS ARENA", 58, Color.white);
        Fill(new Rect(78, 294, 54, 4), gold);
        Text(new Rect(76, 325, 430, 66), "Bước vào đấu trường. Đọc đòn tấn công.\nNé đúng lúc và làm chủ trận đấu.", 19, muted);
        GUI.enabled = panel == Panel.None;
        if (Button(new Rect(76, 420, 350, 58), "BẮT ĐẦU     →", true)) UnityEngine.SceneManagement.SceneManager.LoadScene("CharizardArena");
        if (Button(new Rect(76, 497, 168, 48), "CÀI ĐẶT")) panel = Panel.Settings;
        if (Button(new Rect(258, 497, 168, 48), "THOÁT")) panel = Panel.Quit;
        GUI.enabled = true;
        Text(new Rect(76, 578, 430, 25), "WASD  Di chuyển     •     Chuột  Ngắm chiêu", 13, muted);
        ArenaCard(638, 160, "01", "CHARIZARD", "ĐẤU TRƯỜNG LỬA", new Color32(220, 113, 68, 255), fire, false);
        ArenaCard(908, 238, "02", "BLASTOISE", "ĐẤU TRƯỜNG NƯỚC", new Color32(78, 174, 205, 255), water, true);
        Fill(new Rect(48, 656, 1184, 1), new Color32(47, 65, 78, 255));
        Text(new Rect(76, 674, 650, 24), "POKÉMON BOSS ARENA     /     BẢN THỬ MÀN HÌNH CHÍNH", 11, muted);
        Text(new Rect(1020, 674, 185, 24), "MENU PROTOTYPE  0.1", 11, muted, TextAnchor.UpperRight);
        if (panel != Panel.None) DrawPanel();
        GUI.matrix = previous;
    }

    private void ArenaCard(float x, float y, string number, string name, string subtitle, Color accent, Texture2D sprite, bool locked)
    {
        Fill(new Rect(x + 8, y + 8, 244, 335), new Color(0, 0, 0, .3f));
        Fill(new Rect(x, y, 244, 335), new Color32(24, 39, 51, 255));
        Fill(new Rect(x, y, 244, 4), accent);
        Text(new Rect(x + 20, y + 18, 200, 24), "BOSS / " + number, 13, accent);
        for (int i = 0; i < 7; i++)
            Fill(new Rect(x + 20 + i * 29, y + 175 - (i % 3) * 13, 24, 46 + (i % 3) * 13), new Color(accent.r, accent.g, accent.b, .13f));
        Fill(new Rect(x + 55, y + 202, 134, 12), new Color(0, 0, 0, .3f));
        if (sprite)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 1.6f + x) * 3;
            float frameHeight = sprite == fire ? 48 : 40;
            // First downward-facing frame; preserve the source pixel aspect ratio.
            GUI.DrawTextureWithTexCoords(new Rect(x + 62, y + 65 + bob, 120, frameHeight * 3), sprite,
                new Rect(0, 1 - frameHeight / sprite.height, 40f / sprite.width, frameHeight / sprite.height));
        }
        Text(new Rect(x + 20, y + 232, 215, 35), name, 24, Color.white);
        Text(new Rect(x + 20, y + 273, 220, 23), subtitle, 12, accent);
        Text(new Rect(x + 20, y + 305, 220, 22), locked ? "KHÓA • Thắng Charizard để mở" : "MÀN 01 • Thử thách đầu tiên", 11, muted);
    }

    private void DrawPanel()
    {
        Fill(new Rect(0, 0, 1280, 720), new Color(0, 0, 0, .8f));
        Fill(new Rect(380, 206, 520, 310), new Color32(24, 39, 51, 255));
        Fill(new Rect(380, 206, 520, 4), gold);
        string title = panel == Panel.Settings ? "CÀI ĐẶT" : panel == Panel.Quit ? "RỜI ĐẤU TRƯỜNG?" : "HÀNH TRÌNH SẮP BẮT ĐẦU";
        Text(new Rect(410, 233, 460, 42), title, 24, gold);
        if (panel == Panel.Settings)
        {
            Text(new Rect(410, 295, 450, 35), "Âm lượng tổng: " + Mathf.RoundToInt(volume * 100) + "%", 18, Color.white);
            float next = GUI.HorizontalSlider(new Rect(410, 345, 460, 25), volume, 0, 1);
            if (!Mathf.Approximately(next, volume)) { volume = next; AudioListener.volume = volume; PlayerPrefs.SetFloat("Menu.Volume", volume); }
            Text(new Rect(410, 380, 460, 35), "Bản thử hiện chưa có nhạc và âm thanh.", 14, muted);
        }
        else Text(new Rect(410, 296, 460, 120), panel == Panel.Quit ? "Bạn có muốn thoát game không?" : "Màn chiến đấu chưa được triển khai.\n\nHành trình dự kiến: Charizard → Blastoise.", 18, Color.white);
        if (Button(new Rect(410, 447, 215, 44), "QUAY LẠI")) { PlayerPrefs.Save(); panel = Panel.None; }
        if (panel == Panel.Quit && Button(new Rect(655, 447, 215, 44), "THOÁT GAME", true))
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) { panel = Panel.None; Event.current.Use(); }
    }
}
