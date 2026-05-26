using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "TravelEvent",
    menuName = "Fiore/Travel Event"
)]
public class TravelEventData
    : BaseData
{
    [Header("Event Text")]
    [TextArea]
    public string EventText;

    [Header("Conditions")]
    [Range(0, 100)]
    public int TriggerChance = 30;

    public RegionData AllowedRegion;

    [Header("Choices")]
    public List<EventChoice> Choices;

    [Header("Requirements")]
    public List<EventRequirement> Requirements;

    [Header("Rewards")] 
    public string SetFlag;
}