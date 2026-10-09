using System;
using UnityEngine;

[Serializable]
public class StatusEffect
{
    public enum EffectType
    {
        HealOverTime,
        SpeedBoost,
        // Adicione outros tipos conforme necessário
    }

    public EffectType effectType;
    public float duration = 5f;          // Duração total em segundos
    public float tickInterval = 1f;      // Intervalo entre aplicações do efeito
    public int healPerTick = 5;          // Para HealOverTime
    public float speedMultiplier = 1.5f; // Para SpeedBoost
    // Adicione outros campos específicos conforme novos tipos
}
