using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthBarSlider;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        SetMaxHealth(maxHealth);
        IsAlive = true;
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive) 
        {
            return;
        }
        if (damage <= 0) 
        {
            return;
        }

        SetHealth(currentHealth - damage);
    }

    public void Heal(int healAmount)
    {
        if (!IsAlive) 
        {
            return;
        }
        if (healAmount <= 0) 
        {
            return;
        }

        SetHealth(currentHealth + healAmount);
    }

    public void SetMaxHealth(int newMax, bool fullHeal = true)
    {
        maxHealth = Mathf.Max(1, newMax);

        // Allows max health upgrades that don't heal the player
        if (fullHeal)
        {
            SetHealth(maxHealth);
        }
    }

    private void SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        SyncUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void SyncUI()
    {
        if (healthBarSlider == null) 
        {
            return;
        }

        healthBarSlider.maxValue = maxHealth;
        healthBarSlider.value = currentHealth;
    }

    private void Die()
    {
        if (!IsAlive) 
        {
            return;
        }

        IsAlive = false;
        Debug.Log("Player died");
        FindFirstObjectByType<EndingRouter>()?.LoadEndingForProgress();
    }
}
