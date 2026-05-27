using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ExplorationEvent",
    menuName = "Fiore/Exploration Event"
)]
public class ExplorationEventData : BaseData
{
    public bool IsUnique;

    public int Weight = 1;

    public EnemyData Enemy;

    public List<RequirementData> Requirements =
        new();

    public List<ExplorationEventChoiceData> Choices =
        new();

    public RewardData Reward = new();

    public List<DialogueActionData> Actions =
        new();
}
