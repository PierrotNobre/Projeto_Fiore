using System;

[Serializable]
public class RequirementData
{
    public RequirementType Type;

    public StatType StatType;

    public int RequiredValue;

    public string RequiredQuestID;

    public QuestStatus RequiredQuestStatus =
        QuestStatus.Completed;

    public string RequiredEventID;

    public int RequiredGuildLevel = 1;

    public int RequiredCoins;

    public string RequiredItemID;

    public int RequiredItemQuantity = 1;

    public string RequiredNPCID;

    public NPCRelationshipLevel RequiredFriendshipLevel =
        NPCRelationshipLevel.Acquaintance;

    public int RequiredFriendshipPoints;

    public NPCRomanceLevel RequiredRomanceLevel =
        NPCRomanceLevel.Interest;

    public int RequiredRomancePoints;

    public bool RequiredBool = true;

    public int RequiredTimesTalked;

    public TimeOfDay RequiredTimeOfDay;

    public FioreSeason RequiredSeason;

    public string RequiredCityID;

    public string RequiredExplorationAreaID;
}
