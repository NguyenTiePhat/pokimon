using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

// Self-contained LAN lobby and equal-stat duel; existing solo scenes stay untouched.
public sealed class LanDuelMenu : MonoBehaviour
{
    MainMenu menu;
    readonly LanTransport transport = new LanTransport();
    LanDuelRules.State state = new LanDuelRules.State();
    LanDuelRules.Command local = new LanDuelRules.Command();
    readonly LanDuelRules.Command[] inputs = { new LanDuelRules.Command(), new LanDuelRules.Command() };
    readonly string[] names = { "PIKACHU", "CHARIZARD", "BLASTOISE", "VENUSAUR", "GENGAR", "LUCARIO" };
    readonly string[] types = { "ĐIỆN", "LỬA", "NƯỚC", "CỎ", "MA", "GIÁC ĐẤU" };
    readonly string[] folders = { "Pikachu", "CharizardBoss", "BlastoiseBoss", "VenusaurBoss", "GengarBoss", "LucarioBoss" };
    readonly string[] maps = { "ĐẤU TRƯỜNG LỬA", "ĐỀN NƯỚC", "KHU VƯỜN CỔ", "PHẾ TÍCH MA ÁM", "VÕ ĐƯỜNG TRÊN NÚI" };
    readonly Color[] accents = { new Color32(255,207,106,255), new Color32(232,119,79,255), new Color32(89,195,222,255), new Color32(140,203,113,255), new Color32(180,143,236,255), new Color32(104,207,233,255) };
    readonly Texture2D[] portraits = new Texture2D[6];
    readonly Vector2[] frames = new Vector2[6];
    readonly Dictionary<int,GUIStyle> styles = new Dictionary<int,GUIStyle>();
    readonly Color ink = new Color32(12,23,34,255), panel = new Color32(24,39,51,255), muted = new Color32(151,171,183,255);
    string address = "192.168.1.10", localAddresses, status = "Tạo phòng hoặc nhập IP của máy chủ để kết nối.";
    bool host, wasConnected, previousRunInBackground;
    float sendTimer, accumulator, lastInput;
    int Me => host ? 0 : 1;
    Color Gold => accents[0];
    public static void Open(MainMenu owner)
    {
        owner.enabled = false;
        var screen = owner.gameObject.AddComponent<LanDuelMenu>(); screen.menu = owner;
    }
    void Awake()
    {
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        localAddresses = LanTransport.LocalAddresses();
        for (int i=0;i<6;i++)
        {
            portraits[i] = Resources.Load<Texture2D>(folders[i]+"/Idle-Anim");
            var xml = Resources.Load<TextAsset>(folders[i]+"/AnimData");
            if (!xml) continue;
            foreach (var a in XDocument.Parse(xml.text).Root.Element("Anims").Elements("Anim"))
                if ((string)a.Element("Name")=="Idle") frames[i]=new Vector2((int)a.Element("FrameWidth"),(int)a.Element("FrameHeight"));
        }
    }
    void OnDestroy() { transport.Dispose(); Application.runInBackground = previousRunInBackground; if (menu) menu.enabled = true; }
    void OnApplicationQuit() { transport.Dispose(); }
    void ResetSession(bool isHost)
    {
        host=isHost; state=new LanDuelRules.State(); local=new LanDuelRules.Command();
        inputs[0]=new LanDuelRules.Command(); inputs[1]=new LanDuelRules.Command();
        wasConnected=false; accumulator=sendTimer=0;
    }
    void Receive(string message)
    {
        if (host)
        {
            var c=JsonUtility.FromJson<LanDuelRules.Command>(message);
            if (!LanDuelRules.Valid(c)) throw new InvalidOperationException("Sai phiên bản hoặc dữ liệu.");
            inputs[1]=c; lastInput=Time.unscaledTime;
            if (state.step<2 && c.step==state.step && c.map==state.map)
            { state.players[1].pokemon=c.pokemon; state.players[1].ready=c.ready; }
        }
        else
        {
            var next=JsonUtility.FromJson<LanDuelRules.State>(message);
            if (next==null || next.version!=LanDuelRules.Version || next.players==null || next.players.Length!=2
                || next.map<0 || next.map>=5 || next.step<0 || next.step>3 || next.shots==null || next.shots.Count>100)
                throw new InvalidOperationException("Sai phiên bản hoặc dữ liệu.");
            foreach(var p in next.players)
                if(p==null || p.pokemon<0 || p.pokemon>=6 || float.IsNaN(p.x) || float.IsNaN(p.y) || Mathf.Abs(p.x)>10 || Mathf.Abs(p.y)>6)
                    throw new InvalidOperationException("Dữ liệu người chơi không hợp lệ.");
            if(next.step!=state.step || next.map!=state.map)
            {
                local.ready=false;
                status=next.step==0?"Chọn Pokémon và xác nhận sẵn sàng.":next.step==1?"Host chọn map. Cả hai xác nhận lại để vào trận.":next.step==2?"WASD di chuyển • Chuột trái bắn • Space + hướng lướt":"Trận đấu đã kết thúc. Chờ Host mở trận mới.";
            }
            state=next;
        }
    }
    void Update()
    {
        transport.Pump(Receive);
        if (transport.Connected && !wasConnected) { status="Đã kết nối • Cả hai hãy chọn Pokémon."; lastInput=Time.unscaledTime; }
        if (!transport.Connected && wasConnected)
        { state.step=0; local.ready=false; foreach(var p in state.players) p.ready=false; }
        wasConnected=transport.Connected;
        if (transport.Error!=null) status=transport.Error;
        local.step=state.step; local.map=state.map;
        if (state.step==2 && transport.Connected)
        {
            local.x=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            local.y=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
            Vector2 mouse=CanvasMouse(); Vector2 origin=WorldToCanvas(state.players[Me].x,state.players[Me].y);
            Vector2 aim=(new Vector2(mouse.x-origin.x,origin.y-mouse.y)).normalized;
            local.aimX=aim.x; local.aimY=aim.y;
            local.fire=Input.GetMouseButton(0) && mouse.y>170 && mouse.y<610;
            local.dash=Input.GetKey(KeyCode.Space);
        }
        else { local.x=local.y=0; local.fire=local.dash=false; }
        if(host)
        {
            inputs[0]=local;
            if(state.step<2) { state.players[0].pokemon=local.pokemon; state.players[0].ready=local.ready; }
            if(Time.unscaledTime-lastInput>.5f) { inputs[1].x=inputs[1].y=0; inputs[1].fire=inputs[1].dash=false; }
            if(transport.Connected)
            {
                accumulator+=Mathf.Min(Time.unscaledDeltaTime,.1f);
                while(accumulator>=.02f) { LanDuelRules.Tick(state,inputs,.02f); accumulator-=.02f; }
            }
        }
        sendTimer+=Time.unscaledDeltaTime;
        if(transport.Connected && sendTimer>=.05f)
        { sendTimer=0; transport.Send(host?JsonUtility.ToJson(state):JsonUtility.ToJson(local)); }
    }
    Vector2 CanvasMouse()
    {
        float s=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        return new Vector2((Input.mousePosition.x-(Screen.width-1280*s)/2)/s,
            (Screen.height-Input.mousePosition.y-(Screen.height-720*s)/2)/s);
    }
    static Vector2 WorldToCanvas(float x,float y) => new Vector2(640+x*54,390-y*46);
    void Fill(Rect r,Color c) { GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=Color.white; }
    void Label(Rect r,string text,int size,Color color)
    {
        if(!styles.TryGetValue(size,out var style)) { style=new GUIStyle(GUI.skin.label){fontSize=size,wordWrap=true}; styles[size]=style; }
        style.normal.textColor=color; GUI.Label(r,text,style);
    }
    bool Button(Rect r,string text,bool primary=false)
    {
        Color c=primary?Gold:panel;
        if(r.Contains(Event.current.mousePosition)) c=Color.Lerp(c,Color.white,.15f);
        Fill(new Rect(r.x,r.y+4,r.width,r.height),new Color(0,0,0,.3f)); Fill(r,GUI.enabled?c:Color.Lerp(c,ink,.65f));
        Label(new Rect(r.x+14,r.y+9,r.width-20,r.height-10),text,16,primary?ink:Color.white);
        return GUI.Button(r,GUIContent.none,GUIStyle.none);
    }
    void Portrait(int id,Rect r,int direction=0)
    {
        var tex=portraits[id]; var f=frames[id]; if(!tex || f.x<=0 || f.y<=0) return;
        float scale=Mathf.Min(r.width/f.x,r.height/f.y);
        GUI.DrawTextureWithTexCoords(new Rect(r.center.x-f.x*scale/2,r.center.y-f.y*scale/2,f.x*scale,f.y*scale),tex,
            new Rect(0,1-(direction+1)*f.y/tex.height,f.x/tex.width,f.y/tex.height));
    }
    void OnGUI()
    {
        var previous=GUI.matrix; bool enabled=GUI.enabled;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,new Vector3(scale,scale,1));
        Fill(new Rect(0,0,1280,720),ink);
        for(int y=0;y<720;y+=48) for(int x=0;x<1280;x+=64) Fill(new Rect(x+2,y+2,60,44),new Color(.08f,.13f,.17f,.35f));
        Fill(new Rect(40,32,6,38),Gold);
        Label(new Rect(62,30,420,35),"POKÉMON / LINK ARENA",24,Color.white);
        Label(new Rect(64,66,580,28),"ĐẤU ĐỐI KHÁNG 1v1  •  MẠNG LAN",12,muted);
        if(Button(new Rect(1040,32,200,44),"← RỜI PHÒNG")) { Destroy(this); GUI.matrix=previous; GUI.enabled=enabled; return; }
        for(int i=0;i<3;i++)
        {
            bool active=state.step==i || (i==2 && state.step==3);
            Fill(new Rect(40+i*400,110,390,38),active?Gold:panel);
            Label(new Rect(54+i*400,117,370,25),(i+1).ToString("00")+"  /  "+new[]{"CHỌN POKÉMON","CHỌN ĐẤU TRƯỜNG","VÀO TRẬN"}[i],14,active?ink:muted);
        }
        if(state.step>=2) DrawBattle(); else DrawLobby();
        Fill(new Rect(40,654,1200,1),panel);
        Label(new Rect(40,669,890,42),status,14,transport.Error==null?muted:accents[1]);
        Label(new Rect(1015,669,230,35),transport.Connected?(host?"● HOST / P1":"● GUEST / P2"):"○ CHƯA KẾT NỐI",13,transport.Connected?accents[3]:muted);
        GUI.enabled=enabled; GUI.matrix=previous;
    }
    void DrawLobby()
    {
        Fill(new Rect(40,170,290,462),panel);
        Label(new Rect(60,188,250,30),"PHÒNG KẾT NỐI",19,Color.white);
        Label(new Rect(60,231,250,58),"IP máy này:\n"+localAddresses,13,Gold);
        GUI.enabled=!transport.Connected && !transport.Listening;
        if(Button(new Rect(60,301,250,44),"+ TẠO PHÒNG",true))
        { ResetSession(true); transport.Host(); status=transport.Error??"Đang chờ máy thứ hai kết nối vào IP của bạn…"; }
        Label(new Rect(60,366,250,24),"HOẶC NHẬP IP MÁY CHỦ",12,muted);
        var field=new GUIStyle(GUI.skin.textField){fontSize=20,padding=new RectOffset(12,12,10,10)};
        address=GUI.TextField(new Rect(60,397,250,45),address,45,field);
        if(Button(new Rect(60,459,250,44),"KẾT NỐI →"))
        { ResetSession(false); transport.Join(address.Trim()); status=transport.Error??"Đang kết nối…"; }
        GUI.enabled=true;
        if(Button(new Rect(60,522,250,40),"NGẮT / THỬ LẠI")) { transport.Dispose(); ResetSession(false); status="Đã ngắt kết nối. Có thể tạo hoặc vào phòng mới."; }
        Label(new Rect(60,580,245,44),"Cùng mạng Wi-Fi / LAN\nTCP 7777 • 2 người chơi",12,muted);
        if(state.step==0) DrawRoster(); else DrawMaps();
    }
    void DrawRoster()
    {
        Label(new Rect(356,171,870,38),"CHỌN BẠN ĐỒNG HÀNH",27,Color.white);
        Label(new Rect(356,212,870,28),"6 Pokémon • Cùng chỉ số PvP: 100 HP, đạn 10 sát thương, lướt hồi 2,5s",14,muted);
        for(int i=0;i<6;i++)
        {
            var r=new Rect(356+i%3*295,253+i/3*133,280,120);
            bool selected=local.pokemon==i;
            Fill(r,panel); Fill(new Rect(r.x,r.y,selected?5:2,r.height),selected?accents[i]:muted);
            Portrait(i,new Rect(r.x+12,r.y+18,80,84));
            Label(new Rect(r.x+105,r.y+23,170,28),names[i],18,selected?accents[i]:Color.white);
            Label(new Rect(r.x+105,r.y+57,160,25),types[i],12,muted);
            Label(new Rect(r.x+105,r.y+83,160,24),selected?"● ĐANG CHỌN":"CHỌN →",12,accents[i]);
            GUI.enabled=!local.ready;
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)) local.pokemon=i;
            GUI.enabled=true;
        }
        DrawReady(530);
        GUI.enabled=host && transport.Connected && state.players[0].ready && state.players[1].ready;
        if(Button(new Rect(962,583,265,44),"CHỌN MAP →",true)) Advance(1);
        GUI.enabled=true;
    }
    void DrawReady(float y)
    {
        string peer=transport.Connected?names[state.players[1-Me].pokemon]+(state.players[1-Me].ready?" • SẴN SÀNG":" • ĐANG CHỌN"):"ĐANG CHỜ NGƯỜI CHƠI…";
        Label(new Rect(356,y,870,30),"ĐỐI THỦ: "+peer,15,muted);
        GUI.enabled=transport.Connected;
        if(Button(new Rect(356,583,280,44),local.ready?"✓ ĐÃ SẴN SÀNG / HỦY":"SẴN SÀNG",true)) local.ready=!local.ready;
        GUI.enabled=true;
        Label(new Rect(653,587,290,43),host?"Chủ phòng chuyển bước khi cả hai đã sẵn sàng.":"Chờ chủ phòng chuyển bước.",12,muted);
    }
    void DrawMaps()
    {
        Label(new Rect(356,171,870,38),"CHỌN NƠI SO TÀI",27,Color.white);
        Label(new Rect(356,214,870,28),"Host chọn map • Đấu trường PvP riêng, lấy cảm hứng từ các màn boss",14,muted);
        for(int i=0;i<5;i++)
        {
            var r=new Rect(356+i*177,257,164,241); Color c=accents[i+1];
            Fill(r,panel); Fill(new Rect(r.x,r.y,164,4),c);
            // Miniature court with thematic terrain colors and two spawn points.
            Fill(new Rect(r.x+12,r.y+24,140,115),Color.Lerp(ink,c,.2f));
            for(int k=0;k<4;k++) Fill(new Rect(r.x+20+k*32,r.y+35,2,90),new Color(c.r,c.g,c.b,.25f));
            Fill(new Rect(r.x+34,r.y+77,10,10),Gold); Fill(new Rect(r.x+120,r.y+77,10,10),c);
            Label(new Rect(r.x+12,r.y+158,145,55),maps[i],16,Color.white);
            Label(new Rect(r.x+12,r.y+211,145,25),state.map==i?"● ĐÃ CHỌN":"CHỌN MAP",12,state.map==i?Gold:muted);
            GUI.enabled=host;
            if(GUI.Button(r,GUIContent.none,GUIStyle.none) && state.map!=i)
            { state.map=i; local.ready=false; foreach(var p in state.players) p.ready=false; }
            GUI.enabled=true;
        }
        DrawReady(530);
        GUI.enabled=host && transport.Connected && state.players[0].ready && state.players[1].ready;
        if(Button(new Rect(962,583,265,44),"BẮT ĐẦU TRẬN →",true)) Advance(2);
        GUI.enabled=true;
    }
    void Advance(int step)
    {
        state.step=step; local.ready=false;
        foreach(var p in state.players) p.ready=false;
        inputs[1]=new LanDuelRules.Command();
        status=step==2?"WASD di chuyển • Giữ chuột trái bắn • Giữ Space + hướng để lướt":"Cả hai xác nhận sẵn sàng sau khi chọn map.";
    }
    void DrawBattle()
    {
        var accent=accents[state.map+1];
        Fill(new Rect(140,170,1000,440),Color.Lerp(ink,accent,.16f));
        for(int y=178;y<600;y+=32) for(int x=148;x<1132;x+=40)
            Fill(new Rect(x,y,37,29),new Color(accent.r,accent.g,accent.b,.09f));
        Fill(new Rect(146,176,988,3),accent); Fill(new Rect(146,602,988,3),accent);
        Fill(new Rect(146,176,3,429),accent); Fill(new Rect(1131,176,3,429),accent);
        for(int y=180;y<600;y+=22) Fill(new Rect(639,y,2,12),new Color(1,1,1,.2f));
        Label(new Rect(468,175,400,28),maps[state.map]+" / PvP",15,accent);
        foreach(var shot in state.shots)
        {
            Vector2 p=WorldToCanvas(shot.x,shot.y);
            Fill(new Rect(p.x-8,p.y-8,16,16),new Color(1,1,1,.2f));
            Fill(new Rect(p.x-4,p.y-4,8,8),accents[state.players[shot.owner].pokemon]);
        }
        for(int i=0;i<2;i++)
        {
            var f=state.players[i]; Vector2 p=WorldToCanvas(f.x,f.y);
            Fill(new Rect(p.x-24,p.y+22,48,9),new Color(0,0,0,.4f));
            Portrait(f.pokemon,new Rect(p.x-40,p.y-50,80,80),f.facing);
            Label(new Rect(p.x-38,p.y+30,90,23),i==Me?"BẠN":"ĐỐI THỦ",12,i==Me?Gold:Color.white);
            float x=i==0?40:956;
            Fill(new Rect(x,169,284,58),panel);
            Label(new Rect(x+10,177,270,24),"P"+(i+1)+" / "+names[f.pokemon]+"   "+f.hp+" HP",14,Color.white);
            Fill(new Rect(x+10,208,264,7),ink); Fill(new Rect(x+10,208,264*f.hp/100f,7),accents[f.pokemon]);
        }
        Label(new Rect(140,617,1000,30),"WASD  DI CHUYỂN     /     CHUỘT TRÁI  BẮN     /     SPACE + HƯỚNG  LƯỚT     •     HỒI LƯỚT: "+state.players[Me].dashCooldown.ToString("0.0")+"s",13,muted);
        if(state.step==3)
        {
            Fill(new Rect(360,270,560,245),ink); Fill(new Rect(360,270,560,4),Gold);
            Label(new Rect(400,300,490,55),state.winner<0?"HÒA!":state.winner==Me?"CHIẾN THẮNG!":"TRẬN ĐẤU KẾT THÚC",32,Gold);
            Label(new Rect(400,365,480,35),"Sẵn sàng cho một trận đấu mới?",18,muted);
            GUI.enabled=host && transport.Connected;
            if(Button(new Rect(400,426,480,48),host?"CHƠI LẠI / CHỌN POKÉMON":"CHỜ CHỦ PHÒNG CHƠI LẠI",true))
            { state=new LanDuelRules.State(); local.ready=false; inputs[1]=new LanDuelRules.Command(); status="Chọn Pokémon và xác nhận sẵn sàng cho trận mới."; }
            GUI.enabled=true;
        }
    }
}
