using UnityEngine;

public class PuzzlePanel_script : Puzzle_script
{
    public enum PuzzleType
    {
        PuzzleA,
        PuzzleB
    }

    [Header("Puzzle Settings")]
    public PuzzleType puzzleType;
    public bool isCompleted = false;

    [Header("Puzzle UI References")]
    public GameObject puzzleA_UI;
    public GameObject puzzleB_UI;

    private GameObject activePuzzleUI;
    private bool isPuzzleActive = false;

    public override void ActivatePuzzle()
    {
        base.ActivatePuzzle();

        if (isCompleted)
        {
            Debug.Log($"{name} puzzle already completed!");
            return;
        }

        if (isPuzzleActive)
        {
            ClosePuzzle();
            return;
        }

        switch (puzzleType)
        {
            case PuzzleType.PuzzleA:
                activePuzzleUI = puzzleA_UI;
                break;
            case PuzzleType.PuzzleB:
                activePuzzleUI = puzzleB_UI;
                break;
        }

        if (activePuzzleUI == null)
        {
            Debug.LogWarning($"No UI assigned for {puzzleType} on {name}");
            return;
        }

        OpenPuzzle();
    }

    private void OpenPuzzle()
    {
        isPuzzleActive = true;
        activePuzzleUI.SetActive(true);

        // Unlock mouse for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Reset puzzle progress when opened
        ResetPuzzleProgress();
    }

    public void ClosePuzzle()
    {
        if (!isPuzzleActive) return;

        isPuzzleActive = false;
        activePuzzleUI.SetActive(false);

        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Reset progress (since player left)
        ResetPuzzleProgress();
    }

    public void PuzzleCompleted()
    {
        isCompleted = true;
        Debug.Log($"{name} puzzle completed!");
        ClosePuzzle();
    }

    private void ResetPuzzleProgress()
    {
        // You can call into each puzzle UI’s script to reset it
        switch (puzzleType)
        {
            case PuzzleType.PuzzleA:
                var puzzleA = activePuzzleUI.GetComponent<PuzzleA_UI>();
                if (puzzleA != null) puzzleA.ResetPuzzle();
                break;

            case PuzzleType.PuzzleB:
                var puzzleB = activePuzzleUI.GetComponent<PuzzleB_UI>();
                if (puzzleB != null) puzzleB.ResetPuzzle();
                break;
        }
    }
}
