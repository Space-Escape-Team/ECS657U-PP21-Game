using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonCameraPosition : MonoBehaviour
{
    public Transform POV;

    // Update is called once per frame
    void Update()
    {
        transform.position = POV.position;
    }
}
