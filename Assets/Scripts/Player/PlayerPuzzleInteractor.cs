using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

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
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, puzzleLayer))
        {
            Puzzle_script puzzle = hit.collider.GetComponent<Puzzle_script>();

            if (puzzle != null)
            {
                if (puzzle != currentPuzzle)
                {
                    currentPuzzle = puzzle;
                    ShowPrompt(puzzle.puzzlePrompt); // use string defined in Puzzle_script
                }
                return;
            }
        }

        HidePrompt();
        currentPuzzle = null;
    }

    private void TryInteract()
    {
        if (currentPuzzle != null)
        {
            currentPuzzle.TryOpenPuzzle();
            HidePrompt();
        }
    }

    private void TryCancel()
    {
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
