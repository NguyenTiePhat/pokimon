using UnityEngine;

public sealed class AncientGarden : MonoBehaviour
{
    private CombatPixelSurface floor,pollen;
    private int frame;
    private void Awake()=>Build();
    public void Build()
    {
        if(floor!=null)return;
        floor=new CombatPixelSurface("Old garden paving roots flowers and ruins",transform,1024,640,24,0);
        var stone=new Color32(135,146,113,255); var moss=new Color32(52,94,52,255);
        for(int y=0;y<640;y++) for(int x=0;x<1024;x++)
        {
            float radius=Vector2.Distance(new Vector2(x,y),new Vector2(512,320));
            int noise=(x/3*17+y/3*11)%13;
            Color32 c=new Color32((byte)(28+noise),(byte)(55+noise),(byte)(34+noise/2),255);
            if(radius<224)
            {
                int tile=(x/28*7+y/22*13)%16;
                c=new Color32((byte)(102+tile),(byte)(115+tile),(byte)(86+tile),255);
                if(x%28<2||y%22<2)c=new Color32(64,83,58,255);
                if(radius>195 && noise<4)c=moss;
            }
            floor.Dot(x,y,c);
        }
        floor.Ellipse(512,320,226,226,stone,false,5);
        floor.Ellipse(512,320,210,210,moss,false,2);
        floor.Ellipse(512,320,44,44,new Color32(145,154,112,255),false,2);
        // Broken garden gates and low walls remain beyond the combat circle.
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4; Vector2 p=new Vector2(512,320)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*248;
            int x=(int)p.x,y=(int)p.y;
            floor.Ellipse(x,y-4,26,9,new Color32(8,26,16,130));
            floor.Box(x-17,y,34,8,stone); floor.Box(x-10,y+8,20,25+i%3*9,new Color32(100,118,88,255));
            floor.Box(x-7,y+9,4,23+i%3*9,stone);
            floor.Line(new Vector2(x-14,y+2),new Vector2(x+8,y+37),moss,4);
        }
        for(int i=0;i<32;i++)
        {
            float a=i*2.39996f; Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            Vector2 p=new Vector2(512,320)+direction*(240+i%4*18);
            floor.Ellipse((int)p.x,(int)p.y,22,13,moss);
            floor.Ellipse((int)p.x-5,(int)p.y+4,14,9,new Color32(67,119,58,255));
            if(i%3==0)
            {
                for(int petal=0;petal<5;petal++)
                {
                    float b=petal*Mathf.PI*2/5;
                    floor.Ellipse((int)(p.x+Mathf.Cos(b)*9),(int)(p.y+Mathf.Sin(b)*7)+8,7,6,new Color32(187,95,118,255));
                }
                floor.Ellipse((int)p.x,(int)p.y+8,5,4,new Color32(242,202,102,255));
            }
            Vector2 root=p-direction*26;
            floor.Line(p,root,new Color32(75,65,39,255),5);
            floor.Line(root,root-direction*13+new Vector2(-direction.y,direction.x)*8,new Color32(101,87,48,255),3);
        }
        floor.Apply(); pollen=new CombatPixelSurface("Sparse garden pollen",transform,1024,640,24,2);
        pollen.Clear(); pollen.Apply();
    }
    private void Update()
    {
        if(pollen==null||frame==Mathf.FloorToInt(Time.time*10))return;
        frame=Mathf.FloorToInt(Time.time*10); pollen.Clear();
        for(int i=0;i<24;i++)
        {
            float a=i*2.4f+Time.time*.015f;
            Vector2 p=new Vector2(512,320)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(230+i%5*10);
            pollen.Ellipse((int)p.x,(int)(p.y+Mathf.Sin(Time.time+i)*5),1,1,new Color32(239,221,137,95));
        }
        pollen.Apply();
    }
    private void OnDestroy(){floor?.Dispose();pollen?.Dispose();}
}
