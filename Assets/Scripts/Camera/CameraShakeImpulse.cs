using UnityEngine;


[RequireComponent(typeof(Unity.Cinemachine.CinemachineImpulseSource))]
public class CameraShakeImpulse : MonoBehaviour
{
    public static CameraShakeImpulse Instance { get; private set; }

    [SerializeField] private Unity.Cinemachine.CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (impulseSource == null)
            impulseSource = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
    }

    public static void PlayLongMediumHit()
    {
        PlayImpact(0.08f, 0.16f);
    }

    public static void PlayMediumHit()
    {
        PlayImpact(0.055f, 0.12f);
    }

    public static void PlayHeavyHit()
    {
        PlayImpact(0.14f, 0.2f);
    }

    private float nextImpact;
    private float lastForce;

    public static void PlayImpact(float force, float duration)
    {
        if (Instance == null || Instance.impulseSource == null || force <= 0f) return;
        // Coalesce rain impacts and lethal-hit pairs instead of stacking camera displacement.
        if (Time.unscaledTime < Instance.nextImpact && force <= Instance.lastForce) return;
        float emittedForce = Time.unscaledTime < Instance.nextImpact ? force - Instance.lastForce : force;
        Instance.nextImpact = Time.unscaledTime + 0.08f;
        Instance.lastForce = force;
        Instance.impulseSource.ImpulseDefinition.ImpulseDuration = Mathf.Clamp(duration, 0.05f, 0.25f);
        Instance.impulseSource.GenerateImpulse(Mathf.Clamp(emittedForce, 0f, 0.2f));
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Play(float force)
    {
        if (Instance == null)
        {
            Debug.LogWarning("CameraShakeImpulse.Play called, but no CameraShakeImpulse exists in the scene.");
            return;
        }

        Instance.impulseSource.GenerateImpulse(force);
    }
}
