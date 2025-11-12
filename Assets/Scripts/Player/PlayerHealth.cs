using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public Slider healthBarSlider; // Slider for UI health bar
    private int maxHealth;
    private int currentHealth;
    private bool alive;

    void Start()
    {
        SetMaxHealth(100);
        SetCurrentHealth(maxHealth);
        alive = true;
    }

    private void SetMaxHealth(int health)
    {
        maxHealth = health;
        healthBarSlider.maxValue = maxHealth;
    }

    private void SetCurrentHealth(int health)
    {
        if (health >= maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if (health <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            currentHealth = health;
        }

        healthBarSlider.value = currentHealth;
    }

    public void TakeDamage(int damage)
    {
        SetCurrentHealth(currentHealth -= damage);
    }

    public void Heal(int healAmount)
    {
        if (alive)
        {
            SetCurrentHealth(currentHealth += healAmount);
        }
        else
        {
            Debug.Log("Can't heal once dead");
        }
    }

    void Die()
    {
        if (alive)
        {
            Debug.Log("Player died");
            alive = false;
        }
    }
}