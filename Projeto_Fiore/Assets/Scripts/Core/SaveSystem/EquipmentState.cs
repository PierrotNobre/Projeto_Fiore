using System;

[Serializable]
public class EquipmentState
{
    public string MainHandItemID;

    public string OffHandItemID;

    public string WeaponItemID;

    public string HeadItemID;

    public string ChestItemID;

    public string LegsItemID;

    public string AccessoryItemID;

    public string GetItemID(
        EquipmentSlot slot)
    {
        EnsureRuntimeDefaults();

        return slot switch
        {
            EquipmentSlot.MainHand => MainHandItemID,

            EquipmentSlot.OffHand => OffHandItemID,

            EquipmentSlot.Head => HeadItemID,

            EquipmentSlot.Chest => ChestItemID,

            EquipmentSlot.Legs => LegsItemID,

            EquipmentSlot.Accessory => AccessoryItemID,

            _ => null
        };
    }

    public void SetItemID(
        EquipmentSlot slot,
        string itemID)
    {
        EnsureRuntimeDefaults();

        switch (slot)
        {
            case EquipmentSlot.MainHand:
                MainHandItemID = itemID;
                WeaponItemID = itemID;
                break;

            case EquipmentSlot.OffHand:
                OffHandItemID = itemID;
                break;

            case EquipmentSlot.Head:
                HeadItemID = itemID;
                break;

            case EquipmentSlot.Chest:
                ChestItemID = itemID;
                break;

            case EquipmentSlot.Legs:
                LegsItemID = itemID;
                break;

            case EquipmentSlot.Accessory:
                AccessoryItemID = itemID;
                break;
        }
    }

    public void EnsureRuntimeDefaults()
    {
        if (string.IsNullOrEmpty(MainHandItemID) &&
            !string.IsNullOrEmpty(WeaponItemID))
        {
            MainHandItemID =
                WeaponItemID;
        }

        WeaponItemID =
            MainHandItemID;
    }
}
