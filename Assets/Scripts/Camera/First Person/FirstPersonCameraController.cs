using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public InputActionReference lookAction;
    public Animator animator;

    [Header("Values")]
    public float xSens;
    public float ySens;

    public bool isProne;
    private Vector2 lookInput;
    private float xRotation;
    private float yRotation;

    public float proneMinPitch = -5f;
    public float proneMaxPitch = 25f;
    public float proneSensMultiplier = 0.5f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lookAction.action.Enable();
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        lookInput = lookAction.action.ReadValue<Vector2>();

        float xDir = lookInput.x * xSens * Time.deltaTime;
        float yDir = lookInput.y * ySens * Time.deltaTime;

        if (isProne)
        {
            xDir *= proneSensMultiplier;
            yDir *= proneSensMultiplier;
        }

        // Correct for Unity First Person
        yRotation += xDir;
        xRotation -= yDir;

        // Prevent you from looking too far up to break or neck or too far down to notice missing body
        if (isProne)
        {
            xRotation = Mathf.Clamp(xRotation, proneMinPitch, proneMaxPitch);
        }
        xRotation = Mathf.Clamp(xRotation, -90f, 63f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0); // Rotate camera along both axes
        orientation.transform.rotation = Quaternion.Euler(0, yRotation, 0); // Rotate player along y axis
    }

    public void SetCursorLock(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

}