using System.Collections;
using UnityEngine;

public class ChangeTurnBanner : MonoBehaviour
{
    [Header("Banners")]
    [SerializeField] private CanvasGroup playerTurnBanner;
    [SerializeField] private CanvasGroup enemyTurnBanner;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float holdDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    private Coroutine currentRoutine;

    public float TotalDuration => fadeInDuration + holdDuration + fadeOutDuration;

    private void Awake()
    {
        HideInstant(playerTurnBanner);
        HideInstant(enemyTurnBanner);
    }

    public void ShowPlayerTurn()
    {
        ShowBanner(playerTurnBanner);
    }

    public void ShowEnemyTurn()
    {
        ShowBanner(enemyTurnBanner);
    }

    private void ShowBanner(CanvasGroup banner)
    {
        if (banner == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        HideInstant(playerTurnBanner);
        HideInstant(enemyTurnBanner);

        currentRoutine = StartCoroutine(PlayBannerRoutine(banner));
    }

    private IEnumerator PlayBannerRoutine(CanvasGroup banner)
    {
        banner.gameObject.SetActive(true);

        yield return Fade(banner, 0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
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