using System;
using UnityEngine;

public class Loot : MonoBehaviour
{
    public ItemSO ItemSO;
    public SpriteRenderer sr;

    public int quantity;
    public static event Func<ItemSO, int, bool> OnItemLooted;   // <-- agora retorna bool

    private void OnValidate()
    {
        if (ItemSO == null) return;
        if (sr != null) sr.sprite = ItemSO.icon;
        name = ItemSO.itemName;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            bool collected = false;
            if (OnItemLooted != null)
                collected = OnItemLooted.Invoke(ItemSO, quantity);

            if (collected)
                Destroy(gameObject);
        }
    }
}