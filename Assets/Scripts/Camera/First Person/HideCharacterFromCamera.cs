using UnityEngine;

public class HideCharacterFromCamera : MonoBehaviour
{
    public SkinnedMeshRenderer characterRenderer; // Assign in Inspector

    void Start()
    {
        // Hide the visual mesh but keep shadows
        if (characterRenderer != null)
        {
            characterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}