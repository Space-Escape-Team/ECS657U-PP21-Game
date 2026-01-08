using UnityEngine;

public class ArmsPivot
{
    public Transform armsPivot;
    public float downwardAngle = 10f; // tweak as needed

    void LateUpdate()
    {
        armsPivot.localRotation = Quaternion.Euler(downwardAngle, 0f, 0f);
    }
}
