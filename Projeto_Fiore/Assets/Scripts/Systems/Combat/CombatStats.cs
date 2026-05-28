using System;
using System.Collections.Generic;

[Serializable]
public class CombatStats
{
    public int MaxHealth;

    public int CurrentHealth;

    public int MaxEnergy;

    public int CurrentEnergy;

    public int PhysicalAttack;

    public int MagicalAttack;

    public int Defense;

    public int Speed;

    public int Accuracy = 100;

    public int Evasion;

    public int CriticalChance;

    public ElementType PrimaryElement =
        ElementType.None;

    public List<ElementModifier> Resistances =
        new();

    public List<ElementModifier> Weaknesses =
        new();

    public List<ElementModifier> PowerBonuses =
        new();

    public int GetResistance(
        ElementType elementType)
    {
        return GetValue(
            Resistances,
            elementType
        );
    }

    public int GetWeakness(
        ElementType elementType)
    {
        return GetValue(
            Weaknesses,
            elementType
        );
    }

    public int GetPowerBonus(
        ElementType elementType)
    {
        return GetValue(
            PowerBonuses,
            elementType
        );
    }

    private static int GetValue(
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
