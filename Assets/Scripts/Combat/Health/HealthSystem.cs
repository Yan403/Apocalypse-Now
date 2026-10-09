using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Invincibility")]
    [Tooltip("Se verdadeiro, o personagem não sofre dano (ex.: i-frames).")]
    public bool invincible = false;

    public bool destroyOnDeath = false;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnDamaged;           // 🆕 NOVO

    void Awake()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    [ContextMenu("Damage 20")]
    void DebugDamage()
    {
        TakeDamage(20);
    }

    [ContextMenu("Heal 20")]
    void DebugHeal()
    {
        Heal(20);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        if (invincible) return;   // ⬅️ bloqueia dano durante i‑frames

        OnDamaged?.Invoke();                 // 🆕 NOVO - notifica qualquer dano recebido

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}