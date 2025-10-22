using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed;
    public Transform orientation;
    public InputActionReference move;
    public Rigidbody rb;

    private float horizInput;
    private float vertInput;
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
        moveDirection = move.action.ReadValue<Vector3>();
    }

    private void FixedUpdate()
    {
        rb.velocity= new Vector3(moveDirection.x * moveSpeed, moveDirection.y * moveSpeed, moveDirection.z * moveSpeed);
    }
}
