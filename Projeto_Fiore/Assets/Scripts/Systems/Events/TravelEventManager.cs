using UnityEngine;
using System.Collections.Generic;

public class TravelEventManager
    : PersistentSingleton<TravelEventManager>
{
    public TravelEventData CurrentEvent
    {
        get;
        private set;
    }

    public bool HasActiveEvent =>
        CurrentEvent != null;

    public void ClearActiveEvent()
    {
        CurrentEvent = null;
    }

    public void TryTriggerEvent()
    {
        if (HasActiveEvent)
            return;

        RegionData currentRegion =
            TravelManager
                .Instance
                .CurrentCity
                .Region;

        List<TravelEventData> validEvents =
            new();

        foreach (var data
            in DatabaseManager
                .Instance
                .GetAllData<TravelEventData>())
        {
            if (data.AllowedRegion != currentRegion)
                continue;

            if (!RequirementChecker
                .AreRequirementsMet(
                    data.GenericRequirements
                ))
            {
                continue;
            }

            if (WorldStateManager.Instance != null &&
                !WorldStateManager
                    .Instance
                    .CanTriggerEvent(data))
            {
                continue;
            }

            bool valid = true;

            if (data.Requirements != null)
            {
                foreach (var requirement
                    in data.Requirements)
                {
                    bool hasFlag =
                        WorldStateManager
                            .Instance
                            .HasFlag(
                                requirement.RequiredFlag
                            );

                    if (hasFlag
                        != requirement.RequiredValue)
                    {
                        valid = false;
                        break;
                    }
                }
            }

            if (valid)
            {
                validEvents.Add(data);
            }
        }

        if (validEvents.Count == 0)
            return;

        TravelEventData selectedEvent =
            validEvents[
                Random.Range(
                    0,
                    validEvents.Count
                )
            ];

        int roll =
            Random.Range(0, 100);

        if (roll > selectedEvent.TriggerChance)
            return;

        TriggerEvent(selectedEvent);
    }

    private void TriggerEvent(
        TravelEventData data)
    {
        CurrentEvent = data;

        Debug.Log(
            $"Event triggered: {data.ID}"
        );

        WorldStateManager
            .Instance
            ?.MarkEventOccurred(data.ID);

        GameEvents
            .OnTravelEventTriggered
            ?.Invoke(data);
    }

    public void ResolveChoice(
        int choiceIndex)
    {
        if (CurrentEvent == null)
            return;

        if (choiceIndex < 0 ||
            choiceIndex >= CurrentEvent.Choices.Count)
        {
            Debug.LogWarning(
                "Invalid event choice."
            );

            return;
        }

        EventChoice choice =
            CurrentEvent.Choices[choiceIndex];

        if (!RequirementChecker
            .AreRequirementsMet(
                choice.GenericRequirements
            ))
        {
            GameFeedbackUI.ShowNotification(
                "Requisito nao cumprido."
            );

            return;
        }

        ResolveChoice(choice);

        if (!string.IsNullOrEmpty(
            CurrentEvent.SetFlag))
        {
            WorldStateManager
                .Instance
                .SetFlag(
                    CurrentEvent.SetFlag
                );
        }

        Debug.Log(
            $"Event choice resolved: {choice.ChoiceText}"
        );

        CurrentEvent = null;

        SaveManager
            .Instance
            .SaveGame();

        GameEvents
            .OnTravelEventResolved
            ?.Invoke();
    }

    public void ResolveCurrentEventWithoutChoice()
    {
        if (CurrentEvent == null)
            return;

        if (!string.IsNullOrEmpty(
            CurrentEvent.SetFlag))
        {
            WorldStateManager
                .Instance
                .SetFlag(
                    CurrentEvent.SetFlag
                );
        }

        Debug.Log(
            $"Event resolved without choice: {CurrentEvent.ID}"
        );

        CurrentEvent = null;

        SaveManager
            .Instance
            .SaveGame();

        GameEvents
            .OnTravelEventResolved
            ?.Invoke();
    }

    private void ResolveChoice(
        EventChoice choice)
    {
        switch (choice.Consequence.Type)
        {
            case ConsequenceType.None:
                break;

            case ConsequenceType.GainGold:

                WalletManager
                    .GetOrCreate()
                    .AddCoins(
                        choice.Consequence.Value
                    );

                break;

            case ConsequenceType.LoseGold:

                WalletManager
                    .GetOrCreate()
                    .SpendCoins(
                        choice.Consequence.Value
                    );

                break;

            case ConsequenceType.AdvanceTime:

                TimeManager
                    .Instance
                    .AdvanceHours(
                        choice.Consequence.Value
                    );

                break;

            case ConsequenceType.ChangeReputation:

                if (choice.Consequence.Faction != null)
                {
                    ReputationManager
                        .Instance
                        .AddReputation(
                            choice.Consequence.Faction.ID,
                            choice.Consequence.Value
                        );
                }

                break;
        }
    }
}
