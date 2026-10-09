using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Temporary Effect")]
public class TemporaryEffects : ItemEffectSO
{
    public StatusEffect statusEffect;

    public override void Apply(PlayerController player)
    {
        if (player == null) return;

        StatusEffectManager manager = player.GetComponent<StatusEffectManager>();
        if (manager != null)
        {
            manager.AddEffect(statusEffect);
        }
        else
        {
            Debug.LogWarning("StatusEffectManager não encontrado no jogador.");
        }
    }
}