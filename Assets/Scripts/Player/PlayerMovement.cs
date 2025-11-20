using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using UnityEngine.Windows;

public class PlayerMovement : MonoBehaviour
{
    [Header("Configurables")]
    public float moveSpeed; // Default walk speed
    public float cameraTransitionSpeed = 5f;

    [Header("References")]
    public Transform orientation;
    public Transform pov;
    public InputActionReference move;
    public InputActionReference crouch;
    public Rigidbody rb;
    public Animator animator;

    private Vector3 moveDirection;
    private Vector2 input;
    private bool isCrouching = false;
    private float currentSpeed; // Influenced by running, crouching, crawling

    private Vector3 standPos = new (0.0f, 1.43f, 0.22f);
    private Vector3 crouchPos = new (0.1f, 0.95f, 0.37f);

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

        if (pov != null)
        {
            pov.localPosition = standPos;
        }

        currentSpeed = moveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        input = move.action.ReadValue<Vector2>();

        // Change only horizontal direction based on camera
        moveDirection = (orientation.forward * input.y + orientation.right * input.x).normalized;
        moveDirection.y = 0;

        UpdateAnimations();
        MovePOV();
    }
    private void UpdateAnimations()
    {
        if (animator == null) return;
        HandleLocomotion();
        HandleCrouch();
    }
    private void HandleLocomotion()
    {
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

    private void HandleCrouch()
    {
        // Check if crouch button was pressed
        if (crouch != null && crouch.action.triggered)
        {
            isCrouching = !isCrouching;

            // Update animator parameter
            animator.SetBool("IsCrouching", isCrouching);

            // Adjust movement speed and POV position based on isCrouching value
            currentSpeed = isCrouching ? moveSpeed / 2 : moveSpeed;
        }
    }

    private void MovePOV()
    {
        Vector3 newPosition = isCrouching ? crouchPos : standPos;

        // Smoothly interpolate towards the target position
        pov.localPosition = Vector3.Lerp(
            pov.localPosition,
            newPosition,
            cameraTransitionSpeed * Time.deltaTime
        );
    }
    private void FixedUpdate()
    {
        // Horizontal directions from WASD/Joystick inputs  
        float horizX = moveDirection.x * currentSpeed;
        float horizZ = moveDirection.z * currentSpeed;

        rb.velocity= new Vector3(horizX, rb.velocity.y, horizZ);
    }
}
