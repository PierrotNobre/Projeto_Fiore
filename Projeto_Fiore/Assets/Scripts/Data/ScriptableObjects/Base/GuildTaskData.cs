using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GuildTask",
    menuName = "Fiore/Guild Task"
)]
public class GuildTaskData : BaseData
{
    public int DurationInPeriods = 2;

    public int RequiredGuildLevel = 1;

    public RewardData Reward = new();

    public List<RequirementData> Requirements =
        new();
}
