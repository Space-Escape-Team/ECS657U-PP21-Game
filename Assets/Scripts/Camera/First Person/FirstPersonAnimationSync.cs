using UnityEngine;

public class FirstPersonAnimationSync : MonoBehaviour
{
    public Animator fullBodyAnimator;
    public Animator firstPersonArmsAnimator;

    void Update()
    {
        // Copy all relevant parameters to sync arm animations with full body animations
        firstPersonArmsAnimator.SetFloat("Speed", fullBodyAnimator.GetFloat("Speed"));
        firstPersonArmsAnimator.SetBool("IsMoving", fullBodyAnimator.GetBool("IsMoving"));
        firstPersonArmsAnimator.SetFloat("Forward", fullBodyAnimator.GetFloat("Forward"));
        firstPersonArmsAnimator.SetFloat("Strafe", fullBodyAnimator.GetFloat("Strafe"));
    }
}