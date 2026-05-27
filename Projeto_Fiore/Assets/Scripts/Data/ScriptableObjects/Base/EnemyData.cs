using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Enemy",
    menuName = "Fiore/Enemy"
)]
public class EnemyData : BaseData
{
    public Sprite Sprite;

    public int MaxHealth = 10;

    public int Attack = 1;

    public int Defense = 0;

    public RewardData Reward = new();

    public List<RewardItemData> Drops =
        new();

    public bool CanAppearInExploration = true;
}
