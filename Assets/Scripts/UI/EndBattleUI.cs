using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndBattleUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private Image resultImage;
    [SerializeField] private Sprite victoryStamp;
    [SerializeField] private Sprite defeatStamp;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text averagePowerText;

    [Header("Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Scene Flow")]
    [SerializeField] private string nextBattleSceneName;
    [SerializeField] private string noButtonSceneFlow;

    private void Awake()
    {

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesPressed);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoPressed);
        }

        Hide();
    }

    public void Show(BattleEndStats stats)
    {

        gameObject.SetActive(true);

        resultImage.sprite = (stats.result == BattleState.Victory) ? victoryStamp : defeatStamp;

        if (timeText != null)
        {
            timeText.text = $"{FormatTime(stats.battleTimeSeconds)}";
        }

        if (turnText != null)
        {
            turnText.text = $"{stats.turnCount}";
        }

        if (averagePowerText != null)
        {
            averagePowerText.text = $"{stats.avgPowerUsedPercent:0}%";
        }

        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(true);
        }

        if (noButton != null)
        {
            noButton.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnYesPressed()
    {
        if (string.IsNullOrWhiteSpace(nextBattleSceneName))
        {
            Debug.LogWarning("EndBattleUI: No next battle scene name assigned.");
            return;
        }

        SceneManager.LoadScene(nextBattleSceneName);
    }

    private void OnNoPressed()
    {
        // Go back to MainMenu or Replay 
        //Hide();
        SceneManager.LoadScene(noButtonSceneFlow);
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
