using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class AlbumUI : MonoBehaviour
{
    [Header("References")]
    public GameObject albumPanel;
    public Transform photoGridContent;
    public GameObject photoThumbnailPrefab;
    public Button closeAlbumButton;
    public TextMeshProUGUI albumStatusText;
    public FirstPersonCameraController firstPersonCameraController;

    [Header("Display Settings")]
    public int thumbnailsPerRow = 4;
    public float thumbnailSpacing;

    private CameraMode cameraMode;
    private GridLayoutGroup gridLayout;

    void Start()
    {
        cameraMode = FindFirstObjectByType<CameraMode>();
        gridLayout = photoGridContent.GetComponent<GridLayoutGroup>();

        closeAlbumButton.onClick.AddListener(() => ToggleAlbum());

        // Album hidden initially
        albumPanel.SetActive(false);
    }

    public void OpenAlbum()
    {
        albumPanel.SetActive(true);
        RefreshAlbumDisplay();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (firstPersonCameraController != null)
        {
            firstPersonCameraController.enabled = false;
        }
    }
      
    public void RefreshAlbumDisplay()
    {
        // Clear existing thumbnails
        foreach (Transform child in photoGridContent)
        {
            Destroy(child.gameObject);
        }

        if (cameraMode == null || cameraMode.PhotoAlbum == null)
        {
            albumStatusText.text = "No photos in album";
            return;
        }

        var album = cameraMode.PhotoAlbum;

        if (album.Count == 0)
        {
            albumStatusText.text = "Album is empty";
            return;
        }

        albumStatusText.text = $"{album.Count} photos";

        var i = 0;
        foreach (var entry in album)
        {
            GameObject thumbnail = Instantiate(photoThumbnailPrefab, photoGridContent);
            SetupThumbnail(thumbnail, entry, i);
            i++;
        }
    }

    // Set up each photo with details (image, buttons, info)
    private void SetupThumbnail(GameObject thumbnail, AlbumEntry entry, int photoIndex)
    {
        RawImage image = thumbnail.GetComponentInChildren<RawImage>();
        TextMeshProUGUI infoText = thumbnail.GetComponentInChildren<TextMeshProUGUI>();
        Button button = thumbnail.GetComponent<Button>();
        Button deleteButton = thumbnail.transform.Find("DeleteButton")?.GetComponent<Button>();

        if (image == null || infoText == null) return;

        Texture2D photoTexture = cameraMode.Base64ToTexture2D(entry.photoBase64);
        image.texture = photoTexture;

        string enemyIcon = entry.containsEnemy ? "☠ " : "✓ ";
        infoText.text = $"{enemyIcon}{entry.timestamp}";

        infoText.color = entry.containsEnemy ? Color.red : Color.green;

        if (button != null)
        {
            button.onClick.AddListener(() => OnThumbnailClicked(entry));
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() => DeletePhoto(photoIndex));
        }
    }

    private void DeletePhoto(int index)
    {
        if (cameraMode != null)
        {
            bool success = cameraMode.RemovePhotoAtIndex(index);
            if (success)
            {
                RefreshAlbumDisplay();
                Debug.Log($"Deleted photo at index {index}");
            }
        }
    }

    private void OnThumbnailClicked(AlbumEntry entry)
    {
        Debug.Log($"Photo clicked: {entry.timestamp}, Enemy: {entry.containsEnemy}");
    }

    public void CloseAlbum()
    {
        albumPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (firstPersonCameraController != null)
        {
            firstPersonCameraController.enabled = true;
        }
    }
    public void ToggleAlbum()
    {
        if (albumPanel.activeSelf)
        {
            CloseAlbum();
        }
        else
        {
            OpenAlbum();
        }
    }
}