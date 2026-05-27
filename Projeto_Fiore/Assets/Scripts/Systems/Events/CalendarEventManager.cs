using UnityEngine;
using System.Collections.Generic;

public class CalendarEventManager
    : PersistentSingleton<CalendarEventManager>
{
    public static CalendarEventManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject eventObject =
            new GameObject(
                "CalendarEventManager"
            );

        return eventObject
            .AddComponent<CalendarEventManager>();
    }

    public void CheckCalendarEvents()
    {
        if (DatabaseManager.Instance == null ||
            TimeManager.Instance == null ||
            WorldStateManager.Instance == null)
        {
            return;
        }

        List<CalendarEventData> events =
            DatabaseManager
                .Instance
                .GetAllData<CalendarEventData>();

        foreach (CalendarEventData eventData
            in events)
        {
            TryTriggerCalendarEvent(eventData);
        }
    }

    public bool TryTriggerCalendarEvent(
        CalendarEventData eventData)
    {
        if (eventData == null ||
            eventData.TriggerType ==
            CalendarEventTriggerType.Manual)
        {
            return false;
        }

        if (!MatchesCurrentTime(eventData) ||
            !WorldStateManager
                .Instance
                .CanTriggerEvent(
                    eventData.ID,
                    eventData.IsUnique) ||
            !RequirementChecker
                .AreRequirementsMet(
                    eventData.Requirements))
        {
            return false;
        }

        WorldStateManager
            .Instance
            .MarkEventOccurred(eventData.ID);

        if (!string.IsNullOrEmpty(
            eventData.NotificationMessage))
        {
            GameFeedbackUI.ShowNotification(
                eventData.NotificationMessage
            );
        }
        else
        {
            GameFeedbackUI.ShowEventNotification(
                eventData.DisplayName
            );
        }

        RewardManager.ApplyReward(
            eventData.Reward,
            eventData.ID
        );

        if (eventData.Actions != null &&
            eventData.Actions.Count > 0 &&
            DialogueManager.Instance != null)
        {
            DialogueManager
                .Instance
                .ApplyActions(eventData.Actions);
        }

        Debug.Log(
            $"Calendar event triggered: {eventData.ID}"
        );

        return true;
    }

    private bool MatchesCurrentTime(
        CalendarEventData eventData)
    {
        TimeData time =
            TimeManager.Instance.CurrentTime;

        switch (eventData.TriggerType)
        {
            case CalendarEventTriggerType.SpecificDate:
                return MatchesDate(
                    eventData,
                    time
                );

            case CalendarEventTriggerType.SeasonStart:
                return IsStartOfSeason(time) &&
                    TimeManager.Instance.CurrentSeason ==
                    eventData.Season &&
                    IsMorningUnlessTimeSpecified(
                        eventData,
                        time
                    );

            case CalendarEventTriggerType.MonthStart:
                return time.Day == 1 &&
                    IsMorningUnlessTimeSpecified(
                        eventData,
                        time
                    );

            case CalendarEventTriggerType.DayStart:
                return time.TimeOfDay ==
                    TimeOfDay.Morning;

            case CalendarEventTriggerType.TimeOfDayReached:
                return time.TimeOfDay ==
                    eventData.TimeOfDay;

            default:
                return false;
        }
    }

    private static bool MatchesDate(
        CalendarEventData eventData,
        TimeData time)
    {
        if (eventData.UseYear &&
            eventData.Year != time.Year)
        {
            return false;
        }

        if (eventData.Month != time.Month ||
            eventData.Day != time.Day)
        {
            return false;
        }

        if (eventData.UseTimeOfDay &&
            eventData.TimeOfDay != time.TimeOfDay)
        {
            return false;
        }

        return true;
    }

    private static bool IsMorningUnlessTimeSpecified(
        CalendarEventData eventData,
        TimeData time)
    {
        if (eventData.UseTimeOfDay)
        {
            return eventData.TimeOfDay ==
                time.TimeOfDay;
        }

        return time.TimeOfDay ==
            TimeOfDay.Morning;
    }

    private static bool IsStartOfSeason(
        TimeData time)
    {
        return time.Day == 1 &&
            ((time.Month - 1) %
                TimeManager.MonthsPerSeason) == 0;
    }
}
