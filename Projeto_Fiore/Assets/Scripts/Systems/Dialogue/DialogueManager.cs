using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DialogueManager
    : PersistentSingleton<DialogueManager>
{
    private DialogueData currentDialogue;

    private DialogueNode currentNode;

    private NPCData currentNPC;

    public DialogueData CurrentDialogue =>
        currentDialogue;

    public DialogueNode CurrentNode =>
        currentNode;

    public NPCData CurrentNPC =>
        currentNPC;

    public bool IsDialogueActive =>
        currentDialogue != null &&
        currentNode != null;

    public void StartDialogue(
        string dialogueID)
    {
        StartDialogue(
            dialogueID,
            null
        );
    }

    public void StartDialogue(
        string dialogueID,
        NPCData npc)
    {
        currentNPC =
            npc;

        currentDialogue =
            DatabaseManager
                .Instance
                .GetData<DialogueData>(
                    dialogueID
                );

        if (currentDialogue == null)
        {
            Debug.LogWarning(
                $"Dialogue not found: {dialogueID}"
            );

            return;
        }

        currentNode =
            GetNode(
                currentDialogue.StartNodeID
            );

        if (currentNPC != null)
        {
            RelationshipManager
                .GetOrCreate()
                .IncrementTalkCount(
                    currentNPC.ID
                );

            RelationshipManager
                .GetOrCreate()
                .AddFriendship(
                    currentNPC.ID,
                    1
                );

            QuestManager
                .Instance
                ?.ReportObjectiveProgress(
                    new QuestObjectiveContext(
                        QuestStepObjectiveType.TalkToNPC,
                        currentNPC.ID,
                        1,
                        currentDialogue.ID
                    )
                );
        }

        GameManager.Instance
            .ChangeState(
                GameState.Dialogue
            );

        DisplayCurrentNode();
    }

    private DialogueNode GetNode(string nodeID)
    {
        return currentDialogue
            .Nodes
            .FirstOrDefault(
                x => x.NodeID == nodeID
            );
    }

    private void DisplayCurrentNode()
    {
        if (currentNode == null)
        {
            EndDialogue();
            return;
        }

        Debug.Log(
            $"NPC: {currentNode.SpeakerText}"
        );

        List<DialogueChoice> availableChoices =
            GetVisibleChoices();

        for (int i = 0;
            i < availableChoices.Count;
            i++)
        {
            Debug.Log(
                $"{i}: {availableChoices[i].ChoiceText}"
            );
        }
    }

    public void SelectChoice(int index)
    {
        if (currentNode == null)
            return;

        List<DialogueChoice> availableChoices =
            GetAvailableChoices();

        if (index < 0 ||
            index >= availableChoices.Count)
        {
            Debug.LogWarning(
                "Invalid dialogue choice."
            );

            return;
        }

        DialogueChoice choice =
            availableChoices[index];

        SelectChoice(choice);
    }

    public void SelectChoice(
        DialogueChoice choice)
    {
        if (choice == null ||
            !ChoiceMeetsRequirements(choice))
        {
            GameFeedbackUI.ShowNotification(
                "Requisito nao cumprido."
            );

            return;
        }

        Debug.Log(
            $"Dialogue choice selected: {choice.ChoiceText}"
        );

        ApplyConsequences(
            choice.Consequences
        );

        ApplyActions(
            choice.Actions
        );

        if (string.IsNullOrEmpty(
            choice.NextNodeID))
        {
            EndDialogue();
            return;
        }

        currentNode =
            GetNode(choice.NextNodeID);

        DisplayCurrentNode();
    }

    public List<DialogueChoice> GetAvailableChoices()
    {
        if (currentNode == null ||
            currentNode.Choices == null)
        {
            return new List<DialogueChoice>();
        }

        return currentNode
            .Choices
            .Where(
                ChoiceMeetsRequirements
            )
            .ToList();
    }

    public List<DialogueChoice> GetVisibleChoices()
    {
        if (currentNode == null ||
            currentNode.Choices == null)
        {
            return new List<DialogueChoice>();
        }

        return currentNode
            .Choices
            .Where(choice =>
                ChoiceMeetsRequirements(choice) ||
                !choice.HideIfRequirementsFail)
            .ToList();
    }

    public bool ChoiceMeetsRequirements(
        DialogueChoice choice)
    {
        if (choice == null)
            return false;

        foreach (var requirement
            in choice.Requirements)
        {
            if (!MeetsRequirement(
                requirement))
            {
                return false;
            }
        }

        return RequirementChecker
            .AreRequirementsMet(
                choice.GenericRequirements,
                currentNPC != null
                    ? currentNPC.ID
                    : null
            );
    }

    public void ContinueDialogue()
    {
        if (currentNode == null)
            return;

        if (!string.IsNullOrEmpty(
            currentNode.NextNodeID))
        {
            currentNode =
                GetNode(currentNode.NextNodeID);

            DisplayCurrentNode();
            return;
        }

        EndDialogue();
    }

    private bool MeetsRequirement(
        DialogueRequirement requirement)
    {
        switch (requirement.Type)
        {
            case DialogueRequirementType.None:
                return true;

            case DialogueRequirementType.HasFlag:
                return WorldStateManager
                    .Instance
                    .HasFlag(
                        requirement.TargetID
                    );

            case DialogueRequirementType.MissingFlag:
                return !WorldStateManager
                    .Instance
                    .HasFlag(
                        requirement.TargetID
                    );

            case DialogueRequirementType.MinimumReputation:
                return ReputationManager
                    .Instance
                    .HasMinimumReputation(
                        requirement.TargetID,
                        requirement.Value
                    );

            case DialogueRequirementType.MinimumStat:
                return CharacterManager
                    .Instance
                    .GetStat(
                        requirement.Stat
                    ) >= requirement.Value;

            case DialogueRequirementType.HasItem:
                return InventoryManager
                    .Instance
                    .HasItem(
                        requirement.TargetID,
                        requirement.Value
                    );

            case DialogueRequirementType.MinimumGold:
                return WalletManager
                    .GetOrCreate()
                    .CanAfford(requirement.Value);
        }

        return false;
    }

    private void ApplyConsequences(
        List<DialogueConsequence> consequences)
    {
        foreach (var consequence
            in consequences)
        {
            ApplyConsequence(
                consequence
            );
        }

        SaveManager.Instance
            .SaveGame();
    }

    private void ApplyConsequence(
        DialogueConsequence consequence)
    {
        switch (consequence.Type)
        {
            case DialogueConsequenceType.None:
                break;

            case DialogueConsequenceType.SetFlag:
                WorldStateManager
                    .Instance
                    .SetFlag(
                        consequence.TargetID
                    );
                break;

            case DialogueConsequenceType.AddReputation:
                ReputationManager
                    .Instance
                    .AddReputation(
                        consequence.TargetID,
                        consequence.Value
                    );
                break;

            case DialogueConsequenceType.AddItem:
                InventoryManager
                    .Instance
                    .AddItem(
                        consequence.TargetID,
                        consequence.Value
                    );
                break;

            case DialogueConsequenceType.RemoveItem:
                InventoryManager
                    .Instance
                    .RemoveItem(
                        consequence.TargetID,
                        consequence.Value
                    );
                break;

            case DialogueConsequenceType.AddGold:
                WalletManager
                    .GetOrCreate()
                    .AddCoins(consequence.Value);
                break;

            case DialogueConsequenceType.RemoveGold:
                WalletManager
                    .GetOrCreate()
                    .SpendCoins(consequence.Value);
                break;

            case DialogueConsequenceType.AdvanceTime:
                TimeManager
                    .Instance
                    .AdvanceHours(
                        consequence.Value
                    );
                break;

            case DialogueConsequenceType.StartQuest:
                QuestData quest =
                    DatabaseManager
                        .Instance
                        .GetData<QuestData>(
                            consequence.TargetID
                        );

                if (quest != null)
                {
                    QuestManager
                        .Instance
                        .AcceptQuest(
                            quest
                        );
                }
                break;

            case DialogueConsequenceType.CompleteQuest:
                QuestManager
                    .Instance
                    .CompleteQuest(
                        consequence.TargetID
                    );
                break;

            case DialogueConsequenceType.FailQuest:
                QuestManager
                    .Instance
                    .FailQuest(
                        consequence.TargetID
                    );
                break;

            case DialogueConsequenceType.ShowNotification:
                GameFeedbackUI.ShowNotification(
                    consequence.TargetID
                );
                break;

            case DialogueConsequenceType.MarkEventOccurred:
                WorldStateManager
                    .Instance
                    .MarkEventOccurred(
                        consequence.TargetID
                    );
                break;
        }
    }

    public void ApplyActions(
        List<DialogueActionData> actions)
    {
        if (actions == null)
            return;

        foreach (DialogueActionData action
            in actions)
        {
            ApplyAction(action);
        }
    }

    private void ApplyAction(
        DialogueActionData action)
    {
        if (action == null)
            return;

        switch (action.ActionType)
        {
            case DialogueActionType.None:
                break;

            case DialogueActionType.StartQuest:
                QuestManager
                    .Instance
                    .StartQuest(action.TargetID);
                break;

            case DialogueActionType.CompleteQuest:
                QuestManager
                    .Instance
                    .CompleteQuest(action.TargetID);
                break;

            case DialogueActionType.FailQuest:
                QuestManager
                    .Instance
                    .FailQuest(action.TargetID);
                break;

            case DialogueActionType.GiveReward:
                RewardManager.ApplyReward(
                    action.Reward,
                    action.TargetID
                );
                break;

            case DialogueActionType.AddItem:
                InventoryManager
                    .Instance
                    .AddItem(
                        action.TargetID,
                        Mathf.Max(1, action.Amount)
                    );
                break;

            case DialogueActionType.RemoveItem:
                InventoryManager
                    .Instance
                    .RemoveItem(
                        action.TargetID,
                        Mathf.Max(1, action.Amount)
                    );
                break;

            case DialogueActionType.AddCoins:
                WalletManager
                    .GetOrCreate()
                    .AddCoins(action.Amount);
                break;

            case DialogueActionType.SpendCoins:
                WalletManager
                    .GetOrCreate()
                    .SpendCoins(action.Amount);
                break;

            case DialogueActionType.AddGuildReputation:
                GuildManager
                    .Instance
                    .AddReputation(action.Amount);
                break;

            case DialogueActionType.MarkEventOccurred:
                WorldStateManager
                    .Instance
                    .MarkEventOccurred(action.TargetID);
                break;

            case DialogueActionType.OpenShop:
                MobileHUDManager.OpenShopFromNPC(currentNPC);
                break;

            case DialogueActionType.OpenQuestBoard:
                MobileHUDManager.TryShowScreen(
                    UIScreenType.QuestBoard
                );
                break;

            case DialogueActionType.OpenGuild:
                MobileHUDManager.TryShowScreen(
                    UIScreenType.Guild
                );
                break;

            case DialogueActionType.OpenInn:
                MobileHUDManager.TryShowScreen(
                    UIScreenType.Inn
                );
                break;

            case DialogueActionType.OpenTravel:
                MobileHUDManager.TryShowScreen(
                    UIScreenType.Travel
                );
                break;

            case DialogueActionType.AddNPCFriendship:
                RelationshipManager
                    .GetOrCreate()
                    .AddFriendship(
                        ResolveActionNPCID(action),
                        action.Amount
                    );
                break;

            case DialogueActionType.AddNPCRomance:
                RelationshipManager
                    .GetOrCreate()
                    .AddRomance(
                        ResolveActionNPCID(action),
                        action.Amount
                    );
                break;

            case DialogueActionType.SetNPCDating:
                RelationshipManager
                    .GetOrCreate()
                    .SetDating(
                        ResolveActionNPCID(action),
                        action.BoolValue
                    );
                break;

            case DialogueActionType.SetNPCMarried:
                RelationshipManager
                    .GetOrCreate()
                    .SetMarried(
                        ResolveActionNPCID(action),
                        action.BoolValue
                    );
                break;

            case DialogueActionType.IncrementNPCTalkCount:
                RelationshipManager
                    .GetOrCreate()
                    .IncrementTalkCount(
                        ResolveActionNPCID(action)
                    );
                break;

            case DialogueActionType.TriggerRelationshipEvent:
                RelationshipManager
                    .GetOrCreate()
                    .CheckRelationshipEventsForNPC(
                        ResolveActionNPCID(action)
                    );
                break;

            case DialogueActionType.ShowNotification:
                GameFeedbackUI.ShowNotification(
                    !string.IsNullOrEmpty(action.NotificationMessage)
                        ? action.NotificationMessage
                        : action.TargetID
                );
                break;
        }
    }

    private string ResolveActionNPCID(
        DialogueActionData action)
    {
        if (action != null &&
            !string.IsNullOrEmpty(action.TargetID))
        {
            return action.TargetID;
        }

        return currentNPC != null
            ? currentNPC.ID
            : null;
    }

    public void EndDialogue()
    {
        currentDialogue = null;

        currentNode = null;

        currentNPC = null;

        GameManager.Instance
            .ChangeState(
                GameState.Cityhub
            );

        Debug.Log("Dialogue ended.");
    }
}
