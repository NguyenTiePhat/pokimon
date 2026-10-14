using UnityEngine;

[DefaultExecutionOrder(-100)]
public class TrainingDummy : MonoBehaviour
{
    public const float MaxHealth = 1000;
    public float Health { get; private set; } = MaxHealth;
    public float HealthCapacity { get; private set; } = MaxHealth;
    protected void ConfigureHealth(float maximum) { HealthCapacity = Health = maximum; }
    public float LastDamage { get; private set; }
    public float TotalDamage { get; private set; }
    public float SlowRemaining { get; private set; }
    public float SlowFraction { get; private set; }
    public bool Moving, Immune;
    public bool IsAlive => Health > 0;
    public string LastResult { get; private set; } = "Sẵn sàng";
    private float phase, hitFlash;
    private Vector2 home;
    private CombatPixelSurface art;

    public void Initialize(Vector2 position)
    {
        home = position; transform.position = position;
        art = new CombatPixelSurface("Wood and straw training dummy", transform, 80, 96, 48, 9, new Vector2(.5f, .125f));
        Color32 wood = new Color32(139, 87, 48,255), dark = new Color32(64,43,35,255), straw = new Color32(211,178,109,255);
        art.Clear();
        art.Ellipse(40, 11, 29, 7, new Color32(0,0,0,100));
        art.Box(35,10,10,53,dark); art.Box(38,12,4,48,wood);
        art.Box(13,43,54,7,dark); art.Box(15,46,50,3,wood);
        art.Box(26,27,28,34,dark); art.Box(28,29,24,30,straw);
        art.Box(29,32,4,24,new Color32(168,127,72,255));
        art.Ellipse(40,70,13,13,dark); art.Ellipse(40,71,11,11,straw);
        art.Box(32,73,4,3,dark); art.Box(44,73,4,3,dark); art.Box(37,65,7,2,dark);
        art.Ellipse(40,44,10,10,new Color32(177,58,54,255));
        art.Ellipse(40,44,7,7,new Color32(241,227,185,255));
        art.Ellipse(40,44,4,4,new Color32(177,58,54,255));
        art.Box(22,29,4,9,wood); art.Box(54,29,4,9,wood);
        art.Line(new Vector2(25,82),new Vector2(55,82),straw,3);
        art.Apply();
    }
    protected virtual void Update() { Tick(Time.deltaTime); }
    public void Tick(float dt)
    {
        SlowRemaining = Mathf.Max(0, SlowRemaining - dt);
        hitFlash = Mathf.Max(0, hitFlash - dt);
        if (Moving && IsAlive)
        {
            phase += dt * (SlowRemaining > 0 ? 1 - SlowFraction : 1) * 1.3f;
            transform.position = home + Vector2.up * Mathf.Sin(phase) * 1.8f;
        }
        if (art != null) art.Renderer.color = !IsAlive ? new Color(.45f,.45f,.45f,1) : hitFlash > 0 ? new Color(1,.55f,.4f,1) : Immune ? new Color(.5f,.8f,1,1) : Color.white;
    }
    public bool TakeDamage(float damage)
    {
        if (!IsAlive) return false;
        if (Immune) { LastResult = "MIỄN NHIỄM"; LastDamage = 0; return false; }
        LastDamage = Mathf.Min(Health, Mathf.Max(0, damage));
        Health -= LastDamage; TotalDamage += LastDamage; hitFlash = .18f;
        LastResult = "−" + Mathf.RoundToInt(LastDamage) + " HP";
        return true;
    }
    public void ApplySlow(float fraction, float seconds)
    { SlowFraction = Mathf.Clamp01(fraction); SlowRemaining = Mathf.Max(SlowRemaining, seconds); }
    public void ResetDummy()
    {
        Health = HealthCapacity; LastDamage = TotalDamage = SlowRemaining = phase = hitFlash = 0;
        transform.position = home; LastResult = "Đã hồi hình nhân";
    }
    protected virtual void OnDestroy() { art?.Dispose(); }
}
