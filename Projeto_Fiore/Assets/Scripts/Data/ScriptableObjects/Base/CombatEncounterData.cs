using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CombatEncounter",
    menuName = "Fiore/Combat Encounter"
)]
public class CombatEncounterData
    : BaseData
{
    public List<EnemyEncounterEntry> Enemies =
        new();

    public RewardData VictoryReward =
        new();

    public bool CanFlee = true;

    public List<RequirementData> Requirements =
        new();

    public string VictoryEventID;

    public string DefeatEventID;
}
