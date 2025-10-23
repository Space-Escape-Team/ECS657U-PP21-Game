using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerPuzzleInteractor : MonoBehaviour
{
    [Header("Puzzle Interaction Settings")]
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask puzzleLayer;

    [Header("UI Elements")]
    public GameObject promptUI;
    public TextMeshProUGUI promptText;

    private Camera cam;
    private Puzzle_script currentPuzzle;

    void Start()
    {
        cam = Camera.main;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Update()
    {
        HandlePuzzlePrompt();

        if (Input.GetKeyDown(interactKey) && currentPuzzle != null)
        {
            currentPuzzle.ActivatePuzzle();
        }
    }

    void HandlePuzzlePrompt()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, puzzleLayer))
        {
            Puzzle_script puzzle = hit.collider.GetComponent<Puzzle_script>();

            if (puzzle != null)
            {
                if (puzzle != currentPuzzle)
                {
                    currentPuzzle = puzzle;
                    ShowPrompt(puzzle.puzzlePrompt);
                }
                return;
            }
        }

        HidePrompt();
        currentPuzzle = null;
    }

    void ShowPrompt(string message)
    {
        if (promptUI != null && promptText != null)
        {
            promptUI.SetActive(true);
            promptText.text = message;
        }
    }

    void HidePrompt()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }
}

