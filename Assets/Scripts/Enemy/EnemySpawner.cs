using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawnable enemies")]
    [SerializeField] private WeightedEnemy[] enemies;

    private void Awake()
    {
        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        GameObject prefab = GetWeightedRandomEnemy();
        if (prefab == null) return;

        Instantiate(prefab, transform.position, transform.rotation);
    }

    private GameObject GetWeightedRandomEnemy()
    {
        int totalWeight = 0;

        // Sum valid weights
        foreach (var enemy in enemies)
        {
            if (enemy.weight > 0)
                totalWeight += enemy.weight;
        }

        // Safety check
        if (totalWeight == 0)
        {
            Debug.LogWarning($"{name}: No enemies with weight > 0");
            return null;
        }

        int randomValue = Random.Range(0, totalWeight);

        // Pick enemy
        foreach (var enemy in enemies)
        {
            if (enemy.weight <= 0)
                continue;

            if (randomValue < enemy.weight)
                return enemy.prefab;

            randomValue -= enemy.weight;
        }

        return null;
    }
}