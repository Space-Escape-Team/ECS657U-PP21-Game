using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public InputActionReference lookAction;

    [Header("Values")]
    public float xSens;
    public float ySens;


    private Vector2 lookInput;
    private float xRotation;
    private float yRotation;

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        lookAction.action.Enable();
    }

    private void Update()
    {
        lookInput = lookAction.action.ReadValue<Vector2>();

        float xDir = lookInput.x * xSens * Time.deltaTime;
        float yDir = lookInput.y * ySens * Time.deltaTime;

        // Correct for Unity First Person
        yRotation += xDir;
        xRotation -= yDir;

        // Prevent you from looking up or down more than 90 degrees as to not break your neck
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0); // Rotate camera along both axes
        orientation.rotation = Quaternion.Euler(0, yRotation, 0); // Rotate player along y axis
    }
}