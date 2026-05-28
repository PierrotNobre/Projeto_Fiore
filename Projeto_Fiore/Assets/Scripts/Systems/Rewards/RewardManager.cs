using UnityEngine;

public static class RewardManager
{
    public static string BuildRewardSummary(
        RewardData reward)
    {
        if (reward == null)
            return "Sem recompensa.";

        string summary =
            string.Empty;

        if (reward.Experience > 0)
        {
            summary +=
                $"XP: {reward.Experience}\n";
        }

        if (reward.Coins > 0)
        {
            summary +=
                $"Moedas: {reward.Coins}\n";
        }

        if (reward.GuildReputation != 0)
        {
            summary +=
                $"Reputacao da guilda: {reward.GuildReputation}\n";
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

                ItemData itemData =
                    DatabaseManager.Instance != null
                        ? DatabaseManager
                            .Instance
                            .GetItemById(
                                itemReward.ItemID
                            )
                        : null;

                string itemName =
                    itemData != null
                        ? itemData.DisplayName
                        : itemReward.ItemID;

                summary +=
                    $"{itemName} x{itemReward.Quantity}\n";
            }
        }

        if (reward.CompanionRewards != null)
        {
            foreach (string companionID
                in reward.CompanionRewards)
            {
                if (string.IsNullOrEmpty(companionID))
                    continue;

                CompanionData companion =
                    DatabaseManager.Instance != null
                        ? DatabaseManager
                            .Instance
                            .GetCompanionById(
                                companionID
                            )
                        : null;

                string companionName =
                    companion != null
                        ? companion.DisplayName
                        : companionID;

                summary +=
                    $"Companheiro: {companionName}\n";
            }
        }

        return string.IsNullOrEmpty(summary)
            ? "Sem recompensa."
            : summary.TrimEnd();
    }

    public static CharacterProgressionResult ApplyReward(
        RewardData reward,
        string sourceID = null)
    {
        if (reward == null)
            return null;

        CharacterProgressionResult progressionResult =
            null;

        if (reward.Experience > 0)
        {
            progressionResult =
                CharacterManager
                .GetOrCreate()
                .AddExperience(
                    reward.Experience
                );

            GameFeedbackUI.ShowNotification(
                $"Recebeu {reward.Experience} XP."
            );
        }

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

        if (reward.CompanionRewards != null)
        {
            foreach (string companionID
                in reward.CompanionRewards)
            {
                if (string.IsNullOrEmpty(companionID))
                    continue;

                CompanionManager
                    .GetOrCreate()
                    .RecruitCompanion(
                        companionID,
                        addToActiveParty: false
                    );
            }
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

        return progressionResult;
    }
}
