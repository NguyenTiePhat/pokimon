using UnityEngine;

// Main menu with unrestricted stage selection.
public sealed class MainMenu : MonoBehaviour
{
    private enum Panel { None, Settings, Quit }
    private Panel panel;
    [System.Serializable] private sealed class BossEntry
    {
        public string name, arena, scene, portrait, accent;
        public int frameWidth=40, frameHeight=40;
        [System.NonSerialized] public Texture2D texture;
        [System.NonSerialized] public Color color;
    }
    [System.Serializable] private sealed class BossCatalog { public BossEntry[] bosses; }
    private BossEntry[] bosses=new BossEntry[0];
    private const int PageSize=4;
    private int page;
    private float volume = 0.8f;
    private readonly Color ink = new Color32(12, 23, 34, 255);
    private readonly Color gold = new Color32(255, 207, 106, 255);
    private readonly Color muted = new Color32(151, 171, 183, 255);

    private void Awake()
    {
        var catalog=Resources.Load<TextAsset>("BossCatalog");
        if(catalog) bosses=JsonUtility.FromJson<BossCatalog>(catalog.text)?.bosses ?? new BossEntry[0];
        foreach(var boss in bosses)
        {
            boss.texture=Resources.Load<Texture2D>(boss.portrait);
            if(boss.texture) boss.texture.filterMode=FilterMode.Point;
            if(!ColorUtility.TryParseHtmlString(boss.accent,out boss.color)) boss.color=gold;
        }
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
        GUI.enabled = panel == Panel.None;
        if (Button(new Rect(984, 32, 220, 44), "KẾT NỐI LAN / 1v1", true)) LanDuelMenu.Open(this);
        GUI.enabled = true;
        Text(new Rect(76, 110, 680, 60), "CHỌN BOSS", 42, Color.white);
        Text(new Rect(78, 176, 850, 30), "Chọn đối thủ và bước vào đấu trường. Tất cả các màn đều mở sẵn.", 18, muted);
        Text(new Rect(960, 132, 245, 30), bosses.Length + " ĐỐI THỦ", 16, gold, TextAnchor.MiddleRight);
        GUI.enabled=panel==Panel.None;
        int pages=Mathf.Max(1,Mathf.CeilToInt(bosses.Length/(float)PageSize));
        page=Mathf.Clamp(page,0,pages-1);
        for(int slot=0;slot<PageSize;slot++)
        {
            int index=page*PageSize+slot;
            if(index>=bosses.Length) break;
            ArenaCard(76+slot*284,226,index,bosses[index]);
        }
        if(bosses.Length==0) Text(new Rect(76,300,1000,80),"Chưa có đấu trường nào. Hãy quay lại sau.",22,muted);
        if(pages>1)
        {
            GUI.enabled=panel==Panel.None && page>0;
            if(Button(new Rect(76,602,160,40),"← TRƯỚC")) page--;
            GUI.enabled=panel==Panel.None && page<pages-1;
            if(Button(new Rect(1048,602,160,40),"TIẾP →")) page++;
            GUI.enabled=panel==Panel.None;
            Text(new Rect(460,605,360,32),"TRANG " +(page+1)+" / "+pages,15,muted,TextAnchor.MiddleCenter);
        }
        Fill(new Rect(48,656,1184,1),new Color32(47,65,78,255));
        Text(new Rect(76,674,700,26),"WASD  Di chuyển     •     Chuột  Ngắm chiêu     •     Space  Lướt",13,muted);
        if(Button(new Rect(880,668,154,36),"CÀI ĐẶT")) panel=Panel.Settings;
        if(Button(new Rect(1050,668,154,36),"THOÁT")) panel=Panel.Quit;
        GUI.enabled=true;
        if (panel != Panel.None) DrawPanel();
        GUI.matrix = previous;
    }

    private void ArenaCard(float x, float y, int index, BossEntry boss)
    {
        Color accent=boss.color; Texture2D sprite=boss.texture;
        Fill(new Rect(x + 8, y + 8, 244, 355), new Color(0, 0, 0, .3f));
        Fill(new Rect(x, y, 244, 355), new Color32(24, 39, 51, 255));
        Fill(new Rect(x, y, 244, 4), accent);
        Text(new Rect(x + 20, y + 18, 200, 24), "BOSS / " + (index+1).ToString("00"), 13, accent);
        for (int i = 0; i < 7; i++)
            Fill(new Rect(x + 20 + i * 29, y + 175 - (i % 3) * 13, 24, 46 + (i % 3) * 13), new Color(accent.r, accent.g, accent.b, .13f));
        Fill(new Rect(x + 55, y + 202, 134, 12), new Color(0, 0, 0, .3f));
        if (sprite)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 1.6f + x) * 3;
            float frameHeight = Mathf.Max(1,boss.frameHeight), frameWidth=Mathf.Max(1,boss.frameWidth);
            // First downward-facing frame; preserve the source pixel aspect ratio.
            float portraitHeight=Mathf.Min(150,frameHeight*120/frameWidth);
            float portraitWidth=portraitHeight*frameWidth/frameHeight;
            GUI.DrawTextureWithTexCoords(new Rect(x + 122 - portraitWidth/2, y + 65 + bob, portraitWidth, portraitHeight), sprite,
                new Rect(0, 1 - frameHeight / sprite.height, frameWidth / sprite.width, frameHeight / sprite.height));
        }
        Text(new Rect(x + 20, y + 232, 215, 35), boss.name, 24, Color.white);
        Text(new Rect(x + 20, y + 273, 220, 23), boss.arena, 12, accent);
        bool enabled=GUI.enabled;
        bool available=!string.IsNullOrEmpty(boss.scene) && Application.CanStreamedLevelBeLoaded(boss.scene);
        GUI.enabled=enabled && available;
        if(Button(new Rect(x+16,y+303,212,38),available?"VÀO ĐẤU TRƯỜNG →":"SẮP RA MẮT",true)) ArenaProgress.Enter(boss.scene);
        GUI.enabled=enabled;
    }

    private void DrawPanel()
    {
        Fill(new Rect(0, 0, 1280, 720), new Color(0, 0, 0, .8f));
        Fill(new Rect(380, 206, 520, 310), new Color32(24, 39, 51, 255));
        Fill(new Rect(380, 206, 520, 4), gold);
        string title = panel == Panel.Settings ? "CÀI ĐẶT" : "RỜI ĐẤU TRƯỜNG?";
        Text(new Rect(410, 233, 460, 42), title, 24, gold);
        if (panel == Panel.Settings)
        {
            Text(new Rect(410, 295, 450, 35), "Âm lượng tổng: " + Mathf.RoundToInt(volume * 100) + "%", 18, Color.white);
            float next = GUI.HorizontalSlider(new Rect(410, 345, 460, 25), volume, 0, 1);
            if (!Mathf.Approximately(next, volume)) { volume = next; AudioListener.volume = volume; PlayerPrefs.SetFloat("Menu.Volume", volume); }
            Text(new Rect(410, 380, 460, 35), "Âm lượng hiệu ứng • Màn Lucario có âm báo chiêu.", 14, muted);
        }
        else Text(new Rect(410, 296, 460, 120), "Bạn có muốn thoát game không?", 18, Color.white);
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
