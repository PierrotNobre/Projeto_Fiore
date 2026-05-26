using System;

[Serializable]
public class DialogueRequirement
{
    public DialogueRequirementType Type;

    public string TargetID;

    public int Value;

    public StatType Stat;
}