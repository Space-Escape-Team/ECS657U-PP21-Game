using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    public int puzzlesCompleted { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterPuzzleCompleted(string puzzleId)
    {
        puzzlesCompleted++;
        Debug.Log($"[PROGRESS] Puzzle completed: {puzzleId} | Total = {puzzlesCompleted}");
    }
    public void ResetProgress()
    {
        puzzlesCompleted = 0;
    }

}
