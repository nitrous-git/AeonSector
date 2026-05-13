using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Combat")]
    [SerializeField] private AudioClip playerShoot;
    [SerializeField] private AudioClip playerMelee;
    [SerializeField] private AudioClip enemyMelee;
    [SerializeField] private AudioClip playerHit;
    [SerializeField] private AudioClip enemyHit;
    [SerializeField] private AudioClip projectileImpact;

    [Header("Turn")]
    [SerializeField] private AudioClip playerStartTurn;
    [SerializeField] private AudioClip enemyStartTurn;

    [Header("UI / Battle Result")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip invalidAction;
    [SerializeField] private AudioClip victory;
    [SerializeField] private AudioClip defeat;

    [Header("Units")]
    [SerializeField] private AudioClip unitSelect;
    [SerializeField] private AudioClip unitMove;

    private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        SetVolume(volume);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }

    public static void PlayerShoot() => Instance?.Play(Instance.playerShoot);
    public static void PlayerMelee() => Instance?.Play(Instance.playerMelee);
    public static void EnemyMelee() => Instance?.Play(Instance.enemyMelee);
    public static void PlayerHit() => Instance?.Play(Instance.playerHit);
    public static void EnemyHit() => Instance?.Play(Instance.enemyHit);
    public static void ProjectileImpact() => Instance?.Play(Instance.projectileImpact);

    public static void PlayerStartTurn() => Instance?.Play(Instance.playerStartTurn);
    public static void EnemyStartTurn() => Instance?.Play(Instance.enemyStartTurn);

    public static void ButtonClick() => Instance?.Play(Instance.buttonClick);
    public static void InvalidAction() => Instance?.Play(Instance.invalidAction);
    public static void Victory() => Instance?.Play(Instance.victory);
    public static void Defeat() => Instance?.Play(Instance.defeat);

    public static void UnitSelect() => Instance?.Play(Instance.unitSelect);
    public static void UnitMove() => Instance?.Play(Instance.unitMove);
}