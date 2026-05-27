using System;

[Serializable]
public class DialogueActionData
{
    public DialogueActionType ActionType;

    public string TargetID;

    public int Amount;

    public bool BoolValue;

    public RewardData Reward = new();

    public string NotificationMessage;
}
