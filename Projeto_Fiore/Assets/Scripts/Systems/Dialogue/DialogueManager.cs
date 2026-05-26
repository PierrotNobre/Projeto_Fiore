using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DialogueManager
    : PersistentSingleton<DialogueManager>
{
    private DialogueData currentDialogue;

    private DialogueNode currentNode;

    public void StartDialogue(string dialogueID)
    {
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

        if (currentNode.EndsDialogue)
        {
            EndDialogue();
            return;
        }

        List<DialogueChoice> availableChoices =
            GetAvailableChoices();

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

        ApplyConsequences(
            choice.Consequences
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

    private List<DialogueChoice> GetAvailableChoices()
    {
        return currentNode
            .Choices
            .Where(
                ChoiceMeetsRequirements
            )
            .ToList();
    }

    private bool ChoiceMeetsRequirements(
        DialogueChoice choice)
    {
        foreach (var requirement
            in choice.Requirements)
        {
            if (!MeetsRequirement(
                requirement))
            {
                return false;
            }
        }

        return true;
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
                return SaveManager
                    .Instance
                    .CurrentSave
                    .Player
                    .Gold >= requirement.Value;
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
                SaveManager
                    .Instance
                    .CurrentSave
                    .Player
                    .Gold += consequence.Value;
                break;

            case DialogueConsequenceType.RemoveGold:
                SaveManager.Instance.CurrentSave.Player.Gold =
                     Mathf.Max(0,SaveManager.Instance.CurrentSave.Player.Gold - consequence.Value);
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
        }
    }

    private void EndDialogue()
    {
        currentDialogue = null;

        currentNode = null;

        GameManager.Instance
            .ChangeState(
                GameState.Cityhub
            );

        Debug.Log("Dialogue ended.");
    }
}