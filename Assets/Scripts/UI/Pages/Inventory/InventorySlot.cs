using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public ItemSO itemSO;
    public int quantity;

    public Image itemImage;
    public TMP_Text quantityText;

    public GameObject selectedIcon;

    public void UpdateUI()
    {
        if (itemSO != null)
        {
            itemImage.sprite = itemSO.icon;
            itemImage.gameObject.SetActive(true);

            if (itemSO.isGun)
            {
                quantityText.text = ""; // armas não acumulam
            }
            else
            {
                quantityText.text = quantity > 0 ? quantity.ToString() : "";
                //quantityText.text = quantity.ToString();
            }
        }
        else
        {
            itemImage.gameObject.SetActive(false);
            quantityText.text = "";
        }
    }

    public void SetSelected(bool value)
    {
        if (selectedIcon != null)
        {
            selectedIcon.SetActive(value);
        }
    }
}