using UnityEngine;

public sealed class MountainDojo : MonoBehaviour
{
    public static readonly Vector2 PlayableHalfSize = new Vector2(8.4f,6.4f);
    private CombatPixelSurface floor, breeze;
    private int frame = -1;
    private static readonly Color32 Dark = new Color32(35,59,73,255), Edge = new Color32(163,174,168,255);
    private void Awake() => Build();
    public void Build()
    {
        if (floor != null) return;
        floor = new CombatPixelSurface("Mountain dojo • snow peaks, stone terrace and cliff",transform,1024,640,24,0);
        for (int y=0;y<640;y++)
            for (int x=0;x<1024;x++)
            {
                float t=y/640f;
                floor.Dot(x,y,new Color32((byte)(69+t*38),(byte)(108+t*52),(byte)(132+t*53),255));
            }
        // Layered distant ridges, drawn before the terrace; snow stays beyond the court.
        for (int i=0;i<13;i++)
        {
            int x=i*103-100, peak=470+(i*47)%150;
            Mountain(x,peak,150,160,new Color32(121,153,170,255),new Color32(209,223,225,255));
        }
        for (int i=0;i<9;i++)
            Mountain(i*150-85,420+(i*61)%135,130,230,new Color32(70,111,132,255),new Color32(185,208,214,255));
        for (int i=0;i<7;i++)
            Mountain(i*181-120,285+(i*37)%92,153,270,new Color32(44,85,106,255),new Color32(93,135,150,255));
        // The rock face establishes a real ledge without occupying walkable ground.
        floor.Box(281,86,462,65,new Color32(39,57,67,255));
        for (int i=0;i<25;i++)
        {
            int x=284+i*18, h=25+(i*13)%36;
            floor.Box(x,145-h,15,h,new Color32((byte)(48+i%3*5),(byte)(69+i%3*5),(byte)(78+i%3*5),255));
            floor.Line(new Vector2(x+2,143),new Vector2(x+5,147-h),new Color32(80,101,108,255),2);
        }
        floor.Box(275,148,474,344,Dark);
        floor.Box(279,152,466,336,Edge);
        floor.Box(286,159,452,322,new Color32(84,108,115,255));
        for (int y=163;y<477;y++)
            for (int x=290;x<734;x++)
            {
                int row=(y-163)/16, offset=row%2*16;
                int tile=((x+offset)/32*11+row*7)%13;
                Color32 c=new Color32((byte)(100+tile),(byte)(119+tile),(byte)(125+tile),255);
                if ((x+offset)%32<1 || (y-163)%16<1) c=new Color32(73,92,101,255);
                else if ((y-163)%16==1) c=new Color32(137,151,151,255);
                floor.Dot(x,y,c);
            }
        // Inset trim sits outside x ±8.4 / y ±6.4, leaving the entire duel space open.
        floor.Box(294,163,3,314,new Color32(188,169,118,255));
        floor.Box(727,163,3,314,new Color32(188,169,118,255));
        floor.Box(294,163,436,3,new Color32(188,169,118,255));
        floor.Box(294,474,436,3,new Color32(188,169,118,255));
        var marking=new Color32(81,104,112,255);
        floor.Ellipse(512,320,58,58,marking,false,2);
        floor.Ellipse(512,320,51,51,marking,false);
        // Eight-point aura crest, subdued to avoid competing with attack markers.
        for (int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;
            Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)), n=new Vector2(-d.y,d.x);
            floor.Line(new Vector2(512,320)+d*37,new Vector2(512,320)+d*18+n*8,marking,2);
            floor.Line(new Vector2(512,320)+d*37,new Vector2(512,320)+d*18-n*8,marking,2);
        }
        floor.Ellipse(512,320,9,9,marking,false,2);
        // Northern open pavilion and south steps are outside gameplay bounds.
        for (int i=0;i<6;i++)
        {
            floor.Box(458-i*5,493+i*6,108+i*10,6,Dark);
            floor.Box(460-i*5,495+i*6,104+i*10,3,Edge);
            floor.Box(472-i*5,144-i*7,80+i*10,5,Edge);
        }
        floor.Box(442,530,140,9,Dark);
        floor.Box(449,538,8,48,new Color32(96,65,56,255));
        floor.Box(567,538,8,48,new Color32(96,65,56,255));
        floor.Box(452,540,2,43,new Color32(181,135,94,255));
        floor.Box(570,540,2,43,new Color32(181,135,94,255));
        floor.Box(457,554,110,6,new Color32(93,68,55,255));
        for (int y=0;y<26;y++)
        {
            floor.Box(425+y*2,581+y,174-y*4,1,new Color32(28,57,75,255));
            if(y%6==0)floor.Box(425+y*2,581+y,174-y*4,1,new Color32(89,134,148,255));
        }
        floor.Line(new Vector2(423,581),new Vector2(601,581),new Color32(197,163,105,255),3);
        floor.Box(493,562,38,15,new Color32(56,78,84,255));
        floor.Line(new Vector2(502,567),new Vector2(522,572),new Color32(207,186,131,255),2);
        // Four flag poles and two stone lanterns: all on the perimeter.
        foreach (int x in new[]{266,758})
            foreach (int y in new[]{214,402})
            {
                floor.Ellipse(x,y-3,14,5,new Color32(33,57,69,220));
                floor.Box(x-7,y-2,14,5,Edge);
                floor.Box(x-2,y,4,65,new Color32(98,71,56,255));
                floor.Box(x-1,y,1,65,new Color32(204,174,119,255));
                floor.Ellipse(x,y+66,3,3,new Color32(222,194,133,255));
            }
        foreach (int x in new[]{320,704})
        {
            floor.Box(x-10,490,20,5,Dark); floor.Box(x-6,495,12,22,Edge);
            floor.Box(x-11,515,22,13,Dark); floor.Box(x-6,518,12,7,new Color32(240,196,115,255));
            floor.Line(new Vector2(x-14,530),new Vector2(x,540),Dark,4);
            floor.Line(new Vector2(x,540),new Vector2(x+14,530),Dark,4);
        }
        // Wind-shaped pines on the flanking slopes.
        for (int i=0;i<8;i++)
        {
            int x=i<4?210-i*42:814+(i-4)*42, y=170+(i*83)%240;
            floor.Line(new Vector2(x,y),new Vector2(x+9,y+63),new Color32(49,58,58,255),5);
            for (int branch=0;branch<4;branch++)
                floor.Line(new Vector2(x-18+branch*3,y+20+branch*12),new Vector2(x+27-branch*4,y+22+branch*12),new Color32(37,83,83,255),7);
        }
        floor.Apply();
        breeze = new CombatPixelSurface("Harmless mountain wind and cloth banners",transform,1024,640,24,1);
        RenderWind(0);
    }
    private void Mountain(int cx,int peak,int halfWidth,int height,Color32 rock,Color32 snow)
    {
        for (int y=Mathf.Max(0,peak-height);y<=peak;y++)
        {
            float t=(peak-y)/(float)height;
            int w=(int)(halfWidth*t);
            for (int x=cx-w;x<=cx+w;x++)
            {
                bool cap=t < .23f+(Mathf.Sin(x*.12f)+Mathf.Sin(x*.037f))*.035f;
                Color32 c=cap?snow:rock;
                if(x>cx+(peak-y)*.13f && !cap)c=new Color32((byte)(c.r*.82f),(byte)(c.g*.86f),(byte)(c.b*.91f),255);
                floor.Dot(x,y,c);
            }
        }
    }
    private void Update()
    {
        if (breeze==null || frame==Mathf.FloorToInt(Time.time*12)) return;
        frame=Mathf.FloorToInt(Time.time*12);
        RenderWind(Time.time);
    }
    public void RenderWind(float time)
    {
        breeze.Clear();
        foreach (int x in new[]{266,758})
            foreach (int y in new[]{214,402})
                for (int col=0;col<30;col++)
                {
                    int wave=Mathf.RoundToInt(Mathf.Sin(time*3.5f-col*.2f)*col*.12f);
                    breeze.Box(x+2+col,y+39+wave,1,24,new Color32(28,98,144,255));
                    breeze.Box(x+2+col,y+39+wave,1,2,new Color32(214,187,119,255));
                    if(col>10&&col<20)breeze.Box(x+2+col,y+47+wave,1,7,new Color32(129,214,229,255));
                }
        for (int i=0;i<12;i++)
        {
            float x=(i*129+time*17)%1024;
            int y=i%2==0?70+i*4:550+i*5;
            breeze.Line(new Vector2(x,y),new Vector2(x+36,y+3),new Color32(208,231,236,65));
        }
        breeze.Apply();
    }
    private void OnDestroy() { floor?.Dispose(); breeze?.Dispose(); }
}
