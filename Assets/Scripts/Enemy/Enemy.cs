using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private int damageAmount = 25;
    private float attackInterval = 1;
    private float lastAttackTime = -Mathf.Infinity; // Enemy can attack immediately

    private void OnCollisionEnter(Collision collision)
    {
        TryToAttack(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryToAttack(collision.gameObject);
    }

    private void TryToAttack(GameObject other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Player player = other.GetComponent<Player>();
        if (player == null || player.playerHealth == null)
        {
            return;
        }

        if (Time.time - lastAttackTime >= attackInterval)
        {
            player.playerHealth.TakeDamage(damageAmount);
            lastAttackTime = Time.time;
        }
    }
}