using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueNode
{
    public string NodeID;

    [TextArea]
    public string SpeakerText;

    public List<DialogueChoice> Choices =
        new();

    public bool EndsDialogue;
}