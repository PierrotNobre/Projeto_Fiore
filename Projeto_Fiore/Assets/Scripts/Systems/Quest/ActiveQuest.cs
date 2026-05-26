using System;

[Serializable]
public class ActiveQuest
{
    public string QuestID;

    public QuestStatus Status =
        QuestStatus.Active;

    public int AcceptedDay;

    public int AcceptedMonth;

    public int AcceptedYear;
}