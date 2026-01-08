using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedEnemy
{
    public GameObject prefab;
    [Min(0)] public int weight = 1;
}