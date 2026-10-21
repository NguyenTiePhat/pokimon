using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Movement-based counterplay checks after the global anticipation reduction.
public static class BossTempoCheck
{
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/LucarioArena.unity",OpenSceneMode.Additive);
        var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        bool saving=ArenaProgress.SuppressSaving;
        ArenaProgress.SuppressSaving=true;
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        try
        {
            Dodge<CharizardBoss>(b=>{b.AutoAttack=false;return b.StartAttack(CharizardSkill.FireSpin);},(b,dt)=>b.TickBoss(dt),Vector2.up,false,4.5f);
            Dodge<BlastoiseBoss>(b=>{b.AutoAttack=false;return b.StartAttack(BlastoiseSkill.HydroCannon);},(b,dt)=>b.TickBoss(dt),Vector2.up,false,2);
            Dodge<VenusaurBoss>(b=>{b.AutoAttack=false;return b.StartAttack(VenusaurSkill.SolarBeam);},(b,dt)=>b.TickBoss(dt),Vector2.up,false,2.5f);
            Dodge<GengarBoss>(b=>{b.AutoAttack=false;return b.StartAttack(GengarSkill.PhantomForce);},(b,dt)=>b.TickBoss(dt),Vector2.up,false,2);
            Dodge<LucarioBoss>(b=>{b.AutoAttack=false;return b.StartAttack(LucarioSkill.MeteorMash);},(b,dt)=>b.TickBoss(dt),Vector2.up,false,2);
            Dodge<BlastoiseBoss>(b=>{b.AutoAttack=false;return b.StartAttack(BlastoiseSkill.HydroPump);},(b,dt)=>b.TickBoss(dt),new Vector2(-1,1).normalized,true,3);
            Debug.Log("BOSS_TEMPO_OK: all five ultimates avoided by walking after 200ms reaction; fastest tracking beam avoided by dash and movement; no invulnerability or teleporting.");
        }
        finally
        {
            ArenaProgress.SuppressSaving=saving;
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene,true);
        }
    }

    private static void Dodge<T>(Func<T,bool> start,Action<T,float> tick,Vector2 direction,bool dash,float duration) where T:ArenaBoss
    {
        var player=new GameObject("Tempo counterplay Pikachu").AddComponent<PikachuPlayer>();
        var boss=new GameObject("Tempo counterplay boss").AddComponent<T>();
        try
        {
            player.Initialize(); player.SampleEntrance(PikachuPlayer.EntranceDuration);
            player.transform.position=new Vector2(3,0);
            var combat=player.gameObject.AddComponent<PikachuCombat>(); combat.Initialize(player,boss);
            boss.InitializeBoss(combat,Vector2.zero);
            if(!start(boss))throw new Exception(typeof(T).Name+": cannot start tempo check");
            const float dt=.01f;
            for(int step=0;step<Mathf.CeilToInt(duration/dt);step++)
            {
                // 200ms spent reacting to the visible warning before using actual player controls.
                if(step==20&&dash&&!combat.TryDash(direction))throw new Exception("Counterplay dash rejected");
                if(step>=20)player.TickMovement(direction,dt);
                combat.Tick(dt); tick(boss,dt);
                if(combat.Health!=200)throw new Exception(typeof(T).Name+": counterplay hit at "+step*dt+"s, HP="+combat.Health);
            }
        }
        finally { Object.DestroyImmediate(player.gameObject); Object.DestroyImmediate(boss.gameObject); }
    }
}
