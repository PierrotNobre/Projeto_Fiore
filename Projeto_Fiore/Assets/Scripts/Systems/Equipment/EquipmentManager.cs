using UnityEngine;

public class EquipmentManager
    : PersistentSingleton<EquipmentManager>
{
    public static EquipmentManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject equipmentObject =
            new GameObject(
                "EquipmentManager"
            );

        return equipmentObject
            .AddComponent<EquipmentManager>();
    }

    public bool EquipItem(
        string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
            return false;

        ItemData itemData =
            DatabaseManager
                .Instance
                .GetItemById(itemID);

        if (itemData == null ||
            !itemData.IsEquipment)
        {
            GameFeedbackUI.ShowNotification(
                "Este item nao pode ser equipado."
            );

            return false;
        }

        if (!InventoryManager
            .Instance
            .HasItem(itemID))
        {
            GameFeedbackUI.ShowNotification(
                "Item indisponivel no inventario."
            );

            return false;
        }

        SaveManager
            .Instance
            .CurrentSave
            .Equipment
            .SetItemID(
                itemData.EquipmentSlot,
                itemID
            );

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"Equipado: {itemData.DisplayName}"
        );

        Debug.Log(
            $"Item equipped: {itemID}"
        );

        return true;
    }

    public bool UnequipSlot(
        EquipmentSlot slot)
    {
        string equippedItemID =
            GetEquippedItem(slot);

        if (string.IsNullOrEmpty(equippedItemID))
            return false;

        SaveManager
            .Instance
            .CurrentSave
            .Equipment
            .SetItemID(
                slot,
                null
            );

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            "Equipamento removido."
        );

        Debug.Log(
            $"Equipment slot unequipped: {slot}"
        );

        return true;
    }

    public void UnequipItem(
        string itemID)
    {
        foreach (EquipmentSlot slot
            in System.Enum.GetValues(
                typeof(EquipmentSlot)))
        {
            if (GetEquippedItem(slot) == itemID)
            {
                SaveManager
                    .Instance
                    .CurrentSave
                    .Equipment
                    .SetItemID(
                        slot,
                        null
                    );
            }
        }
    }

    public bool IsItemEquipped(
        string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
            return false;

        foreach (EquipmentSlot slot
            in System.Enum.GetValues(
                typeof(EquipmentSlot)))
        {
            if (GetEquippedItem(slot) == itemID)
                return true;
        }

        return false;
    }

    public string GetEquippedItem(
        EquipmentSlot slot)
    {
        return SaveManager
            .Instance
            .CurrentSave
            .Equipment
            .GetItemID(slot);
    }

    public int GetTotalStatBonus(
        StatType statType)
    {
        int total = 0;

        foreach (EquipmentSlot slot
            in System.Enum.GetValues(
                typeof(EquipmentSlot)))
        {
            string itemID =
                GetEquippedItem(slot);

            if (string.IsNullOrEmpty(itemID))
                continue;

            ItemData itemData =
                DatabaseManager
                    .Instance
                    .GetItemById(itemID);

            if (itemData == null)
                continue;

            total += itemData.GetStatBonus(statType);
        }

        return total;
    }

    [ContextMenu("Debug/Equip Simple Sword")]
    public void DebugEquipSimpleSword()
    {
        EquipItem("simple_sword");
    }
}
