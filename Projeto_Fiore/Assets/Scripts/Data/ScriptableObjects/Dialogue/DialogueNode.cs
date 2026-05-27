using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueNode
{
    public string NodeID;

    public string SpeakerName;

    public Sprite Portrait;

    [TextArea]
    public string SpeakerText;

    public string NextNodeID;

    public List<DialogueChoice> Choices =
        new();

    public bool EndsDialogue;
}
