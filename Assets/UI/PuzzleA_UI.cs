using UnityEngine;

public class PuzzleA_UI : MonoBehaviour
{
    private PuzzlePanel_script parentPanel;

    void Awake()
    {
        parentPanel = FindObjectOfType<PuzzlePanel_script>();
    }

    public void CompletePuzzle()
    {
        parentPanel.PuzzleCompleted();
    }

    public void ResetPuzzle()
    {
        Debug.Log("Puzzle A reset");
    }

    public void ExitPuzzle()
    {
        parentPanel.ClosePuzzle();
    }
}