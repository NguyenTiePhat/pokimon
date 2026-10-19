using UnityEngine;

// Both sides advance on one timeline. Resolve the earliest contact before any
// later character hit, including projectiles that cross completely in one frame.
public static class SpellClash
{
    public static void ResolveStreamOverlaps(PikachuCombat player,ArenaBoss boss)
    {
        while(boss.SpinActive)
        {
            int nearest=-1; float distance=float.MaxValue;
            for(int i=0;i<player.Projectiles.Count;i++)
            {
                var ball=player.Projectiles[i];
                if(!boss.StreamContact(ball.Position,Vector2.zero,Radius(ball),0,out _)) continue;
                float d=Vector2.Distance(boss.CastOrigin,ball.Position);
                if(d<distance) { nearest=i; distance=d; }
            }
            if(nearest<0) return;
            var shot=player.Projectiles[nearest]; float consumed=Mathf.Min(shot.damage,boss.SpinPower);
            player.ClashExplosion(shot.Position,consumed*2); shot.damage-=consumed; boss.ReduceSpin(consumed);
            if(shot.damage<=Epsilon) player.Projectiles.RemoveAt(nearest);
        }
    }
    private const float Epsilon=.00001f;
    private struct Contact
    {
        public int kind, electric, enemy;
        public float time;
    }
    public static void Step(PikachuCombat player,ArenaBoss boss,float delta)
    {
        float left=Mathf.Max(0,delta);
        while(left>Epsilon && !player.BattleEnded)
        {
            var contact=new Contact { kind=-1,time=left+1,electric=-1,enemy=-1 };
            // Clash events win exact ties with character hits and expiry.
            for(int i=0;i<player.Projectiles.Count;i++)
            {
                var ball=player.Projectiles[i]; Vector2 velocity=Velocity(ball);
                float life=Mathf.Max(0,ball.duration-ball.elapsed), horizon=Mathf.Min(left,life);
                for(int j=0;j<boss.Shots.Count;j++)
                {
                    var shot=boss.Shots[j]; float limit=Mathf.Min(horizon,shot.remaining/shot.speed);
                    if(Sweep(ball.Position-shot.position,velocity-shot.direction*shot.speed,Radius(ball)+shot.radius,limit,out float t))
                        Consider(ref contact,0,i,j,t);
                }
                if(boss.SpinActive && boss.StreamContact(ball.Position,velocity,Radius(ball),horizon,out float spinTime))
                    Consider(ref contact,1,i,-1,spinTime);
            }
            for(int i=0;i<player.Projectiles.Count;i++)
            {
                var ball=player.Projectiles[i]; float life=Mathf.Max(0,ball.duration-ball.elapsed);
                if(Sweep(ball.Position-(Vector2)boss.transform.position,Velocity(ball),.35f+ball.charge*.3f,Mathf.Min(left,life),out float t))
                    Consider(ref contact,2,i,-1,t);
                if(life<=left) Consider(ref contact,4,i,-1,life);
            }
            for(int j=0;j<boss.Shots.Count;j++)
            {
                var shot=boss.Shots[j]; float life=Mathf.Max(0,shot.remaining/shot.speed);
                if(Sweep(shot.position-(Vector2)player.Player.transform.position,shot.direction*shot.speed,shot.radius+.22f,Mathf.Min(left,life),out float t))
                    Consider(ref contact,3,-1,j,t);
                if(life<=left) Consider(ref contact,5,-1,j,life);
            }
            float step=contact.kind<0?left:contact.time;
            foreach(var ball in player.Projectiles) ball.elapsed=Mathf.Min(ball.duration,ball.elapsed+step);
            foreach(var shot in boss.Shots) { shot.position+=shot.direction*shot.speed*step; shot.remaining=Mathf.Max(0,shot.remaining-shot.speed*step); }
            left-=step;
            if(contact.kind<0) break;
            if(contact.kind==0)
            {
                var ball=player.Projectiles[contact.electric]; var shot=boss.Shots[contact.enemy];
                Vector2 point=Vector2.Lerp(ball.Position,shot.position,Radius(ball)/(Radius(ball)+shot.radius));
                float consumed=Mathf.Min(ball.damage,shot.damage);
                player.ClashExplosion(point,consumed*2);
                ball.damage=Mathf.Max(0,ball.damage-consumed); shot.damage=Mathf.Max(0,shot.damage-consumed);
                if(ball.damage<=Epsilon) player.Projectiles.RemoveAt(contact.electric);
                if(shot.damage<=Epsilon) boss.Shots.RemoveAt(contact.enemy);
            }
            else if(contact.kind==1)
            {
                var ball=player.Projectiles[contact.electric]; float consumed=Mathf.Min(ball.damage,boss.SpinPower);
                player.ClashExplosion(ball.Position,consumed*2); ball.damage=Mathf.Max(0,ball.damage-consumed); boss.ReduceSpin(consumed);
                if(ball.damage<=Epsilon) player.Projectiles.RemoveAt(contact.electric);
            }
            else if(contact.kind==2 || contact.kind==4)
            {
                var ball=player.Projectiles[contact.electric]; player.Projectiles.RemoveAt(contact.electric);
                player.ResolveProjectileImpact(ball,ball.Position,contact.kind==2);
            }
            else
            {
                var shot=boss.Shots[contact.enemy]; boss.Shots.RemoveAt(contact.enemy);
                boss.ProjectileImpact(shot,contact.kind==3);
            }
        }
    }
    private static Vector2 Velocity(ElectricProjectile ball)=>(ball.end-ball.start)/ball.duration;
    private static float Radius(ElectricProjectile ball)=>ball.basic?.18f:.25f+ball.charge*.3f;
    private static void Consider(ref Contact current,int kind,int electric,int enemy,float time)
    {
        if(time<current.time-Epsilon) current=new Contact { kind=kind,electric=electric,enemy=enemy,time=Mathf.Max(0,time) };
    }
    // Time of first circle contact from relative position and velocity.
    public static bool Sweep(Vector2 relative,Vector2 velocity,float radius,float horizon,out float time)
    {
        time=0;
        float c=relative.sqrMagnitude-radius*radius;
        if(c<=0) return true;
        float a=velocity.sqrMagnitude, b=Vector2.Dot(relative,velocity);
        if(a<Epsilon || b>=0) return false;
        float discriminant=b*b-a*c;
        if(discriminant<0) return false;
        time=(-b-Mathf.Sqrt(discriminant))/a;
        return time>=0 && time<=horizon;
    }
    // Clip a swept circle against the live Fire Spin trapezoid in ground space.
    public static bool ConeContact(ArenaBoss boss,Vector2 origin,Vector2 velocity,float radius,float horizon,out float time)
    {
        Vector2 axis=boss.LockedDirection, side=new Vector2(-axis.y,axis.x), offset=origin-boss.CastOrigin;
        float x=Vector2.Dot(offset,axis), y=Vector2.Dot(offset,side);
        float vx=Vector2.Dot(velocity,axis), vy=Vector2.Dot(velocity,side);
        float enter=0,exit=horizon, margin=radius*Mathf.Sqrt(1+.18f*.18f);
        bool hit=Clip(x-.4f+radius,vx,ref enter,ref exit) && Clip(7+radius-x,-vx,ref enter,ref exit)
            && Clip(.25f+.18f*x-y+margin,.18f*vx-vy,ref enter,ref exit)
            && Clip(.25f+.18f*x+y+margin,.18f*vx+vy,ref enter,ref exit);
        time=enter; return hit && enter<=exit;
    }
    private static bool Clip(float distance,float rate,ref float enter,ref float exit)
    {
        if(Mathf.Abs(rate)<Epsilon) return distance>=0;
        float t=-distance/rate;
        if(rate>0) enter=Mathf.Max(enter,t); else exit=Mathf.Min(exit,t);
        return enter<=exit;
    }
    public static float TraceThunderbolt(PikachuCombat player,ArenaBoss boss,Vector2 start,Vector2 end,float power,out Vector2 beamEnd)
    {
        Vector2 segment=end-start;
        beamEnd=end;
        while(power>Epsilon)
        {
            int index=-1; bool spin=false; float nearest=2;
            for(int i=0;i<boss.Shots.Count;i++)
                if(Sweep(start-boss.Shots[i].position,segment,boss.Shots[i].radius+.1f,1,out float t) && t<nearest)
                { nearest=t; index=i; }
            if(boss.SpinActive && boss.StreamContact(start,segment,.1f,1,out float streamTime) && streamTime<nearest)
            { nearest=streamTime; index=-1; spin=true; }
            if(nearest>1) break;
            Vector2 point=start+segment*nearest;
            float opposing=spin?boss.SpinPower:boss.Shots[index].damage;
            float consumed=Mathf.Min(power,opposing);
            player.ClashExplosion(point,consumed*2); power=Mathf.Max(0,power-consumed);
            if(spin) boss.ReduceSpin(consumed);
            else
            {
                boss.Shots[index].damage=Mathf.Max(0,opposing-consumed);
                if(boss.Shots[index].damage<=Epsilon) boss.Shots.RemoveAt(index);
            }
            if(power<=Epsilon) beamEnd=point;
        }
        return power;
    }
}
