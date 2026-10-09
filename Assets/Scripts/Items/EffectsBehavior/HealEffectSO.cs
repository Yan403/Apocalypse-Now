using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Heal")]
public class HealEffectSO : ItemEffectSO
{
    public int healAmount = 20;

    public override void Apply(PlayerController player)
    {
        HealthSystem health = player.GetComponent<HealthSystem>();
        if (health != null)
        {
            health.Heal(healAmount);
        }
    }
}
