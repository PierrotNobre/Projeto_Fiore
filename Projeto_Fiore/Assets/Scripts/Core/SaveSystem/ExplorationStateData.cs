using System;
using System.Collections.Generic;

[Serializable]
public class ExplorationStateData
{
    public bool IsExploring;

    public string CurrentAreaID;

    public string OriginCityID;

    public int ExploredSteps;

    public int MaxStepsBeforeReturn = 6;

    public List<string> TriggeredExplorationEventIDs =
        new();

    public List<ExplorationResourceCollectionState>
        CollectedResources =
        new();

    public void EnsureRuntimeDefaults()
    {
        if (TriggeredExplorationEventIDs == null)
        {
            TriggeredExplorationEventIDs =
                new List<string>();
        }

        if (CollectedResources == null)
        {
            CollectedResources =
                new List<ExplorationResourceCollectionState>();
        }

        if (MaxStepsBeforeReturn <= 0)
        {
            MaxStepsBeforeReturn = 6;
        }
    }
}
