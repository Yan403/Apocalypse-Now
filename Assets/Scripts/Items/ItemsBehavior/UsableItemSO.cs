using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Inventory/Usable Item")]
public class UsableItemSO : ItemSO
{
    [Header("Effects")]
    public List<ItemEffectSO> effects = new List<ItemEffectSO>();

    // (Opcional) Manter campos antigos para migração, mas não serão usados diretamente.
    // Recomendo removê-los e usar apenas a lista de efeitos.

    public virtual void Use(PlayerController player)
    {
        if (player == null) return;

        foreach (var effect in effects)
        {
            if (effect != null)
                effect.Apply(player);
        }
    }
}