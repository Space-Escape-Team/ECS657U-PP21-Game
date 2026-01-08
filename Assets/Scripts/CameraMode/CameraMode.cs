using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMode : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject overlayFilter;
    public GameObject reticle;
    public AlbumUI albumUI;

    [Header("Zoom Settings")]
    public float normalFOV = 60f;
    public float zoomFOV = 25f;
    public float zoomSpeed = 8f;

    [Header("Input")]
    public InputActionReference toggleCameraAction;
    public InputActionReference captureAction;
    public InputActionReference toggleAlbumAction;

    [Header("Album Settings")]
    public int maxPhotos = 50;
    public int thumbnailWidth = 256;
    public int thumbnailHeight = 144;

    private bool cameraModeActive = false;
    private List<AlbumEntry> photoAlbum = new List<AlbumEntry>();

    public List<AlbumEntry> PhotoAlbum { get { return photoAlbum; } }
    void Start()
    {
        LoadAlbumFromStorage();
    }

    private void OnEnable()
    {
        toggleCameraAction.action.Enable();
        captureAction.action.Enable();
        toggleAlbumAction.action.Enable();

        toggleCameraAction.action.performed += ToggleCamera;
        captureAction.action.performed += Capture;
        toggleAlbumAction.action.performed += ToggleAlbum;
    }

    private void OnDisable()
    {
        toggleCameraAction.action.performed -= ToggleCamera;
        captureAction.action.performed -= Capture;
        toggleAlbumAction.action.performed -= ToggleAlbum;
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
    private void ToggleAlbum(InputAction.CallbackContext ctx)
    {
        if (albumUI != null)
        {
            albumUI.ToggleAlbum();
        }
        else
        {
            Debug.LogWarning("AlbumUI reference not found in CameraMode.");
        }
    }

    // Main capture function
    private void Capture(InputAction.CallbackContext ctx)
    {
        if (!cameraModeActive) return;

        // 1. Capture screenshot
        Texture2D fullResTex = CaptureCameraView(playerCamera);

        // 2. Create thumbnail version (for album display)
        Texture2D thumbnailTex = CreateThumbnail(fullResTex, thumbnailWidth, thumbnailHeight);

        // 3. Analyze for enemies
        bool enemyDetected = false;

        // 4. Create and store album entry
        AlbumEntry newEntry = new AlbumEntry(
            Texture2DToBase64(thumbnailTex),
            System.DateTime.Now.ToString("MM/dd HH:mm"),
            enemyDetected
        );

        // 5. Manage album size
        photoAlbum.Insert(0, newEntry);
        if (photoAlbum.Count > maxPhotos)
        {
            photoAlbum.RemoveAt(photoAlbum.Count - 1);
        }

        // 6. Save to persistent storage
        SaveAlbumToStorage();

        // 7. Cleanup
        Destroy(fullResTex);
        Destroy(thumbnailTex);

        Debug.Log($"Photo captured! Enemy: {enemyDetected}. Album: {photoAlbum.Count} photos");
    }

    // Capture camera view to Texture2D
    private Texture2D CaptureCameraView(Camera cam)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);

        cam.targetTexture = renderTexture;
        cam.Render();
        RenderTexture.active = renderTexture;

        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        cam.targetTexture = null;
        RenderTexture.active = currentRT;
        Destroy(renderTexture);

        return screenshot;
    }

    // Create resized thumbnail
    private Texture2D CreateThumbnail(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        RenderTexture.active = rt;

        // Use Graphics.Blit for efficient resize
        Graphics.Blit(source, rt);

        Texture2D thumbnail = new Texture2D(width, height, TextureFormat.RGB24, false);
        thumbnail.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        thumbnail.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return thumbnail;
    }

    //private bool CheckImageForEnemy()
    //{
    //    Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    //    RaycastHit hit;

    //    if (Physics.Raycast(ray, out hit, 100f))
    //    {
    //        // Check if the hit object has an "Enemy" tag or component
    //        if (hit.collider.CompareTag("Enemy") || hit.collider.GetComponent<EnemyType>() != null)
    //        {
    //            return true;
    //        }
    //    }
    //    return false;
    //}

    public bool RemovePhotoAtIndex(int index)
    {
        // Safety check
        if (index < 0 || index >= photoAlbum.Count)
        {
            Debug.LogWarning($"Index {index} is out of range. Album has {photoAlbum.Count} photos.");
            return false;
        }

        // 1. Remove from the list
        photoAlbum.RemoveAt(index);

        // 2. Immediately save the updated list to persistent storage
        SaveAlbumToStorage();

        // 3. (Optional) Refresh any in-memory caches or references
        Debug.Log($"Removed photo at index {index}. {photoAlbum.Count} photos remaining.");

        return true;
    }

    public void SaveAlbumToStorage()
    {
        AlbumDataWrapper wrapper = new AlbumDataWrapper(photoAlbum);
        string albumJson = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("PlayerPhotoAlbum", albumJson);
        PlayerPrefs.Save();
    }

    public void LoadAlbumFromStorage()
    {
        if (PlayerPrefs.HasKey("PlayerPhotoAlbum"))
        {
            string albumJson = PlayerPrefs.GetString("PlayerPhotoAlbum");
            AlbumDataWrapper wrapper = JsonUtility.FromJson<AlbumDataWrapper>(albumJson);

            if (wrapper != null && wrapper.entries != null)
            {
                photoAlbum = wrapper.entries;
            }
        }
        Debug.Log($"Loaded {photoAlbum.Count} photos from storage");
    }

    public string Texture2DToBase64(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToPNG();
        return Convert.ToBase64String(imageBytes);
    }

    public Texture2D Base64ToTexture2D(string base64)
    {
        byte[] imageBytes = Convert.FromBase64String(base64);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageBytes); // This auto-resizes the texture
        return tex;
    }
}
