using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Ammo")]
public class AmmoSO : ItemSO
{
    public int ammoAmount = 10;

    [Tooltip("Arma que utiliza esta munição. Se vazio, funciona para qualquer arma (não recomendado).")]
    public GunSO weaponType;   // 🆕
}
