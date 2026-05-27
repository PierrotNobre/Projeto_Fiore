using System;

[Serializable]
public class QuestObjectiveContext
{
    public QuestStepObjectiveType ObjectiveType;

    public string TargetID;

    public int Amount = 1;

    public string SourceID;

    public QuestObjectiveContext()
    {
    }

    public QuestObjectiveContext(
        QuestStepObjectiveType objectiveType,
        string targetID,
        int amount = 1,
        string sourceID = null)
    {
        ObjectiveType =
            objectiveType;

        TargetID =
            targetID;

        Amount =
            amount;

        SourceID =
            sourceID;
    }
}
