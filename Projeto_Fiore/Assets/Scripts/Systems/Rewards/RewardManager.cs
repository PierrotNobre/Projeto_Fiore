using UnityEngine;

public static class RewardManager
{
    public static void ApplyReward(
        RewardData reward,
        string sourceID = null)
    {
        if (reward == null)
            return;

        if (reward.Coins > 0)
        {
            WalletManager
                .GetOrCreate()
                .AddCoins(reward.Coins);
        }

        if (reward.Items != null)
        {
            foreach (RewardItemData itemReward
                in reward.Items)
            {
                if (itemReward == null ||
                    string.IsNullOrEmpty(itemReward.ItemID) ||
                    itemReward.Quantity <= 0)
                {
                    continue;
                }

                InventoryManager
                    .Instance
                    .AddItem(
                        itemReward.ItemID,
                        itemReward.Quantity
                    );

                ItemData itemData =
                    DatabaseManager
                        .Instance
                        .GetItemById(
                            itemReward.ItemID
                        );

                string itemName =
                    itemData != null
                        ? itemData.DisplayName
                        : itemReward.ItemID;

                GameFeedbackUI.ShowNotification(
                    $"Recebeu {itemName} x{itemReward.Quantity}."
                );
            }
        }

        if (reward.GuildReputation != 0)
        {
            GuildManager
                .Instance
                ?.AddReputation(
                    reward.GuildReputation
                );
        }

        if (reward.StatRewards != null)
        {
            foreach (StatReward statReward
                in reward.StatRewards)
            {
                if (statReward == null ||
                    statReward.Value == 0)
                {
                    continue;
                }

                SaveManager
                    .Instance
                    .CurrentSave
                    .Stats
                    .AddStat(
                        statReward.StatType,
                        statReward.Value
                    );

                Debug.Log(
                    $"Stat reward applied: {statReward.StatType} {statReward.Value}"
                );
            }
        }

        SaveManager.Instance.SaveGame();

        if (!string.IsNullOrEmpty(sourceID))
        {
            Debug.Log(
                $"Reward applied from: {sourceID}"
            );
        }
    }
}
