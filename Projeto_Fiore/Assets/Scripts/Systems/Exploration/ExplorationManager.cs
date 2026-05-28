using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExplorationManager
    : PersistentSingleton<ExplorationManager>
{
    public ExplorationEventData ActiveEvent { get; private set; }

    public string LastResultText { get; private set; }

    public static ExplorationManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject explorationObject =
            new GameObject(
                "ExplorationManager"
            );

        return explorationObject
            .AddComponent<ExplorationManager>();
    }

    public ExplorationStateData State
    {
        get
        {
            SaveData save =
                SaveManager.Instance.CurrentSave;

            save.EnsureRuntimeDefaults();

            return save.Exploration;
        }
    }

    public ExplorationAreaData CurrentArea =>
        !string.IsNullOrEmpty(State.CurrentAreaID) &&
        DatabaseManager.Instance != null
            ? DatabaseManager
                .Instance
                .GetData<ExplorationAreaData>(
                    State.CurrentAreaID)
            : null;

    public List<ExplorationAreaData> GetAreasForCurrentCity()
    {
        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        if (city == null)
            return new List<ExplorationAreaData>();

        List<ExplorationAreaData> areas =
            new();

        if (city.ExplorationAreas != null)
        {
            foreach (ExplorationAreaData area
                in city.ExplorationAreas)
            {
                TryAddArea(areas, area, city);
            }
        }

        foreach (ExplorationAreaData area
            in DatabaseManager
                .Instance
                .GetAllData<ExplorationAreaData>())
        {
            TryAddArea(areas, area, city);
        }

        return areas;
    }

    public bool CanEnterArea(
        ExplorationAreaData area)
    {
        if (area == null)
            return false;

        if (!area.IsUnlockedByDefault &&
            !RequirementChecker
                .AreRequirementsMet(
                    area.UnlockRequirements))
        {
            return false;
        }

        return RequirementChecker
            .AreRequirementsMet(
                area.UnlockRequirements);
    }

    public bool StartExploration(
        ExplorationAreaData area)
    {
        if (area == null ||
            !CanEnterArea(area))
        {
            GameFeedbackUI.ShowNotification(
                "Area indisponivel."
            );

            return false;
        }

        State.IsExploring = true;
        State.CurrentAreaID = area.ID;
        State.OriginCityID =
            SaveManager
                .Instance
                .CurrentSave
                .Location
                .CurrentCityID;
        State.ExploredSteps = 0;
        State.MaxStepsBeforeReturn =
            Mathf.Max(
                1,
                area.MaxStepsBeforeReturn
            );

        ActiveEvent = null;
        LastResultText = string.Empty;

        if (area.TravelTimeCostInPeriods > 0)
        {
            TimeManager
                .Instance
                .AdvancePeriods(
                    area.TravelTimeCostInPeriods,
                    $"Entrada em area: {area.ID}"
                );
        }

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.EnterExplorationArea,
                    area.ID,
                    1,
                    "Exploration"
                )
            );

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"Explorando: {area.DisplayName}"
        );

        Debug.Log(
            $"Exploration started: {area.ID}"
        );

        return true;
    }

    public void ReturnToOriginCity()
    {
        string originCityID =
            State.OriginCityID;

        State.IsExploring = false;
        State.CurrentAreaID = string.Empty;
        State.ExploredSteps = 0;
        ActiveEvent = null;
        LastResultText = string.Empty;

        if (!string.IsNullOrEmpty(originCityID))
        {
            SaveManager
                .Instance
                .CurrentSave
                .Location
                .CurrentCityID =
                originCityID;
        }

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            "Voce retornou para a cidade."
        );
    }

    public bool Explore()
    {
        ExplorationAreaData area =
            CurrentArea;

        if (area == null)
        {
            GameFeedbackUI.ShowNotification(
                "Area de exploracao nao encontrada."
            );

            return false;
        }

        State.ExploredSteps++;

        ExplorationEventData eventData =
            PickExplorationEvent(area);

        if (eventData != null)
        {
            TriggerEvent(eventData);
            return true;
        }

        TimeManager
            .Instance
            .AdvancePeriod(
                "Exploracao"
            );

        LastResultText =
            "Voce explorou a area, mas nada importante aconteceu.";

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            "A exploracao avancou."
        );

        return true;
    }

    public bool CollectResource(
        ResourceNodeData resource)
    {
        ExplorationAreaData area =
            CurrentArea;

        if (area == null ||
            resource == null)
        {
            return false;
        }

        if (resource.OncePerDay &&
            WasResourceCollectedToday(
                area.ID,
                resource.ResourceNodeID))
        {
            GameFeedbackUI.ShowNotification(
                "Este recurso ja foi coletado hoje."
            );

            return false;
        }

        if (!RequirementChecker
            .AreRequirementsMet(
                resource.Requirements))
        {
            GameFeedbackUI.ShowNotification(
                "Requisitos de coleta nao cumpridos."
            );

            return false;
        }

        foreach (RewardItemData itemReward
            in resource.PossibleItems)
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
        }

        if (resource.OncePerDay)
        {
            MarkResourceCollectedToday(
                area.ID,
                resource.ResourceNodeID
            );
        }

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.CollectResource,
                    resource.ResourceNodeID,
                    1,
                    area.ID
                )
            );

        TimeManager
            .Instance
            .AdvancePeriods(
                Mathf.Max(1, resource.TimeCostInPeriods),
                $"Coleta: {resource.ResourceNodeID}"
            );

        LastResultText =
            $"Recurso coletado: {resource.DisplayName}";

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            LastResultText
        );

        return true;
    }

    public bool ResolveActiveEventChoice(
        ExplorationEventChoiceData choice)
    {
        if (ActiveEvent == null ||
            choice == null)
        {
            return false;
        }

        if (!RequirementChecker
            .AreRequirementsMet(choice.Requirements))
        {
            GameFeedbackUI.ShowNotification(
                "Requisito nao cumprido."
            );

            return false;
        }

        CombatEncounterData encounter =
            choice.CombatEncounter;

        if (encounter == null &&
            !string.IsNullOrEmpty(choice.CombatEncounterID))
        {
            encounter =
                DatabaseManager
                    .Instance
                    .GetData<CombatEncounterData>(
                        choice.CombatEncounterID
                    );
        }

        if (encounter != null)
        {
            string eventID =
                ActiveEvent.ID;

            ActiveEvent = null;

            CombatManager
                .GetOrCreate()
                .StartCombat(
                    encounter,
                    State.CurrentAreaID,
                    eventID
                );

            return true;
        }

        RewardManager.ApplyReward(
            ActiveEvent.Reward,
            ActiveEvent.ID
        );

        RewardManager.ApplyReward(
            choice.Reward,
            $"{ActiveEvent.ID}:choice"
        );

        if (ActiveEvent.Actions != null &&
            ActiveEvent.Actions.Count > 0 &&
            DialogueManager.Instance != null)
        {
            DialogueManager
                .Instance
                .ApplyActions(
                    ActiveEvent.Actions
                );
        }

        if (choice.Actions != null &&
            choice.Actions.Count > 0 &&
            DialogueManager.Instance != null)
        {
            DialogueManager
                .Instance
                .ApplyActions(
                    choice.Actions
                );
        }

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.CompleteExplorationEvent,
                    ActiveEvent.ID,
                    1,
                    State.CurrentAreaID
                )
            );

        if (ActiveEvent.Enemy != null)
        {
            QuestManager
                .Instance
                ?.ReportObjectiveProgress(
                    new QuestObjectiveContext(
                        QuestStepObjectiveType.SurviveEncounter,
                        ActiveEvent.Enemy.ID,
                        1,
                        ActiveEvent.ID
                    )
                );
        }

        if (choice.TimeCostInPeriods > 0)
        {
            TimeManager
                .Instance
                .AdvancePeriods(
                    choice.TimeCostInPeriods,
                    $"Evento de exploracao: {ActiveEvent.ID}"
                );
        }

        LastResultText =
            !string.IsNullOrEmpty(choice.ResultText)
                ? choice.ResultText
                : "O evento foi resolvido.";

        bool shouldEnd =
            choice.EndsExploration;

        ActiveEvent = null;

        if (shouldEnd)
        {
            ReturnToOriginCity();
        }
        else
        {
            SaveManager.Instance.SaveGame();
        }

        GameFeedbackUI.ShowNotification(
            LastResultText
        );

        return true;
    }

    public bool WasResourceCollectedToday(
        string areaID,
        string resourceNodeID)
    {
        TimeData time =
            TimeManager.Instance.CurrentTime;

        return State
            .CollectedResources
            .Any(x =>
                x != null &&
                x.Matches(
                    areaID,
                    resourceNodeID,
                    time
                ));
    }

    private void TryAddArea(
        List<ExplorationAreaData> areas,
        ExplorationAreaData area,
        CityData city)
    {
        if (area == null ||
            city == null ||
            area.AssociatedCity != city ||
            areas.Contains(area))
        {
            return;
        }

        areas.Add(area);
    }

    private ExplorationEventData PickExplorationEvent(
        ExplorationAreaData area)
    {
        List<ExplorationEventData> candidates =
            new();

        if (area.PossibleEvents != null)
        {
            candidates.AddRange(
                area.PossibleEvents
            );
        }

        candidates =
            candidates
                .Where(CanTriggerExplorationEvent)
                .ToList();

        if (candidates.Count == 0)
            return null;

        int totalWeight =
            candidates.Sum(
                x => Mathf.Max(1, x.Weight)
            );

        int roll =
            Random.Range(0, totalWeight);

        foreach (ExplorationEventData eventData
            in candidates)
        {
            roll -=
                Mathf.Max(1, eventData.Weight);

            if (roll < 0)
                return eventData;
        }

        return candidates[0];
    }

    private bool CanTriggerExplorationEvent(
        ExplorationEventData eventData)
    {
        if (eventData == null)
            return false;

        if (eventData.IsUnique &&
            WorldStateManager
                .Instance
                .HasEventOccurred(eventData.ID))
        {
            return false;
        }

        return RequirementChecker
            .AreRequirementsMet(
                eventData.Requirements);
    }

    private void TriggerEvent(
        ExplorationEventData eventData)
    {
        ActiveEvent =
            eventData;

        LastResultText =
            string.Empty;

        if (eventData.IsUnique)
        {
            WorldStateManager
                .Instance
                .MarkEventOccurred(
                    eventData.ID
                );
        }

        if (!State.TriggeredExplorationEventIDs
            .Contains(eventData.ID))
        {
            State.TriggeredExplorationEventIDs
                .Add(eventData.ID);
        }

        SaveManager.Instance.SaveGame();

        Debug.Log(
            $"Exploration event triggered: {eventData.ID}"
        );
    }

    private void MarkResourceCollectedToday(
        string areaID,
        string resourceNodeID)
    {
        TimeData time =
            TimeManager.Instance.CurrentTime;

        State.CollectedResources.Add(
            new ExplorationResourceCollectionState
            {
                AreaID = areaID,
                ResourceNodeID = resourceNodeID,
                Year = time.Year,
                Month = time.Month,
                Day = time.Day
            }
        );
    }
}
