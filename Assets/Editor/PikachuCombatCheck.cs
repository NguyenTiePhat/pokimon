using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PikachuCombatCheck
{
    private static PikachuPlayer player;
    private static PikachuCombat combat;
    private static TrainingDummy dummy;
    private static Camera camera;
    private static int assertions;

    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/CharizardArena.unity",OpenSceneMode.Additive);
        try
        {
            foreach(var root in scene.GetRootGameObjects()) if(root.TryGetComponent<Camera>(out var cam)) camera=cam;
            var p=new GameObject("Combat check player"); SceneManager.MoveGameObjectToScene(p,scene);
            player=p.AddComponent<PikachuPlayer>(); player.Initialize(camera); player.SampleEntrance(PikachuPlayer.EntranceDuration);
            var d=new GameObject("Combat check dummy"); SceneManager.MoveGameObjectToScene(d,scene);
            dummy=d.AddComponent<TrainingDummy>(); dummy.Initialize(new Vector2(3,0));
            combat=p.AddComponent<PikachuCombat>(); combat.Initialize(player,dummy);
            RunAssertions();
            CheckBasicAttack();
            CheckAimAndHud();
            RenderPreviews();
            Debug.Log("PIKACHU_COMBAT_OK: "+assertions+" assertions; Thunderbolt lock/immunity, costs/cooldowns, buff/cast/dash speeds, Iron Tail range/damage/slow, ultimate gate/early/full/miss, persistent craters, intro lock; four rendered skill previews.");
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }
    private static void Require(bool yes,string message)
    { assertions++; if(!yes) throw new System.Exception("Combat regression: "+message); }
    private static bool Near(float a,float b)=>Mathf.Abs(a-b)<.001f;
    private static Vector2 Target= new Vector2(3,0);
    private static void Clean(bool refill=true)
    {
        combat.CancelForEntrance(); combat.Tick(100);
        player.transform.position=Vector3.zero;
        dummy.ResetDummy(); dummy.Immune=dummy.Moving=false;
        if(refill) combat.RefillTrainingResources();
    }
    private static void RunAssertions()
    {
        Require(!combat.TryCast(PikachuSkill.ElectroBall,Target),"ultimate should need 15 lightning");
        Require(combat.IsPointOnDummy(Target+Vector2.up*1.4f),"clicking dummy head should select it");
        Require(Near(combat.Mana,100)&&combat.Lightning==0,"failed cast spent resources");
        Require(combat.TryCast(PikachuSkill.Thunderbolt,Target),"Thunderbolt should cast");
        Require(Near(combat.Mana,70)&&Near(combat.Cooldown(PikachuSkill.Thunderbolt),15),"Thunderbolt cost/cooldown");
        var before=player.transform.position; player.TickMovement(Vector2.right,1);
        Require(player.transform.position==before&&!combat.TryDash(Vector2.right),"casting must lock move/dash");
        dummy.transform.position=new Vector2(4,1);
        combat.Tick(combat.thunderbolt.castSeconds);
        Require(Near(dummy.LastDamage,20)&&Near(dummy.Health,980),"targeted bolt did not follow moved enemy");
        Require(combat.Lightning==3,"Thunderbolt lightning award");
        float mana=combat.Mana;
        Require(!combat.TryCast(PikachuSkill.Thunderbolt,dummy.transform.position)&&Near(combat.Mana,mana),"cooldown bypass");
        combat.Tick(16);
        Require(Near(combat.Mana,100),"mana should regenerate and cap at 100");
        dummy.transform.position=new Vector2(3,0);
        dummy.Immune=true;
        Require(combat.TryCast(PikachuSkill.Thunderbolt,dummy.transform.position),"immune target should be selectable"); combat.Tick(combat.thunderbolt.castSeconds);
        Require(Near(dummy.Health,980)&&Near(dummy.LastDamage,0),"immunity must block Thunderbolt damage");

        Clean();
        Require(combat.TryCast(PikachuSkill.QuickAttack,Target),"Quick Attack cast");
        Require(Near(combat.Mana,70)&&Near(combat.Cooldown(PikachuSkill.QuickAttack),20),"Quick Attack cost/cooldown");
        combat.Tick(combat.quickAttack.castSeconds);
        Require(Near(combat.QuickRemaining,5)&&Near(combat.SpeedMultiplier,1.5f),"Quick Attack duration/speed");
        player.TickMovement(Vector2.right,.2f);
        Require(Near(player.transform.position.x,.96f),"buffed movement speed");
        Require(combat.TryCast(PikachuSkill.Thunderbolt,Target),"buffed Thunderbolt cast"); combat.Tick(combat.thunderbolt.castSeconds / combat.quickCastMultiplier + .001f);
        Require(!combat.IsCasting&&Near(dummy.LastDamage,20),"buffed cast speed");
        Require(combat.TryDash(Vector2.up),"buffed dash"); before=player.transform.position; combat.Tick(.16f);
        Require(Near(Vector3.Distance(before,player.transform.position),2.64f),"buffed dash speed");
        combat.Tick(5);
        Require(Near(combat.SpeedMultiplier,1)&&Near(combat.CastMultiplier,1),"buff did not expire");
        Require(combat.TryDash(Vector2.down),"normal dash"); before=player.transform.position; combat.Tick(.16f);
        Require(Near(Vector3.Distance(before,player.transform.position),1.76f),"normal dash speed");

        Clean(); mana=combat.Mana;
        Require(!combat.TryCast(PikachuSkill.IronTail,Target)&&Near(combat.Mana,mana)&&Near(combat.Cooldown(PikachuSkill.IronTail),0),"out-of-range tail spent resources");
        player.transform.position=new Vector2(1.5f,0);
        player.CombatPose("Idle",0,Vector2.left);
        Require(combat.TryCast(PikachuSkill.IronTail,Vector2.left*100,true),"nearby tail must auto target behind facing, ignoring pointer");
        Require(Near(combat.Mana,80)&&Near(combat.Cooldown(PikachuSkill.IronTail),10),"tail cost/cooldown");
        combat.Tick(combat.ironTail.castSeconds);
        Require(Near(dummy.LastDamage,25)&&Near(dummy.SlowRemaining,2)&&Near(dummy.SlowFraction,.4f),"tail damage/slow");
        dummy.Tick(2.01f); Require(Near(dummy.SlowRemaining,0),"slow did not expire");

        Clean();
        Require(combat.TryCast(PikachuSkill.ElectroBall,Target),"full-resource ultimate");
        Require(Near(combat.Mana,30)&&combat.Lightning==0&&Near(combat.Cooldown(PikachuSkill.ElectroBall),60),"ultimate resource gate/cost/cooldown");
        combat.Tick(combat.electroBall.castSeconds * .5f);
        Require(combat.ReleaseElectroBall(Target)&&combat.Projectiles.Count==1,"early ultimate release");
        Require(Near(combat.Projectiles[0].damage,60),"partial charge damage scaling");
        combat.Tick(1);
        Require(Near(dummy.LastDamage,60)&&combat.Craters.Count==1,"partial hit/crater");
        int oldCraters=combat.Craters.Count;
        combat.RefillTrainingResources(); dummy.ResetDummy();
        Require(combat.Craters.Count==oldCraters,"training refill should preserve crater");
        Require(combat.TryCast(PikachuSkill.ElectroBall,Target),"full charge cast"); combat.Tick(combat.electroBall.castSeconds);
        Require(combat.Projectiles.Count==1&&Near(combat.Projectiles[0].damage,100),"max charge should auto release for 100");
        combat.Tick(1); Require(Near(dummy.LastDamage,100),"full ultimate damage");

        Clean(); oldCraters=combat.Craters.Count;
        Require(combat.TryCast(PikachuSkill.ElectroBall,Target),"miss test cast"); combat.Tick(combat.electroBall.castSeconds);
        Vector2 fixedDestination=combat.Projectiles[0].end;
        dummy.transform.position=new Vector2(3,3);
        combat.Tick(.1f);
        Require(combat.Projectiles[0].end==fixedDestination,"projectile homed after release");
        combat.Tick(1);
        Require(Near(dummy.Health,TrainingDummy.MaxHealth)&&combat.Craters.Count==oldCraters+1,"dodgeable ball should miss and crater ground");
        Clean();
        combat.TryCast(PikachuSkill.ElectroBall,new Vector2(5,3)); combat.ReleaseElectroBall(new Vector2(5,3)); combat.Tick(1);
        Require(combat.Lightning==0,"ultimate should consume all 15 lightning");
        Require(combat.TryCast(PikachuSkill.QuickAttack,Target),"Quick Attack at exactly 30 mana"); combat.Tick(combat.quickAttack.castSeconds);
        Require(combat.Lightning==2,"Quick Attack should grant exactly 2 lightning");
        mana=combat.Mana;
        Require(!combat.TryCast(PikachuSkill.Thunderbolt,Target)&&Near(combat.Mana,mana),"insufficient mana should not start a cast");
        combat.Tick(21); player.transform.position=new Vector2(1.5f,0);
        combat.TryCast(PikachuSkill.IronTail,Target); combat.Tick(combat.ironTail.castSeconds);
        Require(combat.Lightning==7,"Iron Tail should grant exactly 5 lightning");
        oldCraters=combat.Craters.Count;
        combat.CancelForEntrance(); player.ReplayEntrance();
        Require(!combat.TryCast(PikachuSkill.QuickAttack,Target),"intro allowed cast");
        Require(combat.Craters.Count==oldCraters,"replay erased permanent crater");
        player.SampleEntrance(PikachuPlayer.EntranceDuration);
    }

    private static void CheckBasicAttack()
    {
        Clean(); int craters=combat.Craters.Count, lightning=combat.Lightning;
        Require(combat.TryBasicAttack(Target+Vector2.up*.45f),"basic shot should fire toward pointer");
        Require(Near(combat.Mana,95)&&Near(combat.BasicCooldownRemaining,1),"basic shot cost/cooldown");
        Require(!combat.TryBasicAttack(Target)&&Near(combat.Mana,95),"basic cooldown must reject without spending");
        Require(!combat.MovementLocked && combat.Projectiles[0].basic,"basic shot must allow movement");
        combat.Tick(.4f);
        Require(Near(dummy.LastDamage,5)&&Near(dummy.Health,995)&&combat.Projectiles.Count==0,"basic shot must hit once for 5 damage");
        Require(combat.Craters.Count==craters && combat.Lightning==lightning,"basic shot must not create crater or grant lightning");
        combat.Tick(.59f); Require(!combat.TryBasicAttack(Target),"basic cooldown expired too early");
        combat.Tick(.011f); Require(combat.TryBasicAttack(Vector2.left*3),"basic shot must be ready after 1 second");
        combat.Tick(1); Require(Near(dummy.Health,995)&&combat.Projectiles.Count==0,"missed basic shot must expire without auto hit");
        Clean(); dummy.Immune=true; combat.TryBasicAttack(Target+Vector2.up*.45f); combat.Tick(1);
        Require(Near(dummy.Health,1000),"immunity must block basic shot");
        Clean(); combat.BeginAim(PikachuSkill.Thunderbolt,Target);
        Require(!combat.TryBasicAttack(Target)&&Near(combat.Mana,100),"E aiming must take precedence over basic shot");
        combat.CancelAim(); combat.TryCast(PikachuSkill.ElectroBall,Target);
        Require(!combat.TryBasicAttack(Target),"X charge must take precedence over basic shot");
        Clean();
        for(int i=0;i<20;i++) { Require(combat.TryBasicAttack(Vector2.left*3),"basic attack resource drain"); combat.Tick(1); }
        Require(Near(combat.Mana,0)&&!combat.TryBasicAttack(Target),"basic attack must reject insufficient mana");
    }

    private static void RenderPreviews()
    {
        Directory.CreateDirectory("Docs/Previews");
        Clean(); combat.TryCast(PikachuSkill.Thunderbolt,Target); combat.Tick(combat.thunderbolt.castSeconds); Render("Thunderbolt",10);
        Clean(); combat.TryCast(PikachuSkill.QuickAttack,Target); combat.Tick(combat.quickAttack.castSeconds); Render("QuickAttack",11);
        Clean(); player.transform.position=new Vector2(1.5f,0); combat.TryCast(PikachuSkill.IronTail,Target); combat.Tick(combat.ironTail.castSeconds * .65f); Render("IronTail",12);
        Clean(); combat.TryCast(PikachuSkill.ElectroBall,Target); combat.Tick(combat.electroBall.castSeconds * .93f); Render("ElectroBall",13);
        Clean(); combat.BeginAim(PikachuSkill.Thunderbolt,Target); camera.orthographicSize=3.65f; Render("Aim-E",14);
        Clean(); camera.orthographicSize=3.65f; player.transform.position=new Vector2(1.5f,0); combat.TryCast(PikachuSkill.IronTail,Vector2.left*100); Render("Auto-C",15);
        Clean(); combat.TryBasicAttack(Target+Vector2.up*.45f); combat.Tick(.15f); Render("BasicAttack",16);
    }
    private static void CheckAimAndHud()
    {
        Clean();
        Require(combat.BeginAim(PikachuSkill.Thunderbolt,Target+Vector2.up*1.4f),"E aim should start");
        Require(combat.AimTargetValid && combat.AimPoint==Target && Near(combat.IndicatorRange,4),"E ring must match cast range and target ground");
        var start=player.transform.position; player.TickMovement(Vector2.left,.1f);
        Require(player.transform.position.x<start.x,"aiming should allow movement");
        combat.UpdateAim(new Vector2(100,100));
        Require(!combat.AimTargetValid && Vector2.Distance(combat.AimPoint,player.transform.position)<=4.001f,"E marker must clamp to range");
        combat.CancelAim();
        Require(!combat.BeginAim(PikachuSkill.IronTail,Target) && combat.AimingSkill==-1,"C must not enter manual aiming");
        Require(!combat.TryCast(PikachuSkill.IronTail,Target),"C must reject distant enemy");
        player.transform.position=new Vector2(1.5f,0);
        Require(combat.TryCast(PikachuSkill.IronTail,Vector2.left*100),"C must auto select without mouse selection");
        Require(combat.AimTargetValid && combat.AimPoint==Target,"nearby C marker should snap to target");
        combat.GetComponent<CombatVisuals>().RenderFrame(100);
        Require(combat.GetComponent<CombatVisuals>().RangeVisible,"range indicator missing");
        combat.Tick(combat.ironTail.castSeconds); combat.GetComponent<CombatVisuals>().RenderFrame(101);
        Require(!combat.GetComponent<CombatVisuals>().RangeVisible,"range ring left behind after aim ends");
        Require(!CombatHud.BlocksCanvasPoint(new Vector2(900,550)),"removed ability panel still blocks casts");
        Require(!CombatHud.BlocksCanvasPoint(new Vector2(100,660)),"empty bottom edge blocks casts");
        for(int i=0;i<4;i++) Require(CombatHud.BlocksCanvasPoint(CombatHud.IconRect(i).center),"ability icon must block click-through");
        Clean(); dummy.transform.position=new Vector2(4.01f,0);
        Require(!combat.TryCast(PikachuSkill.Thunderbolt,dummy.transform.position) && Near(combat.Mana,100) && Near(combat.Cooldown(PikachuSkill.Thunderbolt),0),"E must reject beyond reduced range without spending resources");
        dummy.transform.position=new Vector2(4,0);
        Require(combat.TryCast(PikachuSkill.Thunderbolt,dummy.transform.position),"E must accept target at 4-unit boundary");
        Clean(); combat.TryCast(PikachuSkill.Thunderbolt,Target); combat.Tick(.34f);
        Require(combat.IsCasting,"E fired before the new 0.35s windup"); combat.Tick(.011f);
        Require(!combat.IsCasting && Near(dummy.LastDamage,20),"E must fire at the new shorter windup");
    }
    private static void Render(string name,float time)
    {
        combat.GetComponent<CombatVisuals>().RenderFrame(time);
        var rt=new RenderTexture(1280,720,24);
        var old=RenderTexture.active;
        var shot=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            shot.ReadPixels(new Rect(0,0,1280,720),0,0); shot.Apply();
            File.WriteAllBytes("Docs/Previews/Pikachu-"+name+".png",shot.EncodeToPNG());
        }
        finally { camera.targetTexture=null; RenderTexture.active=old; Object.DestroyImmediate(shot); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
