using System;
using System.Collections.Generic;
using UnityEngine;

// Host owns all gameplay state; clients send intentions only.
public static class LanDuelRules
{
    public const int Version = 1;
    [Serializable] public sealed class Command
    {
        public int version = Version, pokemon, map, step;
        public bool ready, fire, dash;
        public float x, y, aimX = 1, aimY;
    }
    [Serializable] public sealed class Fighter
    {
        public int pokemon, hp = 100, facing;
        public bool ready;
        public float x, y, shotCooldown, dashCooldown;
    }
    [Serializable] public sealed class Shot
    {
        public int owner;
        public float x, y, vx, vy, life = 1.6f;
    }
    [Serializable] public sealed class State
    {
        public int version = Version, step, map, winner = -1;
        public Fighter[] players = { new Fighter { x = -5, facing = 2 }, new Fighter { x = 5, facing = 6 } };
        public List<Shot> shots = new List<Shot>();
    }
    public static bool Valid(Command c) => c != null && c.version == Version && c.pokemon >= 0 && c.pokemon < 6
        && c.map >= 0 && c.map < 5 && c.step >= 0 && c.step <= 3
        && Finite(c.x) && Finite(c.y) && Finite(c.aimX) && Finite(c.aimY);
    static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    public static void Tick(State state, Command[] inputs, float dt)
    {
        if (state.step != 2) return;
        for (int i = 0; i < 2; i++)
        {
            var p = state.players[i]; var c = inputs[i];
            p.shotCooldown = Mathf.Max(0, p.shotCooldown - dt); p.dashCooldown = Mathf.Max(0, p.dashCooldown - dt);
            Vector2 move = Vector2.ClampMagnitude(new Vector2(Mathf.Clamp(c.x,-1,1), Mathf.Clamp(c.y,-1,1)), 1);
            if (move.sqrMagnitude > .01f) p.facing = PikachuPlayer.DirectionFor(move);
            float speed = 3.6f;
            Vector2 pos = new Vector2(p.x, p.y) + move * speed * dt;
            if (c.dash && p.dashCooldown <= 0 && move.sqrMagnitude > .01f) { pos += move * 1.6f; p.dashCooldown = 2.5f; }
            p.x = Mathf.Clamp(pos.x, -8.7f, 8.7f); p.y = Mathf.Clamp(pos.y, -4.3f, 4.3f);
            if (c.fire && p.shotCooldown <= 0)
            {
                Vector2 aim = new Vector2(c.aimX, c.aimY);
                // Normalize safely after clamping untrusted inputs.
                aim = new Vector2(Mathf.Clamp(aim.x,-1,1), Mathf.Clamp(aim.y,-1,1)).normalized;
                if (aim.sqrMagnitude < .1f) aim = i == 0 ? Vector2.right : Vector2.left;
                state.shots.Add(new Shot { owner = i, x = p.x, y = p.y, vx = aim.x * 9, vy = aim.y * 9 });
                p.shotCooldown = .45f;
            }
        }
        for (int i = state.shots.Count - 1; i >= 0; i--)
        {
            var s = state.shots[i]; Vector2 from = new Vector2(s.x, s.y);
            s.x += s.vx * dt; s.y += s.vy * dt; s.life -= dt;
            var target = state.players[1 - s.owner];
            Vector2 segment = new Vector2(s.x,s.y) - from;
            float t = segment.sqrMagnitude == 0 ? 0 : Mathf.Clamp01(Vector2.Dot(new Vector2(target.x,target.y)-from,segment)/segment.sqrMagnitude);
            if (Vector2.Distance(from + segment*t,new Vector2(target.x,target.y)) < .48f)
            { target.hp = Mathf.Max(0,target.hp-10); state.shots.RemoveAt(i); }
            else if (s.life <= 0 || Mathf.Abs(s.x)>9 || Mathf.Abs(s.y)>4.6f) state.shots.RemoveAt(i);
        }
        if (state.players[0].hp == 0 || state.players[1].hp == 0)
        { state.step = 3; state.winner = state.players[0].hp == state.players[1].hp ? -1 : state.players[0].hp > 0 ? 0 : 1; }
    }
}
