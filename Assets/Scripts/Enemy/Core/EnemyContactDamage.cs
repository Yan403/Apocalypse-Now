using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Colocado nas hitboxes do inimigo. Causa dano ao jogador quando ele as toca.
/// </summary>
public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float damageCooldown = 1f; // tempo mínimo entre danos consecutivos ao mesmo alvo

    private Dictionary<IDamageable, float> lastDamageTime = new Dictionary<IDamageable, float>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable target = null;

        // Tenta obter via Hitbox (padrão do projeto)
        if (other.TryGetComponent<Hitbox>(out Hitbox hitbox) && hitbox.damageable != null)
        {
            target = hitbox.damageable;
        }
        else
        {
            // Fallback: IDamageable direto (para objetos sem Hitbox)
            target = other.GetComponent<IDamageable>();
        }

        if (target == null) return;

        // Verifica cooldown para evitar múltiplos danos no mesmo frame
        if (lastDamageTime.TryGetValue(target, out float lastTime) && Time.time - lastTime < damageCooldown)
            return;

        target.TakeDamage(damage);
        lastDamageTime[target] = Time.time;
    }

    private void OnDisable()
    {
        lastDamageTime.Clear(); // evita lixo de referências
    }
}
