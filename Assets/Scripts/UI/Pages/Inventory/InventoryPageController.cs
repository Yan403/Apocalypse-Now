using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryPageController : MonoBehaviour
{
    [Header("Pages")]
    public GameObject gunPage;
    public GameObject itemPage;

    [Header("Tab Icons")]
    public GameObject gunIconNormal;
    public GameObject gunIconSelected;

    public GameObject itemIconNormal;
    public GameObject itemIconSelected;

    [Header("Inventory")]
    public InventoryController inventoryController;

    void Update()
    {
        // Só funciona se UI estiver ativa
        if (!GameInputManager.Instance.uiInputEnabled)
            return;

        // Só funciona se a página Inventory estiver aberta
        if (!UIStateManager.Instance.IsPage(UIStateManager.UIPage.Inventory))
            return;

        // Q → Weapons
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            OpenGunPage();
        }

        // E → Items
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenItemPage();
        }
    }

    void Start()
    {
        OpenGunPage();
    }
    public void OpenGunPage()
    {
        gunPage.SetActive(true);
        itemPage.SetActive(false);

        gunIconNormal.SetActive(false);
        gunIconSelected.SetActive(true);

        itemIconNormal.SetActive(true);
        itemIconSelected.SetActive(false);

        if (inventoryController != null)
            inventoryController.EnterGunInventory();
    }

    public void OpenItemPage()
    {
        gunPage.SetActive(false);
        itemPage.SetActive(true);

        gunIconNormal.SetActive(true);
        gunIconSelected.SetActive(false);

        itemIconNormal.SetActive(false);
        itemIconSelected.SetActive(true);

        if (inventoryController != null)
            inventoryController.EnterItemInventory();
    }
}