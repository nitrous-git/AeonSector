using UnityEngine;

// Optional scene presentation layer. Never decides legality, damage, occupancy or completion.
public sealed class CombatPresentation : MonoBehaviour
{
    public static CombatPresentation Instance { get; private set; }
    public CombatEffect charge, muzzle, slash, impact, blast, destruction, warning;
    public FloatingDamageNumber damageNumber;
    public CombatVfxProfile fallbackProfile;
    [Range(0f, 1f)] public float shakeMultiplier = 1f;
    private float nextAreaShake;

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public static CombatVfxProfile Profile(CombatUnit unit)
    {
        var feedback = unit != null ? unit.GetComponent<UnitHitFeedback>() : null;
        return feedback != null && feedback.profile != null ? feedback.profile : Instance != null ? Instance.fallbackProfile : null;
    }

    public static Vector3 HitPosition(CombatUnit unit)
    {
        if (unit == null) return Vector3.zero;
        var feedback = unit.GetComponent<UnitHitFeedback>();
        return feedback != null && feedback.body != null ? feedback.body.bounds.center : unit.transform.position + Vector3.up * 0.3f;
    }

    public static GameObject Projectile(CombatUnit unit, GameObject fallback)
    {
        var p = Profile(unit);
        return Instance != null && p != null && p.projectile != null ? p.projectile : fallback;
    }

    private CombatEffect Spawn(CombatEffect prefab, Vector3 position, CombatVfxProfile p, float scale = 1f,
        Vector3 direction = default, float duration = -1f, CombatUnit owner = null, bool atWeapon = false)
    {
        if (prefab == null || p == null) return null;
        var effect = Instantiate(prefab, position, Quaternion.identity, transform);
        effect.Initialize(p.energy, p.edge, scale, direction, duration, owner, atWeapon);
        return effect;
    }

    public static void Begin(CombatUnit unit, CommandMode mode, float duration)
    {
        if (Instance == null || unit == null) return;
        bool ranged = mode == CommandMode.RangedAttack;
        var position = ranged ? unit.GetProjectileOrigin(HitPosition(unit)) : HitPosition(unit);
        Instance.Spawn(Instance.charge, position, Profile(unit), mode == CommandMode.AreaAttack ? 1.25f : 0.8f,
            duration: Mathf.Max(0.1f, duration), owner: unit, atWeapon: ranged);
    }

    public static void Launch(CombatUnit unit, Vector3 origin, Vector3 target)
    {
        if (Instance == null) return;
        Instance.Spawn(Instance.muzzle, origin, Profile(unit), 0.85f, target - origin);
    }

    public static void MeleeContact(CombatUnit unit, CombatUnit target)
    {
        if (Instance == null || unit == null || target == null) return;
        var p = Profile(unit);
        Vector3 direction = HitPosition(target) - HitPosition(unit);
        Instance.Spawn(Instance.slash, HitPosition(target) - direction.normalized * 0.35f, p, p != null ? p.impactScale : 1f, direction);
    }

    public static void AreaWarning(CombatUnit unit, Vector3 position, float duration)
    {
        if (Instance != null) Instance.Spawn(Instance.warning, position, Profile(unit), 1f, duration: duration);
    }

    public static void AreaImpact(CombatUnit unit, Vector3 position)
    {
        if (Instance == null) return;
        // Empty cells get a small ground ring; a damaging hit adds the brighter target burst.
        Instance.Spawn(Instance.blast, position, Profile(unit), 0.58f);
    }

    public static void Damage(CombatUnit target, CombatUnit attacker, int actualDamage, bool lethal, CommandMode mode)
    {
        if (Instance == null || target == null || actualDamage <= 0) return;
        var p = Profile(attacker);
        var targetProfile = Profile(target);
        Vector3 hit = HitPosition(target);
        Vector3 direction = attacker != null ? hit - HitPosition(attacker) : Vector3.up;
        Instance.Spawn(Instance.impact, hit, p, p != null ? p.impactScale : 1f, direction);
        target.GetComponent<UnitHitFeedback>()?.Hit(direction);
        if (Instance.damageNumber != null)
        {
            var text = Instantiate(Instance.damageNumber, hit + Vector3.up * 0.7f, Quaternion.identity, Instance.transform);
            // Match the existing cards' displayed HP units, including rounding at either end.
            int displayedDamage = actualDamage;
            if (target.Stats != null && target.Stats.DisplayMaxHP > 0 && target.Stats.MaxHP > 0)
            {
                float factor = (float)target.Stats.DisplayMaxHP / target.Stats.MaxHP;
                displayedDamage = Mathf.RoundToInt((target.CurrentHP + actualDamage) * factor)
                    - Mathf.RoundToInt(target.CurrentHP * factor);
            }
            text.Show(displayedDamage, lethal, target.OwnerFaction != null && target.OwnerFaction.Type == FactionType.Player);
        }
        if (p != null && (mode != CommandMode.AreaAttack || Time.unscaledTime >= Instance.nextAreaShake))
        {
            CameraShakeImpulse.PlayImpact(p.impactShake * Instance.shakeMultiplier, p.shakeDuration);
            if (mode == CommandMode.AreaAttack) Instance.nextAreaShake = Time.unscaledTime + 0.16f;
        }
        if (lethal)
        {
            var effect = Instance.Spawn(Instance.destruction, hit, targetProfile, 1.25f);
            effect?.SetGhost(target.GetComponent<UnitHitFeedback>()?.body);
            CameraShakeImpulse.PlayImpact(0.14f * Instance.shakeMultiplier, 0.2f);
            SFXManager.ProjectileImpact();
        }
    }
}
