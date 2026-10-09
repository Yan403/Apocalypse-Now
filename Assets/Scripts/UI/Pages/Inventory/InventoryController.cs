using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InventoryController : MonoBehaviour
{
    public InventorySlot[] itemSlots;
    public InventorySlot[] gunSlots;

    public int itemSelectedIndex;
    public int gunSelectedIndex;

    public PlayerController player;
    public PlayerPageController playerPage; // arraste no Inspector

    public bool navigatingItems = false;

    public System.Action OnSelectionChanged;
    void Awake()
    {
        // Garante índices válidos mesmo se os arrays estiverem vazios
        if (itemSlots == null) itemSlots = new InventorySlot[0];
        if (gunSlots == null) gunSlots = new InventorySlot[0];
        // 🔒 Garante que os índices nunca fiquem fora dos limites,
        // mesmo que o Inspector tenha valores negativos.
        itemSelectedIndex = (itemSlots.Length > 0) ? Mathf.Clamp(itemSelectedIndex, 0, itemSlots.Length - 1) : 0;
        gunSelectedIndex = (gunSlots.Length > 0) ? Mathf.Clamp(gunSelectedIndex, 0, gunSlots.Length - 1) : 0;
    }

    void Start()
    {
        UpdateItemSelectionUI();
        UpdateGunSelectionUI();

        foreach (var slot in itemSlots) slot.UpdateUI();
        foreach (var slot in gunSlots) slot.UpdateUI();

        // Aplica o efeito passivo do item inicial APENAS se houver slots
        if (player != null && itemSlots.Length > 0)
        {
            ItemSO startingItem = itemSlots[itemSelectedIndex].itemSO;
            player.SetPassiveItem(startingItem);
        }
    }

    void Update()
    {
        if (!GameInputManager.Instance.uiInputEnabled)
            return;

        if (!UIStateManager.Instance.IsPage(UIStateManager.UIPage.Inventory))
            return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (navigatingItems)
                MoveItemSelection(1);
            else
                MoveGunSelection(1, allowEmpty: true);
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (navigatingItems)
                MoveItemSelection(-1);
            else
                MoveGunSelection(-1, allowEmpty: true);
        }

        // Uso de item com Espaço (apenas na aba de itens)
        if (navigatingItems && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            UseSelectedItem();
        }
    }
    private void OnEnable()
    {
        Loot.OnItemLooted += AddItem;
    }
    private void OnDisable()
    {
        Loot.OnItemLooted -= AddItem;
    }
    public bool AddItem(ItemSO itemSO, int quantity)
    {
        // Munição tem tratamento especial
        if (itemSO is AmmoSO ammo)
        {
            if (player != null)
                return player.AddAmmoForWeapon(ammo.weaponType, ammo.ammoAmount);   // retorna true se adicionou
            return false;
        }

        // Items normais (armas, usáveis, etc.) sempre coletados
        // ... (código existente para armas e itens normais) ...
        // Ao final de cada adição bem-sucedida, coloque return true; (armas e itens)
        // Se não encontrou slot, return false (opcional, mas pode manter false para indicar que não foi coletado)

        // Exemplo para armas:
        if (itemSO.isGun)
        {
            foreach (var slot in gunSlots)
            {
                if (slot.itemSO == null)
                {
                    slot.itemSO = itemSO;
                    slot.quantity = quantity;
                    slot.UpdateUI();
                    return true;
                }
                else if (slot.itemSO == itemSO)
                    return false;   // já possui a arma, não coleta duplicata
            }
            return false;   // sem slots livres
        }
        else
        {
            foreach (var slot in itemSlots)
            {
                if (slot.itemSO == null)
                {
                    slot.itemSO = itemSO;
                    slot.quantity = quantity;
                    slot.UpdateUI();
                    // ... lógica de auto-seleção ...
                    return true;
                }
                else if (slot.itemSO == itemSO)
                {
                    slot.quantity += quantity;
                    slot.UpdateUI();
                    return true;
                }
            }
            return false;   // sem slots livres
        }
    }
    public bool HasGun(GunSO gun)
    {
        if (gunSlots == null) return false;
        foreach (var slot in gunSlots)
        {
            if (slot.itemSO == gun)
                return true;
        }
        return false;
    }
    void MoveItemSelection(int direction)
    {
        int startIndex = itemSelectedIndex;
        if (startIndex < 0) startIndex = 0;

        int newIndex = startIndex;
        int attempts = 0;

        do
        {
            newIndex += direction;
            if (newIndex >= itemSlots.Length) newIndex = 0;
            if (newIndex < 0) newIndex = itemSlots.Length - 1;

            attempts++;
            if (attempts > itemSlots.Length) break;
        }
        while (itemSlots[newIndex].itemSO == null && newIndex != startIndex);

        if (itemSlots[newIndex].itemSO != null)
        {
            itemSelectedIndex = newIndex;
            SelectItem(itemSelectedIndex);
        }
    }

    void MoveGunSelection(int direction, bool allowEmpty = false)
    {
        int startIndex = gunSelectedIndex;
        if (startIndex < 0) startIndex = 0;

        int newIndex = startIndex;
        int attempts = 0;

        do
        {
            newIndex += direction;
            if (newIndex >= gunSlots.Length) newIndex = 0;
            if (newIndex < 0) newIndex = gunSlots.Length - 1;

            attempts++;
            if (attempts > gunSlots.Length) break;
        }
        while (!allowEmpty && gunSlots[newIndex].itemSO == null && newIndex != startIndex);

        gunSelectedIndex = newIndex;
        SelectWeapon(gunSelectedIndex);
    }

    public void SelectWeapon(int index)
    {
        if (index < 0 || index >= gunSlots.Length) return;

        GunSO gun = gunSlots[index].itemSO as GunSO;
        if (player != null)
        {
            player.equippedWeapon = gun; // pode ser null (soco)
            player.OnWeaponChanged(gun);
        }

        UpdateGunSelectionUI();
        OnSelectionChanged?.Invoke();
    }

    public void SelectItem(int index)
    {
        if (index < 0 || index >= itemSlots.Length) return;

        ItemSO item = itemSlots[index].itemSO;

        // Define o campo do PlayerController com segurança
        if (player != null)
        {
            // Se o item for apenas passivo, NÃO o equipa como UsableItemSO
            if (item != null && item.isPassiveOnly)
            {
                player.equippedItem = null;
            }
            else
            {
                // Caso contrário, pode ser um UsableItemSO comum (ou nulo se for ItemSO base)
                player.equippedItem = item as UsableItemSO;
            }
        }

        ApplyPassiveFromSelectedItem();
        UpdateItemSelectionUI();
        OnSelectionChanged?.Invoke();
    }

    /// <summary>
    /// Aplica o efeito passivo do item atualmente selecionado e remove qualquer efeito anterior.
    /// </summary>
    private void ApplyPassiveFromSelectedItem()
    {
        if (player == null) return;

        ItemSO currentItem = itemSlots[itemSelectedIndex].itemSO;
        player.SetPassiveItem(currentItem);
    }

    void UpdateItemSelectionUI()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i].SetSelected(i == itemSelectedIndex);
        }
    }

    void UpdateGunSelectionUI()
    {
        for (int i = 0; i < gunSlots.Length; i++)
        {
            gunSlots[i].SetSelected(i == gunSelectedIndex);
        }
    }

    public void EnterItemInventory()
    {
        navigatingItems = true;
        // Seleciona o item atual, mas só se houver slots
        if (itemSlots.Length > 0)
            SelectItem(itemSelectedIndex);
    }

    public void EnterGunInventory()
    {
        navigatingItems = false;
        if (gunSlots.Length > 0)
            SelectWeapon(gunSelectedIndex);
    }

    public void NextWeapon()
    {
        if (gunSlots.Length == 0) return;
        MoveGunSelection(1, allowEmpty: true);
    }

    public void PreviousWeapon()
    {
        if (gunSlots.Length == 0) return;
        MoveGunSelection(-1, allowEmpty: true);
    }

    public void NextItem()
    {
        if (itemSlots.Length == 0) return;
        MoveItemSelection(1);
    }

    public void PreviousItem()
    {
        if (itemSlots.Length == 0) return;
        MoveItemSelection(-1);
    }

    public void UseSelectedItem()
    {
        if (player == null) return;

        UsableItemSO item = player.equippedItem;
        if (item == null) return;

        // Encontra o slot correspondente ao item equipado
        InventorySlot slot = FindItemSlot(item);
        if (slot == null || slot.quantity <= 0) return;

        // Aplica os efeitos do item
        item.Use(player);

        // Remove uma unidade
        RemoveItem(slot, 1);
    }

    private InventorySlot FindItemSlot(ItemSO itemSO)
    {
        foreach (var slot in itemSlots)
        {
            if (slot.itemSO == itemSO)
                return slot;
        }
        return null;
    }

    public void RemoveItem(InventorySlot slot, int amount)
    {
        if (slot == null || slot.itemSO == null) return;

        slot.quantity -= amount;
        if (slot.quantity <= 0)
        {
            slot.itemSO = null;
            slot.quantity = 0;

            // Se o item removido era o equipado, desequipa
            if (player != null && player.equippedItem == slot.itemSO)
            {
                player.equippedItem = null;
            }

            // Re-seleciona o primeiro item disponível ou limpa seleção
            SelectFirstAvailableItem();
        }

        slot.UpdateUI();
        OnSelectionChanged?.Invoke();
    }
    private void SelectFirstAvailableItem()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i].itemSO != null)
            {
                itemSelectedIndex = i;
                SelectItem(i);
                return;
            }
        }
        itemSelectedIndex = -1;
        if (player != null) player.equippedItem = null;
        UpdateItemSelectionUI();
        OnSelectionChanged?.Invoke();
    }
}

/*
using UnityEngine;
using UnityEngine.InputSystem;
using System;

[DefaultExecutionOrder(100)]
public class InventoryController : MonoBehaviour
{
    public InventorySlot[] itemSlots;
    public InventorySlot[] gunSlots;

    public int itemSelectedIndex;
    public int gunSelectedIndex;

    public PlayerController player;

    public bool navigatingItems = false;

    public event Action<int> OnWeaponSelectionChanged;
    public event Action<int> OnItemSelectionChanged;

    private void Start()
    {
        itemSelectedIndex = 0;
        gunSelectedIndex = 0;

        UpdateItemSelectionUI();
        UpdateGunSelectionUI();

        foreach (var slot in itemSlots)
            slot.UpdateUI();

        foreach (var slot in gunSlots)
            slot.UpdateUI();
    }

    void Update()
    {
        if (!GameInputManager.Instance.uiInputEnabled)
            return;

        if (!UIStateManager.Instance.IsPage(UIStateManager.UIPage.Inventory))
            return;

        if (PlayerPageController.arrowConsumedThisFrame)
            return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (navigatingItems)
                MoveItemSelection(1);
            else
                MoveGunSelection(1);
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (navigatingItems)
                MoveItemSelection(-1);
            else
                MoveGunSelection(-1);
        }
    }

    private void OnEnable()
    {
        Loot.OnItemLooted += AddItem;
    }

    private void OnDisable()
    {
        Loot.OnItemLooted -= AddItem;
    }

    public void AddItem(ItemSO itemSO, int quantity)
    {
        if (itemSO.isGun)
        {
            foreach (var slot in gunSlots)
            {
                if (slot.itemSO == null)
                {
                    slot.itemSO = itemSO;
                    slot.quantity = quantity;
                    slot.UpdateUI();
                    return;
                }
                else if (slot.itemSO == itemSO)
                {
                    return;
                }
            }
        }
        else
        {
            foreach (var slot in itemSlots)
            {
                if (slot.itemSO == null)
                {
                    slot.itemSO = itemSO;
                    slot.quantity = quantity;
                    slot.UpdateUI();
                    return;
                }
                else if (slot.itemSO == itemSO)
                {
                    slot.quantity += quantity;
                    slot.UpdateUI();
                    return;
                }
            }
        }
    }

    public void MoveItemSelection(int direction)
    {
        itemSelectedIndex += direction;
        itemSelectedIndex = Mathf.Clamp(itemSelectedIndex, 0, itemSlots.Length - 1);
        SelectItem(itemSelectedIndex);
    }

    public void MoveGunSelection(int direction)
    {
        gunSelectedIndex += direction;
        gunSelectedIndex = Mathf.Clamp(gunSelectedIndex, 0, gunSlots.Length - 1);
        SelectWeapon(gunSelectedIndex);
    }

    public void SelectWeapon(int index)
    {
        GunSO gun = gunSlots[index].itemSO as GunSO;

        if (player != null)
            player.equippedWeapon = gun;

        UpdateGunSelectionUI();
        OnWeaponSelectionChanged?.Invoke(index);
    }

    public void SelectItem(int index)
    {
        UsableItemSO item = itemSlots[index].itemSO as UsableItemSO;

        if (player != null)
            player.equippedItem = item;

        UpdateItemSelectionUI();
        OnItemSelectionChanged?.Invoke(index);
    }

    void UpdateItemSelectionUI()
    {
        for (int i = 0; i < itemSlots.Length; i++)
            itemSlots[i].SetSelected(i == itemSelectedIndex);
    }

    void UpdateGunSelectionUI()
    {
        for (int i = 0; i < gunSlots.Length; i++)
            gunSlots[i].SetSelected(i == gunSelectedIndex);
    }

    public void EnterItemInventory()
    {
        navigatingItems = true;
        SelectItem(itemSelectedIndex);
    }

    public void EnterGunInventory()
    {
        navigatingItems = false;
        SelectWeapon(gunSelectedIndex);
    }
}
*/