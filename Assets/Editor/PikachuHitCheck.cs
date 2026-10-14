using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PikachuHitCheck
{
    private static PikachuPlayer player;
    private static PikachuCombat combat;
    private static TrainingDummy dummy;
    private static Camera camera;
    private static Scene scene;
    private static int count;
    public static void Run()
    {
        scene=EditorSceneManager.OpenScene("Assets/Scenes/CharizardArena.unity",OpenSceneMode.Additive);
        try
        {
            foreach(var root in scene.GetRootGameObjects()) if(root.TryGetComponent<Camera>(out var cam)) camera=cam;
            Fresh(); combat.TakeDamage(5);
            Check(combat.Health==195 && combat.HitRemaining>0 && combat.StunRemaining==0,"small hit feedback without stun");
            combat.GetComponent<CombatVisuals>().RenderFrame(1);
            Check(combat.GetComponent<CombatVisuals>().HitVisible,"hit sparks missing");
            combat.Tick(.3f); combat.GetComponent<CombatVisuals>().RenderFrame(2);
            Check(!combat.GetComponent<CombatVisuals>().HitVisible,"hit sparks did not expire");
            Fresh(); combat.TakeDamage(10); combat.Tick(.75f); combat.TakeDamage(10); combat.Tick(.75f); combat.TakeDamage(5);
            Check(combat.Health==175 && Near(combat.StunRemaining,.8f),"25 HP lost over 1.5 seconds must stun without killing");
            Fresh(); combat.TakeDamage(24); combat.Tick(2); combat.TakeDamage(1);
            Check(Near(combat.StunRemaining,.8f),"25 damage at the exact 2-second boundary must stun");
            Fresh(); combat.TakeDamage(16); combat.Tick(2.01f); combat.TakeDamage(9);
            Check(combat.StunRemaining==0,"old damage outside rolling window counted");
            combat.TakeDamage(16); Check(Near(combat.StunRemaining,.8f),"rolling threshold should stun");
            var position=player.transform.position; player.TickMovement(Vector2.right,1);
            Check(player.transform.position==position && !combat.TryDash(Vector2.right) && !combat.TryBasicAttack(Vector2.right),"stun failed to block movement/dash/basic");
            Check(!combat.TryCast(PikachuSkill.QuickAttack,Vector2.zero) && !combat.BeginAim(PikachuSkill.Thunderbolt,Vector2.right),"stun failed to block abilities/aim");
            Render();
            combat.TakeDamage(5); Check(combat.Health==154 && Near(combat.StunRemaining,.8f),"stun must allow damage without extension");
            combat.Tick(.81f); Check(combat.StunRemaining==0 && combat.CanAct,"stun did not expire");
            combat.TakeDamage(25); Check(combat.StunRemaining==0 && combat.Health==129,"post-stun protection must not block damage");
            combat.Tick(2); combat.TakeDamage(25); Check(combat.StunRemaining>0,"stun protection never expires");
            Fresh(); combat.TryCast(PikachuSkill.Thunderbolt,dummy.transform.position); combat.TakeDamage(25);
            Check(!combat.IsCasting && Near(combat.Cooldown(PikachuSkill.Thunderbolt),2) && combat.Mana==70,"cast interruption cooldown/refund");
            combat.Tick(1); Check(dummy.Health==1000 && combat.Lightning==0,"interrupted E resolved or granted lightning");
            Fresh(); combat.RefillTrainingResources(); combat.TryCast(PikachuSkill.ElectroBall,Vector2.right*3); combat.TakeDamage(25);
            Check(!combat.ReleaseElectroBall(Vector2.right*3) && combat.Projectiles.Count==0 && combat.Lightning==0 && combat.Mana==30,"interrupted X released/refunded");
            Check(Near(combat.Cooldown(PikachuSkill.ElectroBall),2),"interrupted X cooldown");
            Fresh(); combat.TryDash(Vector2.right); combat.TakeDamage(25); position=player.transform.position; combat.Tick(.1f);
            Check(combat.DashRemaining==0 && player.transform.position==position,"stun failed to stop dash");
            Fresh(); combat.BeginAim(PikachuSkill.Thunderbolt,dummy.transform.position); combat.TakeDamage(25);
            Check(combat.AimingSkill==-1 && combat.Cooldown(PikachuSkill.Thunderbolt)==0,"aim cancellation incorrectly charged cooldown");
            combat.TakeDamage(200); Check(combat.BattleEnded && combat.StunRemaining==0,"death did not clear stun");
            Fresh(); player.ReplayEntrance(); combat.TakeDamage(25); Check(combat.Health==200 && combat.HitRemaining==0,"intro took damage");
            Debug.Log("PIKACHU_HIT_OK: "+count+" assertions; feedback, rolling window, stun lock, damage/protection, cast/ultimate/dash interrupts, death/intro; rendered stun preview.");
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }
    private static bool Near(float a,float b)=>Mathf.Abs(a-b)<.001f;
    private static void Check(bool ok,string message) { count++; if(!ok) throw new System.Exception(message); }
    private static void Fresh()
    {
        if(player) Object.DestroyImmediate(player.gameObject); if(dummy) Object.DestroyImmediate(dummy.gameObject);
        var p=new GameObject("Hit check Pikachu"); SceneManager.MoveGameObjectToScene(p,scene);
        player=p.AddComponent<PikachuPlayer>(); player.Initialize(camera); player.SampleEntrance(PikachuPlayer.EntranceDuration);
        var d=new GameObject("Hit check dummy"); SceneManager.MoveGameObjectToScene(d,scene);
        dummy=d.AddComponent<TrainingDummy>(); dummy.Initialize(new Vector2(3,0));
        combat=p.AddComponent<PikachuCombat>(); combat.Initialize(player,dummy);
    }
    private static void Render()
    {
        var visuals=combat.GetComponent<CombatVisuals>(); visuals.RenderFrame(3);
        Check(visuals.StunVisible,"stun stars missing");
        camera.transform.position=new Vector3(0,0,-10); camera.orthographicSize=2;
        var rt=new RenderTexture(960,540,24); var old=RenderTexture.active;
        var image=new Texture2D(960,540,TextureFormat.RGB24,false);
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,960,540),0,0); image.Apply(); File.WriteAllBytes("Docs/Previews/Pikachu-Stunned.png",image.EncodeToPNG()); }
        finally { camera.targetTexture=null; RenderTexture.active=old; Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt); }
    }
}
