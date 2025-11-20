using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnModel : MonoBehaviour
{
    public Transform Orientation;

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Orientation.rotation;
    }
}
