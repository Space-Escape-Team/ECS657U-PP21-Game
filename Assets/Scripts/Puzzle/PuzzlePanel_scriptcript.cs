using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzlePanel_script : Puzzle_script
{
    [Header("Puzzle UI Reference")]
    public GameObject puzzleUIScreen; // Assign in Inspector

    private bool isPuzzleActive = false;

    public override void ActivatePuzzle()
    {
        base.ActivatePuzzle();

        if (puzzleUIScreen == null)
        {
            Debug.LogWarning($"No puzzle UI assigned to {name}");
            return;
        }

        // Toggle  screen
        isPuzzleActive = !isPuzzleActive;
        puzzleUIScreen.SetActive(isPuzzleActive);

        //cursor visibility
        Cursor.lockState = isPuzzleActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPuzzleActive;
    }
}
