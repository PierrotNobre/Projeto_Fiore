using UnityEngine;
using System.Collections.Generic;

public class WorldStateManager
    : PersistentSingleton<
        WorldStateManager>
{
    private Dictionary<
        string,
        bool> flags =
            new();

    [SerializeField]
    private List<string> debugOccurredEventIds =
        new();

    protected override void Awake()
    {
        base.Awake();
    }

    public void LoadFlags()
    {
        flags.Clear();

        if (SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null)
        {
            Debug.LogWarning(
                "Save not loaded yet."
            );

            return;
        }

        foreach (var flag
            in SaveManager.Instance
                .CurrentSave
                .WorldState
                .Flags)
        {
            flags[flag.Key] =
                flag.Value;
        }

        Debug.Log(
            $"Loaded {flags.Count} flags."
        );

        RefreshDebugEventHistory();
    }

    public bool HasFlag(
        string key)
    {
        return flags.TryGetValue(
            key,
            out bool value)
            && value;
    }

    public void SetFlag(
        string key,
        bool value = true)
    {
        flags[key] = value;

        SaveFlag(
            key,
            value
        );

        Debug.Log(
            $"Flag Set: " +
            $"{key} = {value}"
        );
    }

    private void SaveFlag(
        string key,
        bool value)
    {
        var saveFlags =
            SaveManager
            .Instance
            .CurrentSave
            .WorldState
            .Flags;

        var existing =
            saveFlags.Find(
                x => x.Key == key
            );

        if (existing != null)
        {
            existing.Value =
                value;
        }
        else
        {
            saveFlags.Add(
                new WorldFlag
                {
                    Key = key,
                    Value = value
                });
        }

        SaveManager
            .Instance
            .SaveGame();
    }

    public bool HasEventOccurred(
        string eventId)
    {
        EventHistoryData history =
            GetEventHistory();

        if (history == null)
            return false;

        return history
            .HasEventOccurred(eventId);
    }

    public bool CanTriggerEvent(
        string eventId,
        bool isUnique)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            Debug.LogWarning(
                "Event blocked because it has no stable ID."
            );

            return false;
        }

        if (isUnique &&
            HasEventOccurred(eventId))
        {
            Debug.Log(
                $"Event blocked because it already occurred: {eventId}"
            );

            return false;
        }

        return true;
    }

    public bool CanTriggerEvent(
        WorldEventData eventData)
    {
        if (eventData == null)
            return false;

        return CanTriggerEvent(
            eventData.ID,
            eventData.IsUnique
        );
    }

    public bool CanTriggerEvent(
        TravelEventData eventData)
    {
        if (eventData == null)
            return false;

        return CanTriggerEvent(
            eventData.ID,
            eventData.IsUnique
        );
    }

    public void MarkEventOccurred(
        string eventId)
    {
        EventHistoryData history =
            GetEventHistory();

        if (history == null ||
            string.IsNullOrEmpty(eventId))
        {
            return;
        }

        bool wasAdded =
            history.MarkEventOccurred(eventId);

        if (wasAdded)
        {
            Debug.Log(
                $"Event marked as occurred: {eventId}"
            );

            RefreshDebugEventHistory();

            SaveManager
                .Instance
                .SaveGame();
        }
    }

    [ContextMenu("Debug/Reset Event History")]
    public void ResetEventHistory()
    {
        EventHistoryData history =
            GetEventHistory();

        if (history == null)
            return;

        history.Reset();

        RefreshDebugEventHistory();

        SaveManager
            .Instance
            .SaveGame();

        Debug.Log(
            "Event history reset."
        );
    }

    [ContextMenu("Debug/Refresh Event History")]
    public void RefreshDebugEventHistory()
    {
        debugOccurredEventIds.Clear();

        EventHistoryData history =
            GetEventHistory();

        if (history == null ||
            history.OccurredEventIds == null)
        {
            return;
        }

        debugOccurredEventIds.AddRange(
            history.OccurredEventIds
        );
    }

    [ContextMenu("Debug/List Event History")]
    public void LogEventHistory()
    {
        EventHistoryData history =
            GetEventHistory();

        if (history == null ||
            history.OccurredEventIds.Count == 0)
        {
            Debug.Log(
                "Event history is empty."
            );

            return;
        }

        foreach (string eventId
            in history.OccurredEventIds)
        {
            Debug.Log(
                $"Occurred Event: {eventId}"
            );
        }
    }

    public void TriggerWorldEvents(
        EventTriggerType triggerType,
        CityData city = null,
        CityData originCity = null,
        CityData destinationCity = null)
    {
        if (DatabaseManager.Instance == null)
            return;

        foreach (var eventData
            in DatabaseManager
                .Instance
                .GetAllData<WorldEventData>())
        {
            if (!MatchesWorldEvent(
                eventData,
                triggerType,
                city,
                originCity,
                destinationCity))
            {
                continue;
            }

            TryTriggerWorldEvent(eventData);
        }
    }

    public bool TryTriggerWorldEvent(
        WorldEventData eventData)
    {
        if (eventData == null ||
            !CanTriggerEvent(eventData) ||
            !MatchesWorldEventConditions(eventData))
        {
            return false;
        }

        Debug.Log(
            $"Event triggered: {eventData.ID}"
        );

        GameFeedbackUI.ShowEventNotification(
            eventData.DisplayName
        );

        MarkEventOccurred(eventData.ID);

        RewardManager.ApplyReward(
            eventData.Rewards,
            eventData.ID
        );

        ExecuteWorldEventActions(eventData);

        GameEvents
            .OnWorldEventTriggered
            ?.Invoke(eventData);

        return true;
    }

    private void ExecuteWorldEventActions(
        WorldEventData eventData)
    {
        if (eventData.Actions == null)
            return;

        foreach (var action
            in eventData.Actions)
        {
            ExecuteWorldEventAction(action);
        }
    }

    private void ExecuteWorldEventAction(
        WorldEventAction action)
    {
        if (action == null)
            return;

        switch (action.Type)
        {
            case WorldEventActionType.None:
                break;

            case WorldEventActionType.StartQuest:
                QuestManager
                    .Instance
                    ?.StartQuest(action.QuestID);
                break;

            case WorldEventActionType.CompleteQuest:
                QuestManager
                    .Instance
                    ?.CompleteQuest(action.QuestID);
                break;

            case WorldEventActionType.FailQuest:
                QuestManager
                    .Instance
                    ?.FailQuest(action.QuestID);
                break;

            case WorldEventActionType.AddGuildReputation:
                GuildManager
                    .Instance
                    ?.AddReputation(action.Value);
                break;

            case WorldEventActionType.SetGuildLevel:
                GuildManager
                    .Instance
                    ?.SetGuildLevel(action.Value);
                break;

            case WorldEventActionType.ShowNotification:
                GameFeedbackUI.ShowNotification(
                    action.Message
                );
                break;
        }
    }

    private EventHistoryData GetEventHistory()
    {
        if (SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null)
        {
            return null;
        }

        SaveManager
            .Instance
            .CurrentSave
            .WorldState
            .EnsureRuntimeDefaults();

        return SaveManager
            .Instance
            .CurrentSave
            .WorldState
            .EventHistory;
    }

    private bool MatchesWorldEvent(
        WorldEventData eventData,
        EventTriggerType triggerType,
        CityData city,
        CityData originCity,
        CityData destinationCity)
    {
        if (eventData == null ||
            eventData.TriggerType != triggerType)
        {
            return false;
        }

        if (eventData.City != null &&
            eventData.City != city)
        {
            return false;
        }

        if (eventData.OriginCity != null &&
            eventData.OriginCity != originCity)
        {
            return false;
        }

        if (eventData.DestinationCity != null &&
            eventData.DestinationCity != destinationCity)
        {
            return false;
        }

        return true;
    }

    private bool MatchesWorldEventConditions(
        WorldEventData eventData)
    {
        if (eventData.RequiresSeason)
        {
            if (TimeManager.Instance == null)
                return false;

            if (TimeManager.Instance.CurrentSeason
                != eventData.RequiredSeason)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(
            eventData.RequiredWorldFlag))
        {
            bool hasFlag =
                HasFlag(
                    eventData.RequiredWorldFlag
                );

            if (hasFlag !=
                eventData.RequiredWorldFlagValue)
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(
            eventData.RequiredQuestID) &&
            QuestManager.Instance != null)
        {
            QuestStatus questStatus =
                QuestManager
                    .Instance
                    .GetQuestState(
                        eventData.RequiredQuestID
                    );

            if (questStatus !=
                eventData.RequiredQuestStatus)
            {
                return false;
            }
        }

        if (!RequirementChecker
            .AreRequirementsMet(
                eventData.GenericRequirements
            ))
        {
            return false;
        }

        return true;
    }
}
