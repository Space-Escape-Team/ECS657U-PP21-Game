using UnityEngine;
using UnityEngine.InputSystem;

public class Puzzle_script : MonoBehaviour
{
    protected Controls controls;

    [Header("Puzzle UI (Canvas GameObject to toggle)")]
    [SerializeField] protected GameObject puzzleUIScreen;

    [Header("Prompt Text (for your prompt system)")]
    [SerializeField] public string puzzlePrompt = "Press E to interact";

    protected bool playerInRange = false;
    protected bool puzzleActive = false;

    protected IPuzzleUI puzzleUI;

    protected virtual void Awake()
    {
        controls = new Controls();

        controls.Gameplay.Interact.performed += _ => TryOpenPuzzle();
        controls.Gameplay.Cancel.performed += _ => TryClosePuzzle();

        CachePuzzleUI();

        if (puzzleUIScreen != null)
            puzzleUIScreen.SetActive(false);
    }

    protected virtual void OnEnable() => controls.Enable();
    protected virtual void OnDisable() => controls.Disable();

    private void CachePuzzleUI()
    {
        puzzleUI = null;
        if (puzzleUIScreen != null)
            puzzleUI = puzzleUIScreen.GetComponentInChildren<IPuzzleUI>(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    public virtual void TryOpenPuzzle()
    {
        if (!playerInRange) return;
        if (puzzleActive) return;

        OpenPuzzle();
    }

    public virtual void TryClosePuzzle()
    {
        if (!puzzleActive) return;
        ClosePuzzle();
    }

    protected virtual void OpenPuzzle()
    {
        if (puzzleUIScreen == null) return;

        CachePuzzleUI();
        puzzleUI?.ResetPuzzle();

        puzzleUIScreen.SetActive(true);
        puzzleActive = true;
    }

    protected virtual void ClosePuzzle()
    {
        if (puzzleUIScreen == null) return;

        puzzleUI?.ResetPuzzle();

        puzzleUIScreen.SetActive(false);
        puzzleActive = false;
    }
}
