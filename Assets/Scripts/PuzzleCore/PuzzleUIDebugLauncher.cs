using UnityEngine;
using UnityEngine.InputSystem;

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
        controls.Enable();
        CloseAll();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
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
        if (currentPuzzle == null) return;

        ResetCurrent();
        currentPuzzle.SetActive(false);
        currentPuzzle = null;
    }

    private void ResetCurrent()
    {
        if (currentPuzzle == null) return;

        var puzzle = currentPuzzle.GetComponentInChildren<IPuzzleUI>(true);
        puzzle?.ResetPuzzle();
    }

    private void CloseAll()
    {
        if (puzzleA) puzzleA.SetActive(false);
        if (puzzleB) puzzleB.SetActive(false);
        if (puzzleC) puzzleC.SetActive(false);
        if (puzzleD) puzzleD.SetActive(false);

        currentPuzzle = null;
    }

    public void NotifyPuzzleSolved(string puzzleName)
    {
        puzzleCompleted = true;
        lastCompletedPuzzle = puzzleName;

        Debug.Log($"[PUZZLE SOLVED] {puzzleName} reported completion (DEBUG).", this);
    }
}
