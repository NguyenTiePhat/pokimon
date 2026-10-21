using UnityEngine;

public sealed class LucarioVisuals : MonoBehaviour
{
    private LucarioBoss boss;
    private CombatPixelSurface ground, effects;
    private readonly SpriteRenderer[] echoes = new SpriteRenderer[5];
    private float nextFrame;
    private static readonly Color32 Ink = new Color32(19,35,52,255);
    private static readonly Color32 Cyan = new Color32(111,236,255,255);
    private static readonly Color32 White = new Color32(240,255,255,255);
    private static readonly Color32 Gold = new Color32(255,215,105,255);
    private static readonly Color32 Orange = new Color32(255,145,73,255);
    public void Initialize(LucarioBoss owner)
    {
        boss = owner;
        ground = new CombatPixelSurface("Locked Lucario ground warnings", transform, 768, 576, 24, 5);
        effects = new CombatPixelSurface("Aura, steel slashes and meteor shockwave", transform, 768, 576, 24, 14);
        for (int i = 0; i < echoes.Length; i++)
        {
            var obj = new GameObject("Extreme Speed afterimage " + i);
            obj.transform.SetParent(transform, false);
            echoes[i] = obj.AddComponent<SpriteRenderer>();
            echoes[i].sortingOrder = 9;
            echoes[i].enabled = false;
        }
        Render(0);
    }
    private void LateUpdate()
    {
        if (Time.unscaledTime < nextFrame) return;
        nextFrame = Time.unscaledTime + 1f / 30;
        Render(Time.time);
    }
    private static Vector2 Pixel(Vector2 world) => new Vector2(384,288) + world * 24;
    public void Render(float time)
    {
        if (boss == null || ground == null) return;
        ground.Renderer.transform.position = Vector3.zero;
        effects.Renderer.transform.position = Vector3.zero;
        ground.Clear();
        effects.Clear();
        foreach (var echo in echoes) echo.enabled = false;
        Vector2 body = Pixel(boss.transform.position);
        if (boss.IsAlive && !boss.Opponent.BattleEnded && !boss.Opponent.Player.IsEntering)
        {
            if (boss.Awakened)
            {
                for (int i = 0; i < 7; i++)
                {
                    float a = i * Mathf.PI * 2 / 7 + time * 2;
                    Vector2 p = body + new Vector2(Mathf.Cos(a)*16, Mathf.Sin(a)*6 + 10);
                    effects.Line(p, p + Vector2.up * (4 + i % 3), new Color32(107,228,255,140), 2);
                }
            }
            if (boss.AwakeningRemaining > 0)
            {
                int r = 18 + (int)((1.4f - boss.AwakeningRemaining) * 40);
                effects.Ellipse((int)body.x,(int)body.y,r,r/2,new Color32(123,223,255,170),false,2);
            }
            if (boss.ActiveSkill >= 0) Attack(time);
            foreach (var shot in boss.Shots)
            {
                Vector2 p = Pixel(shot.position);
                for (int i = 3; i > 0; i--)
                {
                    Vector2 q = p - shot.direction * i * 7;
                    effects.Ellipse((int)q.x,(int)q.y,8-i,8-i,new Color32(64,173,247,(byte)(120-i*24)));
                }
                Orb(p,8,time);
            }
        }
        ground.Apply();
        effects.Apply();
    }
    private void Attack(float time)
    {
        int skill = boss.ActiveSkill;
        Vector2 start = Pixel(boss.CastOrigin), target = Pixel(boss.Target);
        Vector2 dir = boss.LockedDirection, side = new Vector2(-dir.y,dir.x);
        float t = Mathf.Clamp01(boss.PhaseTime / LucarioBoss.Windups[skill]);
        if (skill == 0)
        {
            if (boss.WindingUp)
            {
                Vector2 end = start + dir * 24 * 22;
                ground.Line(start + side*14,end + side*14,new Color32(99,226,255,150));
                ground.Line(start - side*14,end - side*14,new Color32(99,226,255,150));
                for (int i = 1; i < 12; i++) Arrow(start + dir * i * 35, dir, Cyan, 4);
                Orb(start + dir*15,4+(int)(t*4),time*2);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3 + time * 3;
                    Vector2 d = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    effects.Line(start+dir*15+d*23*(1-t),start+dir*15+d*29*(1-t),Cyan);
                }
            }
        }
        else if (skill == 1)
        {
            float aim = Mathf.Atan2(dir.y,dir.x);
            float radius = LucarioBoss.ClawRange * 24;
            for (int a = -55; a <= 55; a += 2)
            {
                Vector2 d = new Vector2(Mathf.Cos(aim+a*Mathf.Deg2Rad),Mathf.Sin(aim+a*Mathf.Deg2Rad));
                ground.Line(start,start+d*radius,new Color32(255,159,74,45),2);
            }
            Arc(ground,start,radius,aim,55,Ink,5);
            Arc(ground,start,radius,aim,55,Orange,2);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                float a = aim + sign * LucarioBoss.ClawHalfAngle * Mathf.Deg2Rad;
                ground.Line(start,start + new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,Orange,2);
            }
            // Three small beats remain readable separately from the damage arc.
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = start - dir*11 + side*(i-1)*9;
                ground.Ellipse((int)p.x,(int)p.y,3,3,i < boss.ClawsReleased ? Ink : Gold);
            }
            if (!boss.WindingUp)
            {
                float beat = boss.PhaseTime % .3f;
                if (beat >= .03f && beat < .21f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float r = radius - 6 - i*8;
                        Arc(effects,start,r,aim+(boss.ClawsReleased%2==0?-.2f:.2f),43,Cyan,5);
                        Arc(effects,start,r,aim,43,White,2);
                    }
                }
            }
        }
        else if (skill == 2)
        {
            Vector2 end = Pixel(boss.DashEnd), lane = (end-start).normalized;
            int width = Mathf.RoundToInt(LucarioBoss.LaneRadius*24);
            Capsule(start,end,width+3,Ink);
            Capsule(start,end,width,Gold);
            Capsule(start,end,width-2,new Color32(222,143,52,65));
            for (float d = 20; d < Vector2.Distance(start,end)-8; d += 27) Arrow(start+lane*d,lane,Gold,6);
            if (boss.WindingUp)
            {
                Vector2 p = Vector2.Lerp(start,end,t);
                ground.Line(p-side*width,p+side*width,White,2);
                effects.Line(start-dir*15+side*9,start-dir*7+side*9,Cyan,2);
                effects.Line(start-dir*15-side*9,start-dir*7-side*9,Cyan,2);
            }
            else
            {
                for (int i = 0; i < echoes.Length; i++)
                {
                    float progress = Mathf.Clamp01((boss.PhaseTime-.025f*(i+1))/.28f);
                    echoes[i].sprite = boss.Body.sprite;
                    echoes[i].transform.position = Vector2.Lerp(boss.CastOrigin,boss.DashEnd,progress);
                    echoes[i].transform.localScale = boss.Body.transform.localScale;
                    echoes[i].color = new Color(.35f,.85f,1,.4f-i*.065f);
                    echoes[i].enabled = true;
                }
                Vector2 p = Pixel(boss.transform.position);
                for (int i = -1; i <= 1; i++)
                    effects.Line(p-dir*35+side*i*7,p-dir*5+side*i*7,i==0?White:Cyan,i==0?3:1);
            }
        }
        else
        {
            int r = Mathf.RoundToInt(LucarioBoss.MeteorRadius*24), x = (int)target.x, y = (int)target.y;
            ground.Ellipse(x,y,r,r,new Color32(255,160,64,55));
            ground.Ellipse(x,y,r+3,r+3,Ink,false,5);
            ground.Ellipse(x,y,r,r,Gold,false,3);
            ground.Ellipse(x,y,r-5,r-5,new Color32(255,176,78,170),false);
            for (int i = 0; i < 12; i++)
            {
                float a = i*Mathf.PI/6;
                Vector2 d = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                ground.Line(target+d*(r+5),target+d*(r+11),Gold,2);
            }
            ground.Line(target-Vector2.right*7,target+Vector2.right*7,Gold,2);
            ground.Line(target-Vector2.up*7,target+Vector2.up*7,Gold,2);
            if (boss.WindingUp)
            {
                int countdown = Mathf.Max(2,(int)(r*(1-t)));
                ground.Ellipse(x,y,countdown,countdown,White,false,2);
                Vector2 p = Pixel(boss.transform.position)+Vector2.up*(boss.Height*24+14);
                Orb(p+dir*8,5+(int)(t*5),time*2);
                if (t > .65f)
                {
                    effects.Line(p+Vector2.up*20,p,Gold,3);
                    effects.Line(p+Vector2.up*28+Vector2.left*7,p+Vector2.left*7,Cyan,2);
                }
            }
            else
            {
                // The shockwave is entirely inside the advertised circle, with one damage event.
                float impact = Mathf.Clamp01(boss.PhaseTime/.4f);
                int wave = Mathf.Max(2,(int)(r*impact));
                effects.Ellipse(x,y,wave,wave,Gold,false,4);
                effects.Ellipse(x,y,Mathf.Max(2,wave-5),Mathf.Max(2,wave-5),White,false,2);
                for (int i = 0; i < 9; i++)
                {
                    float a = i*2.39996f;
                    Vector2 d = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    Vector2 p = target+d*(8+impact*(r-10));
                    effects.Line(p,p+d*5, i%2==0?Gold:Cyan,3);
                    ground.Line(target+d*12,target+d*(r-14),new Color32(43,57,65,230),2);
                }
            }
        }
    }
    private void Capsule(Vector2 a, Vector2 b, int radius, Color32 color)
    {
        Vector2 segment=b-a;
        float length=segment.sqrMagnitude;
        for(int y=Mathf.Max(0,(int)Mathf.Min(a.y,b.y)-radius);y<=Mathf.Min(ground.Height-1,Mathf.Max(a.y,b.y)+radius);y++)
            for(int x=Mathf.Max(0,(int)Mathf.Min(a.x,b.x)-radius);x<=Mathf.Min(ground.Width-1,Mathf.Max(a.x,b.x)+radius);x++)
            {
                Vector2 p=new Vector2(x,y);
                Vector2 nearest=a+segment*(length<.001f?0:Mathf.Clamp01(Vector2.Dot(p-a,segment)/length));
                if((p-nearest).sqrMagnitude<=radius*radius)ground.Dot(x,y,color);
            }
    }
    private void Arrow(Vector2 p, Vector2 dir, Color32 color, float size)
    {
        Vector2 side = new Vector2(-dir.y,dir.x);
        ground.Line(p-dir*size+side*size,p,color,2);
        ground.Line(p-dir*size-side*size,p,color,2);
    }
    private static void Arc(CombatPixelSurface surface, Vector2 p, float radius, float aim, float degrees, Color32 color, int width)
    {
        Vector2 previous = Vector2.zero;
        for (int i = 0; i <= 28; i++)
        {
            float a = aim + Mathf.Lerp(-degrees,degrees,i/28f)*Mathf.Deg2Rad;
            Vector2 current = p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
            if (i > 0) surface.Line(previous,current,color,width);
            previous = current;
        }
    }
    private void Orb(Vector2 p, int radius, float time)
    {
        int x=(int)p.x,y=(int)p.y;
        effects.Ellipse(x,y,radius+4,radius+4,new Color32(64,166,255,65));
        effects.Ellipse(x,y,radius,radius,new Color32(30,108,206,255));
        effects.Ellipse(x,y,radius-2,radius-2,Cyan);
        effects.Ellipse(x-2,y+2,Mathf.Max(2,radius-5),Mathf.Max(2,radius-5),White);
        for (int i=0;i<3;i++)
        {
            float a=time*7+i*Mathf.PI*2/3;
            Vector2 q=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius+2);
            effects.Box((int)q.x,(int)q.y,2,2,White);
        }
    }
    private void OnDestroy() { ground?.Dispose(); effects?.Dispose(); }
}
