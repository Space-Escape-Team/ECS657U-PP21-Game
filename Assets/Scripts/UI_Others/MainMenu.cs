using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string hubSceneName = "Hub";

    public void PlayGame()
    {
        Debug.Log("[MAIN MENU] New Game");

        // Reset progress for a new run
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.ResetProgress();

        SceneManager.LoadScene(hubSceneName);
    }

    public void LoadGame()
    {
        Debug.Log("[MAIN MENU] Continue Game");

        // Load hub without resetting progress
        SceneManager.LoadScene(hubSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[MAIN MENU] Quit");
        Application.Quit();
    }
}