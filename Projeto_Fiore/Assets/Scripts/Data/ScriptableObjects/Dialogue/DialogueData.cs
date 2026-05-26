using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "Dialogue",
    menuName = "Fiore/Dialogue"
)]
public class DialogueData : BaseData
{
    public string StartNodeID = "start";

    public List<DialogueNode> Nodes =
        new();
}