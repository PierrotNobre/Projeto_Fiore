using System;
using System.Collections.Generic;

[Serializable]
public class WorldStateData
{
    public List<WorldFlag> Flags = new();

    public EventHistoryData EventHistory = new();

    public void EnsureRuntimeDefaults()
    {
        if (Flags == null)
        {
            Flags =
                new List<WorldFlag>();
        }

        if (EventHistory == null)
        {
            EventHistory =
                new EventHistoryData();
        }

        EventHistory.EnsureRuntimeDefaults();
    }
}
