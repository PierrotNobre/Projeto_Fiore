using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "Race",
    menuName = "Fiore/Race"
)]
public class RaceData
    : BaseData
{
    [Header("Availability")]
    public bool IsPlayable = true;

    [Header("Stats")]
    public int StrengthBonus;

    public int DexterityBonus;

    public int IntelligenceBonus;

    public int FaithBonus;

    public int VitalityBonus;

    public int CharismaBonus;

    [Header("Elements")]
    public ElementType PrimaryElement =
        ElementType.None;

    public List<ElementModifier> ElementalPowerModifiers =
        new();

    public List<ElementModifier> ElementalResistanceModifiers =
        new();

    [Header("Starting")]
    public List<string> StartingItemIDs =
        new();

    public int AverageLifespan;

    public int GetStatBonus(
        StatType statType)
    {
        return statType switch
        {
            StatType.Strength => StrengthBonus,

            StatType.Dexterity => DexterityBonus,

            StatType.Intelligence => IntelligenceBonus,

            StatType.Faith => FaithBonus,

            StatType.Vitality => VitalityBonus,

            StatType.Charisma => CharismaBonus,

            _ => 0
        };
    }
}
