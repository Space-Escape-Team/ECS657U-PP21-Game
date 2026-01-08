using UnityEngine;

/// Puzzle panel wrapper that blocks opening once completed.
/// MarkCompleted() latches a completion flag and logs for debugging; context menu provides an editor-only shortcut.
public class PuzzlePanel_script : Puzzle_script
{
    [Header("Completion Flag")]
    [SerializeField] private bool isCompleted = false;

    public bool Completed => isCompleted;

    public override void TryOpenPuzzle()
    {
        /// Prevent opening the puzzle UI if this panel has already been completed.
        if (isCompleted) return;
        base.TryOpenPuzzle();
    }

    public void MarkCompleted()
    {
        /// One-way completion latch (idempotent).
        if (isCompleted) return;

        isCompleted = true;

        Debug.Log($"[PUZZLE COMPLETE] {gameObject.name} marked complete.", this);
    }

    [ContextMenu("DEBUG: Mark Completed Now")]
    private void DebugMarkCompletedNow()
    {
        /// Editor context menu helper to mark completion without solving.
        MarkCompleted();
    }
}
