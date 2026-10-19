using UnityEngine;

public sealed class BlastoiseVisuals : MonoBehaviour
{
    private BlastoiseBoss boss;
    private CombatPixelSurface water,warning;
    private int lastFrame=-1;
    private bool pumpWasActive;
    private float pumpStoppedAt=-10;
    private Vector2 lastPumpEnd,lastPumpDirection;
    private static readonly Color32 Cyan=new Color32(77,202,239,235), White=new Color32(221,255,255,255), Deep=new Color32(28,101,188,200);
    public void Initialize(BlastoiseBoss owner)
    {
        boss=owner;
        water=new CombatPixelSurface("Blastoise water jets and pulses",transform,1024,1024,24,14);
        warning=new CombatPixelSurface("Blastoise locked danger lanes",transform,1024,1024,24,4);
        water.Clear(); water.Apply(); warning.Clear(); warning.Apply();
    }
    private void LateUpdate() { Render(Time.time); }
    public void Render(float time)
    {
        if(!boss) return;
        Vector2 origin=boss.transform.position; water.Renderer.transform.position=warning.Renderer.transform.position=origin;
        int frame=Mathf.FloorToInt(time*30); if(frame==lastFrame) return; lastFrame=frame;
        water.Clear(); warning.Clear();
        Vector2 center=new Vector2(512,512), start=center+(boss.CastOrigin-origin)*24;
        Vector2 d=boss.LockedDirection,n=new Vector2(-d.y,d.x);
        if(boss.IsAlive && boss.ActiveSkill>=0)
        {
            if(boss.WindingUp && boss.ActiveSkill!=1)
            {
                Vector2 end=start+d*boss.LaneLength*24; float width=boss.LaneHalfWidth*24;
                var alert=new Color32(255,173,88,220);
                warning.Line(start,end,new Color32(91,219,237,35),Mathf.CeilToInt(width*2));
                warning.Line(start+n*width,end+n*width,alert,2); warning.Line(start-n*width,end-n*width,alert,2);
                warning.Line(end-n*width,end+n*width,alert,2);
                for(int i=1;i<boss.LaneLength;i++)
                {
                    Vector2 p=start+d*i*24; warning.Line(p-d*8-n*6,p,alert,2); warning.Line(p-d*8+n*6,p,alert,2);
                }
                float fraction=boss.PhaseTime/BlastoiseBoss.Windups[boss.ActiveSkill];
                warning.Ellipse((int)start.x,(int)start.y,20,20,new Color32(100,232,255,160),false,2);
                if(boss.ActiveSkill==3)
                {
                    Vector2 mouth=center+(boss.MouthOrigin-origin)*24+Vector2.up*12;
                    int r=Mathf.RoundToInt(3+fraction*5);
                    water.Ellipse((int)mouth.x,(int)mouth.y,r+4,r+4,new Color32(88,198,252,65));
                    water.Ellipse((int)mouth.x,(int)mouth.y,r,r,Cyan);
                    water.Ellipse((int)mouth.x,(int)mouth.y,Mathf.Max(2,r-3),Mathf.Max(2,r-3),White);
                    for(int i=0;i<8;i++)
                    {
                        float a=i*Mathf.PI/4+time*3,travel=Mathf.Repeat(time*1.8f+i*.125f,1);
                        Vector2 p=mouth+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Mathf.Lerp(22,5,travel);
                        water.Ellipse((int)p.x,(int)p.y,2,2,new Color32(180,240,255,210));
                    }
                }
                if(boss.ActiveSkill==0)
                    for(int side=-1;side<=1;side+=2)
                    {
                        Vector2 socket=center+(boss.CannonSocket(side)-origin)*24+Vector2.up*15;
                        int r=Mathf.RoundToInt(3+fraction*5);
                        water.Ellipse((int)socket.x,(int)socket.y,r,r,new Color32(131,235,255,190),false,2);
                        for(int drop=0;drop<4;drop++)
                        {
                            float a=drop*Mathf.PI/2+time*5;
                            Vector2 p=socket+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(14-8*fraction);
                            water.Box((int)p.x,(int)p.y,2,2,White);
                        }
                    }
            }
            if(boss.ActiveSkill==1)
            {
                if(boss.WindingUp)
                {
                    Vector2 mouth=start+d*14.4f+Vector2.up*12;
                    int r=Mathf.RoundToInt(3+7*Mathf.Clamp01(boss.PhaseTime/BlastoiseBoss.Windups[1]));
                    water.Ellipse((int)mouth.x,(int)mouth.y,r,r,Cyan);
                    water.Ellipse((int)mouth.x-2,(int)mouth.y+2,Mathf.Max(1,r/3),Mathf.Max(1,r/3),White);
                }
                Vector2 p=center+(boss.LockedPoint-origin)*24;
                warning.Ellipse((int)p.x,(int)p.y,43,43,new Color32(255,175,80,200),false,2);
                warning.Ellipse((int)p.x,(int)p.y,38,38,new Color32(107,229,246,90),false,1);
                warning.Line(p-Vector2.right*6,p+Vector2.right*6,White,1); warning.Line(p-Vector2.up*6,p+Vector2.up*6,White,1);
            }
            if(boss.SpinActive && boss.ActiveSkill==0)
            {
                DrawPump(center,origin,time);
                lastPumpEnd=boss.StreamEnd; lastPumpDirection=d;
            }
            if(boss.SpinActive && boss.ActiveSkill==3) DrawCannon(center,origin,time);
            if(boss.ActiveSkill==2 && !boss.WindingUp)
            {
                water.Ellipse(512,525,32,19,new Color32(135,236,246,150),false,3);
                for(int i=0;i<6;i++)
                {
                    float a=i*Mathf.PI/3+time*18; Vector2 p=center+Vector2.up*13+new Vector2(Mathf.Cos(a)*32,Mathf.Sin(a)*19);
                    water.Line(p,p+new Vector2(-Mathf.Sin(a)*9,Mathf.Cos(a)*5),White,2);
                }
            }
        }
        bool pumping=boss.IsAlive && boss.SpinActive && boss.ActiveSkill==0;
        if(pumpWasActive && !pumping) pumpStoppedAt=time;
        pumpWasActive=pumping;
        float fade=(time-pumpStoppedAt)/.3f;
        if(!pumping && fade>=0 && fade<1 && boss.IsAlive && !boss.Opponent.BattleEnded)
            PumpFoam(center+(lastPumpEnd-origin)*24+Vector2.up*15,lastPumpDirection,time,1-fade);
        foreach(var shot in boss.Shots)
        {
            Vector2 p=center+(shot.position-origin)*24+Vector2.up*12;
            water.Ellipse((int)p.x,(int)p.y,13,13,new Color32(99,214,255,70));
            water.Ellipse((int)p.x,(int)p.y,10,10,Deep); water.Ellipse((int)p.x-1,(int)p.y+1,8,8,Cyan);
            water.Ellipse((int)p.x-3,(int)p.y+3,3,3,White);
            Vector2 destination=shot.position+shot.direction*shot.remaining;
            Vector2 target=center+(destination-origin)*24;
            warning.Ellipse((int)target.x,(int)target.y,43,43,new Color32(255,185,100,140),false,1);
        }
        foreach(var wave in boss.Waves)
        {
            Vector2 p=center+(wave.position-origin)*24; int r=Mathf.RoundToInt(wave.Radius*24);
            water.Ellipse((int)p.x,(int)p.y,r,r,new Color32(183,247,255,(byte)(255*(1-wave.age/.65f))),false,3);
            water.Ellipse((int)p.x,(int)p.y,Mathf.Max(1,r-4),Mathf.Max(1,r-4),new Color32(65,176,238,130),false,2);
        }
        water.Apply(); warning.Apply();
    }
    private void DrawCannon(Vector2 center,Vector2 origin,float time)
    {
        // Use the same curved centerline and tapered radius as damage/clash checks.
        var colors=new[]{new Color32(71,173,246,45),new Color32(29,116,200,240),
            new Color32(73,198,242,255),new Color32(163,239,255,255),new Color32(236,255,255,255)};
        for(int layer=0;layer<colors.Length;layer++)
            for(int i=0;i<=192;i++)
            {
                float t=i/192f;
                Vector2 p=center+(boss.CannonPoint(t)-origin)*24+Vector2.up*12;
                float radius=boss.CannonRadius(t)*24;
                float r=layer==0?radius+3:layer==1?radius:layer==2?radius*.82f:layer==3?radius*.58f:radius*.28f;
                int pixels=Mathf.Max(1,Mathf.RoundToInt(r));
                water.Ellipse((int)p.x,(int)p.y,pixels,pixels,colors[layer]);
            }
        Vector2 normal=new Vector2(-boss.LockedDirection.y,boss.LockedDirection.x);
        for(int i=0;i<22;i++)
        {
            float t=Mathf.Repeat(time*1.4f+i/22f,1);
            Vector2 p=center+(boss.CannonPoint(t)-origin)*24+Vector2.up*12+normal*Mathf.Sin(i*7)*boss.CannonRadius(t)*15;
            water.Line(p,p-boss.LockedDirection*4,new Color32(214,252,255,230),1);
        }
        Vector2 tip=boss.CannonPoint(1),tipDirection=(tip-boss.CannonPoint(.98f)).normalized;
        PumpFoam(center+(tip-origin)*24+Vector2.up*12,tipDirection,time,.8f);
    }
    private void DrawPump(Vector2 center,Vector2 origin,float time)
    {
        Vector2 join=center+(boss.StreamJoin-origin)*24+Vector2.up*15;
        Vector2 end=center+(boss.StreamEnd-origin)*24+Vector2.up*15;
        // Full-length silhouette is visible immediately, including the first damaging frame.
        float pressure=Mathf.Lerp(.78f,1,Mathf.Clamp01(boss.PhaseTime/.12f));
        int width=Mathf.RoundToInt(boss.LaneHalfWidth*48*pressure);
        // Draw the whole Y one color layer at a time, so no branch border cuts
        // across the other branch's white core at their shared junction.
        var colors=new[]{new Color32(78,183,255,45),new Color32(25,93,185,245),
            new Color32(58,171,235,255),new Color32(139,229,255,255),new Color32(234,255,255,255)};
        for(int layer=0;layer<colors.Length;layer++)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Vector2 socket=center+(boss.CannonSocket(side)-origin)*24+Vector2.up*15;
                PumpRibbon(socket,join,9,width,time,layer,colors[layer],true);
            }
            PumpRibbon(join,end,width,width,time,layer,colors[layer],false);
        }
        // Small bright collars sit at the actual cannon sockets, never at the belly.
        for(int side=-1;side<=1;side+=2)
        {
            Vector2 socket=center+(boss.CannonSocket(side)-origin)*24+Vector2.up*15;
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5+time*3;
                Vector2 p=socket+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*3;
                water.Ellipse((int)p.x,(int)p.y,3,3,new Color32(188,244,255,235));
            }
            water.Ellipse((int)socket.x,(int)socket.y,3,3,White);
        }
        PumpFoam(end,boss.LockedDirection,time,1);
    }
    private void PumpRibbon(Vector2 start,Vector2 end,float startWidth,float endWidth,float time,int layer,Color32 color,bool branch)
    {
        Vector2 direction=boss.LockedDirection,normal=new Vector2(-direction.y,direction.x);
        float length=Vector2.Distance(start,end);
        Vector2 c1=start+direction*length*.42f,c2=end-direction*length*.42f;
        int steps=Mathf.Max(24,Mathf.CeilToInt(length*1.5f));
        for(int i=0;i<=steps;i++)
        {
            float t=i/(float)steps,u=1-t;
            // Matching tangents at the merge remove the sharp V. Smooth width
            // interpolation gives both tributaries the same thickness at the join.
            Vector2 p=branch?u*u*u*start+3*u*u*t*c1+3*u*t*t*c2+t*t*t*end:Vector2.Lerp(start,end,t);
            if(!branch) p+=normal*Mathf.Sin(t*21-time*15)*Mathf.Pow(Mathf.Sin(t*Mathf.PI),2)*.65f;
            float width=Mathf.Lerp(startWidth,endWidth,t*t*(3-2*t));
            float thickness=layer==0?width+6:layer==1?width:layer==2?width-3:layer==3?width*.65f:width*.36f;
            int radius=Mathf.Max(1,Mathf.RoundToInt(thickness*.5f));
            water.Ellipse(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),radius,radius,color);
        }
        if(layer!=4 || branch) return;
        for(int i=0;i<9;i++)
        {
            float t=Mathf.Repeat(time*1.7f+i/9f,1);
            Vector2 p=Vector2.Lerp(start,end,t)+normal*(i%2==0?1:-1)*endWidth*.28f;
            water.Line(p,p-direction*5,new Color32(211,251,255,240),2);
        }
    }
    private void PumpFoam(Vector2 end,Vector2 direction,float time,float opacity)
    {
        Vector2 normal=new Vector2(-direction.y,direction.x);
        // Expanding pale lobes and outward droplets, matching the reference's splash cap.
        for(int i=0;i<12;i++)
        {
            float age=Mathf.Repeat(time*2+i*.0833f,1), angle=i*2.39996f;
            Vector2 p=end+direction*(age*21+Mathf.Cos(angle)*8)+normal*Mathf.Sin(angle)*(7+age*22);
            int r=Mathf.RoundToInt(4+age*7);
            water.Ellipse((int)p.x,(int)p.y,r+3,r+3,new Color32(105,206,249,(byte)(60*opacity*(1-age))));
            water.Ellipse((int)p.x,(int)p.y,r,r,new Color32(190,239,252,(byte)(210*opacity*(1-age))));
            water.Ellipse((int)p.x-1,(int)p.y+1,Mathf.Max(2,r-2),Mathf.Max(2,r-2),new Color32(239,255,255,(byte)(245*opacity*(1-age))));
        }
        for(int i=0;i<14;i++)
        {
            float age=Mathf.Repeat(time*1.8f+i/14f,1);
            Vector2 p=end+direction*(8+age*39)+normal*Mathf.Sin(i*5.7f)*age*38-Vector2.up*age*age*13;
            water.Line(p,p-direction*3,new Color32(184,241,255,(byte)(230*opacity*(1-age))),2);
        }
    }
    private void OnDestroy() { water?.Dispose(); warning?.Dispose(); }
}
