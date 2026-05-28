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

    [Header("Consumable")]
    public int EffectValue = 10;

    [Header("Equipment")]
    public EquipmentSlot EquipmentSlot =
        EquipmentSlot.MainHand;

    public bool IsTwoHanded;

    public bool CanEquipInMainHand = true;

    public bool CanEquipInOffHand;

    public ElementType AttackElement =
        ElementType.None;

    public List<StatModifier> StatModifiers =
        new();

    public List<ElementModifier> ElementalPowerModifiers =
        new();

    public List<ElementModifier> ElementalResistanceModifiers =
        new();

    public List<RequirementData> EquipRequirements =
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

    public int GetElementPowerBonus(
        ElementType elementType)
    {
        return GetElementModifierTotal(
            ElementalPowerModifiers,
            elementType
        );
    }

    public int GetElementResistanceBonus(
        ElementType elementType)
    {
        return GetElementModifierTotal(
            ElementalResistanceModifiers,
            elementType
        );
    }

    public bool CanEquipInSlot(
        EquipmentSlot slot)
    {
        if (!IsEquipment)
            return false;

        if (slot == EquipmentSlot.MainHand)
        {
            return CanEquipInMainHand ||
                EquipmentSlot == EquipmentSlot.MainHand;
        }

        if (slot == EquipmentSlot.OffHand)
        {
            return CanEquipInOffHand ||
                EquipmentSlot == EquipmentSlot.OffHand;
        }

        return EquipmentSlot == slot;
    }

    private static int GetElementModifierTotal(
        List<ElementModifier> modifiers,
        ElementType elementType)
    {
        if (modifiers == null)
            return 0;

        int total = 0;

        foreach (ElementModifier modifier
            in modifiers)
        {
            if (modifier != null &&
                modifier.ElementType == elementType)
            {
                total += modifier.Value;
            }
        }

        return total;
    }
}
