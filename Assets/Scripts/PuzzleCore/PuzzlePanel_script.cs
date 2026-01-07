using UnityEngine;

public class PuzzlePanel_script : Puzzle_script
{
    [Header("Completion Flag")]
    [SerializeField] private bool isCompleted = false;

    public bool Completed => isCompleted;

    public override void TryOpenPuzzle()
    {
        if (isCompleted) return; // optional lock-out after solved
        base.TryOpenPuzzle();
    }

    public void MarkCompleted()
    {
        if (isCompleted) return;
        isCompleted = true;

        // closing will also reset UI state (per spec)
        TryClosePuzzle();
    }
}
