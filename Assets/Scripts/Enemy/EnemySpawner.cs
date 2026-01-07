using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;

    private void Awake()
    {
        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject enemy = Instantiate(
            enemyPrefabs[randomIndex],
            transform.position,
            transform.rotation
        );
    }
}