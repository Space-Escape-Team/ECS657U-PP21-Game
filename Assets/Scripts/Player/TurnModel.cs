using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnModel : MonoBehaviour
{
    public Transform Orientation;

    // Update is called once per frame
    void Update()
    {
        Vector3 euler = Orientation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
    }
}
