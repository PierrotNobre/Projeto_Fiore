using UnityEngine;
using System.Collections.Generic;

public class QuestManager
    : PersistentSingleton<QuestManager>
{
    private const string MeetBlackCatsGuildQuestID =
        "meet_black_cats_guild";

    private const string ReturnToLunarisQuestID =
        "return_to_lunaris";

    public QuestStatus GetQuestState(
        string questId)
    {
        QuestStateData state =
            GetQuestStateData(
                questId,
                false
            );

        if (state != null)
            return state.Status;

        ActiveQuest activeQuest =
            SaveManager
                .Instance
                .CurrentSave
                .ActiveQuests
                .Find(
                    x => x.QuestID == questId
                );

        if (activeQuest != null)
            return activeQuest.Status;

        return QuestStatus.NotStarted;
    }

    public void SetQuestState(
        string questId,
        QuestStatus status)
    {
        if (string.IsNullOrEmpty(questId))
            return;

        QuestStateData state =
            GetQuestStateData(
                questId,
                true
            );

        QuestStatus previousStatus =
            state.Status;

        state.Status =
            status;

        if (status == QuestStatus.Active)
        {
            InitializeQuestStepIfNeeded(
                questId,
                state
            );
        }

        ActiveQuest activeQuest =
            SaveManager
                .Instance
                .CurrentSave
                .ActiveQuests
                .Find(
                    x => x.QuestID == questId
                );

        if (activeQuest != null)
        {
            activeQuest.Status =
                status;
        }

        SaveManager
            .Instance
            .SaveGame();

        Debug.Log(
            $"Quest state changed: {questId} -> {status}"
        );

        if (previousStatus == status)
            return;

        NotifyQuestStatusChanged(
            questId,
            status
        );
    }

    public bool IsQuestCompleted(
        string questId)
    {
        return GetQuestState(questId)
            == QuestStatus.Completed;
    }

    public bool CanStartQuest(
        string questId)
    {
        QuestStatus status =
            GetQuestState(questId);

        return status == QuestStatus.NotStarted ||
            status == QuestStatus.Available;
    }

    public bool StartQuest(
        string questId)
    {
        if (string.IsNullOrEmpty(questId) ||
            !CanStartQuest(questId))
        {
            return false;
        }

        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        if (quest == null)
        {
            Debug.LogWarning(
                $"Quest not found: {questId}"
            );

            return false;
        }

        EnsureActiveQuest(quest);

        QuestStateData state =
            GetQuestStateData(
                quest.ID,
                true
            );

        InitializeQuestStepIfNeeded(
            quest.ID,
            state
        );

        SetQuestState(
            quest.ID,
            QuestStatus.Active
        );

        Debug.Log(
            $"Quest started: {quest.ID}"
        );

        TryMatchCurrentCityAfterStart();

        return true;
    }

    public bool CompleteQuest(
        string questId)
    {
        if (string.IsNullOrEmpty(questId) ||
            IsQuestCompleted(questId))
        {
            return false;
        }

        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        QuestStateData state =
            GetQuestStateData(
                questId,
                true
            );

        if (quest != null &&
            !state.CompletionRewardClaimed)
        {
            ApplyQuestRewards(quest);
            state.CompletionRewardClaimed = true;
        }

        SetQuestState(
            questId,
            QuestStatus.Completed
        );

        if (quest != null &&
            quest.QuestCategory ==
            QuestCategory.Guild)
        {
            GuildManager
                .GetOrCreate()
                .RegisterGuildMissionCompleted();
        }

        Debug.Log(
            $"Quest completed: {questId}"
        );

        if (questId == MeetBlackCatsGuildQuestID)
        {
            StartQuest(ReturnToLunarisQuestID);
        }

        return true;
    }

    public bool FailQuest(
        string questId)
    {
        if (string.IsNullOrEmpty(questId) ||
            GetQuestState(questId) ==
            QuestStatus.Completed)
        {
            return false;
        }

        SetQuestState(
            questId,
            QuestStatus.Failed
        );

        Debug.Log(
            $"Quest failed: {questId}"
        );

        return true;
    }

    public void AcceptQuest(
        QuestData quest)
    {
        if (quest == null)
            return;

        StartQuest(quest.ID);
    }

    public QuestStepData GetCurrentStep(
        string questId)
    {
        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        QuestStateData state =
            GetQuestStateData(
                questId,
                false
            );

        if (quest == null ||
            state == null ||
            quest.Steps == null ||
            quest.Steps.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(
            state.CurrentStepID))
        {
            QuestStepData byId =
                quest.Steps.Find(
                    x => x.StepID == state.CurrentStepID
                );

            if (byId != null)
                return byId;
        }

        if (state.CurrentStepIndex >= 0 &&
            state.CurrentStepIndex < quest.Steps.Count)
        {
            return quest.Steps[
                state.CurrentStepIndex
            ];
        }

        return quest.Steps[0];
    }

    public int GetCurrentStepProgress(
        string questId)
    {
        QuestStateData state =
            GetQuestStateData(
                questId,
                false
            );

        return state != null
            ? state.CurrentStepProgress
            : 0;
    }

    public void SetQuestStep(
        string questId,
        string stepId)
    {
        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        if (quest == null ||
            quest.Steps == null)
        {
            return;
        }

        int index =
            quest.Steps.FindIndex(
                x => x.StepID == stepId
            );

        if (index < 0)
            return;

        QuestStateData state =
            GetQuestStateData(
                questId,
                true
            );

        state.CurrentStepIndex =
            index;

        state.CurrentStepID =
            stepId;

        state.CurrentStepProgress =
            0;

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"Etapa atualizada: {quest.Steps[index].Title}"
        );
    }

    public bool AdvanceQuestStep(
        string questId)
    {
        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        QuestStateData state =
            GetQuestStateData(
                questId,
                true
            );

        if (quest == null ||
            quest.Steps == null ||
            quest.Steps.Count == 0)
        {
            return CompleteQuest(questId);
        }

        int nextIndex =
            Mathf.Clamp(
                state.CurrentStepIndex + 1,
                0,
                quest.Steps.Count
            );

        if (nextIndex >= quest.Steps.Count)
        {
            return CompleteQuest(questId);
        }

        QuestStepData nextStep =
            quest.Steps[nextIndex];

        state.CurrentStepIndex =
            nextIndex;

        state.CurrentStepID =
            nextStep.StepID;

        state.CurrentStepProgress =
            0;

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"Nova etapa: {nextStep.Title}"
        );

        Debug.Log(
            $"Quest step advanced: {questId} -> {nextStep.StepID}"
        );

        return true;
    }

    public void AddQuestProgress(
        string questId,
        int amount)
    {
        QuestStateData state =
            GetQuestStateData(
                questId,
                true
            );

        state.CurrentStepProgress +=
            Mathf.Max(1, amount);

        SaveManager.Instance.SaveGame();
    }

    public bool IsCurrentStepComplete(
        string questId)
    {
        QuestStepData step =
            GetCurrentStep(questId);

        if (step == null)
            return false;

        return GetCurrentStepProgress(questId) >=
            Mathf.Max(1, step.RequiredAmount);
    }

    public void ReportObjectiveProgress(
        QuestObjectiveContext context)
    {
        if (context == null)
            return;

        SaveData save =
            SaveManager.Instance.CurrentSave;

        save.EnsureRuntimeDefaults();

        List<QuestStateData> snapshot =
            new(save.QuestStates);

        foreach (QuestStateData state
            in snapshot)
        {
            if (state == null ||
                state.Status != QuestStatus.Active)
            {
                continue;
            }

            TryCompleteCurrentStep(
                state.QuestID,
                context
            );
        }
    }

    public bool TryCompleteCurrentStep(
        string questId,
        QuestObjectiveContext context)
    {
        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        QuestStateData state =
            GetQuestStateData(
                questId,
                false
            );

        QuestStepData step =
            GetCurrentStep(questId);

        if (quest == null ||
            state == null ||
            step == null ||
            !StepMatchesContext(step, context))
        {
            return false;
        }

        if (step.ObjectiveType ==
            QuestStepObjectiveType.HaveItem)
        {
            state.CurrentStepProgress =
                InventoryManager.Instance.HasItem(
                    step.TargetID,
                    Mathf.Max(1, step.RequiredAmount))
                    ? Mathf.Max(1, step.RequiredAmount)
                    : 0;
        }
        else
        {
            state.CurrentStepProgress +=
                Mathf.Max(1, context.Amount);
        }

        if (!IsCurrentStepComplete(questId))
        {
            SaveManager.Instance.SaveGame();
            return false;
        }

        ApplyStepRewardIfNeeded(
            state,
            step
        );

        GameFeedbackUI.ShowNotification(
            $"Etapa concluida: {step.Title}"
        );

        return AdvanceQuestStep(questId);
    }

    public void CheckQuestProgress()
    {
        CityData currentCity =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        if (currentCity != null)
        {
            ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.EnterCity,
                    currentCity.ID,
                    1,
                    "CityManager"
                )
            );
        }

        CheckLegacyObjectives();
    }

    private void CheckLegacyObjectives()
    {
        foreach (ActiveQuest activeQuest
            in SaveManager.Instance
            .CurrentSave
            .ActiveQuests)
        {
            if (activeQuest.Status ==
                QuestStatus.Completed ||
                activeQuest.Status ==
                QuestStatus.Failed)
            {
                continue;
            }

            QuestData quest =
                DatabaseManager
                .Instance
                .GetData<QuestData>(
                    activeQuest.QuestID
                );

            if (quest == null ||
                HasSteps(quest) ||
                quest.Objectives == null)
            {
                continue;
            }

            foreach (QuestObjective objective
                in quest.Objectives)
            {
                switch (objective.Type)
                {
                    case QuestObjectiveType.TravelToCity:
                        if (SaveManager
                            .Instance
                            .CurrentSave
                            .Location
                            .CurrentCityID ==
                            objective.TargetID)
                        {
                            CompleteQuest(
                                activeQuest.QuestID
                            );
                        }
                        break;
                }
            }
        }
    }

    private bool StepMatchesContext(
        QuestStepData step,
        QuestObjectiveContext context)
    {
        if (step == null ||
            context == null ||
            step.ObjectiveType != context.ObjectiveType)
        {
            return false;
        }

        if (string.IsNullOrEmpty(step.TargetID))
            return true;

        return step.TargetID == context.TargetID;
    }

    private void ApplyStepRewardIfNeeded(
        QuestStateData state,
        QuestStepData step)
    {
        if (state == null ||
            step == null ||
            string.IsNullOrEmpty(step.StepID) ||
            state.ClaimedStepRewardIDs.Contains(step.StepID) ||
            !RewardHasAnyValue(step.StepReward))
        {
            return;
        }

        RewardManager.ApplyReward(
            step.StepReward,
            $"{state.QuestID}:{step.StepID}"
        );

        state.ClaimedStepRewardIDs
            .Add(step.StepID);
    }

    private void ApplyQuestRewards(
        QuestData quest)
    {
        if (quest == null)
            return;

        if (quest.Rewards != null)
        {
            RewardManager.ApplyReward(
                quest.Rewards,
                quest.ID
            );
        }

        if (quest.GoldReward > 0)
        {
            WalletManager
                .GetOrCreate()
                .AddCoins(
                    quest.GoldReward
                );
        }

        if (quest.ItemRewards != null &&
            InventoryManager.Instance != null)
        {
            foreach (ItemReward reward
                in quest.ItemRewards)
            {
                if (reward.Item == null)
                    continue;

                InventoryManager
                    .Instance
                    .AddItem(
                        reward.Item.ID,
                        reward.Quantity
                    );
            }
        }

        if (quest.ReputationRewards != null &&
            ReputationManager.Instance != null)
        {
            foreach (ReputationReward reward
                in quest.ReputationRewards)
            {
                if (reward.Faction == null)
                    continue;

                ReputationManager
                    .Instance
                    .AddReputation(
                        reward.Faction.ID,
                        reward.Amount
                    );
            }
        }
    }

    private QuestStateData GetQuestStateData(
        string questId,
        bool createIfMissing)
    {
        if (string.IsNullOrEmpty(questId) ||
            SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null)
        {
            return null;
        }

        SaveData save =
            SaveManager.Instance.CurrentSave;

        save.EnsureRuntimeDefaults();

        QuestStateData state =
            save.QuestStates.Find(
                x => x.QuestID == questId
            );

        if (state == null &&
            createIfMissing)
        {
            state =
                new QuestStateData
                {
                    QuestID = questId
                };

            save.QuestStates.Add(
                state
            );
        }

        state?.EnsureRuntimeDefaults();

        return state;
    }

    private void EnsureActiveQuest(
        QuestData quest)
    {
        ActiveQuest activeQuest =
            SaveManager
                .Instance
                .CurrentSave
                .ActiveQuests
                .Find(
                    x => x.QuestID == quest.ID
                );

        if (activeQuest != null)
            return;

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
    }

    private void InitializeQuestStepIfNeeded(
        string questId,
        QuestStateData state)
    {
        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        if (quest == null ||
            !HasSteps(quest) ||
            state == null ||
            !string.IsNullOrEmpty(state.CurrentStepID))
        {
            return;
        }

        state.CurrentStepIndex =
            0;

        state.CurrentStepID =
            quest.Steps[0].StepID;

        state.CurrentStepProgress =
            0;
    }

    private void TryMatchCurrentCityAfterStart()
    {
        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        if (city == null)
            return;

        ReportObjectiveProgress(
            new QuestObjectiveContext(
                QuestStepObjectiveType.EnterCity,
                city.ID,
                1,
                "StartQuest"
            )
        );
    }

    private static bool HasSteps(
        QuestData quest)
    {
        return quest.Steps != null &&
            quest.Steps.Count > 0;
    }

    private static bool RewardHasAnyValue(
        RewardData reward)
    {
        if (reward == null)
            return false;

        return reward.Coins != 0 ||
            reward.GuildReputation != 0 ||
            (reward.Items != null &&
                reward.Items.Count > 0) ||
            (reward.StatRewards != null &&
                reward.StatRewards.Count > 0);
    }

    private void NotifyQuestStatusChanged(
        string questId,
        QuestStatus status)
    {
        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        string questName =
            quest != null
                ? quest.DisplayName
                : questId;

        if (status == QuestStatus.Active)
        {
            GameFeedbackUI.ShowQuestStarted(
                questName
            );
        }
        else if (status == QuestStatus.Completed)
        {
            GameFeedbackUI.ShowQuestCompleted(
                questName
            );
        }
    }

    [ContextMenu("Debug/List Quest States")]
    public void LogQuestStates()
    {
        foreach (QuestStateData state
            in SaveManager
                .Instance
                .CurrentSave
                .QuestStates)
        {
            Debug.Log(
                $"Quest State: {state.QuestID} -> {state.Status} step {state.CurrentStepID} progress {state.CurrentStepProgress}"
            );
        }
    }

    [ContextMenu("Debug/Reset Quest States")]
    public void ResetQuestStates()
    {
        SaveManager
            .Instance
            .CurrentSave
            .QuestStates
            .Clear();

        SaveManager
            .Instance
            .CurrentSave
            .ActiveQuests
            .Clear();

        SaveManager
            .Instance
            .SaveGame();

        Debug.Log(
            "Quest states reset."
        );
    }
}
