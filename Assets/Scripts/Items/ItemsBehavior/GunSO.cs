using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Gun")]
public class GunSO : ItemSO
{
    public GameObject projectilePrefab;

    public float projectileSpeed;
    public float projectileLifetime;
    public float fireRate;

    public int damage;

    [Header("Munição")]
    public int maxAmmo = 30;        // capacidade máxima desta arma
    public int startingAmmo = 0;    // munição inicial ao equipar (0 = precisa coletar)
}
