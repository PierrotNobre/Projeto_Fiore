using System;
using System.Collections.Generic;

[Serializable]
public class ExplorationNodeData
{
    public string NodeID;

    public string DisplayName;

    public string Description;

    public List<RequirementData> Requirements =
        new();
}
