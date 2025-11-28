using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PuzzleDebug : MonoBehaviour
{
    private void Start()
    {
        Debug.Log($"Puzzle Box layer: {LayerMask.LayerToName(gameObject.layer)} tag: {gameObject.tag} isTrigger: {GetComponent<Collider>().isTrigger}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"TriggerEnter by {other.name} tag:{other.tag}");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"TriggerExit by {other.name}");
    }
}
