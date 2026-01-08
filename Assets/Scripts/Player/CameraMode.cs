using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

public class CameraMode : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject overlayFilter;
    public GameObject reticle;

    [Header("Zoom Settings")]
    public float normalFOV = 60f;
    public float zoomFOV = 25f;
    public float zoomSpeed = 8f;

    [Header("Input")]
    public InputActionReference toggleCameraAction;
    public InputActionReference captureAction;

    private bool cameraModeActive = false;

    private void OnEnable()
    {
        toggleCameraAction.action.actionMap.Enable();
        captureAction.action.actionMap.Enable();

        toggleCameraAction.action.performed += ToggleCamera;
        captureAction.action.performed += Capture;
    }

    private void OnDisable()
    {
        toggleCameraAction.action.performed -= ToggleCamera;
        captureAction.action.performed -= Capture;
    }

    private void Update()
    {
        float targetFOV = cameraModeActive ? zoomFOV : normalFOV;
        playerCamera.fieldOfView =
            Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }

    private void ToggleCamera(InputAction.CallbackContext ctx)
    {
        cameraModeActive = !cameraModeActive;
        overlayFilter.SetActive(cameraModeActive);
        reticle.SetActive(cameraModeActive);
    }

    private void Capture(InputAction.CallbackContext ctx)
    {
        if (!cameraModeActive) return;

        string folderPath = Path.Combine(Application.persistentDataPath, "Photos");
        Directory.CreateDirectory(folderPath);

        string filename = $"Photo_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = Path.Combine(folderPath, filename);

        ScreenCapture.CaptureScreenshot(fullPath);
        Debug.Log($"Photo saved to: {fullPath}");
    }
}
