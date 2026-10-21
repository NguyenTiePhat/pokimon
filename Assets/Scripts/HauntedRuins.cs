using UnityEngine;

public sealed class HauntedRuins : MonoBehaviour
{
    private CombatPixelSurface floor,mist;
    private int frame=-1;
    private void Awake()=>Build();
    public void Build()
    {
        if(floor!=null)return;
        floor=new CombatPixelSurface("Moonlit brick court and broken crypts",transform,1024,640,24,0);
        var stone=new Color32(103,110,151,255);
        for(int y=0;y<640;y++)for(int x=0;x<1024;x++)
        {
            float radius=Vector2.Distance(new Vector2(x,y),new Vector2(512,320));
            int noise=(x/3*17+y/3*11)%9;
            Color32 c=new Color32((byte)(20+noise),(byte)(22+noise),(byte)(43+noise),255);
            if(radius<224)
            {
                int tile=((x+(y/16%2)*16)/32*7+y/16*13)%12;
                c=new Color32((byte)(66+tile),(byte)(69+tile),(byte)(99+tile),255);
                if((x+(y/16%2)*16)%32<2||y%16<2)c=new Color32(39,42,66,255);
            }
            floor.Dot(x,y,c);
        }
        floor.Ellipse(512,320,226,226,stone,false,5);
        floor.Ellipse(512,320,211,211,new Color32(82,104,142,255),false,2);
        // Decorative darkness never enters the playable radius (8.4 units).
        for(int i=0;i<18;i++)
        {
            float a=i*2.39996f; Vector2 p=new Vector2(512,320)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(268+i%3*9);
            int x=(int)p.x,y=(int)p.y;
            floor.Ellipse(x,y,23,11,new Color32(12,8,29,230));
            if(i%3==0)
            {
                floor.Box(x-26,y,10,40,stone); floor.Box(x+17,y,10,32,stone);
                for(int segment=0;segment<16;segment++)
                {
                    if(segment>=4&&segment<=6)continue;
                    float a0=segment*Mathf.PI/16,a1=(segment+1)*Mathf.PI/16;
                    floor.Line(new Vector2(x+22*Mathf.Cos(a0),y+32+18*Mathf.Sin(a0)),
                        new Vector2(x+22*Mathf.Cos(a1),y+32+18*Mathf.Sin(a1)),stone,7);
                }
                floor.Box(x-29,y-4,16,5,new Color32(133,145,181,255));
                floor.Line(new Vector2(x-23,y+27),new Vector2(x-17,y+17),new Color32(35,32,57,255),2);
            }
            else
            {
                floor.Box(x-13,y-2,27,7,stone);
                floor.Box(x-10,y+5,20,23,new Color32(69,68,103,255));
                floor.Ellipse(x,y+28,10,7,stone);
                floor.Line(new Vector2(x,y+10),new Vector2(x,y+26),new Color32(151,162,195,255),2);
                floor.Line(new Vector2(x-5,y+21),new Vector2(x+5,y+21),new Color32(151,162,195,255),2);
            }
        }
        floor.Ellipse(810,558,25,25,new Color32(170,194,220,255));
        floor.Ellipse(819,566,23,23,new Color32(24,26,47,255));
        floor.Apply();
        mist=new CombatPixelSurface("Faint perimeter mist below attack markers",transform,1024,640,24,1);
        mist.Clear(); mist.Apply();
    }
    private void Update()
    {
        if(mist==null||frame==Mathf.FloorToInt(Time.time*8))return;
        frame=Mathf.FloorToInt(Time.time*8); mist.Clear();
        for(int i=0;i<14;i++)
        {
            float a=i*2.4f+Mathf.Sin(Time.time*.1f)*.03f;
            Vector2 p=new Vector2(512,320)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*275;
            mist.Ellipse((int)p.x,(int)p.y,30,6,new Color32(140,151,205,22));
        }
        mist.Apply();
    }
    private void OnDestroy() { floor?.Dispose(); mist?.Dispose(); }
}
