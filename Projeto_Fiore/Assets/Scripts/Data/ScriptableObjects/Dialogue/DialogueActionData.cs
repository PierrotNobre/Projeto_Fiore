using System;

[Serializable]
public class DialogueActionData
{
    public DialogueActionType ActionType;

    public string TargetID;

    public int Amount;

    public bool BoolValue;

    public string CompanionID;

    public string NPCID;

    public bool AddToActiveParty;

    public RewardData Reward = new();

    public string NotificationMessage;
}
