using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewBehaviourScript : MonoBehaviour
{
    public Transform player;
    public Transform playerObj;
    public Transform orientation;
    public Rigidbody rb;
    public float rotationSpeed;
    public InputActionReference moveAction;

    private Vector2 moveInput;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        moveAction.action.Enable();
    }

    // Update is called once per frame
    private void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();

        // Rotate orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;

        // Rotate PlayerObj
        Vector3 inputDir = orientation.forward * moveInput.y + orientation.right * moveInput.x;

        if (inputDir != Vector3.zero)
            playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
    }
}
