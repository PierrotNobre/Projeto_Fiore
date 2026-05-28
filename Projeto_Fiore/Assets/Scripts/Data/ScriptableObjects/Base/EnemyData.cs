using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Enemy",
    menuName = "Fiore/Enemy"
)]
public class EnemyData : BaseData
{
    public Sprite Sprite;

    public ElementType PrimaryElement =
        ElementType.None;

    public int MaxHealth = 10;

    public int Attack = 1;

    public int Defense = 0;

    public int Speed = 4;

    public RewardData Reward = new();

    public List<RewardItemData> Drops =
        new();

    public List<ElementModifier> Resistances =
        new();

    public List<ElementModifier> Weaknesses =
        new();

    public bool CanAppearInExploration = true;

    public int GetResistance(
        ElementType elementType)
    {
        return GetModifierValue(
            Resistances,
            elementType
        );
    }

    public int GetWeakness(
        ElementType elementType)
    {
        return GetModifierValue(
            Weaknesses,
            elementType
        );
    }

    private static int GetModifierValue(
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
