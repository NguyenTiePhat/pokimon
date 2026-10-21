using UnityEngine;
using UnityEngine.SceneManagement;

public static class ArenaProgress
{
    public const string UnlockKey="Arena.BlastoiseUnlocked";
    public static bool SuppressSaving;
    private static bool sessionUnlocked;
    public static bool IsWater => SceneManager.GetActiveScene().name=="BlastoiseArena";
    public static bool IsHaunted => SceneManager.GetActiveScene().name=="GengarArena";
    public static bool IsGarden => SceneManager.GetActiveScene().name=="VenusaurArena";
    public static bool IsDojo => SceneManager.GetActiveScene().name=="LucarioArena";
    public static bool WaterUnlocked => sessionUnlocked || PlayerPrefs.GetInt(UnlockKey,0)==1;
    public static void RecordVictory(ArenaBoss boss)
    {
        if(!(boss is CharizardBoss) || WaterUnlocked) return;
        sessionUnlocked=true;
        if(!SuppressSaving) { PlayerPrefs.SetInt(UnlockKey,1); PlayerPrefs.Save(); }
    }
    public static void Enter(string scene)
    { ArenaPreview.TrainingMode=false; SceneManager.LoadScene(scene); }
    public static Vector2 Clamp(Vector2 p)
        => Clamp(p,SceneManager.GetActiveScene());
    public static Vector2 Clamp(Vector2 p,Scene scene)
    {
        if(scene.name=="LucarioArena") return new Vector2(Mathf.Clamp(p.x,-MountainDojo.PlayableHalfSize.x,MountainDojo.PlayableHalfSize.x),Mathf.Clamp(p.y,-MountainDojo.PlayableHalfSize.y,MountainDojo.PlayableHalfSize.y));
        if(scene.name=="BlastoiseArena"||scene.name=="VenusaurArena"||scene.name=="GengarArena") return Vector2.ClampMagnitude(p,8.4f);
        return new Vector2(Mathf.Clamp(p.x,-PikachuPlayer.MovementBounds.x,PikachuPlayer.MovementBounds.x),Mathf.Clamp(p.y,-PikachuPlayer.MovementBounds.y,PikachuPlayer.MovementBounds.y));
    }
}
