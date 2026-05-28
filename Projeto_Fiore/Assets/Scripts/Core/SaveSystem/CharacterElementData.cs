using System;
using System.Collections.Generic;

[Serializable]
public class CharacterElementData
{
    public ElementType PrimaryElement =
        ElementType.None;

    public List<ElementModifier> PowerBonuses =
        new();

    public List<ElementModifier> Resistances =
        new();

    public void EnsureRuntimeDefaults()
    {
        if (PowerBonuses == null)
            PowerBonuses = new List<ElementModifier>();

        if (Resistances == null)
            Resistances = new List<ElementModifier>();
    }

    public int GetPowerBonus(
        ElementType elementType)
    {
        return GetValue(
            PowerBonuses,
            elementType
        );
    }

    public int GetResistance(
        ElementType elementType)
    {
        return GetValue(
            Resistances,
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
