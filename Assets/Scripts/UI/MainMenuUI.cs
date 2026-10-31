using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";

    public void Play()
    {
        if (string.IsNullOrWhiteSpace(battleSceneName))
        {
            Debug.LogWarning("MainMenuUI: No battle scene name assigned.");
            return;
        }

        SFXManager.ButtonClick();

        SceneManager.LoadScene(battleSceneName);
    }

    [SerializeField] private GameObject optionsPanel;

    public void Options()
    {
        if (optionsPanel == null) return;
        optionsPanel.SetActive(true);
        var slider = optionsPanel.GetComponentInChildren<UnityEngine.UI.Slider>(true);
        if (slider != null) slider.SetValueWithoutNotify(AudioListener.volume);
    }

    public void CloseOptions() => optionsPanel?.SetActive(false);
    public void SetVolume(float volume) => AudioListener.volume = Mathf.Clamp01(volume);

    public void Exit()
    {
        Debug.Log("Exit game.");

        SFXManager.Defeat();

        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}