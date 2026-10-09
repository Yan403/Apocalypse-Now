using System.Collections.Generic;
using UnityEngine;

public class DamageAuraPassive : PassiveEffectBehaviour, ITickable
{
    private float damagePerSecond;
    private float radius;
    private LayerMask targetLayer;
    private float tickInterval;
    public float TickInterval => tickInterval;

    public DamageAuraPassive(float dps, float r, float interval, LayerMask layer)
    {
        damagePerSecond = dps;
        radius = r;
        targetLayer = layer;
        tickInterval = Mathf.Max(0.1f, interval); // segurança: mínimo 0.1s
    }

    public void OnTick()
    {
        Debug.Log($"[DamageTick] Tick disparado! Dano={damagePerSecond}, Raio={radius}");
        if (player == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, radius, targetLayer);

        // Conjunto para armazenar IDamageable já processados
        HashSet<IDamageable> damagedSet = new HashSet<IDamageable>();

        Debug.Log($"[DamageTick] Colliders detectados: {hits.Length}");
        foreach (Collider2D hit in hits)
        {
            // Ignora o dono
            if (hit.transform.root == player.transform.root)
                continue;

            // Só processa se for um Torso (sem multiplicadores)
            if (hit.TryGetComponent<Hitbox>(out Hitbox hitbox) && hitbox.bodyPart == BodyPart.Torso)
            {
                if (hitbox.damageable != null && !damagedSet.Contains(hitbox.damageable))
                {
                    damagedSet.Add(hitbox.damageable);
                    hitbox.damageable.TakeDamage(Mathf.RoundToInt(damagePerSecond));
                }
            }
            else
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(Mathf.RoundToInt(damagePerSecond));
            }
        }
    }

    // (Opcional) Visualização no Editor durante o play
    public override void OnEquip(PlayerController owner)
    {
        base.OnEquip(owner);
    }
}