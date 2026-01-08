using UnityEngine;
using UnityEngine.InputSystem;

/// Debug-only launcher for opening/closing puzzle UI roots via keyboard.
/// 1-4 opens PuzzleA-D, R resets the currently open puzzle, and the Gameplay.Cancel action closes the current puzzle.
/// Tracks last completion via NotifyPuzzleSolved for inspector/debug visibility.
public class PuzzleUIDebugLauncher : MonoBehaviour
{
    [Header("Puzzle UI Roots")]
    [SerializeField] private GameObject puzzleA;
    [SerializeField] private GameObject puzzleB;
    [SerializeField] private GameObject puzzleC;
    [SerializeField] private GameObject puzzleD;

    [Header("Debug State")]
    [SerializeField] private bool puzzleCompleted;
    [SerializeField] private string lastCompletedPuzzle;

    private Controls controls;
    private GameObject currentPuzzle;

    public static PuzzleUIDebugLauncher Instance { get; private set; }

    private void Awake()
    {
        /// Singleton instance setup and input binding for Cancel -> CloseCurrent.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        controls = new Controls();
        controls.Gameplay.Cancel.performed += _ => CloseCurrent();
    }

    private void OnEnable()
    {
        /// Enable input and ensure all puzzle roots start closed.
        controls.Enable();
        CloseAll();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        /// Keyboard shortcuts: 1-4 open puzzles, R resets current.
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) Open(puzzleA, "PuzzleA");
        if (kb.digit2Key.wasPressedThisFrame) Open(puzzleB, "PuzzleB");
        if (kb.digit3Key.wasPressedThisFrame) Open(puzzleC, "PuzzleC");
        if (kb.digit4Key.wasPressedThisFrame) Open(puzzleD, "PuzzleD");

        if (kb.rKey.wasPressedThisFrame) ResetCurrent();
    }

    private void Open(GameObject target, string puzzleName)
    {
        /// Close any open puzzle, clear debug completion state, then activate the requested puzzle and reset it.
        if (target == null) return;

        CloseCurrent();

        puzzleCompleted = false;
        lastCompletedPuzzle = string.Empty;

        currentPuzzle = target;
        currentPuzzle.SetActive(true);

        Debug.Log($"[PUZZLE DEBUG] Opened {puzzleName}", this);

        ResetCurrent();
    }

    private void CloseCurrent()
    {
        /// Reset then deactivate the currently open puzzle root.
        if (currentPuzzle == null) return;

        ResetCurrent();
        currentPuzzle.SetActive(false);
        currentPuzzle = null;
    }

    private void ResetCurrent()
    {
        /// Call ResetPuzzle() on the first IPuzzleUI found under the active puzzle root.
        if (currentPuzzle == null) return;

        var puzzle = currentPuzzle.GetComponentInChildren<IPuzzleUI>(true);
        puzzle?.ResetPuzzle();
    }

    private void CloseAll()
    {
        /// Deactivate all puzzle roots and clear current selection.
        if (puzzleA) puzzleA.SetActive(false);
        if (puzzleB) puzzleB.SetActive(false);
        if (puzzleC) puzzleC.SetActive(false);
        if (puzzleD) puzzleD.SetActive(false);

        currentPuzzle = null;
    }

    public void NotifyPuzzleSolved(string puzzleName)
    {
        /// Called by puzzles to record completion state for debugging/logging.
        puzzleCompleted = true;
        lastCompletedPuzzle = puzzleName;

        Debug.Log($"[PUZZLE SOLVED] {puzzleName} reported completion (DEBUG).", this);
    }
}
