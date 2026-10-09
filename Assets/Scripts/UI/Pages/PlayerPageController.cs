using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerPageController : MonoBehaviour
{
    public bool pageActive;

    [Header("UI References")]
    public Image weaponIcon;
    public Image itemIcon;
    public Image punchIcon;

    [Header("Inventory")]
    public InventoryController inventoryController;

    [Header("Munição UI")]
    public TMP_Text ammoText;

    private void Start()
    {
        if (inventoryController != null)
        {
            inventoryController.player.OnAmmoChanged += UpdateAmmoText;
            inventoryController.OnSelectionChanged += RefreshUI;
            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        if (inventoryController != null) 
        {
            inventoryController.OnSelectionChanged -= RefreshUI;
            inventoryController.player.OnAmmoChanged -= UpdateAmmoText;
        }
    }

    void Update()
    {
        if (!pageActive) return;

        if (!GameInputManager.Instance.uiInputEnabled) return;

        if (!UIStateManager.Instance.IsPage(UIStateManager.UIPage.Player)) return;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            inventoryController?.NextItem();
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            inventoryController?.PreviousItem();
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            inventoryController?.NextWeapon();
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            inventoryController?.PreviousWeapon();
        }

        // Uso do item equipado com Espaço
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            inventoryController?.UseSelectedItem();
        }
    }

    public void RefreshUI()
    {
        Debug.Log($"[RefreshUI] Chamado. equippedItem: {inventoryController?.player?.equippedItem?.itemName}, " +
            $"CurrentPassiveItem: {inventoryController?.player?.CurrentPassiveItem?.itemName}");

        if (inventoryController == null || inventoryController.player == null) return;

        // Atualiza Arma
        GunSO weapon = inventoryController.player.equippedWeapon;
        if (weapon != null)
        {
            if (weaponIcon != null) weaponIcon.sprite = weapon.icon;
            if (weaponIcon != null) weaponIcon.gameObject.SetActive(true);
            if (punchIcon != null) punchIcon.gameObject.SetActive(false);
        }
        else
        {
            // Modo Soco
            if (weaponIcon != null) weaponIcon.gameObject.SetActive(false);
            if (punchIcon != null) punchIcon.gameObject.SetActive(true); // ou ícone de soco
        }

        // Atualiza Item
        UsableItemSO item = inventoryController.player.equippedItem;
        ItemSO passiveItem = inventoryController.player.CurrentPassiveItem;

        if (item != null)
        {
            if (itemIcon != null) itemIcon.sprite = item.icon;
            if (itemIcon != null) itemIcon.gameObject.SetActive(true);
        }
        else if (passiveItem != null)
        {
            // Mostra o item passivo no holder
            if (itemIcon != null) itemIcon.sprite = passiveItem.icon;
            if (itemIcon != null) itemIcon.gameObject.SetActive(true);
        }
        else
        {
            if (itemIcon != null) itemIcon.gameObject.SetActive(false);
        }

        if (ammoText != null && inventoryController.player != null)
        {
            bool hasWeapon = inventoryController.player.equippedWeapon != null;
            ammoText.gameObject.SetActive(hasWeapon);
            if (hasWeapon)
                ammoText.text = inventoryController.player.currentAmmo.ToString();
        }
    }

    public void SetActive(bool active)
    {
        pageActive = active;
        if (active) RefreshUI();
    }
    private void UpdateAmmoText(int ammo)
    {
        if (ammoText == null) return;

        // Só mostra munição se houver arma equipada
        bool hasWeapon = inventoryController.player != null && inventoryController.player.equippedWeapon != null;
        ammoText.gameObject.SetActive(hasWeapon);

        if (hasWeapon)
            ammoText.text = ammo.ToString();
    }
}

/*
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class PlayerPageController : MonoBehaviour
{
    public bool pageActive;

    [Header("Referências")]
    public InventoryController inventoryController;

    [Header("Holders de exibição")]
    public Image weaponHolder;
    public Image itemHolder;

    public static bool arrowConsumedThisFrame = false;

    private void OnEnable()
    {
        if (inventoryController == null) return;
        inventoryController.OnWeaponSelectionChanged += RefreshWeaponHolder;
        inventoryController.OnItemSelectionChanged += RefreshItemHolder;
    }

    private void OnDisable()
    {
        if (inventoryController == null) return;
        inventoryController.OnWeaponSelectionChanged -= RefreshWeaponHolder;
        inventoryController.OnItemSelectionChanged -= RefreshItemHolder;
    }

    void Update()
    {
        arrowConsumedThisFrame = false;

        if (!pageActive) return;
        if (!GameInputManager.Instance.uiInputEnabled) return;
        if (!UIStateManager.Instance.IsPage(UIStateManager.UIPage.Player)) return;
        if (inventoryController == null) return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            arrowConsumedThisFrame = true;
            inventoryController.MoveGunSelection(1);
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            arrowConsumedThisFrame = true;
            inventoryController.MoveGunSelection(-1);
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            arrowConsumedThisFrame = true;
            inventoryController.MoveItemSelection(-1);
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            arrowConsumedThisFrame = true;
            inventoryController.MoveItemSelection(1);
        }
    }

    void RefreshWeaponHolder(int _)
    {
        if (weaponHolder == null || inventoryController == null) return;

        InventorySlot slot = inventoryController.gunSlots[inventoryController.gunSelectedIndex];

        weaponHolder.gameObject.SetActive(slot.itemSO != null);

        if (slot.itemSO != null)
            weaponHolder.sprite = slot.itemSO.icon;
    }

    void RefreshItemHolder(int _)
    {
        if (itemHolder == null || inventoryController == null) return;

        InventorySlot slot = inventoryController.itemSlots[inventoryController.itemSelectedIndex];

        itemHolder.gameObject.SetActive(slot.itemSO != null);

        if (slot.itemSO != null)
            itemHolder.sprite = slot.itemSO.icon;
    }

    public void OnPageActivated()
    {
        pageActive = true;
        RefreshWeaponHolder(0);
        RefreshItemHolder(0);
    }

    public void OnPageDeactivated()
    {
        pageActive = false;
    }
}
*/