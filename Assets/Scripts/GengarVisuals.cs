using UnityEngine;

public sealed class GengarVisuals : MonoBehaviour
{
    private GengarBoss boss;
    private CombatPixelSurface ground,effects;
    private static readonly Color32 Cyan=new Color32(159,249,255,255), Gold=new Color32(255,234,152,255);
    public void Initialize(GengarBoss owner)
    {
        boss=owner;
        ground=new CombatPixelSurface("Persistent ghost warning rings",transform,1024,1024,24,5);
        effects=new CombatPixelSurface("Shadow projectiles and ghost strikes",transform,1024,1024,24,14);
    }
    private void LateUpdate()=>Render(Time.time);
    private Vector2 Pixel(Vector2 world)=>new Vector2(512,512)+(world-(Vector2)transform.position)*24;
    public void Render(float time)
    {
        ground.Clear(); effects.Clear();
        foreach(var patch in boss.Patches)
        {
            Vector2 p=Pixel(patch.position);
            ground.Ellipse((int)p.x,(int)p.y,35,35,new Color32(100,163,43,155));
            ground.Ellipse((int)p.x,(int)p.y,35,35,new Color32(201,242,101,255),false,2);
            for(int i=0;i<7;i++) { float a=i*2.4f+time; Vector2 q=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*22; ground.Ellipse((int)q.x,(int)q.y,3,3,Gold,false); }
        }
        if(boss.IsAlive&&boss.ActiveSkill>=0)
        {
            Vector2 start=Pixel(boss.CastOrigin),target=Pixel(boss.Target);
            float t=Mathf.Clamp01(boss.PhaseTime/GengarBoss.Windups[boss.ActiveSkill]);
            if(boss.ActiveSkill==0&&boss.WindingUp)
            {
                Vector2 n=new Vector2(-boss.LockedDirection.y,boss.LockedDirection.x)*8,end=start+boss.LockedDirection*432;
                ground.Line(start+n,end+n,Cyan); ground.Line(start-n,end-n,Cyan);
                Orb(start+boss.LockedDirection*16,6+(int)(t*5),false);
            }
            else if(boss.ActiveSkill>=1)
            {
                int r=Mathf.RoundToInt(boss.Radius*24),x=(int)target.x,y=(int)target.y;
                Color32 rim=boss.ActiveSkill==1?Gold:Cyan;
                ground.Ellipse(x,y,r,r,new Color32(141,77,205,55));
                ground.Ellipse(x,y,r+3,r+3,new Color32(16,10,35,255),false,4);
                ground.Ellipse(x,y,r,r,rim,false,2);
                // A fixed boundary remains visible even while the countdown contracts.
                if(boss.WindingUp)ground.Ellipse(x,y,Mathf.Max(2,(int)(r*(1-t))),Mathf.Max(2,(int)(r*(1-t))),new Color32(239,208,255,180),false);
                ground.Line(target+Vector2.left*7,target+Vector2.right*7,rim,2);
                ground.Line(target+Vector2.down*7,target+Vector2.up*7,rim,2);
                for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a)); ground.Line(target+d*(r+4),target+d*(r+9),rim,2); }
                if(boss.ActiveSkill==1&&boss.WindingUp)Orb(Vector2.Lerp(start,target,t)+Vector2.up*(Mathf.Sin(t*Mathf.PI)*42),8,true);
                if(!boss.WindingUp)
                {
                    int burst=(int)(r*Mathf.Clamp01(boss.PhaseTime/.35f));
                    effects.Ellipse(x,y,Mathf.Max(2,burst),Mathf.Max(2,burst),rim,false,3);
                    if(boss.ActiveSkill>=2)
                    {
                        effects.Box(x-10,y+5,20,17,new Color32(158,107,223,230));
                        for(int i=0;i<4;i++)effects.Box(x-10+i*5,y+19,4,7,Cyan);
                    }
                }
            }
        }
        foreach(var shot in boss.Shots)Orb(Pixel(shot.position),8,false);
        ground.Apply(); effects.Apply();
    }
    private void Orb(Vector2 p,int r,bool poison)
    {
        int x=(int)p.x,y=(int)p.y;
        effects.Ellipse(x,y,r+4,r+4,new Color32(155,88,224,70));
        effects.Ellipse(x,y,r,r,poison?new Color32(155,204,59,255):new Color32(178,113,245,255));
        effects.Ellipse(x,y,r-3,r-3,new Color32(40,15,69,255));
        effects.Ellipse(x-3,y+3,2,2,poison?Gold:Cyan);
    }
    private void OnDestroy() { ground?.Dispose(); effects?.Dispose(); }
}
