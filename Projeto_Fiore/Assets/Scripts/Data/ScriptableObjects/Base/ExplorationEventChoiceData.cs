using System;
using System.Collections.Generic;

[Serializable]
public class ExplorationEventChoiceData
{
    public string ChoiceText;

    public List<RequirementData> Requirements =
        new();

    public RewardData Reward = new();

    public List<DialogueActionData> Actions =
        new();

    public string ResultText;

    public int TimeCostInPeriods = 1;

    public bool EndsExploration;
}
