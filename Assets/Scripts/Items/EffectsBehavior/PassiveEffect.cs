using UnityEngine;

/// <summary>
/// Define um tipo de efeito passivo. Cada efeito deve ter seu próprio asset.
/// </summary>
[CreateAssetMenu(menuName = "Passive Effects/Passive Effect Data")]
public class PassiveEffect : ScriptableObject
{
    public enum EffectType
    {
        DamageAura,   // Exemplo
        Regen,        // Pode adicionar mais
        SpeedBoost
        // Adicione novos tipos conforme criar novas classes
    }

    public EffectType effectType;

    [Header("Parâmetros (variam por tipo)")]
    public float floatParam1; // Ex: dano por segundo / cura por segundo
    public float floatParam2; // Ex: raio / multiplicador de velocidade
    public LayerMask targetLayer; // Para auras

    [Header("Tick (apenas para efeitos por tick)")]
    [Tooltip("Intervalo entre ticks (segundos). 0 = efeito contínuo.")]
    public float tickInterval = 0f;

    [Header("Visual (opcional)")]
    [Tooltip("Prefab opcional que será instanciado ao ativar o efeito (ex: partículas).")]
    public GameObject visualPrefab;

    /// <summary>
    /// Fábrica que cria a instância concreta do efeito baseado no tipo.
    /// </summary>
    public PassiveEffectBehaviour CreateEffectInstance()
    {
        switch (effectType)
        {
            case EffectType.DamageAura:
                return new DamageAuraPassive(floatParam1, floatParam2, tickInterval, targetLayer);

            // case EffectType.Regen: return new RegenPassive(floatParam1); etc.
            // case EffectType.SpeedBoost: return new SpeedBoostPassive(floatParam1);

            default:
                Debug.LogError($"Tipo de efeito não implementado: {effectType}");
                return null;
        }
    }
}