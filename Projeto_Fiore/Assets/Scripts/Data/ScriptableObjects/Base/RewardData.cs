using System;
using System.Collections.Generic;

[Serializable]
public class RewardData
{
    public int Coins;

    public List<RewardItemData> Items = new();

    public int GuildReputation;

    public List<StatReward> StatRewards = new();
}
