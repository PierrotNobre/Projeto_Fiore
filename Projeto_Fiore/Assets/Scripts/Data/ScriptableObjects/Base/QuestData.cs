using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "Quest",
    menuName = "Fiore/Quest"
)]
public class QuestData
    : BaseData
{
    [Header("Quest")]
    public List<QuestObjective>
        Objectives;

    [Header("Reward")]
    public int GoldReward;

    [Header("Time Limit")]
    public int DeadlineDays;

    [Header("Item Reward")]
    public List<ItemReward> ItemRewards;

    [Header("Reputation Reward")]
    public List<ReputationReward> ReputationRewards;
}