using UnityEngine;
using System.Collections.Generic;

public class QuestManager
    : PersistentSingleton<
        QuestManager>
{
    public void AcceptQuest(
        QuestData quest)
    {
        SaveManager.Instance
            .CurrentSave
            .ActiveQuests
            .Add(
                new ActiveQuest
                {
                    QuestID =
                        quest.ID,

                    AcceptedDay =
                        TimeManager
                        .Instance
                        .CurrentTime
                        .Day,

                    AcceptedMonth =
                        TimeManager
                        .Instance
                        .CurrentTime
                        .Month,

                    AcceptedYear =
                        TimeManager
                        .Instance
                        .CurrentTime
                        .Year
                });

        SaveManager.Instance
            .SaveGame();

        Debug.Log(
            $"Quest Accepted: " +
            $"{quest.DisplayName}"
        );
    }

    public void CheckQuestProgress()
    {
        foreach (var activeQuest
            in SaveManager.Instance
            .CurrentSave
            .ActiveQuests)
        {
            var quest =
                DatabaseManager
                .Instance
                .GetData<QuestData>(
                    activeQuest
                    .QuestID
                );

            foreach (var objective
                in quest.Objectives)
            {
                switch (
                    objective.Type)
                {
                    case QuestObjectiveType
                        .TravelToCity:

                        if (SaveManager
                            .Instance
                            .CurrentSave
                            .Location
                            .CurrentCityID
                            ==
                            objective
                            .TargetID)
                        {
                            CompleteQuest(
                                activeQuest,
                                quest
                            );
                        }

                        break;
                }
            }
        }
    }

    private void CompleteQuest(ActiveQuest activeQuest, QuestData quest)
    {
        activeQuest.Status =
            QuestStatus.Completed;

        SaveManager.Instance
            .CurrentSave
            .Player
            .Gold +=
            quest.GoldReward;

        foreach (var reward in quest.ItemRewards)
        {
            InventoryManager
                .Instance
                .AddItem(
                    reward.Item.ID,
                    reward.Quantity
                );
        }

        foreach (var reward in quest.ReputationRewards)
        {
            ReputationManager
                .Instance
                .AddReputation(
                    reward.Faction.ID,
                    reward.Amount
                );
        }

        Debug.Log(
            $"Quest Completed: " +
            $"{quest.DisplayName}"
        );

        SaveManager.Instance
            .SaveGame();
    }
}