using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public HealthSystem healthSystem;
    public Image healthFill;

    void Start()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += UpdateHealthBar;
        }

        UpdateHealthBar(healthSystem.currentHealth, healthSystem.maxHealth);
    }

    void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
        }
    }

    void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        float healthPercent = (float)currentHealth / maxHealth;
        healthFill.fillAmount = healthPercent;
    }
}