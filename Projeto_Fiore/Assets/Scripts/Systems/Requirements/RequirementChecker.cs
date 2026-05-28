using System.Collections.Generic;
using UnityEngine;

public static class RequirementChecker
{
    public static bool AreRequirementsMet(
        List<RequirementData> requirements)
    {
        return AreRequirementsMet(
            requirements,
            null
        );
    }

    public static bool AreRequirementsMet(
        List<RequirementData> requirements,
        string contextNPCID)
    {
        if (requirements == null ||
            requirements.Count == 0)
        {
            return true;
        }

        foreach (RequirementData requirement
            in requirements)
        {
            if (!IsRequirementMet(
                requirement,
                contextNPCID))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsRequirementMet(
        RequirementData requirement)
    {
        return IsRequirementMet(
            requirement,
            null
        );
    }

    public static bool IsRequirementMet(
        RequirementData requirement,
        string contextNPCID)
    {
        if (requirement == null)
            return true;

        switch (requirement.Type)
        {
            case RequirementType.Stat:
                return CheckStat(requirement);

            case RequirementType.QuestStatus:
                return CheckQuest(requirement);

            case RequirementType.EventOccurred:
                return CheckEvent(requirement);

            case RequirementType.GuildLevel:
                return CheckGuildLevel(requirement);

            case RequirementType.HasCoins:
                return CheckCoins(requirement);

            case RequirementType.HasItem:
                return CheckItem(requirement);

            case RequirementType.NPCFriendshipLevel:
                return CheckNPCFriendshipLevel(
                    requirement,
                    contextNPCID
                );

            case RequirementType.NPCFriendshipPoints:
                return CheckNPCFriendshipPoints(
                    requirement,
                    contextNPCID
                );

            case RequirementType.NPCRomanceLevel:
                return CheckNPCRomanceLevel(
                    requirement,
                    contextNPCID
                );

            case RequirementType.NPCRomancePoints:
                return CheckNPCRomancePoints(
                    requirement,
                    contextNPCID
                );

            case RequirementType.NPCIsDating:
                return CheckNPCBoolState(
                    requirement,
                    contextNPCID,
                    checkDating: true
                );

            case RequirementType.NPCIsMarried:
                return CheckNPCBoolState(
                    requirement,
                    contextNPCID,
                    checkDating: false
                );

            case RequirementType.NPCTimesTalked:
                return CheckNPCTimesTalked(
                    requirement,
                    contextNPCID
                );

            case RequirementType.TimeOfDay:
                return CheckTimeOfDay(
                    requirement
                );

            case RequirementType.Season:
                return CheckSeason(
                    requirement
                );

            case RequirementType.CurrentCity:
                return CheckCurrentCity(
                    requirement
                );

            case RequirementType.CurrentExplorationArea:
                return CheckCurrentExplorationArea(
                    requirement
                );

            case RequirementType.Race:
                return CheckRace(requirement);

            case RequirementType.Archetype:
                return CheckArchetype(requirement);

            case RequirementType.Element:
                return CheckElement(requirement);

            case RequirementType.HasElementResistance:
                return CheckElementResistance(requirement);

            case RequirementType.HasEquippedItem:
                return CheckEquippedItem(requirement);

            case RequirementType.HasEquipmentSlotFilled:
                return CheckEquipmentSlotFilled(requirement);
        }

        return true;
    }

    private static bool CheckStat(
        RequirementData requirement)
    {
        int current =
            CharacterManager
                .Instance
                .GetStat(requirement.StatType);

        bool passed =
            current >= requirement.RequiredValue;

        LogFailure(
            passed,
            $"Requirement failed: {requirement.StatType} {current}/{requirement.RequiredValue}"
        );

        return passed;
    }

    private static bool CheckQuest(
        RequirementData requirement)
    {
        QuestStatus status =
            QuestManager
                .Instance
                .GetQuestState(
                    requirement.RequiredQuestID
                );

        bool passed =
            status == requirement.RequiredQuestStatus;

        LogFailure(
            passed,
            $"Requirement failed: quest {requirement.RequiredQuestID} is {status}, expected {requirement.RequiredQuestStatus}"
        );

        return passed;
    }

    private static bool CheckEvent(
        RequirementData requirement)
    {
        bool passed =
            WorldStateManager
                .Instance
                .HasEventOccurred(
                    requirement.RequiredEventID
                );

        LogFailure(
            passed,
            $"Requirement failed: event not occurred {requirement.RequiredEventID}"
        );

        return passed;
    }

    private static bool CheckGuildLevel(
        RequirementData requirement)
    {
        int level =
            GuildManager
                .Instance
                .Guild
                .Level;

        bool passed =
            level >= requirement.RequiredGuildLevel;

        LogFailure(
            passed,
            $"Requirement failed: guild level {level}/{requirement.RequiredGuildLevel}"
        );

        return passed;
    }

    private static bool CheckCoins(
        RequirementData requirement)
    {
        bool passed =
            WalletManager
                .GetOrCreate()
                .CanAfford(
                    requirement.RequiredCoins
                );

        LogFailure(
            passed,
            $"Requirement failed: coins {WalletManager.GetOrCreate().GetCoins()}/{requirement.RequiredCoins}"
        );

        return passed;
    }

    private static bool CheckItem(
        RequirementData requirement)
    {
        bool passed =
            InventoryManager
                .Instance
                .HasItem(
                    requirement.RequiredItemID,
                    requirement.RequiredItemQuantity
                );

        LogFailure(
            passed,
            $"Requirement failed: item {requirement.RequiredItemID} x{requirement.RequiredItemQuantity}"
        );

        return passed;
    }

    private static bool CheckNPCFriendshipLevel(
        RequirementData requirement,
        string contextNPCID)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(requirement, contextNPCID);

        NPCRelationshipLevel current =
            relationship != null
                ? relationship.FriendshipLevel
                : NPCRelationshipLevel.Stranger;

        bool passed =
            current >= requirement.RequiredFriendshipLevel;

        LogFailure(
            passed,
            $"Requirement failed: NPC friendship level {current}/{requirement.RequiredFriendshipLevel}"
        );

        return passed;
    }

    private static bool CheckNPCFriendshipPoints(
        RequirementData requirement,
        string contextNPCID)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(requirement, contextNPCID);

        int current =
            relationship != null
                ? relationship.FriendshipPoints
                : 0;

        bool passed =
            current >= requirement.RequiredFriendshipPoints;

        LogFailure(
            passed,
            $"Requirement failed: NPC friendship points {current}/{requirement.RequiredFriendshipPoints}"
        );

        return passed;
    }

    private static bool CheckNPCRomanceLevel(
        RequirementData requirement,
        string contextNPCID)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(requirement, contextNPCID);

        NPCRomanceLevel current =
            relationship != null
                ? relationship.RomanceLevel
                : NPCRomanceLevel.None;

        bool passed =
            current >= requirement.RequiredRomanceLevel;

        LogFailure(
            passed,
            $"Requirement failed: NPC romance level {current}/{requirement.RequiredRomanceLevel}"
        );

        return passed;
    }

    private static bool CheckNPCRomancePoints(
        RequirementData requirement,
        string contextNPCID)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(requirement, contextNPCID);

        int current =
            relationship != null
                ? relationship.RomancePoints
                : 0;

        bool passed =
            current >= requirement.RequiredRomancePoints;

        LogFailure(
            passed,
            $"Requirement failed: NPC romance points {current}/{requirement.RequiredRomancePoints}"
        );

        return passed;
    }

    private static bool CheckNPCBoolState(
        RequirementData requirement,
        string contextNPCID,
        bool checkDating)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(requirement, contextNPCID);

        bool current =
            relationship != null &&
            (checkDating
                ? relationship.IsDating
                : relationship.IsMarried);

        bool passed =
            current == requirement.RequiredBool;

        LogFailure(
            passed,
            checkDating
                ? $"Requirement failed: NPC dating is {current}, expected {requirement.RequiredBool}"
                : $"Requirement failed: NPC married is {current}, expected {requirement.RequiredBool}"
        );

        return passed;
    }

    private static bool CheckNPCTimesTalked(
        RequirementData requirement,
        string contextNPCID)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(requirement, contextNPCID);

        int current =
            relationship != null
                ? relationship.TimesTalked
                : 0;

        bool passed =
            current >= requirement.RequiredTimesTalked;

        LogFailure(
            passed,
            $"Requirement failed: NPC times talked {current}/{requirement.RequiredTimesTalked}"
        );

        return passed;
    }

    private static NPCRelationshipSaveData GetRelationship(
        RequirementData requirement,
        string contextNPCID)
    {
        string npcId =
            !string.IsNullOrEmpty(requirement.RequiredNPCID)
                ? requirement.RequiredNPCID
                : contextNPCID;

        if (string.IsNullOrEmpty(npcId) ||
            RelationshipManager.GetOrCreate() == null)
        {
            return null;
        }

        return RelationshipManager
            .GetOrCreate()
            .GetRelationship(npcId);
    }

    private static bool CheckTimeOfDay(
        RequirementData requirement)
    {
        TimeOfDay current =
            TimeManager.Instance != null
                ? TimeManager
                    .Instance
                    .GetCurrentTimeOfDay()
                : TimeOfDay.Morning;

        bool passed =
            current == requirement.RequiredTimeOfDay;

        LogFailure(
            passed,
            $"Requirement failed: time of day {current}/{requirement.RequiredTimeOfDay}"
        );

        return passed;
    }

    private static bool CheckSeason(
        RequirementData requirement)
    {
        FioreSeason current =
            TimeManager.Instance != null
                ? TimeManager.Instance.CurrentSeason
                : FioreSeason.Spring;

        bool passed =
            current == requirement.RequiredSeason;

        LogFailure(
            passed,
            $"Requirement failed: season {current}/{requirement.RequiredSeason}"
        );

        return passed;
    }

    private static bool CheckCurrentCity(
        RequirementData requirement)
    {
        string currentCityID =
            SaveManager.Instance != null &&
            SaveManager.Instance.CurrentSave != null
                ? SaveManager
                    .Instance
                    .CurrentSave
                    .Location
                    .CurrentCityID
                : null;

        bool passed =
            currentCityID == requirement.RequiredCityID;

        LogFailure(
            passed,
            $"Requirement failed: city {currentCityID}/{requirement.RequiredCityID}"
        );

        return passed;
    }

    private static bool CheckCurrentExplorationArea(
        RequirementData requirement)
    {
        string currentAreaID =
            SaveManager.Instance != null &&
            SaveManager.Instance.CurrentSave != null &&
            SaveManager.Instance.CurrentSave.Exploration != null
                ? SaveManager
                    .Instance
                    .CurrentSave
                    .Exploration
                    .CurrentAreaID
                : null;

        bool passed =
            currentAreaID ==
            requirement.RequiredExplorationAreaID;

        LogFailure(
            passed,
            $"Requirement failed: exploration area {currentAreaID}/{requirement.RequiredExplorationAreaID}"
        );

        return passed;
    }

    private static bool CheckRace(
        RequirementData requirement)
    {
        string currentRaceID =
            SaveManager.Instance != null &&
            SaveManager.Instance.CurrentSave != null
                ? SaveManager
                    .Instance
                    .CurrentSave
                    .Player
                    .RaceID
                : null;

        bool passed =
            currentRaceID == requirement.RequiredRaceID;

        LogFailure(
            passed,
            $"Requirement failed: race {currentRaceID}/{requirement.RequiredRaceID}"
        );

        return passed;
    }

    private static bool CheckArchetype(
        RequirementData requirement)
    {
        string currentArchetypeID =
            SaveManager.Instance != null &&
            SaveManager.Instance.CurrentSave != null
                ? SaveManager
                    .Instance
                    .CurrentSave
                    .Player
                    .ArchetypeID
                : null;

        bool passed =
            currentArchetypeID ==
            requirement.RequiredArchetypeID;

        LogFailure(
            passed,
            $"Requirement failed: archetype {currentArchetypeID}/{requirement.RequiredArchetypeID}"
        );

        return passed;
    }

    private static bool CheckElement(
        RequirementData requirement)
    {
        ElementType current =
            CharacterManager.Instance != null
                ? CharacterManager.Instance.PrimaryElement
                : ElementType.None;

        bool passed =
            current == requirement.RequiredElementType;

        LogFailure(
            passed,
            $"Requirement failed: element {current}/{requirement.RequiredElementType}"
        );

        return passed;
    }

    private static bool CheckElementResistance(
        RequirementData requirement)
    {
        int current =
            CharacterManager.Instance != null
                ? CharacterManager
                    .Instance
                    .GetElementResistance(
                        requirement.RequiredElementType
                    )
                : 0;

        bool passed =
            current >= requirement.RequiredElementValue;

        LogFailure(
            passed,
            $"Requirement failed: element resistance {requirement.RequiredElementType} {current}/{requirement.RequiredElementValue}"
        );

        return passed;
    }

    private static bool CheckEquippedItem(
        RequirementData requirement)
    {
        bool passed =
            EquipmentManager
                .GetOrCreate()
                .IsItemEquipped(
                    requirement.RequiredEquipmentItemID
                );

        LogFailure(
            passed,
            $"Requirement failed: equipped item {requirement.RequiredEquipmentItemID}"
        );

        return passed;
    }

    private static bool CheckEquipmentSlotFilled(
        RequirementData requirement)
    {
        string itemID =
            EquipmentManager
                .GetOrCreate()
                .GetEquippedItem(
                    requirement.RequiredEquipmentSlot
                );

        bool passed =
            !string.IsNullOrEmpty(itemID);

        LogFailure(
            passed,
            $"Requirement failed: equipment slot empty {requirement.RequiredEquipmentSlot}"
        );

        return passed;
    }

    private static void LogFailure(
        bool passed,
        string message)
    {
        if (passed)
            return;

        Debug.Log(message);
    }
}
