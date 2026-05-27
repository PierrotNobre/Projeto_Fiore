using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "Item",
    menuName = "Fiore/Item"
)]
public class ItemData
    : BaseData
{
    [Header("Item")]
    public ItemType Type;

    public int MaxStack = 99;

    public int BuyPrice = 10;

    public int SellValue = 10;

    public bool CanSell = true;

    [Header("Gifts")]
    public bool CanGift = true;

    public GiftCategory GiftCategory =
        GiftCategory.Misc;

    [Header("Equipment")]
    public EquipmentSlot EquipmentSlot =
        EquipmentSlot.Weapon;

    public List<StatModifier> StatModifiers =
        new();

    [TextArea]
    public string FlavorText;

    public bool IsConsumable =>
        Type == ItemType.Consumable;

    public bool IsEquipment =>
        Type == ItemType.Equipment;

    public int GetStatBonus(
        StatType statType)
    {
        if (StatModifiers == null)
            return 0;

        int total = 0;

        foreach (StatModifier modifier
            in StatModifiers)
        {
            if (modifier != null &&
                modifier.StatType == statType)
            {
                total += modifier.Value;
            }
        }

        return total;
    }
}
