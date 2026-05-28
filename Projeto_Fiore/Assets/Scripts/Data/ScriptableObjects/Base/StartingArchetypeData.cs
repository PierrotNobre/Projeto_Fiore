using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StartingArchetype",
    menuName = "Fiore/Starting Archetype"
)]
public class StartingArchetypeData
    : BaseData
{
    [Header("Stats")]
    public int StrengthBonus;

    public int DexterityBonus;

    public int IntelligenceBonus;

    public int FaithBonus;

    public int VitalityBonus;

    public int CharismaBonus;

    [Header("Elements")]
    public ElementType SuggestedElement =
        ElementType.None;

    [Header("Starting Loadout")]
    public List<StartingEquipmentEntry> StartingEquipment =
        new();

    public List<RewardItemData> StartingItems =
        new();

    public List<string> StartingSkillIDs =
        new();

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
