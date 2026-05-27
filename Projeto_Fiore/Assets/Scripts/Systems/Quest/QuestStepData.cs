using System;

[Serializable]
public class QuestStepData
{
    public string StepID;

    public string Title;

    public string Description;

    public QuestStepObjectiveType ObjectiveType;

    public string TargetID;

    public int RequiredAmount = 1;

    public RewardData StepReward = new();
}
