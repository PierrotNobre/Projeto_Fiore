using System;
using System.Collections.Generic;

[Serializable]
public class ResourceNodeData
{
    public string ResourceNodeID;

    public string DisplayName;

    public string Description;

    public List<RewardItemData> PossibleItems =
        new();

    public int TimeCostInPeriods = 1;

    public bool OncePerDay = true;

    public List<RequirementData> Requirements =
        new();
}
