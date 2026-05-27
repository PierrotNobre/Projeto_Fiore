using System;
using System.Collections.Generic;

[Serializable]
public class EventHistoryData
{
    public List<string> OccurredEventIds = new();

    public bool HasEventOccurred(
        string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
            return false;

        return OccurredEventIds
            .Contains(eventId);
    }

    public bool MarkEventOccurred(
        string eventId)
    {
        if (string.IsNullOrEmpty(eventId) ||
            HasEventOccurred(eventId))
        {
            return false;
        }

        OccurredEventIds.Add(eventId);

        return true;
    }

    public void Reset()
    {
        OccurredEventIds.Clear();
    }

    public void EnsureRuntimeDefaults()
    {
        if (OccurredEventIds == null)
        {
            OccurredEventIds =
                new List<string>();
        }
    }
}
