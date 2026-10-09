using System.Collections.Generic;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    private class ActiveEffect
    {
        public StatusEffect data;
        public float remainingTime;
        public float timeToNextTick;
        public bool isPermanent; // Opcional, para buffs sem duração

        public ActiveEffect(StatusEffect effect)
        {
            data = effect;
            remainingTime = effect.duration;
            timeToNextTick = 0f; // aplica imediatamente no primeiro tick
        }
    }

    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();
    private HealthSystem healthSystem;
    private PlayerController playerController;

    void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (DynamicMusicManager.IsFrozen)
            return;

        if (GameManager.IsPaused) return;

        float deltaTime = Time.deltaTime;

        // Itera de trás para frente para poder remover itens
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];
            effect.remainingTime -= deltaTime;
            effect.timeToNextTick -= deltaTime;

            // Aplica o efeito quando o timer de tick chega a zero
            if (effect.timeToNextTick <= 0f)
            {
                ApplyEffectTick(effect.data);
                effect.timeToNextTick = effect.data.tickInterval;
            }

            // Remove se a duração acabou
            if (effect.remainingTime <= 0f)
            {
                RemoveEffect(effect.data);
                activeEffects.RemoveAt(i);
            }
        }
    }

    private void ApplyEffectTick(StatusEffect effect)
    {
        switch (effect.effectType)
        {
            case StatusEffect.EffectType.HealOverTime:
                if (healthSystem != null)
                    healthSystem.Heal(effect.healPerTick);
                break;

            case StatusEffect.EffectType.SpeedBoost:
                // Exemplo: alterar velocidade no PlayerController
                // Implementação depende de como você gerencia velocidade
                // Podemos armazenar a velocidade original e aplicar multiplicador
                break;

                // Adicione outros casos
        }
    }

    private void RemoveEffect(StatusEffect effect)
    {
        // Reverte modificações permanentes, se houver (ex.: restaurar velocidade original)
        if (effect.effectType == StatusEffect.EffectType.SpeedBoost)
        {
            // Restaurar velocidade
        }
    }

    public void AddEffect(StatusEffect effectData)
    {
        // Verifica se já existe um efeito do mesmo tipo (pode substituir ou empilhar)
        // Por simplicidade, adicionamos um novo (pode ser melhor empilhar durações)
        activeEffects.Add(new ActiveEffect(effectData));
    }

    // Método público para limpar efeitos (útil em morte ou load)
    public void ClearAllEffects()
    {
        foreach (var effect in activeEffects)
        {
            RemoveEffect(effect.data);
        }
        activeEffects.Clear();
    }
}