using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// Handles player interaction with world puzzles via raycast.
/// Shows an on-screen prompt when looking at a puzzle, opens it on Interact, and closes it on Cancel.
public class PlayerPuzzleInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public LayerMask puzzleLayer;

    [Header("UI Elements")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TextMeshProUGUI promptText;

    private Camera cam;
    private Controls controls;
    private Puzzle_script currentPuzzle;

    private void Awake()
    {
        /// Cache main camera, set up input bindings for interact/cancel.
        cam = Camera.main;
        controls = new Controls();

        controls.Gameplay.Interact.performed += ctx => TryInteract();
        controls.Gameplay.Cancel.performed += ctx => TryCancel();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        HandlePuzzlePrompt();
    }

    private void HandlePuzzlePrompt()
    {
        /// Raycast forward to detect puzzles in range and on the puzzle layer.
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

        /// Clear prompt when not looking at a valid puzzle.
        HidePrompt();
        currentPuzzle = null;
    }

    private void TryInteract()
    {
        /// Attempt to open the currently targeted puzzle.
        if (currentPuzzle != null)
        {
            currentPuzzle.TryOpenPuzzle();
            HidePrompt();
        }
    }

    private void TryCancel()
    {
        /// Forward cancel input to the active puzzle, if any.
        if (currentPuzzle != null)
            currentPuzzle.TryClosePuzzle();
    }

    private void ShowPrompt(string message)
    {
        if (promptUI != null && promptText != null)
        {
            promptUI.SetActive(true);
            promptText.text = message;
        }
    }

    private void HidePrompt()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }
}
