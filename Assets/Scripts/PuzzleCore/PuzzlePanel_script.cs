using UnityEngine;

public class PuzzlePanel_script : Puzzle_script
{
    [Header("Completion Flag")]
    [SerializeField] private bool isCompleted = false;

    public bool Completed => isCompleted;

    public override void TryOpenPuzzle()
    {
        if (isCompleted) return;
        base.TryOpenPuzzle();
    }

    public void MarkCompleted()
    {
        if (isCompleted) return;

        isCompleted = true;

        Debug.Log($"[PUZZLE COMPLETE] {gameObject.name} marked complete.", this);
    }

    [ContextMenu("DEBUG: Mark Completed Now")]
    private void DebugMarkCompletedNow()
    {
        MarkCompleted();
    }
}
