using UnityEngine;

public class PuzzleB_UI : MonoBehaviour
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
        Debug.Log("Puzzle B reset");
    }

    public void ExitPuzzle()
    {
        parentPanel.ClosePuzzle();
    }
}