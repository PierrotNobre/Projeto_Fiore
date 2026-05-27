using System.Collections.Generic;
using UnityEngine;

public class RelationshipManager
    : PersistentSingleton<RelationshipManager>
{
    private const bool AllowMultipleMarriages = false;

    public static RelationshipManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject relationshipObject =
            new GameObject(
                "RelationshipManager"
            );

        return relationshipObject
            .AddComponent<RelationshipManager>();
    }

    public NPCRelationshipSaveData GetRelationship(
        string npcId)
    {
        if (string.IsNullOrEmpty(npcId) ||
            SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null)
        {
            return null;
        }

        SaveData save =
            SaveManager.Instance.CurrentSave;

        save.EnsureRuntimeDefaults();

        NPCRelationshipSaveData relationship =
            save.NPCRelationships.Find(
                x => x.NPCID == npcId
            );

        if (relationship == null)
        {
            NPCData npc =
                DatabaseManager.Instance != null
                    ? DatabaseManager
                        .Instance
                        .GetNPCById(npcId)
                    : null;

            relationship =
                new NPCRelationshipSaveData
                {
                    NPCID = npcId,
                    RomanceAvailable = npc != null &&
                        npc.CanRomance
                };

            save.NPCRelationships.Add(
                relationship
            );
        }

        relationship.EnsureRuntimeDefaults();

        return relationship;
    }

    public NPCRelationshipLevel GetFriendshipLevel(
        string npcId)
    {
        return GetRelationship(npcId)
            ?.FriendshipLevel
            ?? NPCRelationshipLevel.Stranger;
    }

    public NPCRomanceLevel GetRomanceLevel(
        string npcId)
    {
        return GetRelationship(npcId)
            ?.RomanceLevel
            ?? NPCRomanceLevel.None;
    }

    public bool AddFriendship(
        string npcId,
        int amount)
    {
        if (amount == 0)
            return false;

        NPCData npc =
            DatabaseManager.Instance.GetNPCById(npcId);

        if (npc == null ||
            !npc.CanGainFriendship)
        {
            Debug.Log(
                $"NPC cannot gain friendship: {npcId}"
            );

            return false;
        }

        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        relationship.FriendshipPoints =
            Mathf.Max(
                0,
                relationship.FriendshipPoints + amount
            );

        relationship.FriendshipLevel =
            ClampFriendshipLevel(
                GetFriendshipLevelFromPoints(
                    relationship.FriendshipPoints),
                npc.MaxFriendshipLevel
            );

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"Amizade com {npc.DisplayName} aumentou."
        );

        Debug.Log(
            $"NPC friendship changed: {npcId} -> {relationship.FriendshipPoints}"
        );

        CheckRelationshipEventsForNPC(npcId);

        return true;
    }

    public bool AddRomance(
        string npcId,
        int amount)
    {
        if (amount == 0)
            return false;

        NPCData npc =
            DatabaseManager.Instance.GetNPCById(npcId);

        if (npc == null ||
            !npc.CanRomance)
        {
            Debug.Log(
                $"NPC cannot gain romance: {npcId}"
            );

            return false;
        }

        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        relationship.RomanceAvailable =
            true;

        relationship.RomancePoints =
            Mathf.Max(
                0,
                relationship.RomancePoints + amount
            );

        relationship.RomanceLevel =
            ClampRomanceLevel(
                GetRomanceLevelFromPoints(
                    relationship.RomancePoints),
                npc.MaxRomanceLevel
            );

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"Relacao com {npc.DisplayName} mudou."
        );

        Debug.Log(
            $"NPC romance changed: {npcId} -> {relationship.RomancePoints}"
        );

        CheckRelationshipEventsForNPC(npcId);

        return true;
    }

    public void IncrementTalkCount(
        string npcId)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        if (relationship == null)
            return;

        relationship.TimesTalked++;

        relationship.LastInteractionDay =
            TimeManager.Instance != null
                ? TimeManager.Instance.CurrentTime.Day
                : relationship.LastInteractionDay;

        SaveManager.Instance.SaveGame();

        Debug.Log(
            $"NPC talked: {npcId} -> {relationship.TimesTalked}"
        );

        CheckRelationshipEventsForNPC(npcId);
    }

    public bool CanGiveGiftToday(
        string npcId)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        if (relationship == null ||
            TimeManager.Instance == null)
        {
            return false;
        }

        TimeData time =
            TimeManager.Instance.CurrentTime;

        bool sameDay =
            relationship.LastGiftYear == time.Year &&
            relationship.LastGiftMonth == time.Month &&
            relationship.LastGiftDay == time.Day;

        return !sameDay ||
            relationship.GiftsGivenToday <= 0;
    }

    public bool GiveGift(
        string npcId,
        string itemId)
    {
        if (string.IsNullOrEmpty(npcId) ||
            string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        NPCData npc =
            DatabaseManager
                .Instance
                .GetNPCById(npcId);

        ItemData item =
            DatabaseManager
                .Instance
                .GetItemById(itemId);

        if (npc == null ||
            item == null)
        {
            GameFeedbackUI.ShowNotification(
                "Presente indisponivel."
            );

            return false;
        }

        if (!npc.CanReceiveGifts ||
            !npc.CanGainFriendship)
        {
            GameFeedbackUI.ShowNotification(
                "Este personagem nao aceita presentes agora."
            );

            return false;
        }

        if (item.Type == ItemType.Quest ||
            !item.CanGift)
        {
            GameFeedbackUI.ShowNotification(
                "Este item nao pode ser dado como presente."
            );

            return false;
        }

        if (!CanGiveGiftToday(npcId))
        {
            GameFeedbackUI.ShowNotification(
                "Voce ja deu um presente para este personagem hoje."
            );

            return false;
        }

        if (!InventoryManager
            .Instance
            .RemoveItem(itemId))
        {
            GameFeedbackUI.ShowNotification(
                "Item indisponivel."
            );

            return false;
        }

        GiftReaction reaction =
            ResolveGiftReaction(
                npc,
                item,
                out int friendshipAmount,
                out int romanceAmount
            );

        ApplyGiftRelationship(
            npc,
            friendshipAmount,
            romanceAmount
        );

        MarkGiftGivenToday(npcId);

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"{npc.DisplayName} recebeu {item.DisplayName}. {GetGiftReactionLabel(reaction)}"
        );

        Debug.Log(
            $"Gift given: {npcId} <- {itemId} ({reaction})"
        );

        CheckRelationshipEventsForNPC(npcId);

        return true;
    }

    public void MarkGiftGivenToday(
        string npcId)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        if (relationship == null ||
            TimeManager.Instance == null)
        {
            return;
        }

        TimeData time =
            TimeManager.Instance.CurrentTime;

        bool sameDay =
            relationship.LastGiftYear == time.Year &&
            relationship.LastGiftMonth == time.Month &&
            relationship.LastGiftDay == time.Day;

        relationship.LastGiftYear =
            time.Year;

        relationship.LastGiftMonth =
            time.Month;

        relationship.LastGiftDay =
            time.Day;

        relationship.GiftsGivenToday =
            sameDay
                ? relationship.GiftsGivenToday + 1
                : 1;
    }

    public bool SetDating(
        string npcId,
        bool value)
    {
        if (value &&
            !CanDateNPC(npcId))
        {
            return false;
        }

        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        if (relationship == null)
            return false;

        relationship.IsDating =
            value;

        relationship.RomanceLevel =
            value
                ? NPCRomanceLevel.Dating
                : GetRomanceLevelFromPoints(
                    relationship.RomancePoints);

        SaveManager.Instance.SaveGame();

        CheckRelationshipEventsForNPC(npcId);

        return true;
    }

    public bool SetMarried(
        string npcId,
        bool value)
    {
        if (value &&
            !CanMarryNPC(npcId))
        {
            return false;
        }

        if (value &&
            !AllowMultipleMarriages &&
            HasAnyMarriageExcept(npcId))
        {
            GameFeedbackUI.ShowNotification(
                "Ja existe um casamento ativo."
            );

            return false;
        }

        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        if (relationship == null)
            return false;

        relationship.IsMarried =
            value;

        relationship.RomanceLevel =
            value
                ? NPCRomanceLevel.Married
                : GetRomanceLevelFromPoints(
                    relationship.RomancePoints);

        SaveManager.Instance.SaveGame();

        CheckRelationshipEventsForNPC(npcId);

        return true;
    }

    public bool CanDateNPC(
        string npcId)
    {
        NPCData npc =
            DatabaseManager.Instance.GetNPCById(npcId);

        return npc != null &&
            npc.CanRomance;
    }

    public bool CanMarryNPC(
        string npcId)
    {
        NPCData npc =
            DatabaseManager.Instance.GetNPCById(npcId);

        if (npc == null ||
            !npc.CanMarry)
        {
            return false;
        }

        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        return relationship != null &&
            relationship.IsDating;
    }

    public bool CanTriggerRelationshipEvent(
        string npcId,
        string eventId)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        return relationship != null &&
            !relationship.UnlockedRelationshipEventIDs
                .Contains(eventId);
    }

    public void MarkRelationshipEventUnlocked(
        string npcId,
        string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
            return;

        NPCRelationshipSaveData relationship =
            GetRelationship(npcId);

        if (relationship == null ||
            relationship.UnlockedRelationshipEventIDs
                .Contains(eventId))
        {
            return;
        }

        relationship.UnlockedRelationshipEventIDs
            .Add(eventId);

        SaveManager.Instance.SaveGame();
    }

    public void CheckRelationshipEventsForNPC(
        string npcId)
    {
        if (DatabaseManager.Instance == null ||
            string.IsNullOrEmpty(npcId))
        {
            return;
        }

        List<NPCRelationshipEventData> events =
            DatabaseManager
                .Instance
                .GetAllData<NPCRelationshipEventData>();

        foreach (NPCRelationshipEventData eventData
            in events)
        {
            if (eventData == null ||
                eventData.NPCID != npcId)
            {
                continue;
            }

            if (eventData.IsUnique &&
                WorldStateManager
                    .Instance
                    .HasEventOccurred(eventData.ID))
            {
                continue;
            }

            if (!RequirementChecker.AreRequirementsMet(
                eventData.Requirements,
                npcId))
            {
                continue;
            }

            TriggerRelationshipEvent(
                eventData,
                npcId
            );
        }
    }

    private void TriggerRelationshipEvent(
        NPCRelationshipEventData eventData,
        string npcId)
    {
        if (eventData == null)
            return;

        WorldStateManager
            .Instance
            .MarkEventOccurred(eventData.ID);

        MarkRelationshipEventUnlocked(
            npcId,
            eventData.ID
        );

        if (!string.IsNullOrEmpty(
            eventData.NotificationMessage))
        {
            GameFeedbackUI.ShowNotification(
                eventData.NotificationMessage
            );
        }

        if (eventData.Reward != null)
        {
            RewardManager.ApplyReward(
                eventData.Reward,
                eventData.ID
            );
        }

        if (eventData.Actions != null &&
            eventData.Actions.Count > 0 &&
            DialogueManager.Instance != null)
        {
            DialogueManager
                .Instance
                .ApplyActions(
                    eventData.Actions
                );
        }

        if (eventData.DialogueToPlay != null)
        {
            NPCData npc =
                DatabaseManager
                    .Instance
                    .GetNPCById(npcId);

            DialogueManager
                .Instance
                .StartDialogue(
                    eventData.DialogueToPlay.ID,
                    npc
                );

            MobileHUDManager.TryShowScreen(
                UIScreenType.Dialogue
            );
        }

        Debug.Log(
            $"Relationship event triggered: {eventData.ID}"
        );
    }

    private bool HasAnyMarriageExcept(
        string npcId)
    {
        SaveData save =
            SaveManager.Instance.CurrentSave;

        foreach (NPCRelationshipSaveData relationship
            in save.NPCRelationships)
        {
            if (relationship == null ||
                relationship.NPCID == npcId)
            {
                continue;
            }

            if (relationship.IsMarried)
                return true;
        }

        return false;
    }

    private static GiftReaction ResolveGiftReaction(
        NPCData npc,
        ItemData item,
        out int friendshipAmount,
        out int romanceAmount)
    {
        friendshipAmount = 1;
        romanceAmount = 0;

        if (npc.ItemGiftPreferences != null)
        {
            foreach (NPCGiftPreference preference
                in npc.ItemGiftPreferences)
            {
                if (preference == null ||
                    preference.ItemID != item.ID)
                {
                    continue;
                }

                friendshipAmount =
                    preference.FriendshipAmount;

                romanceAmount =
                    preference.RomanceAmount;

                return preference.Reaction;
            }
        }

        if (npc.CategoryGiftPreferences != null)
        {
            foreach (GiftCategoryPreference preference
                in npc.CategoryGiftPreferences)
            {
                if (preference == null ||
                    preference.Category != item.GiftCategory)
                {
                    continue;
                }

                friendshipAmount =
                    preference.FriendshipAmount;

                romanceAmount =
                    preference.RomanceAmount;

                return preference.Reaction;
            }
        }

        return GiftReaction.Neutral;
    }

    private void ApplyGiftRelationship(
        NPCData npc,
        int friendshipAmount,
        int romanceAmount)
    {
        NPCRelationshipSaveData relationship =
            GetRelationship(npc.ID);

        if (relationship == null)
            return;

        relationship.FriendshipPoints =
            Mathf.Max(
                0,
                relationship.FriendshipPoints +
                friendshipAmount
            );

        relationship.FriendshipLevel =
            ClampFriendshipLevel(
                GetFriendshipLevelFromPoints(
                    relationship.FriendshipPoints),
                npc.MaxFriendshipLevel
            );

        if (npc.CanRomance &&
            romanceAmount != 0)
        {
            relationship.RomanceAvailable =
                true;

            relationship.RomancePoints =
                Mathf.Max(
                    0,
                    relationship.RomancePoints +
                    romanceAmount
                );

            relationship.RomanceLevel =
                ClampRomanceLevel(
                    GetRomanceLevelFromPoints(
                        relationship.RomancePoints),
                    npc.MaxRomanceLevel
                );
        }
    }

    private static string GetGiftReactionLabel(
        GiftReaction reaction)
    {
        return reaction switch
        {
            GiftReaction.Hate =>
                "Nao pareceu gostar.",

            GiftReaction.Dislike =>
                "A reacao foi fria.",

            GiftReaction.Like =>
                "Gostou do presente.",

            GiftReaction.Love =>
                "Adorou o presente.",

            _ =>
                "Aceitou o presente."
        };
    }

    private static NPCRelationshipLevel GetFriendshipLevelFromPoints(
        int points)
    {
        if (points >= 100)
            return NPCRelationshipLevel.Trusted;

        if (points >= 60)
            return NPCRelationshipLevel.CloseFriend;

        if (points >= 30)
            return NPCRelationshipLevel.Friend;

        if (points >= 10)
            return NPCRelationshipLevel.Acquaintance;

        return NPCRelationshipLevel.Stranger;
    }

    private static NPCRomanceLevel GetRomanceLevelFromPoints(
        int points)
    {
        if (points >= 80)
            return NPCRomanceLevel.Partner;

        if (points >= 30)
            return NPCRomanceLevel.Affection;

        if (points >= 10)
            return NPCRomanceLevel.Interest;

        return NPCRomanceLevel.None;
    }

    private static NPCRelationshipLevel ClampFriendshipLevel(
        NPCRelationshipLevel level,
        NPCRelationshipLevel maxLevel)
    {
        return level > maxLevel
            ? maxLevel
            : level;
    }

    private static NPCRomanceLevel ClampRomanceLevel(
        NPCRomanceLevel level,
        NPCRomanceLevel maxLevel)
    {
        return level > maxLevel
            ? maxLevel
            : level;
    }

    [ContextMenu("Debug/Print NPC Relationships")]
    public void DebugPrintRelationships()
    {
        SaveData save =
            SaveManager.Instance.CurrentSave;

        save.EnsureRuntimeDefaults();

        foreach (NPCRelationshipSaveData relationship
            in save.NPCRelationships)
        {
            Debug.Log(
                $"NPC Relationship: {relationship.NPCID} " +
                $"friendship {relationship.FriendshipPoints}/{relationship.FriendshipLevel} " +
                $"romance {relationship.RomancePoints}/{relationship.RomanceLevel} " +
                $"talks {relationship.TimesTalked} dating {relationship.IsDating} married {relationship.IsMarried}"
            );
        }
    }
}
