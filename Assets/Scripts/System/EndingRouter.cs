using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingRouter : MonoBehaviour
{
    public void LoadEndingForProgress()
    {
        int completed = ProgressManager.Instance != null
            ? ProgressManager.Instance.puzzlesCompleted
            : 0;

        string sceneToLoad;

        switch (completed)
        {
            case 0:
                sceneToLoad = "Death";
                break;
            case 1:
                sceneToLoad = "Ending1";
                break;
            case 2:
                sceneToLoad = "Ending2";
                break;
            case 3:
                sceneToLoad = "Ending3";
                break;
            default:
                sceneToLoad = "Ending4";
                break;
        }

        Debug.Log($"[ENDING] Loading '{sceneToLoad}' (puzzles completed = {completed})");

        SceneManager.LoadScene(sceneToLoad);
    }
}
