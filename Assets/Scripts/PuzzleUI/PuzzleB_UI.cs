using UnityEngine;

public class PuzzleB_UI : MonoBehaviour
{
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    private bool solved = false;

    private void Start()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();
    }

    private void Update()
    {
        if (!solved)
        {
            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                SolvePuzzle();
            }
        }
    }

    private void SolvePuzzle()
    {
        solved = true;
        if (puzzlePanel != null)
            puzzlePanel.MarkCompleted();

        Debug.Log("Puzzle B solved!");
    }
}