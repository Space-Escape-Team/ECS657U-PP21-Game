using UnityEngine;

public class PuzzlePanel_script : Puzzle_script
{
    [SerializeField] private bool isCompleted = false;

    public bool PuzzleCompleted
    {
        get => isCompleted;
        private set => isCompleted = value;
    }

    public void MarkCompleted()
    {
        PuzzleCompleted = true;
        ClosePuzzle();
    }
}