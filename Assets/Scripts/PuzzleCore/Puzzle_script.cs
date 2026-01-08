using UnityEngine;
using UnityEngine.InputSystem;

/// Base class for all world puzzles.
/// Manages player proximity, input handling, puzzle UI lifecycle, and player UI mode locking.
/// Concrete puzzles extend this and optionally override open/close behavior.
public class Puzzle_script : MonoBehaviour
{
    protected Controls controls;

    [Header("Puzzle UI")]
    [SerializeField] protected GameObject puzzleUIScreen;

    [Header("Prompt Text")]
    [SerializeField] public string puzzlePrompt = "Press E to interact";

    [Header("Player Lock")]
    [SerializeField] private PlayerUIModeLock playerLock;

    protected bool playerInRange = false;
    protected bool puzzleActive = false;

    protected IPuzzleUI puzzleUI;

    protected virtual void Awake()
    {
        /// Set up input bindings, cache puzzle UI, hide UI by default, and auto-resolve player lock if needed.
        controls = new Controls();

        controls.Gameplay.Interact.performed += _ => TryOpenPuzzle();
        controls.Gameplay.Cancel.performed += _ => TryClosePuzzle();

        CachePuzzleUI();

        if (puzzleUIScreen != null)
            puzzleUIScreen.SetActive(false);

        if (playerLock == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerLock = player.GetComponent<PlayerUIModeLock>();
        }
    }

    protected virtual void OnEnable() => controls.Enable();
    protected virtual void OnDisable() => controls.Disable();

    private void CachePuzzleUI()
    {
        /// Cache the IPuzzleUI component under the puzzle screen for reset calls.
        puzzleUI = null;
        if (puzzleUIScreen != null)
            puzzleUI = puzzleUIScreen.GetComponentInChildren<IPuzzleUI>(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        /// Track when the player enters interaction range.
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        /// Track when the player leaves interaction range.
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    public virtual void TryOpenPuzzle()
    {
        /// Guard against opening when out of range or already active.
        if (!playerInRange) return;
        if (puzzleActive) return;

        OpenPuzzle();
    }

    public virtual void TryClosePuzzle()
    {
        /// Only allow closing when the puzzle is currently active.
        if (!puzzleActive) return;
        ClosePuzzle();
    }

    protected virtual void OpenPuzzle()
    {
        /// Reset puzzle UI, lock player controls, and show the puzzle screen.
        if (puzzleUIScreen == null) return;

        CachePuzzleUI();
        puzzleUI?.ResetPuzzle();

        playerLock?.EnterPuzzleMode();

        puzzleUIScreen.SetActive(true);
        puzzleActive = true;
    }

    protected virtual void ClosePuzzle()
    {
        /// Reset puzzle UI, hide the screen, and restore player controls.
        if (puzzleUIScreen == null) return;

        puzzleUI?.ResetPuzzle();

        puzzleUIScreen.SetActive(false);
        puzzleActive = false;

        playerLock?.ExitPuzzleMode();
    }
}
