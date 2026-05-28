using UnityEngine;
using System.Collections.Generic;

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
        return EquipItem(
            itemID,
            null
        );
    }

    public bool EquipItem(
        string itemID,
        EquipmentSlot? preferredSlot)
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

        InventoryManager inventoryManager =
            InventoryManager.Instance;

        if (inventoryManager == null ||
            !inventoryManager.HasItem(itemID))
        {
            GameFeedbackUI.ShowNotification(
                "Item indisponivel no inventario."
            );

            return false;
        }

        EquipmentSlot targetSlot =
            preferredSlot.HasValue
                ? preferredSlot.Value
                : ResolveTargetSlot(itemData);

        if (!CanEquipItemInSlot(
            itemData,
            targetSlot,
            out string failureMessage))
        {
            GameFeedbackUI.ShowNotification(
                failureMessage
            );

            return false;
        }

        EquipmentState equipment =
            SaveManager
                .Instance
                .CurrentSave
                .Equipment;

        if (!inventoryManager.RemoveItem(
            itemID,
            1,
            syncEquipment: false,
            saveAfterChange: false))
        {
            GameFeedbackUI.ShowNotification(
                "Item indisponivel no inventario."
            );

            return false;
        }

        ReturnEquippedItemToInventory(
            equipment.GetItemID(targetSlot)
        );

        if (targetSlot == EquipmentSlot.MainHand &&
            itemData.IsTwoHanded)
        {
            ReturnEquippedItemToInventory(
                equipment.GetItemID(
                    EquipmentSlot.OffHand
                )
            );

            equipment.SetItemID(
                EquipmentSlot.OffHand,
                null
            );
        }

        equipment.SetItemID(
            targetSlot,
            itemID
        );

        CharacterManager
            .Instance
            ?.ClampVitalsToCurrentMaximum();

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

        InventoryManager
            .Instance
            ?.AddItem(
                equippedItemID,
                1,
                reportQuestProgress: false
            );

        CharacterManager
            .Instance
            ?.ClampVitalsToCurrentMaximum();

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
        bool changed =
            false;

        foreach (EquipmentSlot slot
            in GetEquipmentSlots())
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

                changed =
                    true;

                InventoryManager
                    .Instance
                    ?.AddItem(
                        itemID,
                        1,
                        reportQuestProgress: false
                    );
            }
        }

        if (!changed)
            return;

        CharacterManager
            .Instance
            ?.ClampVitalsToCurrentMaximum();

        SaveManager.Instance.SaveGame();
    }

    public bool IsItemEquipped(
        string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
            return false;

        foreach (EquipmentSlot slot
            in GetEquipmentSlots())
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
            in GetEquipmentSlots())
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

    public int GetTotalElementPowerBonus(
        ElementType elementType)
    {
        int total = 0;

        foreach (EquipmentSlot slot
            in GetEquipmentSlots())
        {
            ItemData itemData =
                GetEquippedItemData(slot);

            if (itemData == null)
                continue;

            total += itemData
                .GetElementPowerBonus(elementType);
        }

        return total;
    }

    public int GetTotalElementResistanceBonus(
        ElementType elementType)
    {
        int total = 0;

        foreach (EquipmentSlot slot
            in GetEquipmentSlots())
        {
            ItemData itemData =
                GetEquippedItemData(slot);

            if (itemData == null)
                continue;

            total += itemData
                .GetElementResistanceBonus(elementType);
        }

        return total;
    }

    public bool IsOffHandBlocked()
    {
        ItemData mainHand =
            GetEquippedItemData(
                EquipmentSlot.MainHand
            );

        return mainHand != null &&
            mainHand.IsTwoHanded;
    }

    public bool IsDualWielding()
    {
        ItemData mainHand =
            GetEquippedItemData(
                EquipmentSlot.MainHand
            );

        ItemData offHand =
            GetEquippedItemData(
                EquipmentSlot.OffHand
            );

        return IsOneHandedWeapon(mainHand) &&
            IsOneHandedWeapon(offHand);
    }

    public bool HasTwoHandedWeaponEquipped()
    {
        return IsOffHandBlocked();
    }

    public string GetMainHandWeaponID()
    {
        return GetEquippedItem(
            EquipmentSlot.MainHand
        );
    }

    public string GetOffHandWeaponID()
    {
        return GetEquippedItem(
            EquipmentSlot.OffHand
        );
    }

    public bool CanUseOffHandAttack()
    {
        ItemData offHand =
            GetEquippedItemData(
                EquipmentSlot.OffHand
            );

        return IsOneHandedWeapon(offHand);
    }

    public ItemData GetEquippedItemData(
        EquipmentSlot slot)
    {
        string itemID =
            GetEquippedItem(slot);

        return !string.IsNullOrEmpty(itemID) &&
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetItemById(itemID)
                : null;
    }

    public static EquipmentSlot[] GetEquipmentSlots()
    {
        return new[]
        {
            EquipmentSlot.MainHand,
            EquipmentSlot.OffHand,
            EquipmentSlot.Head,
            EquipmentSlot.Chest,
            EquipmentSlot.Legs,
            EquipmentSlot.Accessory
        };
    }

    private EquipmentSlot ResolveTargetSlot(
        ItemData itemData)
    {
        if (itemData.EquipmentSlot == EquipmentSlot.OffHand)
            return EquipmentSlot.OffHand;

        if (itemData.EquipmentSlot == EquipmentSlot.MainHand)
            return EquipmentSlot.MainHand;

        return itemData.EquipmentSlot;
    }

    private static bool IsOneHandedWeapon(
        ItemData itemData)
    {
        if (itemData == null ||
            !itemData.IsEquipment ||
            itemData.IsTwoHanded)
        {
            return false;
        }

        return itemData.CanEquipInMainHand &&
            itemData.CanEquipInOffHand;
    }

    private bool CanEquipItemInSlot(
        ItemData itemData,
        EquipmentSlot targetSlot,
        out string failureMessage)
    {
        failureMessage =
            "Este item nao pode ser equipado neste slot.";

        if (!itemData.CanEquipInSlot(targetSlot))
            return false;

        if (targetSlot == EquipmentSlot.OffHand &&
            IsOffHandBlocked())
        {
            failureMessage =
                "A mao secundaria esta bloqueada por um item de duas maos.";

            return false;
        }

        if (itemData.IsTwoHanded &&
            targetSlot != EquipmentSlot.MainHand)
        {
            failureMessage =
                "Itens de duas maos precisam ocupar a mao principal.";

            return false;
        }

        return true;
    }

    private void ReturnEquippedItemToInventory(
        string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
            return;

        InventoryManager
            .Instance
            ?.AddItem(
                itemID,
                1,
                reportQuestProgress: false
            );
    }

    [ContextMenu("Debug/Equip Simple Sword")]
    public void DebugEquipSimpleSword()
    {
        EquipItem("simple_sword");
    }
}
