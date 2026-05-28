using System;
using System.Collections.Generic;

[Serializable]
public class RewardData
{
    public int Experience;

    public int Coins;

    public List<RewardItemData> Items = new();

    public int GuildReputation;

    public List<string> CompanionRewards =
        new();

    public List<StatReward> StatRewards = new();
}
