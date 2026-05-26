using UnityEngine;

[CreateAssetMenu(
    fileName = "Race",
    menuName = "Fiore/Race"
)]
public class RaceData
    : BaseData
{
    [Header("Stats")]
    public int StrengthBonus;

    public int DexterityBonus;

    public int IntelligenceBonus;

    public int FaithBonus;

    public int VitalityBonus;

    public int CharismaBonus;

    public int AverageLifespan;
}