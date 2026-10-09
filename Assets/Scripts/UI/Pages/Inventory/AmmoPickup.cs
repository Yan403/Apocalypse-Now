using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private AmmoSO ammoData;
    [SerializeField] private SpriteRenderer sr;

    private void OnValidate()
    {
        if (ammoData != null && sr != null)
        {
            sr.sprite = ammoData.icon;
            name = ammoData.itemName;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // Determina a arma alvo
        GunSO targetWeapon = ammoData.weaponType ?? player.equippedWeapon;
        if (targetWeapon == null) return;

        // Verifica se o jogador possui a arma (se weaponType foi especificado)
        if (ammoData.weaponType != null)
        {
            InventoryController inventory = FindAnyObjectByType<InventoryController>(FindObjectsInactive.Include);
            if (inventory == null || !inventory.HasGun(targetWeapon))
                return;
        }

        // Tenta adicionar a munição; se conseguiu, destrói o objeto
        if (player.AddAmmoForWeapon(targetWeapon, ammoData.ammoAmount))
            Destroy(gameObject);
        else
            Debug.Log("Munição cheia – não foi possível coletar.");
    }
}
