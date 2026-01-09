using UnityEngine;

public class LightingInitialiser : MonoBehaviour
{
    [SerializeField] private Material skybox;

    // Fixes issues with lighting persisting from main menu
    private void Awake()
    {
        if (skybox != null)
            RenderSettings.skybox = skybox;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;

        DynamicGI.UpdateEnvironment();
    }
}
