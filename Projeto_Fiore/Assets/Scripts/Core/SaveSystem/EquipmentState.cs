using System;

[Serializable]
public class EquipmentState
{
    public string WeaponItemID;

    public string HeadItemID;

    public string ChestItemID;

    public string LegsItemID;

    public string AccessoryItemID;

    public string GetItemID(
        EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => WeaponItemID,

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
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                WeaponItemID = itemID;
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
}
