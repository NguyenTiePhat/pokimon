using System.Collections.Generic;
using UnityEngine;

public abstract class ArenaBoss : TrainingDummy
{
    public PikachuCombat Opponent { get; protected set; }
    public readonly List<BossProjectile> Shots = new List<BossProjectile>();
    public Vector2 LockedDirection { get; protected set; } = Vector2.left;
    public Vector2 CastOrigin { get; protected set; }
    public float SpinPower { get; protected set; }
    public abstract string DisplayName { get; }
    public abstract string Status { get; }
    public abstract bool SpinActive { get; }
    public abstract void ReduceSpin(float power);
    public abstract void InitializeBoss(PikachuCombat opponent,Vector2 position);
    public override bool TakeDamage(float damage)
    {
        bool hit=base.TakeDamage(damage);
        if(hit && !IsAlive && Application.isPlaying) ArenaProgress.RecordVictory(this);
        return hit;
    }
    public virtual bool StreamContact(Vector2 origin,Vector2 velocity,float radius,float horizon,out float time)
        => SpellClash.ConeContact(this,origin,velocity,radius,horizon,out time);
    public virtual void ProjectileImpact(BossProjectile shot,bool hit)
    { if(hit) Opponent.TakeDamage(shot.damage); }
}
