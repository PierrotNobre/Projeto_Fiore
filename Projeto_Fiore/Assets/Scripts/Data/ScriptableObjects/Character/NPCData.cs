using UnityEngine;
using System.Collections.Generic;

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

    public Sprite Portrait;

    public Sprite FullBodySprite;

    public Sprite IconSprite;

    [Header("Location")]
    public CityData HomeCity;

    public string DefaultLocationID;

    public bool AppearsInCityOverview = true;

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

    public List<NPCServiceType> AvailableServices =
        new();

    public ShopData ShopData;

    public bool IsImportantNPC;

    public List<RequirementData> AppearanceRequirements =
        new();

    [Header("Schedule")]
    public List<NPCScheduleEntry> ScheduleEntries =
        new();

    [Header("Relationship")]
    public bool CanGainFriendship = true;

    public bool CanRomance;

    public bool CanMarry;

    public NPCRelationshipLevel MaxFriendshipLevel =
        NPCRelationshipLevel.Trusted;

    public NPCRomanceLevel MaxRomanceLevel =
        NPCRomanceLevel.None;

    [Header("Gifts")]
    public bool CanReceiveGifts = true;

    public List<NPCGiftPreference> ItemGiftPreferences =
        new();

    public List<GiftCategoryPreference> CategoryGiftPreferences =
        new();

    public bool HasService(
        NPCServiceType service)
    {
        if (AvailableServices != null &&
            AvailableServices.Contains(service))
        {
            return true;
        }

        return service switch
        {
            NPCServiceType.Dialogue =>
                DefaultDialogue != null,

            NPCServiceType.Shop =>
                RelatedService == CityServiceType.Shop ||
                RelatedService == CityServiceType.Market ||
                ShopData != null,

            NPCServiceType.QuestBoard =>
                RelatedService == CityServiceType.QuestBoard,

            NPCServiceType.Guild =>
                RelatedService == CityServiceType.Guild,

            NPCServiceType.Inn =>
                RelatedService == CityServiceType.Inn ||
                RelatedService == CityServiceType.Tavern,

            NPCServiceType.Travel =>
                RelatedService == CityServiceType.Travel,

            _ => false
        };
    }
}
