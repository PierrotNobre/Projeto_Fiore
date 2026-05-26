using System;
using System.Collections.Generic;

[Serializable]
public class DialogueChoice
{
    public string ChoiceText;

    public string NextNodeID;

    public List<DialogueRequirement> Requirements =
        new();

    public List<DialogueConsequence> Consequences =
        new();
}