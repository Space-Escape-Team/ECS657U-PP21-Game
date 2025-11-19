using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public float moveSpeed;
    public Transform orientation;
    public InputActionReference move;
    public Rigidbody rb;
    public Animator animator;

    private Vector3 moveDirection;
    private Vector2 input;

    // Start is called before the first frame update
    //
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        animator = GetComponent<Animator>();

        // Ensure move action is enabled for animator to read it
        if (move != null && move.action != null)
            move.action.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        input = move.action.ReadValue<Vector2>();

        // Change only horizontal direction based on camera
        moveDirection = (orientation.forward * input.y + orientation.right * input.x).normalized;
        moveDirection.y = 0;

        UpdateAnimations();
    }
    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Convert world space movement to local space for animation
        Vector3 localMove = transform.InverseTransformDirection(moveDirection);

        // Implement damping to ensure animator values slowly change for smooth transitions within the blend tree
        float damping = 10f;

        float smoothForward = Mathf.Lerp(
            animator.GetFloat("Forward"),
            localMove.z,
            Time.deltaTime * damping
        );

        float smoothStrafe = Mathf.Lerp(
            animator.GetFloat("Strafe"),
            localMove.x,
            Time.deltaTime * damping
        );

        // Set animator parameters
        animator.SetFloat("Forward", smoothForward);
        animator.SetFloat("Strafe", smoothStrafe);
        animator.SetFloat("Speed", input.magnitude);

        bool isMoving = input.magnitude > 0.2f;
        animator.SetBool("IsMoving", isMoving);
    }
    private void FixedUpdate()
    {
        // Horizontal directions from WASD/Joystick inputs 
        float horizX = moveDirection.x * moveSpeed;
        float horizZ = moveDirection.z * moveSpeed;

        rb.velocity= new Vector3(horizX, rb.velocity.y, horizZ);
    }
}
