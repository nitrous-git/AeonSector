using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeImpulse : MonoBehaviour
{
    public static CameraShakeImpulse Instance { get; private set; }

    [SerializeField] private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public static void PlayLongMediumHit()
    {
        Instance.impulseSource.m_ImpulseDefinition.m_ImpulseDuration = 1.0f;
        Play(0.5f);
    }

    public static void PlayMediumHit()
    {
        Instance.impulseSource.m_ImpulseDefinition.m_ImpulseDuration = 0.13f;
        Play(0.75f);
    }

    public static void PlayHeavyHit()
    {
        Instance.impulseSource.m_ImpulseDefinition.m_ImpulseDuration = 0.35f;
        Play(1.2f);
    }

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