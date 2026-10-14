using UnityEngine;

public sealed class CharizardVisuals : MonoBehaviour
{
    private CharizardBoss boss;
    private CombatPixelSurface fire, warning;
    private int lastFrame=-1;
    private static readonly Color32 Dark=new Color32(113,29,16,240), Red=new Color32(203,55,18,255), Orange=new Color32(255,128,25,255), Yellow=new Color32(255,220,83,255);
    public void Initialize(CharizardBoss owner)
    {
        boss=owner;
        fire=new CombatPixelSurface("Charizard flames, claws and air blades",transform,768,768,32,14);
        warning=new CombatPixelSurface("Charizard dodge warning",transform,768,768,32,4);
    }
    private void LateUpdate() { Render(Time.time); }
    public void Render(float time)
    {
        if(!boss) return;
        Vector2 origin=boss.transform.position;
        fire.Renderer.transform.position=warning.Renderer.transform.position=origin;
        int frame=Mathf.FloorToInt(time*24);
        if(frame==lastFrame) return; lastFrame=frame;
        fire.Clear(); warning.Clear();
        Vector2 axis=boss.LockedDirection, side=new Vector2(-axis.y,axis.x);
        Vector2 start=new Vector2(384,384)+(boss.CastOrigin-origin)*32;
        if(boss.IsAlive && boss.ActiveSkill>=0)
        {
            float width=boss.ActiveSkill==3?1.51f:boss.ActiveSkill==1?.85f:boss.ActiveSkill==2?.7f:.52f;
            if(boss.WindingUp)
            {
                var color=new Color32(255,115,61,180);
                int rays=boss.ActiveSkill==2?3:1;
                for(int i=0;i<rays;i++)
                {
                    Vector2 d=rays==3?(Vector2)(Quaternion.Euler(0,0,(i-1)*20)*axis):axis;
                    float length=boss.ActiveSkill==0?7.65f:boss.ActiveSkill==2?8.65f:7;
                    float nearWidth=boss.ActiveSkill==3?.25f:width;
                    Vector2 n=new Vector2(-d.y,d.x), end=start+d*length*32;
                    warning.Line(start+n*nearWidth*32,end+n*width*32,color,2);
                    warning.Line(start-n*nearWidth*32,end-n*width*32,color,2);
                    warning.Line(end-n*width*32,end+n*width*32,color,2);
                    for(int dash=0;dash<7;dash++) warning.Line(start+d*(dash*32),start+d*(dash*32+14),color,1);
                }
            }
            if(boss.ActiveSkill==1)
            {
                Vector2 center=new Vector2(384,384)+Vector2.up*(boss.Height*32+26)+axis*17;
                for(int claw=-1;claw<=1;claw++)
                {
                    Vector2 p=center+side*claw*8;
                    fire.Bolt(p-axis*12,p+axis*17,frame+claw,new Color32(119,255,205,255),1.3f);
                }
            }
            if(boss.ActiveSkill==3 && !boss.WindingUp)
            {
                // A stream of expanding rotating fire rings, matching the supplied reference.
                Vector2 mouth=start+axis*21+Vector2.up*23;
                for(int i=0;i<19;i++)
                {
                    float t=(i+(frame%8)/8f)/19f;
                    float radius=5+t*26;
                    Vector2 center=mouth+axis*t*200+side*Mathf.Sin(t*19-time*9)*2;
                    Vector2 prior=Vector2.zero;
                    for(int j=0;j<=30;j++)
                    {
                        float a=j*Mathf.PI*2/30+time*7;
                        Vector2 p=center+side*Mathf.Cos(a)*radius+axis*Mathf.Sin(a)*radius*.38f;
                        if(j>0) { fire.Line(prior,p,Dark,9); fire.Line(prior,p,Red,6); fire.Line(prior,p,Orange,3); }
                        prior=p;
                    }
                    for(int spark=0;spark<3;spark++)
                    {
                        float a=spark*2.094f+t*22-time*8;
                        Vector2 p=center+side*Mathf.Cos(a)*radius+axis*Mathf.Sin(a)*radius*.38f;
                        Flame(p,Mathf.RoundToInt(3+t*5),frame+i+spark);
                    }
                }
            }
        }
        foreach(var shot in boss.Shots)
        {
            Vector2 p=new Vector2(384,384)+(shot.position-origin)*32+Vector2.up*18;
            if(!shot.air) Flame(p,8,frame);
            else
            {
                Vector2 n=new Vector2(-shot.direction.y,shot.direction.x), prior=Vector2.zero;
                for(int i=0;i<=20;i++)
                {
                    float a=Mathf.Lerp(-1.25f,1.25f,i/20f);
                    Vector2 point=p+shot.direction*Mathf.Cos(a)*12+n*Mathf.Sin(a)*21;
                    if(i>0) { fire.Line(prior,point,new Color32(95,175,207,90),8); fire.Line(prior,point,new Color32(157,239,247,255),4); fire.Line(prior,point,new Color32(244,255,255,255),1); }
                    prior=point;
                }
            }
        }
        fire.Apply(); warning.Apply();
    }
    private void Flame(Vector2 point,int radius,int frame)
    {
        int x=Mathf.RoundToInt(point.x), y=Mathf.RoundToInt(point.y);
        fire.Ellipse(x,y,radius+3,radius+4,Dark);
        fire.Ellipse(x,y+1,radius+1,radius+2,Red);
        fire.Ellipse(x,y,radius-1,radius,Orange);
        fire.Ellipse(x-1,y-1,Mathf.Max(1,radius/2),Mathf.Max(2,radius/2),Yellow);
        fire.Line(new Vector2(x,y+radius),new Vector2(x+Mathf.Sin(frame)*5,y+radius+6),Orange,3);
    }
    private void OnDestroy() { fire?.Dispose(); warning?.Dispose(); }
}
