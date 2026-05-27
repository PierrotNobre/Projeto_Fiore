using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "WorldEvent",
    menuName = "Fiore/World Event"
)]
public class WorldEventData
    : BaseData
{
    [Header("History")]
    public bool IsUnique = true;

    [Header("Trigger")]
    public EventTriggerType TriggerType =
        EventTriggerType.Manual;

    public CityData City;

    public CityData OriginCity;

    public CityData DestinationCity;

    [Header("Future Conditions")]
    public bool RequiresSeason;

    public FioreSeason RequiredSeason;

    public string RequiredWorldFlag;

    public bool RequiredWorldFlagValue = true;

    public string RequiredQuestID;

    public QuestStatus RequiredQuestStatus =
        QuestStatus.Completed;

    public List<RequirementData> GenericRequirements =
        new();

    [Header("Actions")]
    public List<WorldEventAction> Actions = new();

    [Header("Reward")]
    public RewardData Rewards = new();
}
