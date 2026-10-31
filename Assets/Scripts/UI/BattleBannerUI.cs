using System.Collections;
using UnityEngine;

public enum BattleBannerType
{
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat
}

public class BattleBannerUI : MonoBehaviour
{
    [Header("Banners")]
    [SerializeField] private CanvasGroup playerTurnBanner;
    [SerializeField] private CanvasGroup enemyTurnBanner;
    [SerializeField] private CanvasGroup victoryBanner;
    [SerializeField] private CanvasGroup defeatBanner;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float holdDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    private Coroutine currentRoutine;

    public float TotalDuration => fadeInDuration + holdDuration + fadeOutDuration;

    public float GetDuration(BattleBannerType type) => fadeInDuration + GetHoldDuration(type) + fadeOutDuration;

    private float GetHoldDuration(BattleBannerType type) => holdDuration *
        ((type == BattleBannerType.Victory || type == BattleBannerType.Defeat) ? 2f : 1f);

    private void Awake()
    {
        HideAllInstant();
    }

    public void Show(BattleBannerType bannerType)
    {
        CanvasGroup banner = GetBanner(bannerType);

        if (banner == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        HideAllInstant();

        currentRoutine = StartCoroutine(PlayBannerRoutine(banner, GetHoldDuration(bannerType)));
    }

    private CanvasGroup GetBanner(BattleBannerType bannerType)
    {
        switch (bannerType)
        {
            case BattleBannerType.PlayerTurn:
                return playerTurnBanner;

            case BattleBannerType.EnemyTurn:
                return enemyTurnBanner;

            case BattleBannerType.Victory:
                return victoryBanner;

            case BattleBannerType.Defeat:
                return defeatBanner;

            default:
                return null;
        }
    }

    private IEnumerator PlayBannerRoutine(CanvasGroup banner, float hold)
    {
        banner.gameObject.SetActive(true);
        banner.interactable = false;
        banner.blocksRaycasts = false;

        yield return Fade(banner, 0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(hold);
        yield return Fade(banner, 1f, 0f, fadeOutDuration);

        HideInstant(banner);
        currentRoutine = null;
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            group.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        group.alpha = to;
    }

    private void HideAllInstant()
    {
        HideInstant(playerTurnBanner);
        HideInstant(enemyTurnBanner);
        HideInstant(victoryBanner);
        HideInstant(defeatBanner);
    }

    private void HideInstant(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(false);
    }
}
