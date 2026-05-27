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
    public QuestCategory QuestCategory =
        QuestCategory.Side;

    public int RequiredGuildLevel = 1;

    public List<QuestObjective>
        Objectives;

    public List<QuestStepData> Steps =
        new();

    [Header("Reward")]
    public RewardData Rewards = new();

    public int GoldReward;

    [Header("Time Limit")]
    public int DeadlineDays;

    [Header("Item Reward")]
    public List<ItemReward> ItemRewards;

    [Header("Reputation Reward")]
    public List<ReputationReward> ReputationRewards;
}
