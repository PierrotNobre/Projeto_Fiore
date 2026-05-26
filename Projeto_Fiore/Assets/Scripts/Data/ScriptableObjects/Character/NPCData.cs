using UnityEngine;

[CreateAssetMenu(
    fileName = "NPC",
    menuName = "Fiore/NPC"
)]
public class NPCData : BaseData
{
    [Header("Identity")]
    public RaceData Race;

    public FactionData Faction;

    public NPCRole Role;

    [Header("Location")]
    public CityData HomeCity;

    [Header("Dialogue")]
    public DialogueData DefaultDialogue;

    [Header("Availability")]
    public bool AlwaysAvailable = true;

    [Range(0, 23)]
    public int AvailableFromHour = 8;

    [Range(0, 23)]
    public int AvailableUntilHour = 20;

    [Header("Service")]
    public CityServiceType RelatedService = CityServiceType.None;
}