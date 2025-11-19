using UnityEngine;

public class HideCharacterModel : MonoBehaviour
{

    void Start()
    {
        // Extracts each Skinned Mesh Renderer from model parent object 
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in allRenderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}