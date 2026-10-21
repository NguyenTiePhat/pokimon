using UnityEngine;

public sealed class VenusaurVisuals : MonoBehaviour
{
    private VenusaurBoss boss;
    private CombatPixelSurface effects,ground;
    private static readonly Color32 Green=new Color32(70,170,78,255),Light=new Color32(177,234,99,255),Gold=new Color32(255,226,116,230);
    public void Initialize(VenusaurBoss owner)
    {
        boss=owner; effects=new CombatPixelSurface("Leaves vines and sunlight",transform,1024,1024,24,14);
        ground=new CombatPixelSurface("Garden attack warnings",transform,1024,1024,24,4);
    }
    private void LateUpdate()=>Render(Time.time);
    public void Render(float time)
    {
        if(!boss)return;
        effects.Clear(); ground.Clear();
        Vector2 origin=transform.position,center=new Vector2(512,512),d=boss.LockedDirection,n=new Vector2(-d.y,d.x);
        Vector2 start=center+(boss.CastOrigin-origin)*24,flower=start+Vector2.up*26;
        if(boss.IsAlive&&boss.ActiveSkill>=0)
        {
            float charge=boss.WindingUp?Mathf.Clamp01(boss.PhaseTime/VenusaurBoss.Windups[boss.ActiveSkill]):1;
            if(boss.ActiveSkill==0&&boss.WindingUp)
                for(int i=-2;i<=2;i++)
                {
                    Vector2 dir=Quaternion.Euler(0,0,i*20)*d;
                    ground.Line(start+dir*18,start+dir*170,new Color32(174,223,85,100),2);
                    Leaf(flower+dir*(10+charge*9),dir,7);
                }
            if(boss.ActiveSkill==1)
            {
                Vector2 target=center+(boss.SeedTarget-origin)*24;
                ground.Ellipse((int)target.x,(int)target.y,31,31,new Color32(140,76,36,65));
                ground.Ellipse((int)target.x,(int)target.y,31,31,Gold,false,2);
                // Seeds visibly arc from the flower and land before the first damaging pulse.
                if(boss.WindingUp)
                    for(int i=0;i<3;i++)
                    {
                        float t=Mathf.Clamp01((charge-.15f-i*.08f)/.7f);
                        Vector2 p=Vector2.Lerp(flower,target,t)+Vector2.up*(Mathf.Sin(t*Mathf.PI)*35);
                        effects.Ellipse((int)p.x+i*3,(int)p.y,3,4,new Color32(168,109,55,255));
                        effects.Ellipse((int)p.x+i*3-1,(int)p.y+1,1,2,Light);
                    }
            }
            if(boss.ActiveSkill==2)
            {
                Vector2 previous=start+(Vector2)(Quaternion.Euler(0,0,-65)*d)*77;
                for(int i=1;i<=48;i++)
                {
                    Vector2 p=start+(Vector2)(Quaternion.Euler(0,0,-65+130*i/48f)*d)*77;
                    ground.Line(previous,p,boss.WindingUp?Gold:new Color32(141,217,85,60),2); previous=p;
                }
                if(boss.WindingUp)
                {
                    ground.Line(start,start+(Vector2)(Quaternion.Euler(0,0,-65)*d)*77,Gold,1);
                    ground.Line(start,start+(Vector2)(Quaternion.Euler(0,0,65)*d)*77,Gold,1);
                }
                Vector2 whip=boss.WindingUp?(Vector2)(Quaternion.Euler(0,0,-65)*d):boss.WhipDirection;
                Vine(start+Vector2.up*10,start+whip*(boss.WindingUp?25:77)+Vector2.up*10,14*Mathf.Sin(time*8),time);
            }
            if(boss.ActiveSkill==3)
            {
                Vector2 end=start+d*384;
                if(boss.WindingUp)
                {
                    ground.Line(start,end,new Color32(245,220,94,35),38);
                    ground.Line(start+n*19,end+n*19,Gold,2); ground.Line(start-n*19,end-n*19,Gold,2);
                    for(int i=0;i<14;i++)
                    {
                        float a=i*2.4f+time,travel=Mathf.Repeat(time*.8f+i/14f,1);
                        Vector2 p=flower+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Mathf.Lerp(45,4,travel);
                        effects.Ellipse((int)p.x,(int)p.y,2,2,Gold);
                    }
                    int r=3+Mathf.RoundToInt(charge*10);
                    effects.Ellipse((int)flower.x,(int)flower.y,r+5,r+5,new Color32(232,233,92,75));
                    effects.Ellipse((int)flower.x,(int)flower.y,r,r,Gold);
                    effects.Ellipse((int)flower.x,(int)flower.y,Mathf.Max(2,r-4),Mathf.Max(2,r-4),new Color32(255,255,224,255));
                }
                else if(boss.SpinActive)
                {
                    Vector2 source=center+(boss.BeamStart-origin)*24+Vector2.up*26;
                    Vector2 tip=center+(boss.BeamEnd-origin)*24+Vector2.up*26;
                    var colors=new[]{new Color32(207,239,96,65),new Color32(164,213,65,255),new Color32(250,235,128,255),new Color32(255,255,229,255)};
                    for(int layer=0;layer<4;layer++)
                        for(int i=0;i<=160;i++)
                        {
                            float t=i/160f; Vector2 p=Vector2.Lerp(source,tip,t);
                            int r=Mathf.RoundToInt((layer==0?23:layer==1?19:layer==2?14:8)*Mathf.Lerp(.35f,1,Mathf.Clamp01(t*12)));
                            effects.Ellipse((int)p.x,(int)p.y,r,r,colors[layer]);
                        }
                    for(int i=0;i<18;i++)
                    {
                        float t=Mathf.Repeat(time*2+i/18f,1); Vector2 p=Vector2.Lerp(source,tip,t)+n*Mathf.Sin(i*7)*15;
                        effects.Line(p,p-d*9,new Color32(255,255,240,230),2);
                    }
                }
            }
        }
        foreach(var shot in boss.Shots) Leaf(center+(shot.position-origin)*24+Vector2.up*14,shot.direction,7);
        foreach(var patch in boss.Patches)
        {
            Vector2 p=center+(patch.position-origin)*24;
            ground.Ellipse((int)p.x,(int)p.y,31,31,new Color32(78,91,33,65));
            ground.Ellipse((int)p.x,(int)p.y,31,31,new Color32(173,218,80,180),false,2);
            float growth=Mathf.Clamp01(patch.age/.3f)*Mathf.Clamp01((3.2f-patch.age)/.25f);
            for(int i=0;i<7;i++)
            {
                float a=i*2.4f; Vector2 root=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*20;
                Vine(root,root+Vector2.up*16*growth,4*Mathf.Sin(time*4+i),time);
                Leaf(root+Vector2.up*9*growth,Vector2.right*(i%2==0?1:-1),4);
            }
            float pulse=Mathf.Repeat(patch.age,.6f)/.6f;
            ground.Ellipse((int)p.x,(int)p.y,Mathf.RoundToInt(31*pulse),Mathf.RoundToInt(31*pulse),new Color32(200,231,105,(byte)(130*(1-pulse))),false,1);
        }
        effects.Apply(); ground.Apply();
    }
    private void Leaf(Vector2 p,Vector2 d,int size)
    {
        Vector2 n=new Vector2(-d.y,d.x);
        for(int i=-size;i<=size;i++)
        {
            float w=Mathf.Sin((i+size)/(float)(size*2)*Mathf.PI)*size*.45f;
            effects.Line(p+d*i-n*w,p+d*i+n*w,Green,1);
        }
        effects.Line(p-d*(size-1),p+d*(size-1),Light,1);
    }
    private void Vine(Vector2 a,Vector2 b,float bend,float time)
    {
        Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized;
        for(int layer=0;layer<2;layer++)
        {
            Vector2 previous=a;
            for(int i=1;i<=28;i++)
            {
                float t=i/28f; Vector2 p=Vector2.Lerp(a,b,t)+n*Mathf.Sin(t*Mathf.PI)*bend;
                effects.Line(previous,p,layer==0?new Color32(33,103,52,255):Light,Mathf.Max(1,Mathf.RoundToInt((layer==0?5:2)*(1-t*.7f)))); previous=p;
            }
        }
    }
    private void OnDestroy() { effects?.Dispose(); ground?.Dispose(); }
}
