using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CombatHud : MonoBehaviour
{
    private PikachuCombat combat;
    private static readonly Color Panel = new Color32(13,23,34,242);
    private static readonly Color Muted = new Color32(157,174,190,255);
    private static readonly Color Gold = new Color32(255,212,90,255);
    private readonly CombatPixelSurface[] icons = new CombatPixelSurface[4];
    private static bool trainingOpen;
    private static readonly string[] Keys = { "E", "Q", "C", "X" };
    public void Initialize(PikachuCombat controller)
    {
        combat = controller; trainingOpen = false;
        for (int i=0;i<4;i++) icons[i] = CreateIcon(i);
    }
    public static Rect IconRect(int index) => new Rect(513 + index * 66, 628, 56, 56);
    public static bool BlocksCanvasPoint(Vector2 p)
    {
        if (p.y < 60 || new Rect(16,64,255,153).Contains(p)) return true;
        if (trainingOpen && new Rect(16,226,255,210).Contains(p)) return true;
        if (new Rect(420,65,440,63).Contains(p)) return true;
        for(int i=0;i<4;i++) if(IconRect(i).Contains(p)) return true;
        return false;
    }

    public static bool PointerOverHud(Vector2 mouse)
    {
        float scale = Mathf.Min(Screen.width/1280f,Screen.height/720f);
        Vector2 p = new Vector2((mouse.x-(Screen.width-1280*scale)/2)/scale,
            (Screen.height-mouse.y-(Screen.height-720*scale)/2)/scale);
        return BlocksCanvasPoint(p);
    }
    private static void Fill(Rect rect, Color color)
    { GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=Color.white; }
    private static void Text(Rect rect, string text, int size, Color color, TextAnchor align=TextAnchor.UpperLeft)
    {
        var style=new GUIStyle(GUI.skin.label) { fontSize=size,alignment=align,wordWrap=true };
        style.normal.textColor=color; GUI.Label(rect,text,style);
    }
    private static void Bar(Rect rect,float amount,Color color)
    { Fill(rect,new Color32(34,44,57,255)); Fill(new Rect(rect.x,rect.y,rect.width*Mathf.Clamp01(amount),rect.height),color); }

    private void OnGUI()
    {
        if (!combat) return;
        var previous=GUI.matrix;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,new Vector3(scale,scale,1));
        Fill(new Rect(16,64,255,115),Panel);
        bool bossFight=combat.Dummy is ArenaBoss;
        Text(new Rect(30,74,225,25),bossFight?"PIKACHU":"PIKACHU  /  LUYỆN TẬP",16,Gold);
        Text(new Rect(30,104,210,22),"HP  "+Mathf.CeilToInt(combat.Health)+" / 200",13,Color.white);
        Bar(new Rect(30,128,225,5),combat.Health/200,new Color32(93,207,138,255));
        Text(new Rect(30,141,225,22),"Năng lượng  "+Mathf.FloorToInt(combat.Mana)+" / 100",14,new Color32(117,210,252,255));
        Bar(new Rect(30,167,225,7),combat.Mana/100,new Color32(72,177,237,255));
        if(GUI.Button(new Rect(16,187,126,30),trainingOpen?"Ẩn luyện tập":"Luyện tập")) trainingOpen=!trainingOpen;
        if(trainingOpen)
        {
        Fill(new Rect(16,226,255,173),Panel);
        Text(new Rect(30,234,225,24),bossFight?"ĐẤU BOSS / LUYỆN TẬP":"HÌNH NHÂN THỬ CHIÊU",14,Color.white);
        bool enabled=GUI.enabled;
        GUI.enabled=!combat.Player.IsEntering && !combat.MovementLocked;
        if(combat.Dummy && !bossFight)
        {
            combat.Dummy.Moving=GUI.Toggle(new Rect(30,265,220,22),combat.Dummy.Moving,"Cho hình nhân di chuyển");
            combat.Dummy.Immune=GUI.Toggle(new Rect(30,292,220,22),combat.Dummy.Immune,"Miễn nhiễm sát thương");
            if(GUI.Button(new Rect(30,322,106,28),"Hồi hình nhân")) combat.Dummy.ResetDummy();
            if(GUI.Button(new Rect(145,322,110,28),"Nạp thử 15 sét")) combat.RefillTrainingResources();
            if(GUI.Button(new Rect(30,359,225,28),"ĐÁNH LẠI • xóa hố trên sân")) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        GUI.enabled=enabled;
        if(bossFight && GUI.Button(new Rect(30,269,225,32),"Chuyển sang hình nhân"))
        { ArenaPreview.TrainingMode=true; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        if(!bossFight && GUI.Button(new Rect(30,401,225,30),ArenaProgress.IsWater?"Đấu Blastoise":"Đấu Charizard"))
        { ArenaPreview.TrainingMode=false; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        }

        if(bossFight)
        {
            var boss=(ArenaBoss)combat.Dummy;
            Fill(new Rect(420,65,440,63),Panel);
            Text(new Rect(430,68,420,25),boss.Status+"   "+Mathf.CeilToInt(boss.Health)+" / "+boss.HealthCapacity,15,Gold,TextAnchor.MiddleCenter);
            Bar(new Rect(436,103,408,10),boss.Health/boss.HealthCapacity,new Color32(230,85,48,255));
        }
        if(combat.Dummy && !bossFight && Camera.main)
        {
            Vector3 p=Camera.main.WorldToScreenPoint(combat.Dummy.transform.position+Vector3.up*1.75f);
            float x=(p.x-(Screen.width-1280*scale)/2)/scale;
            float y=(Screen.height-p.y-(Screen.height-720*scale)/2)/scale;
            if(x>280 && x<1250 && y>55 && y<590)
            {
                Fill(new Rect(x-105,y-28,210,74),Panel);
                Text(new Rect(x-102,y-24,204,22),"HÌNH NHÂN  "+Mathf.CeilToInt(combat.Dummy.Health)+" / 1000",12,Color.white,TextAnchor.MiddleCenter);
                Bar(new Rect(x-94,y+2,188,6),combat.Dummy.Health/TrainingDummy.MaxHealth,new Color32(236,117,88,255));
                string status=combat.Dummy.Immune?"MIỄN NHIỄM":combat.Dummy.SlowRemaining>0?"CHẬM "+Mathf.RoundToInt(combat.Dummy.SlowFraction*100)+"% • "+combat.Dummy.SlowRemaining.ToString("0.0")+"s":combat.Dummy.LastResult;
                Text(new Rect(x-102,y+15,204,24),status,14,Gold,TextAnchor.MiddleCenter);
            }
        }
        if(combat.FeedbackRemaining>0)
            Text(new Rect(310,591,660,25),combat.Feedback,14,Gold,TextAnchor.MiddleCenter);
        if(combat.StunRemaining>0)
            Text(new Rect(460,550,360,30),"CHOÁNG • "+combat.StunRemaining.ToString("0.0")+"s",19,Gold,TextAnchor.MiddleCenter);
        for(int i=0;i<4;i++) DrawSkill(i,IconRect(i));
        if(combat.BattleEnded)
        {
            Fill(new Rect(390,255,500,combat.Health>0 && combat.Dummy is CharizardBoss?220:190),Panel);
            Text(new Rect(410,275,460,60),combat.Health>0?"CHIẾN THẮNG!\nĐã hạ gục "+((ArenaBoss)combat.Dummy).DisplayName:"PIKACHU ĐÃ GỤC",26,Gold,TextAnchor.MiddleCenter);
            if(GUI.Button(new Rect(505,367,270,45),"ĐÁNH LẠI")) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            if(combat.Health>0 && combat.Dummy is CharizardBoss && GUI.Button(new Rect(505,420,270,40),"MÀN 02 • ĐỀN NƯỚC NGẬP")) ArenaProgress.Enter("BlastoiseArena");
        }
        GUI.matrix=previous;
    }
    private void DrawSkill(int index,Rect rect)
    {
        var skill=(PikachuSkill)index;
        var data=combat.Settings(skill);
        float remaining=combat.Cooldown(skill);
        bool unavailable=remaining>0 || combat.Mana<data.mana || (index==3 && combat.Lightning<15);
        bool selected=combat.ActiveSkill==index || combat.AimingSkill==index;
        Color border=selected?new Color32(114,255,228,255):unavailable?new Color32(67,77,90,255):Gold;
        Fill(new Rect(rect.x-2,rect.y-2,60,60),border);
        Fill(rect,new Color32(17,27,39,245));
        if(icons[index]!=null)
        {
            GUI.color=unavailable?new Color(.55f,.55f,.55f,1):Color.white;
            GUI.DrawTexture(new Rect(rect.x+4,rect.y+4,48,48),icons[index].Renderer.sprite.texture);
            GUI.color=Color.white;
        }
        if(remaining>0)
        {
            float h=rect.height*Mathf.Clamp01(remaining/Mathf.Max(.01f,data.cooldown));
            Fill(new Rect(rect.x,rect.y,rect.width,h),new Color(0,0,0,.55f));
            Text(new Rect(rect.x,rect.y+13,56,28),Mathf.CeilToInt(remaining).ToString(),20,Color.white,TextAnchor.MiddleCenter);
        }
        if(combat.ActiveSkill==index)
            Bar(new Rect(rect.x,rect.y+52,56,4),index==3?combat.Charge01:combat.CastProgress/Mathf.Max(.01f,data.castSeconds),Gold);
        else if(index==1 && combat.QuickRemaining>0)
            Bar(new Rect(rect.x,rect.y+52,56,4),combat.QuickRemaining/combat.quickDuration,new Color32(129,229,255,255));
        Fill(new Rect(rect.x+3,rect.y+2,15,17),new Color(0,0,0,.7f));
        Text(new Rect(rect.x+3,rect.y+1,15,18),Keys[index],12,Color.white,TextAnchor.MiddleCenter);
        if(index==3)
            Text(new Rect(rect.x-8,rect.y+61,72,23),combat.Lightning+" / 15",15,combat.Lightning>=15?Gold:Muted,TextAnchor.MiddleCenter);
    }

    private CombatPixelSurface CreateIcon(int index)
    {
        var s=new CombatPixelSurface("Skill icon "+Keys[index],transform,48,48,48,0);
        s.Clear();
        Color32 gold=new Color32(255,212,72,255), white=new Color32(247,255,242,255), cyan=new Color32(123,225,255,255);
        if(index==0)
        {
            Vector2[] p={new Vector2(29,40),new Vector2(18,26),new Vector2(29,26),new Vector2(19,9)};
            for(int i=1;i<p.Length;i++) s.Line(p[i-1],p[i],gold,7);
            for(int i=1;i<p.Length;i++) s.Line(p[i-1],p[i],white,2);
            s.Box(9,30,3,6,gold); s.Box(36,14,3,6,gold);
        }
        else if(index==1)
        {
            s.Line(new Vector2(8,15),new Vector2(24,15),cyan,2);
            s.Line(new Vector2(6,24),new Vector2(21,24),cyan,3);
            s.Line(new Vector2(8,33),new Vector2(24,33),cyan,2);
            s.Line(new Vector2(26,12),new Vector2(38,24),cyan,5);
            s.Line(new Vector2(38,24),new Vector2(26,36),cyan,5);
            s.Line(new Vector2(27,15),new Vector2(35,24),white,2);
        }
        else if(index==2)
        {
            Vector2[] p={new Vector2(10,11),new Vector2(22,23),new Vector2(16,28),new Vector2(34,38)};
            for(int i=1;i<p.Length;i++) s.Line(p[i-1],p[i],new Color32(74,122,161,255),9);
            for(int i=1;i<p.Length;i++) s.Line(p[i-1],p[i],new Color32(182,218,239,255),6);
            for(int i=1;i<p.Length;i++) s.Line(p[i-1]+Vector2.up*2,p[i]+Vector2.up*2,white,2);
            s.Line(new Vector2(36,15),new Vector2(36,24),gold,1); s.Line(new Vector2(32,20),new Vector2(40,20),gold,1);
        }
        else
        {
            s.Ellipse(24,24,17,17,new Color32(255,172,36,70));
            s.Ellipse(24,24,13,13,new Color32(231,156,34,255));
            s.Ellipse(23,25,11,11,gold); s.Ellipse(21,27,7,7,white);
            s.Bolt(new Vector2(7,31),new Vector2(12,41),3,gold,.45f);
            s.Bolt(new Vector2(34,11),new Vector2(41,17),7,gold,.45f);
        }
        s.Apply(); s.Renderer.enabled=false; return s;
    }
    private void OnDestroy()
    {
        foreach(var icon in icons) icon?.Dispose();
        trainingOpen=false;
    }
}
