using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NPCRelationshipEvent",
    menuName = "Fiore/NPC Relationship Event"
)]
public class NPCRelationshipEventData : BaseData
{
    public string NPCID;

    public RelationshipEventType EventType;

    public bool IsUnique = true;

    public List<RequirementData> Requirements = new();

    public DialogueData DialogueToPlay;

    public RewardData Reward = new();

    public List<DialogueActionData> Actions = new();

    public string NotificationMessage;
}
