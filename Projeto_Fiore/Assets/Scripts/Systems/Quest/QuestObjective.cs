using System;

[Serializable]
public class QuestObjective
{
    public QuestObjectiveType Type;

    public string TargetID;

    public int RequiredAmount = 1;
}