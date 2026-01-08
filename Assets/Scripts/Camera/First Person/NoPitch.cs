//using UnityEngine;

//public class NoPitch : MonoBehaviour
//{
//    public Transform cameraTransform;

//    void LateUpdate()
//    {
//        // Get camera pitch in degrees (-90 to 90)
//        float pitch = cameraTransform.localEulerAngles.x;
//        if (pitch > 180f) pitch -= 360f;

//        // Cancel pitch ONLY, preserve yaw/roll
//        transform.localRotation = Quaternion.Euler(-pitch, 0f, 0f);
//    }
//}
