using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CrawlHandsIK : MonoBehaviour
{
    [Header("Hand Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Settings")]
    public LayerMask groundMask;
    public float raycastDistance = 0.5f;
    public float handHeightOffset = 0.02f;
    public float handRotationSpeed = 10f;
    public float ikBlendSpeed = 5f;

    [Header("Player State")]
    public bool isProne = false;

    private Animator animator;
    private float ikWeight = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        isProne = animator.GetBool("IsProne");

        // Smoothly blend IK in/out based on prone state
        float targetWeight = isProne ? 1f : 0f;
        ikWeight = Mathf.Lerp(ikWeight, targetWeight, Time.deltaTime * ikBlendSpeed);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!animator || ikWeight <= 0f) return;

        UpdateHandIK(AvatarIKGoal.LeftHand, leftHandTarget);
        UpdateHandIK(AvatarIKGoal.RightHand, rightHandTarget);
    }

    private void UpdateHandIK(AvatarIKGoal hand, Transform target)
    {
        if (!target) return;

        Vector3 handPos = target.position;
        Quaternion handRot = target.rotation;

        // Raycast down from above the hand
        if (Physics.Raycast(handPos + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, raycastDistance + 0.1f, groundMask))
        {
            // Move hand slightly above the floor
            handPos = hit.point + hit.normal * handHeightOffset;

            // Align hand to the floor normal
            Quaternion desiredRot = Quaternion.LookRotation(transform.forward, hit.normal);
            handRot = Quaternion.Slerp(handRot, desiredRot, handRotationSpeed * Time.deltaTime);
        }

        // Apply IK with smooth weight
        animator.SetIKPosition(hand, handPos);
        animator.SetIKRotation(hand, handRot);
        animator.SetIKPositionWeight(hand, ikWeight);
        animator.SetIKRotationWeight(hand, ikWeight);
    }
}
