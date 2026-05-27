using System;
using System.Collections.Generic;

[Serializable]
public class QuestStateData
{
    public string QuestID;

    public QuestStatus Status =
        QuestStatus.NotStarted;

    public string CurrentStepID;

    public int CurrentStepIndex;

    public int CurrentStepProgress;

    public bool CompletionRewardClaimed;

    public List<string> ClaimedStepRewardIDs =
        new();

    public void EnsureRuntimeDefaults()
    {
        if (ClaimedStepRewardIDs == null)
        {
            ClaimedStepRewardIDs =
                new List<string>();
        }
    }
}
