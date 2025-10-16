using UnityEngine;

public abstract class Puzzle_script : MonoBehaviour
{
    [Header("Puzzle Interaction Settings")]
    public string puzzlePrompt = "Press E to interact";

    // Called when the player interacts with the puzzle object
    public virtual void ActivatePuzzle()
    {
        Debug.Log($"Activated puzzle on {gameObject.name}");
    }
}