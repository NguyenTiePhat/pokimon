using UnityEngine;

public sealed class WaterTempleEffects : MonoBehaviour
{
    public static readonly Vector2[] Jets={new Vector2(6,0),new Vector2(0,6),new Vector2(-6,0),new Vector2(0,-6)};
    private CombatPixelSurface surface;
    private PikachuCombat player;
    private float clock;
    private int frame=-1,hitCycle=-1;
    private void Awake() { Initialize(); }
    public void Initialize()
    {
        if(surface!=null) return;
        surface=new CombatPixelSurface("Water reflections, ripples and marked fountains",transform,1024,640,24,2);
        surface.Clear(); surface.Apply();
    }
    private void Update()
    {
        if(!player) player=Object.FindFirstObjectByType<PikachuCombat>();
        if(!player || player.Player.IsEntering || player.BattleEnded) { Render(clock,false); return; }
        clock+=Time.deltaTime;
        int cycle=Mathf.FloorToInt(clock/10); float phase=clock%10;
        if(phase>=8.5f && phase<9.5f && cycle!=hitCycle && Vector2.Distance(player.Player.transform.position,Jets[cycle%4])<.85f)
        { player.TakeDamage(5); hitCycle=cycle; }
        Render(clock,true);
    }
    public void Render(float time,bool hazards=true)
    {
        Initialize(); int next=Mathf.FloorToInt(time*12); if(next==frame) return; frame=next;
        surface.Clear();
        for(int i=0;i<85;i++)
        {
            float angle=i*2.39996f, radius=9.5f+(i%8)*.8f;
            Vector2 p=new Vector2(512,320)+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(radius*24);
            int x=Mathf.RoundToInt(p.x),y=Mathf.RoundToInt(p.y);
            int r=3+(next+i)%14;
            surface.Ellipse(x,y,r,Mathf.Max(1,r/3),new Color32(111,224,235,(byte)(90-r*4)),false);
            surface.Line(p+Vector2.up*4,p+Vector2.up*4+Vector2.right*(6+Mathf.Sin(time+i)*4),new Color32(188,245,239,80),1);
        }
        for(int i=0;i<Jets.Length;i++)
        {
            Vector2 p=new Vector2(512,320)+Jets[i]*24; int x=Mathf.RoundToInt(p.x),y=Mathf.RoundToInt(p.y);
            surface.Ellipse(x,y,19,19,new Color32(50,147,163,150),false,2);
            surface.Ellipse(x,y,5,5,new Color32(45,91,106,255));
            bool selected=hazards && i==Mathf.FloorToInt(time/10)%4;
            float phase=time%10;
            if(selected && phase>=7 && phase<9.5f)
            {
                surface.Ellipse(x,y,21,21,new Color32(255,199,83,210),false,2);
                if(phase>=8.5f)
                {
                    surface.Line(p,p+Vector2.up*30,new Color32(60,155,227,180),13);
                    surface.Line(p,p+Vector2.up*35,new Color32(190,250,255,230),4);
                    for(int drop=0;drop<6;drop++) { float a=drop*1.047f+time*3; Vector2 q=p+new Vector2(Mathf.Cos(a)*17,25+Mathf.Sin(a)*12); surface.Box((int)q.x,(int)q.y,2,4,new Color32(170,243,255,230)); }
                }
            }
        }
        surface.Apply();
    }
    private void OnDestroy() { surface?.Dispose(); }
}
