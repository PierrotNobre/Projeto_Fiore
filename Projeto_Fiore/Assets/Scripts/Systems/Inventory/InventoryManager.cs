using UnityEngine;
using System.Linq;

public class InventoryManager
    : PersistentSingleton<
        InventoryManager>
{
    public void AddItem(
        string itemID,
        int quantity = 1)
    {
        if (string.IsNullOrEmpty(itemID) ||
            quantity <= 0)
        {
            return;
        }

        var inventory =
            SaveManager.Instance
            .CurrentSave
            .Inventory;

        var existing =
            inventory.FirstOrDefault(
                x => x.ItemID
                == itemID
            );

        if (existing != null)
        {
            existing.Quantity +=
                quantity;
        }
        else
        {
            inventory.Add(
                new InventoryItem
                {
                    ItemID =
                        itemID,

                    Quantity =
                        quantity
                });
        }

        SaveManager.Instance
            .SaveGame();

        Debug.Log(
            $"Item added: {itemID} x{quantity}"
        );

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.CollectItem,
                    itemID,
                    quantity,
                    "Inventory"
                )
            );
    }

    public bool RemoveItem(
        string itemID,
        int quantity = 1)
    {
        var inventory =
            SaveManager.Instance
            .CurrentSave
            .Inventory;

        var existing =
            inventory.FirstOrDefault(
                x => x.ItemID
                == itemID
            );

        if (existing == null)
            return false;

        if (existing.Quantity
            < quantity)
        {
            return false;
        }

        existing.Quantity -=
            quantity;

        if (existing.Quantity <= 0)
        {
            inventory.Remove(
                existing
            );

            EquipmentManager
                .GetOrCreate()
                .UnequipItem(itemID);
        }

        SaveManager.Instance
            .SaveGame();

        return true;
    }

    public bool HasItem(
        string itemID,
        int quantity = 1)
    {
        var inventory =
            SaveManager.Instance
            .CurrentSave
            .Inventory;

        var existing =
            inventory.FirstOrDefault(
                x => x.ItemID
                == itemID
            );

        return existing != null
            &&
            existing.Quantity
            >= quantity;
    }

    public int GetItemAmount(
        string itemID)
    {
        var inventory =
            SaveManager.Instance
            .CurrentSave
            .Inventory;

        var existing =
            inventory.FirstOrDefault(
                x => x.ItemID
                == itemID
            );

        return existing?.Quantity ?? 0;
    }

    public int GetItemQuantity(
        string itemID)
    {
        return GetItemAmount(itemID);
    }

    public bool UseItem(
        string itemID)
    {
        ItemData itemData =
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetData<ItemData>(itemID)
                : null;

        if (itemData == null)
        {
            Debug.LogWarning(
                $"Item not found: {itemID}"
            );

            GameFeedbackUI.ShowNotification(
                "Item nao encontrado."
            );

            return false;
        }

        if (!itemData.IsConsumable)
        {
            GameFeedbackUI.ShowNotification(
                "Este item nao pode ser usado."
            );

            return false;
        }

        if (!RemoveItem(itemID))
        {
            GameFeedbackUI.ShowNotification(
                "Item indisponivel."
            );

            return false;
        }

        string itemName =
            !string.IsNullOrEmpty(itemData.DisplayName)
                ? itemData.DisplayName
                : itemID;

        Debug.Log(
            $"Item used: {itemID}"
        );

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.UseItem,
                    itemID,
                    1,
                    "Inventory"
                )
            );

        GameFeedbackUI.ShowNotification(
            $"Item usado: {itemName}"
        );

        return true;
    }

    public bool SellItem(
        string itemID,
        int quantity = 1)
    {
        if (quantity <= 0)
            return false;

        ItemData itemData =
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetItemById(itemID)
                : null;

        if (itemData == null)
        {
            GameFeedbackUI.ShowNotification(
                "Item nao encontrado."
            );

            return false;
        }

        if (itemData.Type == ItemType.Quest ||
            !itemData.CanSell)
        {
            GameFeedbackUI.ShowNotification(
                "Este item nao pode ser vendido."
            );

            return false;
        }

        if (EquipmentManager
            .GetOrCreate()
            .IsItemEquipped(itemID))
        {
            GameFeedbackUI.ShowNotification(
                "Desequipe o item antes de vender."
            );

            return false;
        }

        if (!HasItem(itemID, quantity))
        {
            GameFeedbackUI.ShowNotification(
                "Quantidade indisponivel."
            );

            return false;
        }

        int value =
            Mathf.Max(0, itemData.SellValue)
            * quantity;

        if (!RemoveItem(itemID, quantity))
            return false;

        WalletManager
            .GetOrCreate()
            .AddCoins(value);

        GameFeedbackUI.ShowNotification(
            $"Vendeu {itemData.DisplayName} por {value} moedas."
        );

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.SellItem,
                    itemID,
                    quantity,
                    "Shop"
                )
            );

        return true;
    }

    [ContextMenu("Debug/Clear Inventory")]
    public void ClearInventory()
    {
        SaveManager.Instance
            .CurrentSave
            .Inventory
            .Clear();

        SaveManager.Instance.SaveGame();

        Debug.Log(
            "Inventory cleared."
        );
    }

    [ContextMenu("Debug/Add Potion Simple")]
    public void DebugAddPotionSimple()
    {
        AddItem(
            "potion_simple",
            1
        );
    }

    [ContextMenu("Debug/Print Inventory")]
    public void DebugPrintInventory()
    {
        var inventory =
            SaveManager.Instance
            .CurrentSave
            .Inventory;

        Debug.Log(
            $"Inventory stacks: {inventory.Count}"
        );

        foreach (var item in inventory)
        {
            Debug.Log(
                $"{item.ItemID} x{item.Quantity}"
            );
        }
    }
}
