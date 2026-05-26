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
            $"Added {quantity}x " +
            $"{itemID}"
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
}