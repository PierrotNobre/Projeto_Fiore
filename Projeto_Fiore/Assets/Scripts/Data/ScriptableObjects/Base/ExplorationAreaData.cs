using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ExplorationArea",
    menuName = "Fiore/Exploration Area"
)]
public class ExplorationAreaData : BaseData
{
    [Header("Location")]
    public CityData AssociatedCity;

    public string KingdomName;

    public Sprite AreaSprite;

    public bool IsUnlockedByDefault = true;

    public int RecommendedLevel = 1;

    public int TravelTimeCostInPeriods = 1;

    public int MaxStepsBeforeReturn = 6;

    public List<RequirementData> UnlockRequirements =
        new();

    [Header("Exploration")]
    public List<ExplorationNodeData> Nodes =
        new();

    public List<ExplorationEventData> PossibleEvents =
        new();

    public List<ResourceNodeData> Resources =
        new();
}
