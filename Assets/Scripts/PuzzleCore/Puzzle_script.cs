using UnityEngine;
using UnityEngine.InputSystem;

public class Puzzle_script : MonoBehaviour
{
    protected Controls controls;

    [Header("Puzzle UI Reference")]
    [SerializeField] protected GameObject puzzleUIScreen;

    [Header("Prompt Settings")]
    [SerializeField] public string puzzlePrompt = "Press E to interact";

    protected bool playerInRange = false;
    protected bool puzzleActive = false;

    protected virtual void Awake()
    {
        controls = new Controls();

        controls.Gameplay.Interact.performed += ctx => TryOpenPuzzle();
        controls.Gameplay.Cancel.performed += ctx => TryClosePuzzle();
    }

    protected virtual void OnEnable() => controls.Enable();
    protected virtual void OnDisable() => controls.Disable();

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
        if (playerInRange && !puzzleActive)
            OpenPuzzle();
    }

    public virtual void TryClosePuzzle()
    {
        if (puzzleActive)
            ClosePuzzle();
    }

    protected virtual void OpenPuzzle()
    {
        if (puzzleUIScreen != null)
        {
            puzzleUIScreen.SetActive(true);
            puzzleActive = true;
        }
    }

    protected virtual void ClosePuzzle()
    {
        if (puzzleUIScreen != null)
        {
            puzzleUIScreen.SetActive(false);
            puzzleActive = false;
        }
    }
}

