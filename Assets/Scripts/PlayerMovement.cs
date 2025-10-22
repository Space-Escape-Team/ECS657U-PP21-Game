using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public float moveSpeed;
    public Transform orientation;
    public InputActionReference move;
    public Rigidbody rb;

    private Vector3 moveDirection;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = move.action.ReadValue<Vector2>();

        // Change only horizontal direction based on camera
        moveDirection = (orientation.forward * input.y + orientation.right * input.x).normalized;
        moveDirection.y = 0;
    }

    private void FixedUpdate()
    {
        // Horizontal directions from WASD/Joystick inputs 
        float horizX = moveDirection.x * moveSpeed;
        float horizZ = moveDirection.z * moveSpeed;

        rb.velocity= new Vector3(horizX, rb.velocity.y, horizZ);
    }
}
