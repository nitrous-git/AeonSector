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

    public void Options()
    {
        Debug.Log("Options clicked. Options menu not implemented yet.");
    }

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