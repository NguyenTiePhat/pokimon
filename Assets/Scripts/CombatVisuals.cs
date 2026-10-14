using System.Collections.Generic;
using UnityEngine;

public sealed class CombatVisuals : MonoBehaviour
{
    private PikachuCombat combat;
    private CombatPixelSurface aura, beam, reticle, rangeRing;
    private CombatPixelSurface hitReaction, stunStars;
    public bool StunVisible => stunStars != null && stunStars.Renderer.enabled;
    public bool HitVisible => hitReaction != null && hitReaction.Renderer.enabled;
    public bool RangeVisible => rangeRing != null && rangeRing.Renderer.enabled;
    private readonly List<CombatPixelSurface> balls = new List<CombatPixelSurface>();
    private readonly List<CombatPixelSurface> explosions = new List<CombatPixelSurface>();
    private readonly List<CombatPixelSurface> craters = new List<CombatPixelSurface>();
    private int previousFrame = -1;
    private static readonly Color32 Gold = new Color32(255, 202, 46, 255);
    private static readonly Color32 Cyan = new Color32(132, 228, 255, 210);

    public void Initialize(PikachuCombat owner)
    {
        combat = owner;
        hitReaction = new CombatPixelSurface("Pikachu hit sparks",transform,96,96,32,18);
        stunStars = new CombatPixelSurface("Pikachu stunned orbiting stars",transform,96,96,32,20);
        hitReaction.Renderer.enabled=stunStars.Renderer.enabled=false;
        aura = new CombatPixelSurface("Casting and speed aura", transform, 128, 128, 32, 11);
        beam = new CombatPixelSurface("Thunderbolt cheek discharge", transform, 640, 640, 32, 12);
        reticle = new CombatPixelSurface("Selected impact point", transform, 128, 128, 64, 5);
        rangeRing = new CombatPixelSurface("E C actual cast radius", transform, 544, 544, 32, 4);
        rangeRing.Clear();
        rangeRing.Ellipse(272,272,256,256,new Color32(47,222,205,9));
        rangeRing.Ellipse(272,272,258,258,new Color32(60,234,218,50),false,4);
        rangeRing.Ellipse(272,272,256,256,new Color32(86,246,232,200),false,1);
        rangeRing.Ellipse(272,272,253,253,new Color32(123,248,245,70),false,1);
        rangeRing.Apply();
        aura.Renderer.enabled = beam.Renderer.enabled = reticle.Renderer.enabled = rangeRing.Renderer.enabled = false;
    }
    private void LateUpdate() { RenderFrame(Time.time); }
    public void RenderFrame(float time)
    {
        if (!combat) return;
        int frame = Mathf.FloorToInt(time * 24);
        Vector2 player = combat.Player.transform.position;
        hitReaction.Renderer.transform.position=combat.Player.VisualPosition+Vector2.up*.4f;
        stunStars.Renderer.transform.position=combat.Player.VisualPosition+Vector2.up*1.05f;
        hitReaction.Renderer.enabled=combat.HitRemaining>0;
        stunStars.Renderer.enabled=combat.StunRemaining>0 && !combat.BattleEnded;
        aura.Renderer.transform.position = combat.Player.VisualPosition + Vector2.up * .4f;
        beam.Renderer.transform.position = player;
        reticle.Renderer.transform.position = combat.IndicatorPoint;
        rangeRing.Renderer.transform.position = combat.IndicatorOrigin;
        rangeRing.Renderer.transform.localScale = Vector3.one * (combat.IndicatorRange / 8f);
        rangeRing.Renderer.enabled = combat.IndicatorSkill == 0 || combat.IndicatorSkill == 2;
        aura.Renderer.enabled = !combat.Player.IsEntering && (combat.IsCasting || combat.QuickRemaining > 0 || combat.DashRemaining > 0);
        beam.Renderer.enabled = combat.BeamRemaining > 0;
        reticle.Renderer.enabled = combat.IndicatorSkill >= 0 || combat.ActiveSkill == 3;
        // Move effect sprites every frame; redraw their pixel content at 24 Hz.
        EnsurePool(balls, combat.Projectiles.Count, "Electro Ball", 14);
        EnsurePool(explosions, combat.Impacts.Count, "Impact explosion", 13);
        for (int i = 0; i < combat.Projectiles.Count; i++)
        {
            var ball = combat.Projectiles[i];
            balls[i].Renderer.transform.position = ball.Position + Vector2.up * ball.Height;
            balls[i].Renderer.transform.localScale = Vector3.one * (ball.basic ? .55f : 1f);
        }
        for (int i = 0; i < combat.Impacts.Count; i++) explosions[i].Renderer.transform.position = combat.Impacts[i].position;
        if (frame == previousFrame) return;
        previousFrame = frame;
        if(hitReaction.Renderer.enabled)
        {
            hitReaction.Clear();
            float progress=1-combat.HitRemaining/.28f;
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                hitReaction.Line(new Vector2(48,48)+d*(9+progress*10),new Vector2(48,48)+d*(15+progress*13),new Color32(255,237,202,(byte)(255*(1-progress))),2);
            }
            hitReaction.Apply();
        }
        if(stunStars.Renderer.enabled)
        {
            stunStars.Clear();
            stunStars.Ellipse(48,48,21,7,new Color32(255,221,79,90),false);
            for(int i=0;i<3;i++)
            {
                float a=time*5+i*Mathf.PI*2/3;
                Vector2 center=new Vector2(48+Mathf.Cos(a)*21,48+Mathf.Sin(a)*7);
                Vector2 first=Vector2.zero, previous=Vector2.zero;
                for(int j=0;j<=10;j++)
                {
                    float angle=Mathf.PI*.5f+j*Mathf.PI/5;
                    Vector2 point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(j%2==0?6:2.5f);
                    if(j==0) first=point;
                    else stunStars.Line(previous,j==10?first:point,new Color32(255,222,75,255),2);
                    previous=point;
                }
                stunStars.Ellipse(Mathf.RoundToInt(center.x),Mathf.RoundToInt(center.y),2,2,new Color32(255,255,220,255));
            }
            stunStars.Apply();
        }
        if (aura.Renderer.enabled)
        {
            aura.Clear();
            if (combat.QuickRemaining > 0 || combat.DashRemaining > 0)
            {
                aura.Ellipse(64,57,15,21,new Color32(100,213,255,100),false);
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * 1.2566f + frame * .17f;
                    Vector2 point = new Vector2(64 + Mathf.Cos(angle)*18,57 + Mathf.Sin(angle)*22);
                    aura.Bolt(point,point + new Vector2(3,6), frame+i,Cyan,.45f);
                }
            }
            if (combat.IsCasting)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * .5f + frame*.22f;
                    Vector2 p = new Vector2(64 + Mathf.Cos(angle)*22,59 + Mathf.Sin(angle)*19);
                    aura.Bolt(p,p+new Vector2(6,9),frame+i,Gold,.55f);
                }
                if (combat.ActiveSkill == 3) DrawBall(aura,64,92,combat.Charge01,frame);
                if (combat.ActiveSkill == 2)
                {
                    Vector2 tail = new Vector2(64,58) - combat.Player.FacingVector*13;
                    aura.Bolt(tail,tail + new Vector2(8,17),frame,new Color32(210,241,255,255),1);
                }
            }
            aura.Apply();
        }
        if (beam.Renderer.enabled)
        {
            beam.Clear();
            Vector2 start = new Vector2(320,336) + combat.Player.FacingVector*5;
            Vector2 end = new Vector2(320,320) + (combat.BeamEnd-player)*32;
            beam.Bolt(start+new Vector2(0,3),end,frame,Gold,1.2f);
            beam.Bolt(start-new Vector2(0,3),end+new Vector2(0,3),frame+11,Gold,.65f);
            beam.Ellipse(Mathf.RoundToInt(end.x),Mathf.RoundToInt(end.y),13,18,new Color32(255,231,83,220),false,2);
            beam.Apply();
        }
        if (reticle.Renderer.enabled)
        {
            reticle.Clear();
            bool valid = combat.IsCasting || combat.AimTargetValid;
            Color32 color = valid ? new Color32(81,244,223,230) : new Color32(255,111,95,195);
            Color32 glow = new Color32(color.r,color.g,color.b,45);
            int radius = combat.IndicatorSkill == 2 ? 54 : 47;
            // Two luminous rings, translucent ground fill, inward markers and a bright center.
            reticle.Ellipse(64,64,radius,radius,new Color32(color.r,color.g,color.b,17));
            reticle.Ellipse(64,64,radius+2,radius+2,glow,false,5);
            reticle.Ellipse(64,64,radius,radius,color,false,1);
            reticle.Ellipse(64,64,radius-5,radius-5,new Color32(color.r,color.g,color.b,145),false,1);
            for (int i=0;i<4;i++)
            {
                float angle=i*Mathf.PI*.5f;
                Vector2 axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                Vector2 side=new Vector2(-axis.y,axis.x);
                Vector2 edge=new Vector2(64,64)+axis*(radius-2);
                reticle.Line(edge-side*4,edge-axis*8,color,2);
                reticle.Line(edge+side*4,edge-axis*8,color,2);
                reticle.Line(new Vector2(64,64)+axis*9,edge-axis*13,glow,1);
            }
            reticle.Ellipse(64,64,6,6,glow);
            reticle.Ellipse(64,64,2,2,new Color32(213,255,250,255));
            reticle.Apply();
        }
        for (int i = 0; i < combat.Projectiles.Count; i++)
        { balls[i].Clear(); DrawBall(balls[i],64,64,combat.Projectiles[i].charge,frame); balls[i].Apply(); }
        for (int i = 0; i < combat.Impacts.Count; i++)
        {
            var impact = combat.Impacts[i]; var surface = explosions[i]; surface.Clear();
            float life = Mathf.Clamp01(impact.age / .65f);
            int radius = Mathf.RoundToInt((8 + life*28)*impact.radius);
            Color32 color = impact.iron ? new Color32(220,244,255,(byte)((1-life)*255)) : new Color32(255,202,46,(byte)((1-life)*255));
            if(impact.clash)
            {
                surface.Ellipse(64,64,radius+5,radius+5,new Color32(91,222,255,(byte)((1-life)*150)),false,2);
                surface.Ellipse(64,64,radius,radius,new Color32(255,153,52,(byte)((1-life)*230)),false,3);
            }
            surface.Ellipse(64,64,radius,Mathf.Max(3,radius/2),color,false,2);
            if (life < .35f) surface.Ellipse(64,70,Mathf.RoundToInt((1-life)*13),Mathf.RoundToInt((1-life)*17),new Color32(255,255,211,220));
            for (int ray = 0; ray < 7; ray++)
            {
                float angle = ray * .8976f;
                Vector2 p = new Vector2(64+Mathf.Cos(angle)*radius,64+Mathf.Sin(angle)*radius*.65f);
                surface.Bolt(p,p+new Vector2(Mathf.Cos(angle)*9,Mathf.Sin(angle)*12+4),frame+ray,color,.55f);
            }
            surface.Apply();
        }
    }

    private static void DrawBall(CombatPixelSurface surface, int x, int y, float charge, int frame)
    {
        int radius = Mathf.RoundToInt(7+charge*17);
        surface.Ellipse(x,y,radius+5,radius+5,new Color32(255,201,35,32));
        surface.Ellipse(x,y,radius+2,radius+2,new Color32(255,176,24,90));
        surface.Ellipse(x,y,radius,radius,new Color32(231,153,25,255));
        surface.Ellipse(x-1,y+1,radius-2,radius-2,new Color32(255,222,78,255));
        surface.Ellipse(x-3,y+3,Mathf.Max(2,radius-6),Mathf.Max(2,radius-6),new Color32(255,255,211,255));
        for(int i=0;i<3;i++)
        {
            float a = i*2.094f+frame*.2f;
            Vector2 p = new Vector2(x+Mathf.Cos(a)*(radius+2),y+Mathf.Sin(a)*(radius+2));
            surface.Bolt(p,p+new Vector2(5,8),frame+i,Gold,.5f);
        }
    }
    private void EnsurePool(List<CombatPixelSurface> pool, int needed, string label, int order)
    {
        while (pool.Count < needed) pool.Add(new CombatPixelSurface(label,transform,128,128,32,order));
        for (int i=0;i<pool.Count;i++) pool[i].Renderer.enabled = i < needed;
    }
    public void AddCrater(Vector2 point, float radius)
    {
        var surface = new CombatPixelSurface("Permanent Electro Ball crater",null,128,96,32,1);
        surface.Renderer.transform.position = point;
        surface.Clear();
        int rx = Mathf.RoundToInt(radius*30), ry = Mathf.RoundToInt(radius*17);
        surface.Ellipse(64,48,rx+4,ry+3,new Color32(30,24,24,230));
        surface.Ellipse(64,48,rx,ry,new Color32(91,71,46,255));
        surface.Ellipse(64,50,rx-3,ry-3,new Color32(13,15,20,255));
        surface.Ellipse(64,53,Mathf.Max(2,rx-8),Mathf.Max(2,ry-7),new Color32(7,9,13,255));
        for (int i=0;i<12;i++)
        {
            float a=i*.5236f;
            Vector2 p=new Vector2(64+Mathf.Cos(a)*rx,48+Mathf.Sin(a)*ry);
            surface.Line(p,p+new Vector2(Mathf.Cos(a)*9,Mathf.Sin(a)*6),new Color32(18,17,19,240),2);
            surface.Box(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),3,2,new Color32(118,88,52,255));
        }
        surface.Apply(); craters.Add(surface);
    }
    private void OnDestroy()
    {
        aura?.Dispose(); beam?.Dispose(); reticle?.Dispose(); rangeRing?.Dispose();
        hitReaction?.Dispose(); stunStars?.Dispose();
        foreach(var item in balls) item.Dispose();
        foreach(var item in explosions) item.Dispose();
        foreach(var item in craters) item.Dispose();
    }
}
