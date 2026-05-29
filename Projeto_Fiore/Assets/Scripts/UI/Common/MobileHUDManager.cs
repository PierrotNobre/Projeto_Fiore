using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MobileShopTab
{
    Buy,

    Sell
}

public class MobileHUDManager
    : PersistentSingleton<MobileHUDManager>
{
    private const string ReturnToLunarisQuestID =
        "return_to_lunaris";

    private const string MeetBlackCatsGuildQuestID =
        "meet_black_cats_guild";

    [Header("Runtime UI")]
    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private RectTransform root;

    [SerializeField]
    private TMP_Text topLocationText;

    [SerializeField]
    private TMP_Text topDateText;

    [SerializeField]
    private RectTransform contentRoot;

    [SerializeField]
    private RectTransform bottomNavRoot;

    [SerializeField]
    private RectTransform travelEventPopupRoot;

    [SerializeField]
    private TMP_Text travelEventTitleText;

    [SerializeField]
    private TMP_Text travelEventBodyText;

    [SerializeField]
    private RectTransform travelEventChoicesRoot;

    [SerializeField]
    private RectTransform combatPopupRoot;

    [SerializeField]
    private TMP_Text combatPopupTitleText;

    [SerializeField]
    private RectTransform combatPopupContentRoot;

    [SerializeField]
    private RectTransform combatPopupActionsRoot;

    private readonly Dictionary<UIScreenType, Button>
        navButtons = new();

    private NPCData selectedNPC;

    private CityLocationData selectedLocation;

    private ShopData activeShopData;

    private NPCData activeShopNPC;

    private CityLocationData activeShopLocation;

    private MobileShopTab currentShopTab =
        MobileShopTab.Buy;

    private bool guildQuestBoardMode;

    private SceneCityHUDController sceneCityHUD;

    private UIScreenType currentScreen =
        UIScreenType.City;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        BuildRuntimeUI();
    }

    private void Start()
    {
        if (SaveManager.Instance != null &&
            !SaveManager.Instance.HasActiveGame)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnTimeAdvanced += RefreshAll;
        GameEvents.OnTimeOfDayChanged += HandleTimeOfDayChanged;
        GameEvents.OnTravelStarted += HandleTravelChanged;
        GameEvents.OnTravelFinished += HandleTravelChanged;
        GameEvents.OnWorldEventTriggered += HandleWorldEventTriggered;
        GameEvents.OnTravelEventTriggered += ShowTravelEventPopup;
        GameEvents.OnTravelEventResolved += RefreshCurrentScreen;
        GameEvents.OnTravelEventResolved += HideTravelEventPopup;
    }

    private void OnDisable()
    {
        GameEvents.OnTimeAdvanced -= RefreshAll;
        GameEvents.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
        GameEvents.OnTravelStarted -= HandleTravelChanged;
        GameEvents.OnTravelFinished -= HandleTravelChanged;
        GameEvents.OnWorldEventTriggered -= HandleWorldEventTriggered;
        GameEvents.OnTravelEventTriggered -= ShowTravelEventPopup;
        GameEvents.OnTravelEventResolved -= RefreshCurrentScreen;
        GameEvents.OnTravelEventResolved -= HideTravelEventPopup;
    }

    public static MobileHUDManager OpenOrCreate()
    {
        if (SaveManager.Instance != null &&
            !SaveManager.Instance.HasActiveGame)
        {
            return null;
        }

        if (Instance != null)
        {
            Instance.gameObject.SetActive(true);
            Instance.MoveToScene(
                SceneFlowManager.CitySceneName
            );

            Instance.ShowScreen(
                Instance.currentScreen
            );

            return Instance;
        }

        GameObject hudObject =
            new GameObject(
                "MobileHUDManager"
            );

        MobileHUDManager manager =
            hudObject
                .AddComponent<MobileHUDManager>();

        manager.MoveToScene(
            SceneFlowManager.CitySceneName
        );

        manager.ShowScreen(
            UIScreenType.City
        );

        return manager;
    }

    public static void HideActiveHUD()
    {
        if (Instance == null)
            return;

        Instance.HideSceneCityHUD();
        Instance.gameObject.SetActive(false);
    }

    public static bool TryShowScreen(
        UIScreenType screenType)
    {
        if (SaveManager.Instance != null &&
            !SaveManager.Instance.HasActiveGame)
        {
            return false;
        }

        if (Instance == null)
            return false;

        Instance.ShowScreen(screenType);

        return true;
    }

    public static bool TryShowCombatPopup()
    {
        MobileHUDManager manager =
            OpenOrCreate();

        if (manager == null)
            return false;

        manager.ShowCombatPopup();

        return true;
    }

    public static void RefreshCombatPopup()
    {
        if (Instance == null)
            return;

        Instance.RefreshCombatPopupInternal();
    }

    public static void HideCombatPopup()
    {
        if (Instance == null)
            return;

        Instance.HideCombatPopupInternal();
    }

    public static bool OpenNPCInteraction(
        NPCData npc)
    {
        if (npc == null)
            return false;

        MobileHUDManager manager =
            OpenOrCreate();

        if (manager == null)
            return false;

        manager.selectedNPC =
            npc;

        manager.ShowScreen(
            UIScreenType.NPCInteraction
        );

        Debug.Log(
            $"NPC opened: {npc.ID}"
        );

        return true;
    }

    public static bool OpenShopFromNPC(
        NPCData npc)
    {
        MobileHUDManager manager =
            OpenOrCreate();

        if (manager == null)
            return false;

        manager.activeShopNPC =
            npc;

        manager.activeShopLocation =
            null;

        manager.activeShopData =
            npc != null
                ? npc.ShopData
                : null;

        manager.currentShopTab =
            MobileShopTab.Buy;

        manager.ShowScreen(
            UIScreenType.Shop
        );

        if (npc != null)
        {
            Debug.Log(
                $"Shop opened from NPC: {npc.ID}"
            );
        }

        return true;
    }

    public static bool OpenLocation(
        CityLocationData location)
    {
        if (location == null)
            return false;

        MobileHUDManager manager =
            OpenOrCreate();

        if (manager == null)
            return false;

        manager.selectedLocation =
            location;

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.EnterLocation,
                    location.LocationID,
                    1,
                    "LocationScreen"
                )
            );

        manager.ShowScreen(
            UIScreenType.Location
        );

        return true;
    }

    public static bool OpenCityService(
        CityServiceType service,
        CityLocationData location = null)
    {
        MobileHUDManager manager =
            OpenOrCreate();

        if (manager == null)
            return false;

        manager.OpenCityHUDService(
            service,
            location
        );

        return true;
    }

    public void ShowScreen(
        UIScreenType screenType)
    {
        currentScreen =
            screenType;

        gameObject.SetActive(true);

        RefreshAll();
    }

    public void RefreshAll()
    {
        if (TryUseSceneCityHUD())
        {
            return;
        }

        SetRuntimeRootVisible(true);
        UpdateTopHUD();
        BuildBottomNavigation();
        RefreshCurrentScreen();
    }

    private bool TryUseSceneCityHUD()
    {
        if (currentScreen != UIScreenType.City ||
            SceneManager.GetActiveScene().name !=
            SceneFlowManager.CitySceneName)
        {
            HideSceneCityHUD();
            return false;
        }

        sceneCityHUD =
            SceneCityHUDController.GetOrCreateFromScene();

        if (sceneCityHUD == null)
            return false;

        SetRuntimeRootVisible(false);
        sceneCityHUD.ShowAndRefresh();

        return true;
    }

    private void SetRuntimeRootVisible(
        bool visible)
    {
        if (root == null)
            return;

        if (root.gameObject.activeSelf != visible)
        {
            root.gameObject.SetActive(visible);
        }
    }

    private void HideSceneCityHUD()
    {
        if (sceneCityHUD == null &&
            SceneManager.GetActiveScene().name ==
            SceneFlowManager.CitySceneName)
        {
            sceneCityHUD =
                SceneCityHUDController.GetOrCreateFromScene();
        }

        if (sceneCityHUD != null)
        {
            sceneCityHUD.SetVisible(false);
        }
    }

    private void RefreshCurrentScreen()
    {
        if (contentRoot == null)
            return;

        ClearChildren(contentRoot);

        switch (currentScreen)
        {
            case UIScreenType.City:
                BuildCityScreen();
                break;

            case UIScreenType.Travel:
                BuildTravelScreen();
                break;

            case UIScreenType.ExplorationAreas:
                BuildExplorationAreaSelectionScreen();
                break;

            case UIScreenType.Exploration:
                BuildExplorationScreen();
                break;

            case UIScreenType.ExplorationEvent:
                BuildExplorationEventScreen();
                break;

            case UIScreenType.Combat:
                BuildCombatScreen();
                break;

            case UIScreenType.Character:
                BuildCharacterScreen();
                break;

            case UIScreenType.Inventory:
                BuildInventoryScreen();
                break;

            case UIScreenType.Equipment:
                BuildEquipmentScreen();
                break;

            case UIScreenType.AttributeLevelUp:
                BuildAttributeLevelUpScreen();
                break;

            case UIScreenType.CombatSettings:
                BuildCombatSettingsScreen();
                break;

            case UIScreenType.Party:
                BuildPartyScreen();
                break;

            case UIScreenType.Guild:
                BuildGuildScreen();
                break;

            case UIScreenType.GuildMembers:
                BuildGuildMembersScreen();
                break;

            case UIScreenType.Quests:
                BuildQuestScreen();
                break;

            case UIScreenType.Calendar:
                BuildCalendarScreen();
                break;

            case UIScreenType.SystemMenu:
                BuildSystemMenuScreen();
                break;

            case UIScreenType.Inn:
                BuildInnScreen();
                break;

            case UIScreenType.Shop:
                BuildShopScreen();
                break;

            case UIScreenType.QuestBoard:
                BuildQuestBoardScreen();
                break;

            case UIScreenType.Location:
                BuildLocationScreen();
                break;

            case UIScreenType.NPCInteraction:
                BuildNPCInteractionScreen();
                break;

            case UIScreenType.Gift:
                BuildGiftScreen();
                break;

            case UIScreenType.Dialogue:
                BuildDialogueScreen();
                break;
        }

        RebuildContentLayout();

        HighlightCurrentNavigation();
    }

    private void UpdateTopHUD()
    {
        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        if (topLocationText != null)
        {
            topLocationText.text =
                city != null
                    ? $"{city.DisplayName} - {city.Kingdom}"
                    : "Cidade desconhecida";
        }

        if (topDateText == null ||
            TimeManager.Instance == null)
        {
            return;
        }

        TimeData time =
            TimeManager
                .Instance
                .CurrentTime;

        topDateText.text =
            $"Dia {time.Day} / Mes {time.Month} / Ano {time.Year} - " +
            $"{TimeManager.Instance.GetCurrentSeasonDisplayName()} - " +
            TimeManager.Instance.GetCurrentTimeOfDayDisplayName();
    }

    private void BuildCityScreen()
    {
        CityData city =
            CityManager.Instance.CurrentCity;

        if (city == null)
        {
            AddTitle("Cidade nao encontrada");
            return;
        }

        AddTitle(city.DisplayName);
        AddBody(
            $"Reino: {city.Kingdom}\n\n{city.Description}"
        );

        AddBody("Locais");

        bool hasLocation =
            false;

        if (city.Locations != null)
        {
            foreach (CityLocationData location
                in city.Locations)
            {
                if (location == null ||
                    !location.ShowInCityScreen ||
                    !RequirementChecker
                        .AreRequirementsMet(
                            location.UnlockRequirements
                        ))
                {
                    continue;
                }

                hasLocation =
                    true;

                AddCard(
                    location.DisplayName,
                    location.Description
                );

                CityLocationData capturedLocation =
                    location;

                AddButton(
                    $"Entrar: {location.DisplayName}",
                    () => OpenLocation(capturedLocation)
                );
            }
        }

        if (!hasLocation)
        {
            AddBody("Nenhum local disponivel.");
        }

        if (city.Services != null &&
            city.Services.Count > 0)
        {
            bool hasVisibleService =
                false;

            foreach (CityServiceType service
                in city.Services)
            {
                if (service == CityServiceType.None ||
                    service == CityServiceType.Travel)
                {
                    continue;
                }

                if (!hasVisibleService)
                {
                    AddBody("Servicos diretos");
                    hasVisibleService = true;
                }

                AddButton(
                    GetServiceLabel(service),
                    () => OpenService(service)
                );
            }

            if (!hasVisibleService)
            {
                AddBody("Nenhum servico direto disponivel.");
            }
        }
        else
        {
            AddBody("Nenhum servico direto disponivel.");
        }

        List<NPCData> npcs =
            NPCManager.Instance != null
                ? NPCManager
                    .Instance
                    .GetPublicNPCsInCurrentCity()
                : new List<NPCData>();

        AddBody("Pessoas na cidade");

        if (npcs.Count == 0)
        {
            AddBody("Nenhum personagem disponivel.");
            return;
        }

        foreach (NPCData npc
            in npcs)
        {
            if (npc == null)
                continue;

            string description =
                !string.IsNullOrEmpty(npc.Description)
                    ? npc.Description
                    : "Personagem disponivel nesta cidade.";

            AddCard(
                npc.DisplayName,
                description
            );

            NPCData capturedNPC =
                npc;

            AddButton(
                $"Falar com {npc.DisplayName}",
                () =>
                {
                    selectedLocation = null;
                    OpenNPCInteraction(capturedNPC);
                }
            );
        }
    }

    private void BuildTravelScreen()
    {
        AddTitle("Viagem");

        if (TravelManager.Instance.IsTraveling)
        {
            BuildTravelProgress();
            return;
        }

        AddButton(
            "Abrir mapa",
            () =>
            {
                SceneFlowManager
                    .GetOrCreate()
                    .ShowWorldMap();
            }
        );

        CityData city =
            CityManager.Instance.CurrentCity;

        AddBody(
            city != null
                ? $"Partindo de {city.DisplayName}."
                : "Cidade atual nao encontrada."
        );

        AddBody("Rotas de viagem");

        bool hasRoutes =
            city != null &&
            city.Connections != null &&
            city.Connections.Count > 0;

        if (!hasRoutes)
        {
            AddBody(
                "Nenhuma rota disponivel."
            );
        }

        if (hasRoutes)
        {
            foreach (CityConnection connection
                in city.Connections)
            {
                if (connection == null ||
                    connection.ConnectedCity == null)
                {
                    continue;
                }

                CityData destination =
                    connection.ConnectedCity;

                string description =
                    !string.IsNullOrEmpty(
                        connection.RouteDescription)
                        ? connection.RouteDescription
                        : destination.Description;

                AddCard(
                    destination.DisplayName,
                    $"{description}\nDuracao: {connection.TravelHours}h"
                );

                AddButton(
                    $"Viajar para {destination.DisplayName}",
                    () =>
                    {
                        TravelManager
                            .Instance
                            .TravelTo(destination);

                        ShowScreen(UIScreenType.Travel);
                    }
                );
            }
        }

        AddExplorationAreaSection();
    }

    private void AddExplorationAreaSection()
    {
        AddBody("Areas de exploracao");

        List<ExplorationAreaData> areas =
            ExplorationManager
                .GetOrCreate()
                .GetAreasForCurrentCity();

        if (areas.Count == 0)
        {
            AddBody(
                "Nenhuma area de exploracao disponivel."
            );

            return;
        }

        foreach (ExplorationAreaData area
            in areas)
        {
            if (area == null)
                continue;

            bool canEnter =
                ExplorationManager
                    .GetOrCreate()
                    .CanEnterArea(area);

            AddCard(
                area.DisplayName,
                $"{area.Description}\n" +
                $"Tempo ate a area: {Mathf.Max(0, area.TravelTimeCostInPeriods)} periodo(s)\n" +
                $"Nivel recomendado: {Mathf.Max(1, area.RecommendedLevel)}"
            );

            ExplorationAreaData capturedArea =
                area;

            AddButton(
                canEnter
                    ? $"Explorar {area.DisplayName}"
                    : "Area bloqueada",
                () =>
                {
                    if (!ExplorationManager
                        .GetOrCreate()
                        .StartExploration(capturedArea))
                    {
                        return;
                    }

                    ShowScreen(
                        UIScreenType.Exploration
                    );
                },
                canEnter
                    ? new Color(0.22f, 0.18f, 0.12f, 1f)
                    : new Color(0.1f, 0.1f, 0.1f, 1f)
            );
        }
    }

    private void BuildTravelProgress()
    {
        TravelSession travel =
            TravelManager
                .Instance
                .CurrentTravel;

        CityData origin =
            DatabaseManager
                .Instance
                .GetData<CityData>(
                    travel.OriginCityID
                );

        CityData destination =
            DatabaseManager
                .Instance
                .GetData<CityData>(
                    travel.DestinationCityID
                );

        string route =
            origin != null &&
            destination != null
                ? $"{origin.DisplayName} -> {destination.DisplayName}"
                : "Rota em andamento";

        AddBody(
            $"{route}\nTempo restante: {travel.RemainingHours}h de {travel.TotalHours}h"
        );

        AddButton(
            "Continuar viagem",
            () =>
            {
                TravelManager
                    .Instance
                    .ContinueTravel();

                ShowScreen(UIScreenType.Travel);
            }
        );
    }

    private void BuildExplorationAreaSelectionScreen()
    {
        AddTitle("Explorar");

        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        AddBody(
            city != null
                ? $"Areas proximas de {city.DisplayName}."
                : "Cidade atual nao encontrada."
        );

        List<ExplorationAreaData> areas =
            ExplorationManager
                .GetOrCreate()
                .GetAreasForCurrentCity();

        if (areas.Count == 0)
        {
            AddBody(
                "Nenhuma area de exploracao disponivel."
            );
        }

        foreach (ExplorationAreaData area
            in areas)
        {
            if (area == null)
                continue;

            bool canEnter =
                ExplorationManager
                    .GetOrCreate()
                    .CanEnterArea(area);

            AddCard(
                area.DisplayName,
                $"{area.Description}\n" +
                $"Tempo ate a area: {Mathf.Max(0, area.TravelTimeCostInPeriods)} periodo(s)\n" +
                $"Nivel recomendado: {Mathf.Max(1, area.RecommendedLevel)}"
            );

            ExplorationAreaData capturedArea =
                area;

            AddButton(
                canEnter
                    ? $"Explorar {area.DisplayName}"
                    : "Area bloqueada",
                () =>
                {
                    if (!ExplorationManager
                        .GetOrCreate()
                        .StartExploration(capturedArea))
                    {
                        return;
                    }

                    ShowScreen(
                        UIScreenType.Exploration
                    );
                },
                canEnter
                    ? new Color(0.22f, 0.18f, 0.12f, 1f)
                    : new Color(0.1f, 0.1f, 0.1f, 1f)
            );
        }

        AddButton(
            "Voltar para viagem",
            () => ShowScreen(UIScreenType.Travel)
        );
    }

    private void BuildExplorationScreen()
    {
        ExplorationManager exploration =
            ExplorationManager.GetOrCreate();

        ExplorationAreaData area =
            exploration.CurrentArea;

        if (!exploration.State.IsExploring ||
            area == null)
        {
            AddTitle("Exploracao");
            AddBody("Nenhuma exploracao em andamento.");
            AddButton(
                "Escolher area",
                () => ShowScreen(
                    UIScreenType.ExplorationAreas
                )
            );
            return;
        }

        if (exploration.ActiveEvent != null)
        {
            BuildExplorationEventScreen();
            return;
        }

        AddTitle(area.DisplayName);
        AddSpriteFrame(area.AreaSprite, 150f);
        AddBody(
            $"{area.Description}\n\n" +
            $"Periodo: {TimeManager.Instance.GetCurrentTimeOfDayDisplayName()}\n" +
            $"Passos: {exploration.State.ExploredSteps}/{exploration.State.MaxStepsBeforeReturn}"
        );

        if (!string.IsNullOrEmpty(
            exploration.LastResultText))
        {
            AddCard(
                "Ultimo acontecimento",
                exploration.LastResultText
            );
        }

        AddButton(
            "Explorar",
            () =>
            {
                exploration.Explore();

                ShowScreen(
                    exploration.ActiveEvent != null
                        ? UIScreenType.ExplorationEvent
                        : UIScreenType.Exploration
                );
            }
        );

        AddExplorationResources(area);

        AddButton(
            "Esperar",
            () =>
            {
                TimeManager
                    .Instance
                    .AdvancePeriod(
                        "Espera na exploracao"
                    );

                ShowScreen(UIScreenType.Exploration);
            }
        );

        AddButton(
            "Retornar para cidade",
            () =>
            {
                exploration.ReturnToOriginCity();
                ShowScreen(UIScreenType.City);
            },
            new Color(0.16f, 0.12f, 0.1f, 1f)
        );
    }

    private void AddExplorationResources(
        ExplorationAreaData area)
    {
        if (area.Resources == null ||
            area.Resources.Count == 0)
        {
            return;
        }

        AddBody("Recursos");

        foreach (ResourceNodeData resource
            in area.Resources)
        {
            if (resource == null)
                continue;

            bool collectedToday =
                resource.OncePerDay &&
                ExplorationManager
                    .GetOrCreate()
                    .WasResourceCollectedToday(
                        area.ID,
                        resource.ResourceNodeID
                    );

            AddCard(
                resource.DisplayName,
                $"{resource.Description}\n" +
                $"Tempo: {Mathf.Max(1, resource.TimeCostInPeriods)} periodo(s)" +
                (collectedToday
                    ? "\nJa coletado hoje."
                    : string.Empty)
            );

            ResourceNodeData capturedResource =
                resource;

            AddButton(
                collectedToday
                    ? "Recurso indisponivel"
                    : $"Coletar {resource.DisplayName}",
                () =>
                {
                    if (collectedToday)
                    {
                        GameFeedbackUI.ShowNotification(
                            "Este recurso ja foi coletado hoje."
                        );
                        return;
                    }

                    ExplorationManager
                        .GetOrCreate()
                        .CollectResource(capturedResource);

                    ShowScreen(UIScreenType.Exploration);
                },
                collectedToday
                    ? new Color(0.1f, 0.1f, 0.1f, 1f)
                    : new Color(0.18f, 0.18f, 0.12f, 1f)
            );
        }
    }

    private void BuildExplorationEventScreen()
    {
        ExplorationManager exploration =
            ExplorationManager.GetOrCreate();

        ExplorationEventData eventData =
            exploration.ActiveEvent;

        if (eventData == null)
        {
            AddTitle("Evento");
            AddBody("Nenhum evento ativo.");
            AddButton(
                "Voltar",
                () => ShowScreen(UIScreenType.Exploration)
            );
            return;
        }

        AddTitle(eventData.DisplayName);
        AddBody(eventData.Description);

        if (eventData.Enemy != null)
        {
            AddSpriteFrame(
                eventData.Enemy.Sprite,
                120f
            );

            AddCard(
                eventData.Enemy.DisplayName,
                eventData.Enemy.Description
            );
        }

        if (eventData.Choices == null ||
            eventData.Choices.Count == 0)
        {
            AddButton(
                "Continuar",
                () =>
                {
                    ExplorationEventChoiceData choice =
                        new ExplorationEventChoiceData
                        {
                            ChoiceText = "Continuar",
                            ResultText =
                                "O evento foi resolvido.",
                            TimeCostInPeriods = 1
                        };

                    exploration
                        .ResolveActiveEventChoice(choice);

                    ShowScreen(UIScreenType.Exploration);
                }
            );

            return;
        }

        foreach (ExplorationEventChoiceData choice
            in eventData.Choices)
        {
            if (choice == null)
                continue;

            bool meetsRequirements =
                RequirementChecker
                    .AreRequirementsMet(
                        choice.Requirements
                    );

            ExplorationEventChoiceData capturedChoice =
                choice;

            AddButton(
                meetsRequirements
                    ? choice.ChoiceText
                    : $"{choice.ChoiceText} (Bloqueado)",
                () =>
                {
                    if (!meetsRequirements)
                    {
                        GameFeedbackUI.ShowNotification(
                            "Requisito nao cumprido."
                        );

                        return;
                    }

                    exploration
                        .ResolveActiveEventChoice(
                            capturedChoice
                        );

                    if (CombatManager.Instance != null &&
                        CombatManager.Instance.IsInCombat)
                    {
                        ShowScreen(
                            exploration.State.IsExploring
                                ? UIScreenType.Exploration
                                : UIScreenType.City
                        );

                        TryShowCombatPopup();
                        return;
                    }

                    ShowScreen(
                        exploration.State.IsExploring
                            ? UIScreenType.Exploration
                            : UIScreenType.City
                    );
                }
            );
        }
    }

    private void BuildGuildScreen()
    {
        AddTitle("Guilda dos Gatos Negros");

        EnsureGuildManager();

        GuildStateData guild =
            GuildManager
                .Instance
                .Guild;

        guild.EnsureRuntimeDefaults();

        int nextReputation =
            GuildManager
                .Instance
                .GetReputationRequiredForNextLevel();

        AddBody(
            $"Nivel: {guild.Level}\n" +
            $"Reputacao: {guild.Reputation}/{nextReputation}\n" +
            $"Missoes concluidas: {guild.CompletedGuildMissions}\n" +
            $"Membros recrutados: {guild.RecruitedMemberIDs.Count}\n\n" +
            "Estado atual: a guilda esta em decadencia e precisa recuperar forca."
        );

        CompleteMeetGuildQuestIfNeeded();

        AddButton(
            "Quadro da guilda",
            () =>
            {
                guildQuestBoardMode = true;
                ShowScreen(UIScreenType.QuestBoard);
            }
        );

        AddButton(
            "Membros",
            () => ShowScreen(UIScreenType.GuildMembers)
        );

        AddButton(
            "Grupo",
            () => ShowScreen(UIScreenType.Party)
        );

        AddButton(
            "Melhorias",
            () => GameFeedbackUI.ShowNotification(
                "Melhorias da guilda serao expandidas depois."
            )
        );

        AddButton(
            selectedLocation != null
                ? "Voltar ao local"
                : "Voltar para cidade",
            () => ShowScreen(
                selectedLocation != null
                    ? UIScreenType.Location
                    : UIScreenType.City
            )
        );
    }

    private void BuildQuestScreen()
    {
        AddTitle("Missoes");

        List<QuestStateData> questStates =
            SaveManager
                .Instance
                .CurrentSave
                .QuestStates;

        if (questStates == null ||
            questStates.Count == 0)
        {
            AddBody(
                "Nenhuma missao registrada."
            );

            return;
        }

        foreach (QuestStateData state
            in questStates)
        {
            if (state == null ||
                state.Status == QuestStatus.NotStarted)
            {
                continue;
            }

            QuestData quest =
                DatabaseManager
                    .Instance
                    .GetData<QuestData>(
                        state.QuestID
                    );

            string title =
                quest != null
                    ? quest.DisplayName
                    : state.QuestID;

            string description =
                quest != null
                    ? quest.Description
                    : "Sem descricao.";

            QuestStepData currentStep =
                QuestManager.Instance != null
                    ? QuestManager
                        .Instance
                        .GetCurrentStep(state.QuestID)
                    : null;

            string stepText =
                currentStep != null
                    ? $"\n\nEtapa atual: {currentStep.Title}\n{currentStep.Description}\nProgresso: {QuestManager.Instance.GetCurrentStepProgress(state.QuestID)}/{Mathf.Max(1, currentStep.RequiredAmount)}"
                    : string.Empty;

            string rewardText =
                quest != null
                    ? $"\n\nRecompensa:\n{RewardManager.BuildRewardSummary(quest.Rewards)}"
                    : string.Empty;

            AddCard(
                title,
                $"{description}\nEstado: {state.Status}{stepText}{rewardText}"
            );
        }
    }

    private void BuildGuildMembersScreen()
    {
        AddTitle("Membros da Guilda");

        EnsureGuildManager();

        GuildStateData guild =
            GuildManager.Instance.Guild;

        guild.EnsureRuntimeDefaults();

        CompanionManager companionManager =
            CompanionManager.GetOrCreate();

        List<CompanionState> recruitedCompanions =
            companionManager.GetRecruitedCompanions();

        if (recruitedCompanions.Count == 0)
        {
            GuildManager
                .Instance
                .EnsurePlaceholderMember();
        }

        foreach (GuildMemberState member
            in guild.Members)
        {
            if (member == null)
                continue;

            member.EnsureRuntimeDefaults();

            CompanionState companionState =
                companionManager.GetCompanionState(
                    member.MemberID,
                    false
                );

            bool isCompanion =
                companionState != null &&
                companionState.IsRecruited;

            string memberName =
                isCompanion
                    ? companionManager.GetCompanionDisplayName(
                        member.MemberID
                    )
                    : member.MemberID ==
                        "guild_member_placeholder"
                        ? "Aventureiro da Guilda"
                        : member.MemberID;

            string status =
                isCompanion &&
                companionState.IsInActiveParty
                    ? "Na party"
                    : string.IsNullOrEmpty(
                        member.CurrentTaskID)
                        ? "Disponivel"
                        : $"Em tarefa: {member.CurrentTaskID}\nRetorna em {Mathf.Max(0, member.RemainingTaskPeriods)} periodos";

            AddCard(
                memberName,
                status
            );

            if (string.IsNullOrEmpty(
                member.CurrentTaskID) &&
                member.IsAvailableForGuildTasks &&
                (!isCompanion ||
                    companionManager.CanCompanionDoGuildTasks(
                        member.MemberID
                    )))
            {
                string capturedMemberID =
                    member.MemberID;

                AddButton(
                    "Enviar para tarefa simples",
                    () =>
                    {
                        GuildManager
                            .Instance
                            .SendMemberToTask(
                                capturedMemberID,
                                "simple_guild_errand"
                            );

                        ShowScreen(
                            UIScreenType.GuildMembers
                        );
                    }
                );
            }

            if (isCompanion &&
                !companionState.IsInActiveParty &&
                string.IsNullOrEmpty(
                    companionState.CurrentGuildTaskID))
            {
                string capturedCompanionID =
                    companionState.CompanionID;

                AddButton(
                    $"Adicionar ao grupo: {memberName}",
                    () =>
                    {
                        companionManager.AddToParty(
                            capturedCompanionID
                        );

                        ShowScreen(
                            UIScreenType.GuildMembers
                        );
                    }
                );
            }

            if (isCompanion &&
                companionState.IsInActiveParty)
            {
                string capturedCompanionID =
                    companionState.CompanionID;

                AddButton(
                    $"Remover do grupo: {memberName}",
                    () =>
                    {
                        companionManager.RemoveFromParty(
                            capturedCompanionID
                        );

                        ShowScreen(
                            UIScreenType.GuildMembers
                        );
                    }
                );
            }
        }

        AddButton(
            "Gerenciar grupo",
            () => ShowScreen(UIScreenType.Party)
        );

        AddButton(
            "Voltar para guilda",
            () => ShowScreen(UIScreenType.Guild)
        );
    }

    private void BuildCalendarScreen()
    {
        AddTitle("Calendario");

        TimeData time =
            TimeManager
                .Instance
                .CurrentTime;

        AddBody(
            $"Ano atual: {time.Year}\n" +
            $"Mes atual: {time.Month}\n" +
            $"Dia atual: {time.Day}\n" +
            $"Hora: {time.Hour:00}:00\n" +
            $"Periodo: {TimeManager.Instance.GetCurrentTimeOfDayDisplayName()}\n" +
            $"Estacao: {TimeManager.Instance.GetCurrentSeasonDisplayName()}\n\n" +
            "O ano possui 8 meses.\n" +
            "A estacao muda a cada 2 meses."
        );

        AddButton(
            "Avancar periodo",
            () =>
            {
                TimeManager
                    .Instance
                    .AdvancePeriod(
                        "Acao manual"
                    );

                GameFeedbackUI.ShowNotification(
                    $"Periodo atual: {TimeManager.Instance.GetCurrentTimeOfDayDisplayName()}"
                );

                RefreshAll();
            }
        );
    }

    private void BuildSystemMenuScreen()
    {
        AddTitle("Menu");
        AddBody(
            $"Slot atual: {SaveManager.Instance.CurrentSaveSlot}\n" +
            $"Moedas: {WalletManager.GetOrCreate().GetCoins()}"
        );

        AddButton(
            "Salvar jogo",
            () =>
            {
                if (CombatManager.Instance != null &&
                    CombatManager.Instance.IsInCombat)
                {
                    GameFeedbackUI.ShowNotification(
                        "Nao e possivel salvar durante o combate."
                    );

                    return;
                }

                SaveManager.Instance.SaveGame();
                GameFeedbackUI.ShowNotification(
                    "Jogo salvo."
                );
            }
        );

        AddButton(
            "Carregar jogo por slot",
            () =>
            {
                SceneFlowManager
                    .GetOrCreate()
                    .ShowLoadMenuFromGame();
            }
        );

        AddButton(
            "Voltar ao menu inicial",
            () =>
            {
                SaveManager.Instance.ReturnToMainMenu();
            },
            new Color(0.36f, 0.12f, 0.1f, 1f)
        );
    }

    private void BuildCharacterScreen()
    {
        AddTitle("Personagem");

        SaveData save =
            SaveManager
                .Instance
                .CurrentSave;

        PlayerData player =
            save.Player;

        PlayerStatsData stats =
            save.Stats;

        WalletManager wallet =
            WalletManager.GetOrCreate();

        RaceData race =
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetData<RaceData>(
                        player.RaceID
                    )
                : null;

        StartingArchetypeData archetype =
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetData<StartingArchetypeData>(
                        player.ArchetypeID
                    )
                : null;

        CharacterManager character =
            CharacterManager.Instance;

        AddBody(
            $"Nome: {player.PlayerName}\n" +
            $"Raca: {(race != null ? race.DisplayName : player.RaceID)}\n" +
            $"Arquetipo: {(archetype != null ? archetype.DisplayName : player.ArchetypeID)}\n" +
            $"Elemento: {GetElementLabel(player.Elements.PrimaryElement)}\n" +
            $"Preset visual: {player.BodyPresetID}\n" +
            $"Retrato: {player.PortraitID}\n" +
            $"Cidade atual: {save.Location.CurrentCityID}\n\n" +
            $"Moedas: {wallet.GetCoins()}\n" +
            $"Nivel: {stats.Level}\n" +
            $"Experiencia: {stats.Experience}/{stats.ExperienceToNextLevel}\n" +
            $"Pontos de atributo: {stats.UnspentAttributePoints}\n" +
            $"Grupo ativo: jogador + {save.Party.ActivePartyMemberIDs.Count}/{save.Party.MaxPartySize}\n" +
            $"Vida: {stats.CurrentHP}/{(character != null ? character.MaxHP : stats.CurrentHP)}\n" +
            $"Energia: {stats.CurrentStamina}/{(character != null ? character.MaxStamina : stats.CurrentStamina)}"
        );

        AddCard(
            "Atributos",
            BuildStatLine(StatType.Strength, "Forca") + "\n" +
            BuildStatLine(StatType.Dexterity, "Destreza") + "\n" +
            BuildStatLine(StatType.Intelligence, "Inteligencia") + "\n" +
            BuildStatLine(StatType.Faith, "Fe") + "\n" +
            BuildStatLine(StatType.Vitality, "Vitalidade") + "\n" +
            BuildStatLine(StatType.Charisma, "Carisma")
        );

        AddButton(
            stats.UnspentAttributePoints > 0
                ? "Distribuir pontos"
                : "Distribuir pontos (sem pontos)",
            () => ShowScreen(UIScreenType.AttributeLevelUp),
            stats.UnspentAttributePoints > 0
                ? new Color(0.22f, 0.18f, 0.12f, 1f)
                : new Color(0.1f, 0.1f, 0.1f, 1f)
        );

        AddButton(
            "Configurar combate",
            () => ShowScreen(UIScreenType.CombatSettings)
        );

        AddButton(
            "Gerenciar grupo",
            () => ShowScreen(UIScreenType.Party)
        );

        AddButton(
            "Abrir inventario",
            () => ShowScreen(UIScreenType.Inventory)
        );

        AddButton(
            "Abrir equipamentos",
            () => ShowScreen(UIScreenType.Equipment)
        );
    }

    private void BuildAttributeLevelUpScreen()
    {
        AddTitle("Evoluir");

        PlayerStatsData stats =
            SaveManager
                .Instance
                .CurrentSave
                .Stats;

        AddBody(
            $"Pontos disponiveis: {stats.UnspentAttributePoints}\n" +
            "Cada ponto aumenta 1 atributo base."
        );

        AddAttributeSpendButton(StatType.Strength, "Forca");
        AddAttributeSpendButton(StatType.Dexterity, "Destreza");
        AddAttributeSpendButton(StatType.Intelligence, "Inteligencia");
        AddAttributeSpendButton(StatType.Faith, "Fe");
        AddAttributeSpendButton(StatType.Vitality, "Vitalidade");
        AddAttributeSpendButton(StatType.Charisma, "Carisma");

        AddButton(
            "Voltar ao personagem",
            () => ShowScreen(UIScreenType.Character)
        );
    }

    private void AddAttributeSpendButton(
        StatType statType,
        string label)
    {
        PlayerStatsData stats =
            SaveManager
                .Instance
                .CurrentSave
                .Stats;

        AddCard(
            label,
            $"Base: {stats.GetStat(statType)}\n" +
            $"Total: {CharacterManager.GetOrCreate().GetTotalStat(statType)}"
        );

        AddButton(
            stats.UnspentAttributePoints > 0
                ? $"+1 {label}"
                : "Sem pontos disponiveis",
            () =>
            {
                if (stats.UnspentAttributePoints <= 0)
                {
                    GameFeedbackUI.ShowNotification(
                        "Nenhum ponto de atributo disponivel."
                    );

                    return;
                }

                CharacterManager
                    .GetOrCreate()
                    .SpendAttributePoint(statType);

                ShowScreen(UIScreenType.AttributeLevelUp);
            },
            stats.UnspentAttributePoints > 0
                ? new Color(0.22f, 0.18f, 0.12f, 1f)
                : new Color(0.1f, 0.1f, 0.1f, 1f)
        );
    }

    private void BuildCombatSettingsScreen()
    {
        AddTitle("Combate automatico");

        PlayerData player =
            SaveManager
                .Instance
                .CurrentSave
                .Player;

        player.EnsureRuntimeDefaults();

        AutoCombatSettings settings =
            player.AutoCombat;

        AddBody(
            $"Habilidades automaticas: {(settings.AllowAutoSkills ? "Ativas" : "Inativas")}\n" +
            $"Ataque secundario: {(settings.AllowOffHandAttack ? "Ativo" : "Inativo")}"
        );

        AddButton(
            settings.AllowAutoSkills
                ? "Desativar habilidades"
                : "Ativar habilidades",
            () =>
            {
                settings.AllowAutoSkills =
                    !settings.AllowAutoSkills;

                SaveManager.Instance.SaveGame();
                ShowScreen(UIScreenType.CombatSettings);
            }
        );

        AddButton(
            settings.AllowOffHandAttack
                ? "Desativar ataque secundario"
                : "Ativar ataque secundario",
            () =>
            {
                settings.AllowOffHandAttack =
                    !settings.AllowOffHandAttack;

                SaveManager.Instance.SaveGame();
                ShowScreen(UIScreenType.CombatSettings);
            }
        );

        AddBody("Habilidades");

        if (player.KnownSkillIDs == null ||
            player.KnownSkillIDs.Count == 0)
        {
            AddBody("Nenhuma habilidade conhecida.");
        }
        else
        {
            foreach (string skillID
                in player.KnownSkillIDs)
            {
                SkillData skill =
                    DatabaseManager
                        .Instance
                        .GetData<SkillData>(skillID);

                string skillName =
                    skill != null
                        ? skill.DisplayName
                        : skillID;

                bool enabled =
                    settings.EnabledSkillIDs != null &&
                    settings.EnabledSkillIDs.Contains(skillID);

                AddCard(
                    skillName,
                    $"Estado: {(enabled ? "Habilitada" : "Desabilitada")}\n" +
                    (skill != null
                        ? $"Carga: {skill.ChargeTime:0.#}s | Energia: {skill.EnergyCost}"
                        : string.Empty)
                );

                string capturedSkillID =
                    skillID;

                AddButton(
                    enabled
                        ? $"Desabilitar {skillName}"
                        : $"Habilitar {skillName}",
                    () =>
                    {
                        settings.SetSkillEnabled(
                            capturedSkillID,
                            !enabled
                        );

                        SaveManager.Instance.SaveGame();
                        ShowScreen(UIScreenType.CombatSettings);
                    }
                );
            }
        }

        AddButton(
            "Voltar ao personagem",
            () => ShowScreen(UIScreenType.Character)
        );
    }

    private void BuildPartyScreen()
    {
        AddTitle("Grupo");

        SaveData save =
            SaveManager
                .Instance
                .CurrentSave;

        save.Party.EnsureRuntimeDefaults();

        CompanionManager companionManager =
            CompanionManager.GetOrCreate();

        AddCard(
            save.Player.PlayerName,
            "Protagonista\nSempre participa do combate."
        );

        AddBody(
            $"Companheiros ativos: {save.Party.ActivePartyMemberIDs.Count}/{save.Party.MaxPartySize}"
        );

        List<CompanionState> activeCompanions =
            companionManager.GetActivePartyCompanions();

        if (activeCompanions.Count == 0)
        {
            AddBody(
                "Nenhum companheiro no grupo ativo."
            );
        }

        foreach (CompanionState companionState
            in activeCompanions)
        {
            AddCompanionPartyEntry(
                companionState,
                isActiveEntry: true
            );
        }

        AddBody("Disponiveis na guilda");

        List<CompanionState> recruitedCompanions =
            companionManager.GetRecruitedCompanions();

        bool hasAvailable =
            false;

        foreach (CompanionState companionState
            in recruitedCompanions)
        {
            if (companionState == null ||
                companionState.IsInActiveParty)
            {
                continue;
            }

            hasAvailable = true;

            AddCompanionPartyEntry(
                companionState,
                isActiveEntry: false
            );
        }

        if (!hasAvailable)
        {
            AddBody(
                "Nenhum companheiro disponivel fora do grupo."
            );
        }

        AddButton(
            "Voltar para guilda",
            () => ShowScreen(UIScreenType.Guild)
        );
    }

    private void AddCompanionPartyEntry(
        CompanionState companionState,
        bool isActiveEntry)
    {
        CompanionManager companionManager =
            CompanionManager.GetOrCreate();

        string companionName =
            companionManager.GetCompanionDisplayName(
                companionState.CompanionID
            );

        CompanionData companionData =
            companionManager.GetCompanionById(
                companionState.CompanionID
            );

        string status =
            !string.IsNullOrEmpty(
                companionState.CurrentGuildTaskID)
                ? $"Em tarefa: {companionState.CurrentGuildTaskID}"
                : companionState.IsUnavailable
                    ? "Indisponivel"
                    : companionState.IsInActiveParty
                        ? "Na party"
                        : "Disponivel";

        AddCard(
            companionName,
            $"Nivel: {companionState.Level}\n" +
            $"Raca: {GetCompanionRaceLabel(companionData)}\n" +
            $"Arquetipo: {GetCompanionArchetypeLabel(companionData)}\n" +
            $"Vida: {companionState.CurrentVitals.CurrentHealth}/{companionState.CurrentVitals.MaxHealth}\n" +
            $"Energia: {companionState.CurrentVitals.CurrentEnergy}/{companionState.CurrentVitals.MaxEnergy}\n" +
            $"Estado: {status}"
        );

        string capturedCompanionID =
            companionState.CompanionID;

        if (isActiveEntry)
        {
            AddButton(
                $"Remover {companionName}",
                () =>
                {
                    companionManager.RemoveFromParty(
                        capturedCompanionID
                    );

                    ShowScreen(UIScreenType.Party);
                }
            );

            return;
        }

        bool canAdd =
            string.IsNullOrEmpty(
                companionState.CurrentGuildTaskID
            ) &&
            !companionState.IsUnavailable;

        AddButton(
            canAdd
                ? $"Adicionar {companionName}"
                : $"{companionName} indisponivel",
            () =>
            {
                if (!canAdd)
                {
                    GameFeedbackUI.ShowNotification(
                        "Companheiro indisponivel."
                    );
                    return;
                }

                companionManager.AddToParty(
                    capturedCompanionID
                );

                ShowScreen(UIScreenType.Party);
            },
            canAdd
                ? new Color(0.22f, 0.18f, 0.12f, 1f)
                : new Color(0.1f, 0.1f, 0.1f, 1f)
        );
    }

    private void BuildInventoryScreen()
    {
        AddTitle("Inventario");
        AddBody(
            $"Moedas: {WalletManager.GetOrCreate().GetCoins()}"
        );

        List<InventoryItem> inventory =
            SaveManager
                .Instance
                .CurrentSave
                .Inventory;

        if (inventory == null ||
            inventory.Count == 0)
        {
            AddBody(
                "Inventario vazio."
            );

            AddButton(
                "Voltar ao personagem",
                () => ShowScreen(UIScreenType.Character)
            );

            return;
        }

        foreach (InventoryItem stack
            in inventory)
        {
            if (stack == null ||
                string.IsNullOrEmpty(stack.ItemID) ||
                stack.Quantity <= 0)
            {
                continue;
            }

            ItemData itemData =
                DatabaseManager.Instance != null
                    ? DatabaseManager
                        .Instance
                        .GetData<ItemData>(
                            stack.ItemID
                        )
                    : null;

            string itemName =
                itemData != null &&
                !string.IsNullOrEmpty(itemData.DisplayName)
                    ? itemData.DisplayName
                    : stack.ItemID;

            string description =
                itemData != null &&
                !string.IsNullOrEmpty(itemData.Description)
                    ? itemData.Description
                    : "Descricao indisponivel.";

            AddCard(
                itemName,
                $"{description}\n" +
                $"Tipo: {GetItemTypeLabel(itemData)}\n" +
                $"Quantidade: {stack.Quantity}"
            );

            if (itemData != null &&
                itemData.IsConsumable)
            {
                string itemID =
                    stack.ItemID;

                AddButton(
                    $"Usar {itemName}",
                    () =>
                    {
                        InventoryManager
                            .Instance
                            .UseItem(itemID);

                        ShowScreen(UIScreenType.Inventory);
                    }
                );
            }

            if (itemData != null &&
                itemData.IsEquipment)
            {
                string itemID =
                    stack.ItemID;

                AddButton(
                    $"Equipar {itemName}",
                    () =>
                    {
                        EquipmentManager
                            .GetOrCreate()
                            .EquipItem(itemID);

                        ShowScreen(UIScreenType.Equipment);
                    }
                );
            }
        }

        AddButton(
            "Voltar ao personagem",
            () => ShowScreen(UIScreenType.Character)
        );
    }

    private void BuildEquipmentScreen()
    {
        AddTitle("Equipamentos");

        PlayerData player =
            SaveManager
                .Instance
                .CurrentSave
                .Player;

        AddBody(
            $"{player.PlayerName}\n" +
            "Itens equipados e bonus atuais."
        );

        foreach (EquipmentSlot slot
            in EquipmentManager.GetEquipmentSlots())
        {
            string itemID =
                EquipmentManager
                    .GetOrCreate()
                    .GetEquippedItem(slot);

            ItemData itemData =
                !string.IsNullOrEmpty(itemID)
                    ? DatabaseManager
                        .Instance
                        .GetItemById(itemID)
                    : null;

            string equippedName =
                itemData != null
                    ? itemData.DisplayName
                    : slot == EquipmentSlot.OffHand &&
                        EquipmentManager
                            .GetOrCreate()
                            .IsOffHandBlocked()
                        ? "Bloqueado por item de duas maos"
                        : "Vazio";

            AddCard(
                GetSlotLabel(slot),
                itemData != null
                    ? $"{equippedName}\n{GetEquipmentSummary(itemData)}"
                    : equippedName
            );

            if (!string.IsNullOrEmpty(itemID))
            {
                EquipmentSlot capturedSlot =
                    slot;

                AddButton(
                    $"Desequipar {GetSlotLabel(slot)}",
                    () =>
                    {
                        EquipmentManager
                            .GetOrCreate()
                            .UnequipSlot(capturedSlot);

                        ShowScreen(UIScreenType.Equipment);
                    }
                );
            }
        }

        AddCard(
            "Status totais",
            BuildStatLine(StatType.Strength, "Forca") + "\n" +
            BuildStatLine(StatType.Dexterity, "Destreza") + "\n" +
            BuildStatLine(StatType.Intelligence, "Inteligencia") + "\n" +
            BuildStatLine(StatType.Faith, "Fe") + "\n" +
            BuildStatLine(StatType.Vitality, "Vitalidade") + "\n" +
            BuildStatLine(StatType.Charisma, "Carisma")
        );

        AddTitle("Itens equipaveis");

        bool foundEquipment =
            false;

        foreach (InventoryItem stack
            in SaveManager
                .Instance
                .CurrentSave
                .Inventory)
        {
            if (stack == null ||
                stack.Quantity <= 0)
            {
                continue;
            }

            ItemData itemData =
                DatabaseManager
                    .Instance
                    .GetItemById(stack.ItemID);

            if (itemData == null ||
                !itemData.IsEquipment)
            {
                continue;
            }

            foundEquipment =
                true;

            AddCard(
                itemData.DisplayName,
                $"Slot: {GetSlotLabel(itemData.EquipmentSlot)}\n" +
                $"Compatibilidade: {GetEquipmentCompatibilityText(itemData)}\n" +
                $"{GetEquipmentSummary(itemData)}"
            );

            string itemID =
                stack.ItemID;

            AddEquipButtonIfCompatible(
                itemData,
                itemID,
                EquipmentSlot.MainHand
            );

            AddEquipButtonIfCompatible(
                itemData,
                itemID,
                EquipmentSlot.OffHand
            );

            if (itemData.EquipmentSlot != EquipmentSlot.MainHand &&
                itemData.EquipmentSlot != EquipmentSlot.OffHand)
            {
                AddButton(
                    $"Equipar {itemData.DisplayName}",
                    () =>
                    {
                        EquipmentManager
                            .GetOrCreate()
                            .EquipItem(itemID);

                        ShowScreen(UIScreenType.Equipment);
                    }
                );
            }
        }

        if (!foundEquipment)
        {
            AddBody(
                "Nenhum equipamento no inventario."
            );
        }

        AddButton(
            "Voltar ao personagem",
            () => ShowScreen(UIScreenType.Character)
        );
    }

    private void BuildInnScreen()
    {
        AddTitle("Estalagem");
        AddBody(
            "Descanse em seguranca e deixe o tempo avancar."
        );

        AddButton(
            "Descansar ate amanha",
            () =>
            {
                TimeManager
                    .Instance
                    .RestUntilNextMorning(
                        "Descanso na estalagem"
                    );

                SaveManager.Instance.SaveGame();
                CharacterManager.Instance?.RestoreVitals();

                GameFeedbackUI.ShowNotification(
                    "Voce descansou ate a manha seguinte."
                );
                RefreshAll();
            }
        );

        AddButton(
            "Voltar",
            () => ShowScreen(UIScreenType.City)
        );
    }

    private void BuildQuestBoardScreen()
    {
        AddTitle(
            guildQuestBoardMode
                ? "Quadro da Guilda"
                : "Mural de Missoes"
        );

        if (guildQuestBoardMode)
        {
            AddQuestBoardEntry("guild_test_delivery");
            AddQuestBoardEntry("guild_gather_rough_stone");
            AddQuestBoardEntry("guild_defeat_wild_beast");
            AddQuestBoardEntry("recruit_guild_adventurer_test");
        }
        else
        {
            AddQuestBoardEntry(ReturnToLunarisQuestID);
            AddQuestBoardEntry(MeetBlackCatsGuildQuestID);
        }

        AddButton(
            guildQuestBoardMode
                ? "Voltar para guilda"
                : "Voltar",
            () =>
            {
                if (guildQuestBoardMode)
                {
                    guildQuestBoardMode = false;
                    ShowScreen(UIScreenType.Guild);
                    return;
                }

                ShowScreen(
                    selectedLocation != null
                        ? UIScreenType.Location
                        : UIScreenType.City
                );
            }
        );
    }

    private void AddQuestBoardEntry(
        string questId)
    {
        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    questId
                );

        if (quest == null)
            return;

        QuestStatus status =
            QuestManager
                .Instance
                .GetQuestState(quest.ID);

        AddCard(
            quest.DisplayName,
            $"{quest.Description}\nEstado: {status}" +
            (guildQuestBoardMode
                ? $"\nNivel da guilda: {quest.RequiredGuildLevel}"
                : string.Empty)
        );

        if (guildQuestBoardMode &&
            GuildManager.GetOrCreate().Guild.Level <
            quest.RequiredGuildLevel)
        {
            AddBody("Nivel da guilda insuficiente.");
            return;
        }

        if (!QuestManager
            .Instance
            .CanStartQuest(quest.ID))
        {
            return;
        }

        AddButton(
            $"Aceitar: {quest.DisplayName}",
            () =>
            {
                QuestManager
                    .Instance
                    .StartQuest(quest.ID);

                ShowScreen(UIScreenType.Quests);
            }
        );
    }

    private void BuildShopScreen()
    {
        ShopData shop =
            GetActiveShop();

        AddTitle(
            shop != null &&
            !string.IsNullOrEmpty(shop.DisplayName)
                ? shop.DisplayName
                : "Loja"
        );

        AddBody(
            $"Moedas: {WalletManager.GetOrCreate().GetCoins()}"
        );

        AddButton(
            "Comprar",
            () =>
            {
                currentShopTab =
                    MobileShopTab.Buy;

                ShowScreen(UIScreenType.Shop);
            },
            currentShopTab == MobileShopTab.Buy
                ? new Color(0.46f, 0.34f, 0.16f, 1f)
                : new Color(0.18f, 0.15f, 0.1f, 1f)
        );

        AddButton(
            "Vender",
            () =>
            {
                currentShopTab =
                    MobileShopTab.Sell;

                ShowScreen(UIScreenType.Shop);
            },
            currentShopTab == MobileShopTab.Sell
                ? new Color(0.46f, 0.34f, 0.16f, 1f)
                : new Color(0.18f, 0.15f, 0.1f, 1f)
        );

        if (currentShopTab == MobileShopTab.Sell)
        {
            BuildShopSellTab();
        }
        else
        {
            BuildShopBuyTab(shop);
        }

        AddButton(
            activeShopNPC != null
                ? "Voltar ao NPC"
                : activeShopLocation != null
                    ? "Voltar ao local"
                : "Voltar para cidade",
            () =>
            {
                if (activeShopNPC != null)
                {
                    ShowScreen(UIScreenType.NPCInteraction);
                    return;
                }

                if (activeShopLocation != null)
                {
                    ShowScreen(UIScreenType.Location);
                    return;
                }

                ShowScreen(UIScreenType.City);
            }
        );
    }

    private void BuildShopBuyTab(
        ShopData shop)
    {
        AddBody("Comprar");

        List<ShopItemEntry> entries =
            shop != null &&
            shop.Items != null &&
            shop.Items.Count > 0
                ? shop.Items
                : GetDefaultShopItems();

        foreach (ShopItemEntry entry
            in entries)
        {
            if (entry == null ||
                string.IsNullOrEmpty(entry.ItemID))
            {
                continue;
            }

            ItemData itemData =
                DatabaseManager
                    .Instance
                    .GetItemById(entry.ItemID);

            string itemName =
                itemData != null
                    ? itemData.DisplayName
                    : entry.ItemID;

            string description =
                itemData != null
                    ? itemData.Description
                    : "Item sem dados no banco.";

            int price =
                Mathf.Max(
                    0,
                    entry.Price
                );

            int quantity =
                Mathf.Max(
                    1,
                    entry.QuantityPerPurchase
                );

            AddCard(
                itemName,
                $"{description}\n" +
                $"Tipo: {GetItemTypeLabel(itemData)}\n" +
                $"Preco: {price}\n" +
                $"Quantidade: {quantity}"
            );

            string itemID =
                entry.ItemID;

            AddButton(
                $"Comprar {itemName}",
                () =>
                {
                    if (!WalletManager
                        .GetOrCreate()
                        .SpendCoins(price))
                    {
                        return;
                    }

                    InventoryManager
                        .Instance
                        .AddItem(
                            itemID,
                            quantity
                        );

                    QuestManager
                        .Instance
                        ?.ReportObjectiveProgress(
                            new QuestObjectiveContext(
                                QuestStepObjectiveType.BuyItem,
                                itemID,
                                quantity,
                                "Shop"
                            )
                        );

                    GameFeedbackUI.ShowNotification(
                        $"Comprou {itemName} x{quantity}."
                    );

                    ShowScreen(UIScreenType.Shop);
                }
            );
        }
    }

    private void BuildShopSellTab()
    {
        AddBody("Vender");

        List<InventoryItem> inventory =
            SaveManager
                .Instance
                .CurrentSave
                .Inventory;

        bool hasSellableItem =
            false;

        foreach (InventoryItem stack
            in inventory)
        {
            if (stack == null ||
                string.IsNullOrEmpty(stack.ItemID) ||
                stack.Quantity <= 0)
            {
                continue;
            }

            ItemData itemData =
                DatabaseManager
                    .Instance
                    .GetItemById(stack.ItemID);

            if (!CanSellItemInShop(
                stack.ItemID,
                itemData))
            {
                continue;
            }

            hasSellableItem =
                true;

            string itemName =
                itemData != null &&
                !string.IsNullOrEmpty(itemData.DisplayName)
                    ? itemData.DisplayName
                    : stack.ItemID;

            int sellValue =
                itemData != null
                    ? Mathf.Max(0, itemData.SellValue)
                    : 0;

            AddCard(
                itemName,
                $"{itemData.Description}\n" +
                $"Tipo: {GetItemTypeLabel(itemData)}\n" +
                $"Quantidade: {stack.Quantity}\n" +
                $"Valor de venda: {sellValue}"
            );

            string itemID =
                stack.ItemID;

            AddButton(
                $"Vender {itemName}",
                () =>
                {
                    InventoryManager
                        .Instance
                        .SellItem(itemID);

                    ShowScreen(UIScreenType.Shop);
                },
                new Color(0.16f, 0.18f, 0.12f, 1f)
            );
        }

        if (!hasSellableItem)
        {
            AddBody(
                "Nenhum item disponivel para venda."
            );
        }
    }

    private ShopData GetActiveShop()
    {
        if (activeShopData != null)
            return activeShopData;

        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        return city != null
            ? city.Shop
            : null;
    }

    private bool CanSellItemInShop(
        string itemID,
        ItemData itemData)
    {
        if (itemData == null ||
            itemData.Type == ItemType.Quest ||
            !itemData.CanSell)
        {
            return false;
        }

        if (EquipmentManager
            .GetOrCreate()
            .IsItemEquipped(itemID))
        {
            return false;
        }

        return true;
    }

    private void AddEquipButtonIfCompatible(
        ItemData itemData,
        string itemID,
        EquipmentSlot slot)
    {
        if (itemData == null ||
            !itemData.CanEquipInSlot(slot))
        {
            return;
        }

        if (slot == EquipmentSlot.OffHand &&
            EquipmentManager
                .GetOrCreate()
                .IsOffHandBlocked())
        {
            AddButton(
                "Mao secundaria bloqueada",
                () =>
                {
                    GameFeedbackUI.ShowNotification(
                        "A mao secundaria esta bloqueada por um item de duas maos."
                    );
                },
                new Color(0.1f, 0.1f, 0.1f, 1f)
            );

            return;
        }

        AddButton(
            $"Equipar na {GetSlotLabel(slot)}",
            () =>
            {
                EquipmentManager
                    .GetOrCreate()
                    .EquipItem(
                        itemID,
                        slot
                    );

                ShowScreen(UIScreenType.Equipment);
            }
        );
    }

    private void BuildCombatScreen()
    {
        CombatManager combat =
            CombatManager.GetOrCreate();

        if (combat.IsInCombat ||
            combat.State.AwaitingContinue)
        {
            AddTitle("Combate automatico");
            AddBody(
                "O combate esta acontecendo no popup."
            );
            ShowCombatPopup();
            return;
        }

        AddTitle("Combate");
        AddBody(
            "Nenhum combate ativo."
        );
        AddButton(
            "Voltar",
            () => ShowScreen(UIScreenType.Exploration)
        );
    }

    private List<ShopItemEntry> GetDefaultShopItems()
    {
        return new List<ShopItemEntry>
        {
            new ShopItemEntry
            {
                ItemID = "potion_simple",
                Price = 15,
                QuantityPerPurchase = 1
            },
            new ShopItemEntry
            {
                ItemID = "bread",
                Price = 5,
                QuantityPerPurchase = 1
            },
            new ShopItemEntry
            {
                ItemID = "simple_sword",
                Price = 60,
                QuantityPerPurchase = 1
            },
            new ShopItemEntry
            {
                ItemID = "simple_wand",
                Price = 55,
                QuantityPerPurchase = 1
            },
            new ShopItemEntry
            {
                ItemID = "simple_staff",
                Price = 80,
                QuantityPerPurchase = 1
            },
            new ShopItemEntry
            {
                ItemID = "simple_shield",
                Price = 45,
                QuantityPerPurchase = 1
            }
        };
    }

    private void BuildLocationScreen()
    {
        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        if (selectedLocation == null ||
            city == null)
        {
            AddTitle("Local");
            AddBody("Local nao encontrado.");
            AddButton(
                "Voltar para cidade",
                () => ShowScreen(UIScreenType.City)
            );
            return;
        }

        AddTitle(selectedLocation.DisplayName);
        AddSpriteFrame(
            selectedLocation.LocationSprite,
            150f
        );
        AddBody(selectedLocation.Description);

        if (selectedLocation.Services != null &&
            selectedLocation.Services.Count > 0)
        {
            AddBody("Servicos do local");

            foreach (CityServiceType service
                in selectedLocation.Services)
            {
                if (service == CityServiceType.None ||
                    service == CityServiceType.Travel)
                {
                    continue;
                }

                AddButton(
                    GetServiceLabel(service),
                    () => OpenLocationService(service)
                );
            }
        }

        List<NPCData> npcs =
            NPCManager.Instance != null
                ? NPCManager
                    .Instance
                    .GetNPCsForLocation(
                        city,
                        selectedLocation
                    )
                : new List<NPCData>();

        AddBody("Personagens no local");

        if (npcs.Count == 0)
        {
            AddBody("Nao ha ninguem aqui agora.");
        }
        else
        {
            foreach (NPCData npc
                in npcs)
            {
                if (npc == null)
                    continue;

                AddSpriteFrame(
                    GetNPCPortrait(npc),
                    96f
                );

                AddCard(
                    npc.DisplayName,
                    !string.IsNullOrEmpty(npc.Description)
                        ? npc.Description
                        : "Personagem disponivel neste local."
                );

                NPCData capturedNPC =
                    npc;

                AddButton(
                    $"Falar com {npc.DisplayName}",
                    () => OpenNPCInteraction(capturedNPC)
                );
            }
        }

        AddButton(
            "Voltar para cidade",
            () => ShowScreen(UIScreenType.City)
        );
    }

    private void OpenLocationService(
        CityServiceType service)
    {
        switch (service)
        {
            case CityServiceType.Shop:
            case CityServiceType.Market:
                activeShopNPC = null;
                activeShopLocation =
                    selectedLocation;
                activeShopData =
                    selectedLocation != null
                        ? selectedLocation.ShopData
                        : null;
                currentShopTab = MobileShopTab.Buy;
                ShowScreen(UIScreenType.Shop);
                break;

            case CityServiceType.Guild:
                ShowScreen(UIScreenType.Guild);
                break;

            case CityServiceType.Inn:
            case CityServiceType.Tavern:
                ShowScreen(UIScreenType.Inn);
                break;

            case CityServiceType.QuestBoard:
                guildQuestBoardMode =
                    selectedLocation != null &&
                    selectedLocation.LocationType ==
                    CityLocationType.Guild;
                ShowScreen(UIScreenType.QuestBoard);
                break;

            case CityServiceType.Travel:
                ShowScreen(UIScreenType.Travel);
                break;

            default:
                OpenService(service);
                break;
        }
    }

    private void OpenCityHUDService(
        CityServiceType service,
        CityLocationData location)
    {
        if (service == CityServiceType.None ||
            service == CityServiceType.Travel)
        {
            GameFeedbackUI.ShowNotification(
                "Use o Mapa para viagem e exploracao."
            );

            return;
        }

        selectedLocation =
            location;

        if (location != null)
        {
            OpenLocationService(service);
            return;
        }

        OpenService(service);
    }

    private void BuildNPCInteractionScreen()
    {
        if (selectedNPC == null)
        {
            AddTitle("Personagem");
            AddBody("Nenhum NPC selecionado.");
            AddButton(
                "Voltar para cidade",
                () => ShowScreen(UIScreenType.City)
            );
            return;
        }

        AddTitle(selectedNPC.DisplayName);

        AddSpriteFrame(
            GetNPCPortrait(selectedNPC),
            170f
        );

        AddBody(
            !string.IsNullOrEmpty(selectedNPC.Description)
                ? selectedNPC.Description
                : "Personagem disponivel para interacao."
        );

        AddRelationshipSummary(selectedNPC);

        AddCompanionRecruitmentButton(
            selectedNPC
        );

        if (selectedNPC.CanReceiveGifts &&
            selectedNPC.CanGainFriendship)
        {
            AddButton(
                "Dar Presente",
                () => ShowScreen(UIScreenType.Gift)
            );
        }

        if (selectedNPC.HasService(NPCServiceType.Dialogue) &&
            selectedNPC.DefaultDialogue != null)
        {
            AddButton(
                "Conversar",
                () =>
                {
                    NPCManager
                        .Instance
                        .TalkToNPC(selectedNPC);

                    ShowScreen(UIScreenType.Dialogue);
                }
            );
        }

        AddNPCServiceButton(
            NPCServiceType.Shop,
            "Loja"
        );

        AddNPCServiceButton(
            NPCServiceType.QuestBoard,
            "Mural de Missoes"
        );

        AddNPCServiceButton(
            NPCServiceType.Guild,
            "Guilda dos Gatos Negros"
        );

        AddNPCServiceButton(
            NPCServiceType.Inn,
            "Estalagem"
        );

        AddButton(
            "Voltar para cidade",
            () => ShowScreen(UIScreenType.City)
        );
    }

    private void AddCompanionRecruitmentButton(
        NPCData npc)
    {
        CompanionManager companionManager =
            CompanionManager.GetOrCreate();

        CompanionData companion =
            companionManager.GetCompanionByNPCId(
                npc.ID
            );

        if (companion == null ||
            !companion.CanJoinParty)
        {
            return;
        }

        bool isRecruited =
            companionManager.IsCompanionRecruited(
                companion.ID
            );

        if (isRecruited)
        {
            AddCard(
                "Companheiro",
                companionManager.IsCompanionInParty(
                    companion.ID)
                    ? "Este companheiro esta no grupo."
                    : "Este companheiro esta recrutado na guilda."
            );

            if (!companionManager.IsCompanionInParty(
                companion.ID))
            {
                AddButton(
                    "Adicionar ao grupo",
                    () =>
                    {
                        companionManager.AddToParty(
                            companion.ID
                        );

                        ShowScreen(
                            UIScreenType.NPCInteraction
                        );
                    }
                );
            }

            return;
        }

        bool meetsRequirements =
            RequirementChecker.AreRequirementsMet(
                companion.RecruitmentRequirements,
                npc.ID
            );

        AddButton(
            meetsRequirements
                ? "Recrutar para a guilda"
                : "Recrutar (bloqueado)",
            () =>
            {
                if (!meetsRequirements)
                {
                    GameFeedbackUI.ShowNotification(
                        "Requisitos de recrutamento nao cumpridos."
                    );
                    return;
                }

                companionManager.RecruitCompanion(
                    companion.ID,
                    addToActiveParty: false
                );

                ShowScreen(
                    UIScreenType.NPCInteraction
                );
            },
            meetsRequirements
                ? new Color(0.22f, 0.18f, 0.12f, 1f)
                : new Color(0.1f, 0.1f, 0.1f, 1f)
        );
    }

    private void BuildGiftScreen()
    {
        if (selectedNPC == null)
        {
            AddTitle("Presente");
            AddBody("Nenhum NPC selecionado.");
            AddButton(
                "Voltar",
                () => ShowScreen(UIScreenType.City)
            );
            return;
        }

        AddTitle($"Presente para {selectedNPC.DisplayName}");

        if (!RelationshipManager
            .GetOrCreate()
            .CanGiveGiftToday(selectedNPC.ID))
        {
            AddBody(
                "Voce ja deu um presente para este personagem hoje."
            );

            AddButton(
                "Voltar ao NPC",
                () => ShowScreen(UIScreenType.NPCInteraction)
            );

            return;
        }

        List<InventoryItem> inventory =
            SaveManager
                .Instance
                .CurrentSave
                .Inventory;

        bool hasGift =
            false;

        foreach (InventoryItem stack
            in inventory)
        {
            if (stack == null ||
                string.IsNullOrEmpty(stack.ItemID) ||
                stack.Quantity <= 0)
            {
                continue;
            }

            ItemData itemData =
                DatabaseManager
                    .Instance
                    .GetItemById(stack.ItemID);

            if (itemData == null ||
                itemData.Type == ItemType.Quest ||
                !itemData.CanGift)
            {
                continue;
            }

            hasGift = true;

            string itemID =
                stack.ItemID;

            string itemName =
                !string.IsNullOrEmpty(itemData.DisplayName)
                    ? itemData.DisplayName
                    : itemID;

            AddCard(
                itemName,
                $"{itemData.Description}\nQuantidade: {stack.Quantity}"
            );

            AddButton(
                $"Dar {itemName}",
                () =>
                {
                    RelationshipManager
                        .GetOrCreate()
                        .GiveGift(
                            selectedNPC.ID,
                            itemID
                        );

                    ShowScreen(
                        UIScreenType.NPCInteraction
                    );
                }
            );
        }

        if (!hasGift)
        {
            AddBody(
                "Nenhum item disponivel para presente."
            );
        }

        AddButton(
            "Voltar ao NPC",
            () => ShowScreen(UIScreenType.NPCInteraction)
        );
    }

    private void AddNPCServiceButton(
        NPCServiceType service,
        string label)
    {
        if (selectedNPC == null ||
            !selectedNPC.HasService(service))
        {
            return;
        }

        AddButton(
            label,
            () => OpenNPCService(service)
        );
    }

    private void OpenNPCService(
        NPCServiceType service)
    {
        switch (service)
        {
            case NPCServiceType.Dialogue:
                if (selectedNPC != null &&
                    selectedNPC.DefaultDialogue != null)
                {
                    NPCManager
                        .Instance
                        .TalkToNPC(selectedNPC);

                    ShowScreen(UIScreenType.Dialogue);
                }
                break;

            case NPCServiceType.Shop:
                OpenShopFromNPC(selectedNPC);
                break;

            case NPCServiceType.QuestBoard:
                guildQuestBoardMode =
                    selectedNPC != null &&
                    selectedNPC.HasService(
                        NPCServiceType.Guild);
                ShowScreen(UIScreenType.QuestBoard);
                break;

            case NPCServiceType.Guild:
                ShowScreen(UIScreenType.Guild);
                break;

            case NPCServiceType.Inn:
                ShowScreen(UIScreenType.Inn);
                break;

            case NPCServiceType.Travel:
                ShowScreen(UIScreenType.Travel);
                break;

            default:
                GameFeedbackUI.ShowNotification(
                    "Este servico sera expandido depois."
                );
                break;
        }
    }

    private void BuildDialogueScreen()
    {
        DialogueManager dialogue =
            DialogueManager.Instance;

        if (dialogue == null ||
            !dialogue.IsDialogueActive)
        {
            AddTitle("Dialogo");
            AddBody("Dialogo encerrado.");
            AddButton(
                selectedNPC != null
                    ? "Voltar ao NPC"
                    : "Voltar para cidade",
                () => ShowScreen(
                    selectedNPC != null
                        ? UIScreenType.NPCInteraction
                        : UIScreenType.City
                )
            );
            return;
        }

        DialogueNode node =
            dialogue.CurrentNode;

        NPCData npc =
            dialogue.CurrentNPC != null
                ? dialogue.CurrentNPC
                : selectedNPC;

        string speakerName =
            !string.IsNullOrEmpty(node.SpeakerName)
                ? node.SpeakerName
                : npc != null
                    ? npc.DisplayName
                    : "Dialogo";

        AddTitle(speakerName);
        AddSpriteFrame(
            node.Portrait != null
                ? node.Portrait
                : GetNPCPortrait(npc),
            170f
        );
        AddBody(node.SpeakerText);

        List<DialogueChoice> choices =
            dialogue.GetVisibleChoices();

        if (choices.Count > 0)
        {
            foreach (DialogueChoice choice
                in choices)
            {
                if (choice == null)
                    continue;

                bool meetsRequirements =
                    dialogue.ChoiceMeetsRequirements(choice);

                string label =
                    meetsRequirements
                        ? choice.ChoiceText
                        : !string.IsNullOrEmpty(choice.LockedText)
                            ? choice.LockedText
                            : $"{choice.ChoiceText} (Bloqueado)";

                DialogueChoice capturedChoice =
                    choice;

                AddButton(
                    label,
                    () =>
                    {
                        if (!dialogue
                            .ChoiceMeetsRequirements(
                                capturedChoice))
                        {
                            GameFeedbackUI.ShowNotification(
                                "Requisito nao cumprido."
                            );

                            return;
                        }

                        dialogue.SelectChoice(
                            capturedChoice
                        );

                        if (dialogue.IsDialogueActive)
                        {
                            ShowScreen(UIScreenType.Dialogue);
                        }
                        else if (currentScreen == UIScreenType.Dialogue)
                        {
                            ShowScreen(
                                selectedNPC != null
                                    ? UIScreenType.NPCInteraction
                                    : UIScreenType.City
                            );
                        }
                    },
                    meetsRequirements
                        ? new Color(0.22f, 0.18f, 0.12f, 1f)
                        : new Color(0.12f, 0.12f, 0.12f, 1f)
                );
            }
        }
        else
        {
            AddButton(
                node.EndsDialogue
                    ? "Encerrar"
                    : "Continuar",
                () =>
                {
                    if (node.EndsDialogue)
                    {
                        dialogue.EndDialogue();
                    }
                    else
                    {
                        dialogue.ContinueDialogue();
                    }

                    if (dialogue.IsDialogueActive)
                    {
                        ShowScreen(UIScreenType.Dialogue);
                    }
                    else
                    {
                        ShowScreen(
                            selectedNPC != null
                                ? UIScreenType.NPCInteraction
                                : UIScreenType.City
                        );
                    }
                }
            );
        }

        AddButton(
            "Sair da conversa",
            () =>
            {
                dialogue.EndDialogue();

                ShowScreen(
                    selectedNPC != null
                        ? UIScreenType.NPCInteraction
                        : UIScreenType.City
                );
            },
            new Color(0.16f, 0.12f, 0.1f, 1f)
        );
    }

    private void BuildPlaceholderScreen(
        string title,
        string body)
    {
        AddTitle(title);
        AddBody(body);
        AddButton(
            "Voltar",
            () => ShowScreen(UIScreenType.City)
        );
    }

    private void BuildBottomNavigation()
    {
        ClearChildren(bottomNavRoot);
        navButtons.Clear();

        AddNavButton(
            UIScreenType.City,
            "Cidade"
        );

        AddNavButton(
            UIScreenType.Quests,
            "Missoes"
        );

        AddNavButton(
            UIScreenType.Travel,
            "Viagem"
        );

        AddNavButton(
            UIScreenType.Character,
            "Personagem"
        );

        AddNavButton(
            UIScreenType.SystemMenu,
            "Menu"
        );

        HighlightCurrentNavigation();
    }

    private void AddNavButton(
        UIScreenType screenType,
        string label)
    {
        Button button =
            CreateButtonObject(
                label,
                bottomNavRoot,
                new Color(0.15f, 0.14f, 0.12f, 1f)
            );

        button.onClick.AddListener(
            () => ShowScreen(screenType)
        );

        navButtons[screenType] =
            button;
    }

    private void HighlightCurrentNavigation()
    {
        foreach (var pair
            in navButtons)
        {
            Image image =
                pair.Value.GetComponent<Image>();

            bool isActive =
                pair.Key == currentScreen ||
                (currentScreen == UIScreenType.Inventory &&
                    pair.Key == UIScreenType.Character) ||
                (currentScreen == UIScreenType.Equipment &&
                    pair.Key == UIScreenType.Character) ||
                (currentScreen == UIScreenType.Party &&
                    pair.Key == UIScreenType.Character) ||
                (currentScreen == UIScreenType.NPCInteraction &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.ExplorationAreas &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.Exploration &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.ExplorationEvent &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.Gift &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.Dialogue &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.Location &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.Shop &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.QuestBoard &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.Guild &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.GuildMembers &&
                    pair.Key == UIScreenType.City) ||
                (currentScreen == UIScreenType.Inn &&
                    pair.Key == UIScreenType.City);

            image.color =
                isActive
                    ? new Color(0.46f, 0.34f, 0.16f, 1f)
                    : new Color(0.15f, 0.14f, 0.12f, 1f);
        }
    }

    private void OpenService(
        CityServiceType service)
    {
        switch (service)
        {
            case CityServiceType.Travel:
                ShowScreen(UIScreenType.Travel);
                break;

            case CityServiceType.Guild:
                ShowScreen(UIScreenType.Guild);
                break;

            case CityServiceType.Inn:
            case CityServiceType.Tavern:
                ShowScreen(UIScreenType.Inn);
                break;

            case CityServiceType.Shop:
            case CityServiceType.Market:
                activeShopNPC = null;
                activeShopLocation = null;
                activeShopData = null;
                currentShopTab = MobileShopTab.Buy;
                ShowScreen(UIScreenType.Shop);
                break;

            case CityServiceType.QuestBoard:
                guildQuestBoardMode = false;
                ShowScreen(UIScreenType.QuestBoard);
                break;

            default:
                GameFeedbackUI.ShowNotification(
                    "Este servico sera expandido depois."
                );
                break;
        }
    }

    private bool IsServiceAvailable(
        CityServiceType service)
    {
        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        return city != null &&
            city.Services != null &&
            city.Services.Contains(service);
    }

    private void CompleteMeetGuildQuestIfNeeded()
    {
        if (QuestManager.Instance == null)
            return;

        if (QuestManager
            .Instance
            .GetQuestState(MeetBlackCatsGuildQuestID)
            != QuestStatus.Active)
        {
            return;
        }

        QuestData quest =
            DatabaseManager
                .Instance
                .GetData<QuestData>(
                    MeetBlackCatsGuildQuestID
                );

        if (quest != null &&
            quest.Steps != null &&
            quest.Steps.Count > 0)
        {
            return;
        }

        QuestManager
            .Instance
            .CompleteQuest(MeetBlackCatsGuildQuestID);

        QuestManager
            .Instance
            .StartQuest(ReturnToLunarisQuestID);
    }

    private void HandleTravelChanged()
    {
        currentScreen =
            TravelManager.Instance.IsTraveling
                ? UIScreenType.Travel
                : UIScreenType.City;

        RefreshAll();
    }

    private void HandleTimeOfDayChanged(
        TimeOfDay timeOfDay)
    {
        RefreshAll();
    }

    private void HandleWorldEventTriggered(
        WorldEventData eventData)
    {
        RefreshAll();
    }

    private void EnsureGuildManager()
    {
        if (GuildManager.Instance != null)
            return;

        GameObject managerObject =
            new GameObject(
                "GuildManager"
            );

        managerObject
            .AddComponent<GuildManager>();
    }

    private void BuildRuntimeUI()
    {
        if (canvas != null)
            return;

        canvas =
            gameObject.AddComponent<Canvas>();

        EnsureEventSystem();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            500;

        CanvasScaler scaler =
            gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(720f, 1280f);

        gameObject.AddComponent<GraphicRaycaster>();

        root =
            CreateRect(
                "Root",
                transform,
                Vector2.zero,
                Vector2.one
            );

        AddImage(
            root.gameObject,
            new Color(0.05f, 0.045f, 0.04f, 1f)
        );

        RectTransform topRoot =
            CreateRect(
                "TopHUD",
                root,
                new Vector2(0f, 0.86f),
                new Vector2(1f, 1f)
            );

        AddImage(
            topRoot.gameObject,
            new Color(0.09f, 0.08f, 0.065f, 1f)
        );

        topLocationText =
            CreateText(
                "LocationText",
                topRoot,
                new Vector2(0.05f, 0.46f),
                new Vector2(0.82f, 0.9f),
                30f,
                TextAlignmentOptions.Left
            );

        topDateText =
            CreateText(
                "DateText",
                topRoot,
                new Vector2(0.05f, 0.12f),
                new Vector2(0.82f, 0.48f),
                21f,
                TextAlignmentOptions.Left
            );

        Button calendarButton =
            CreateButtonObject(
                "Calend.",
                topRoot,
                new Color(0.2f, 0.17f, 0.12f, 1f)
            );

        SetAnchors(
            calendarButton.GetComponent<RectTransform>(),
            new Vector2(0.82f, 0.18f),
            new Vector2(0.96f, 0.78f)
        );

        calendarButton.onClick.AddListener(
            () => ShowScreen(UIScreenType.Calendar)
        );

        RectTransform contentViewport =
            CreateRect(
                "ContentViewport",
                root,
                new Vector2(0.04f, 0.14f),
                new Vector2(0.96f, 0.85f)
            );

        contentViewport
            .gameObject
            .AddComponent<RectMask2D>();

        ScrollRect scrollRect =
            contentViewport
                .gameObject
                .AddComponent<ScrollRect>();

        scrollRect.horizontal =
            false;

        scrollRect.vertical =
            true;

        scrollRect.viewport =
            contentViewport;

        contentRoot =
            CreateRect(
                "Content",
                contentViewport,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            );

        contentRoot.pivot =
            new Vector2(0.5f, 1f);

        contentRoot.anchoredPosition =
            Vector2.zero;

        contentRoot.sizeDelta =
            new Vector2(0f, 1600f);

        scrollRect.content =
            contentRoot;

        bottomNavRoot =
            CreateRect(
                "BottomNavigation",
                root,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.13f)
            );

        AddImage(
            bottomNavRoot.gameObject,
            new Color(0.08f, 0.075f, 0.065f, 1f)
        );

        BuildTravelEventPopup();
        BuildCombatPopup();
    }

    private void BuildTravelEventPopup()
    {
        travelEventPopupRoot =
            CreateRect(
                "TravelEventPopup",
                root,
                new Vector2(0.07f, 0.22f),
                new Vector2(0.93f, 0.78f)
            );

        AddImage(
            travelEventPopupRoot.gameObject,
            new Color(0.07f, 0.06f, 0.045f, 0.97f)
        );

        VerticalLayoutGroup popupLayout =
            travelEventPopupRoot
                .gameObject
                .AddComponent<VerticalLayoutGroup>();

        popupLayout.padding =
            new RectOffset(18, 18, 18, 18);

        popupLayout.spacing =
            12f;

        popupLayout.childForceExpandWidth =
            true;

        popupLayout.childForceExpandHeight =
            false;

        travelEventTitleText =
            CreatePopupText(
                "TravelEventTitle",
                travelEventPopupRoot,
                30f,
                58f,
                TextAlignmentOptions.Center
            );

        travelEventBodyText =
            CreatePopupText(
                "TravelEventBody",
                travelEventPopupRoot,
                22f,
                160f,
                TextAlignmentOptions.TopLeft
            );

        travelEventChoicesRoot =
            CreateLayoutRect(
                "TravelEventChoices",
                travelEventPopupRoot,
                190f
            );

        VerticalLayoutGroup choicesLayout =
            travelEventChoicesRoot
                .gameObject
                .AddComponent<VerticalLayoutGroup>();

        choicesLayout.spacing =
            10f;

        choicesLayout.childForceExpandWidth =
            true;

        choicesLayout.childForceExpandHeight =
            false;

        travelEventPopupRoot
            .gameObject
            .SetActive(false);

        if (TravelEventManager.Instance != null &&
            TravelEventManager.Instance.HasActiveEvent)
        {
            ShowTravelEventPopup(
                TravelEventManager.Instance.CurrentEvent
            );
        }
    }

    private void ShowTravelEventPopup(
        TravelEventData eventData)
    {
        if (eventData == null)
            return;

        if (travelEventPopupRoot == null)
        {
            BuildTravelEventPopup();
        }

        travelEventTitleText.text =
            eventData.DisplayName;

        travelEventBodyText.text =
            !string.IsNullOrEmpty(eventData.EventText)
                ? eventData.EventText
                : eventData.Description;

        ClearChildren(travelEventChoicesRoot);

        if (eventData.Choices == null ||
            eventData.Choices.Count == 0)
        {
            AddPopupChoiceButton(
                "Continuar",
                () =>
                {
                    TravelEventManager
                        .Instance
                        .ResolveCurrentEventWithoutChoice();
                }
            );
        }
        else
        {
            for (int i = 0;
                i < eventData.Choices.Count;
                i++)
            {
                int choiceIndex = i;

                EventChoice choice =
                    eventData.Choices[i];

                string label =
                    !string.IsNullOrEmpty(choice.ChoiceText)
                        ? choice.ChoiceText
                        : $"Opcao {i + 1}";

                bool meetsRequirements =
                    RequirementChecker
                        .AreRequirementsMet(
                            choice.GenericRequirements
                        );

                AddPopupChoiceButton(
                    meetsRequirements
                        ? label
                        : $"{label} (Bloqueado)",
                    () =>
                    {
                        if (!meetsRequirements)
                        {
                            GameFeedbackUI.ShowNotification(
                                "Requisito nao cumprido."
                            );

                            return;
                        }

                        TravelEventManager
                            .Instance
                            .ResolveChoice(choiceIndex);
                    }
                );
            }
        }

        travelEventPopupRoot
            .gameObject
            .SetActive(true);

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            travelEventPopupRoot
        );
    }

    private void HideTravelEventPopup()
    {
        if (travelEventPopupRoot == null)
            return;

        travelEventPopupRoot
            .gameObject
            .SetActive(false);
    }

    private void BuildCombatPopup()
    {
        combatPopupRoot =
            CreateRect(
                "CombatPopup",
                root,
                new Vector2(0.06f, 0.16f),
                new Vector2(0.94f, 0.86f)
            );

        AddImage(
            combatPopupRoot.gameObject,
            new Color(0.055f, 0.048f, 0.04f, 0.98f)
        );

        VerticalLayoutGroup popupLayout =
            combatPopupRoot
                .gameObject
                .AddComponent<VerticalLayoutGroup>();

        popupLayout.padding =
            new RectOffset(18, 18, 18, 18);

        popupLayout.spacing =
            10f;

        popupLayout.childForceExpandWidth =
            true;

        popupLayout.childForceExpandHeight =
            false;

        combatPopupTitleText =
            CreatePopupText(
                "CombatPopupTitle",
                combatPopupRoot,
                29f,
                48f,
                TextAlignmentOptions.Center
            );

        combatPopupContentRoot =
            CreateLayoutRect(
                "CombatPopupContent",
                combatPopupRoot,
                580f
            );

        VerticalLayoutGroup contentLayout =
            combatPopupContentRoot
                .gameObject
                .AddComponent<VerticalLayoutGroup>();

        contentLayout.spacing =
            8f;

        contentLayout.childForceExpandWidth =
            true;

        contentLayout.childForceExpandHeight =
            false;

        combatPopupActionsRoot =
            CreateLayoutRect(
                "CombatPopupActions",
                combatPopupRoot,
                84f
            );

        VerticalLayoutGroup actionLayout =
            combatPopupActionsRoot
                .gameObject
                .AddComponent<VerticalLayoutGroup>();

        actionLayout.spacing =
            8f;

        actionLayout.childForceExpandWidth =
            true;

        actionLayout.childForceExpandHeight =
            false;

        combatPopupRoot
            .gameObject
            .SetActive(false);
    }

    private void ShowCombatPopup()
    {
        HideSceneCityHUD();
        SetRuntimeRootVisible(true);

        if (combatPopupRoot == null)
        {
            BuildCombatPopup();
        }

        RefreshCombatPopupInternal();

        combatPopupRoot
            .gameObject
            .SetActive(true);
    }

    private void RefreshCombatPopupInternal()
    {
        if (combatPopupRoot == null)
            return;

        CombatManager combat =
            CombatManager.Instance;

        if (combat == null ||
            combat.State == null ||
            (!combat.State.IsInCombat &&
                !combat.State.AwaitingContinue))
        {
            HideCombatPopupInternal();
            return;
        }

        combatPopupTitleText.text =
            string.IsNullOrEmpty(
                combat.State.EncounterDisplayName)
                ? "Combate"
                : combat.State.EncounterDisplayName;

        ClearChildren(combatPopupContentRoot);
        ClearChildren(combatPopupActionsRoot);

        BuildCombatPopupContent(combat);

        combatPopupRoot
            .gameObject
            .SetActive(true);

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            combatPopupRoot
        );
    }

    private void HideCombatPopupInternal()
    {
        if (combatPopupRoot == null)
            return;

        combatPopupRoot
            .gameObject
            .SetActive(false);
    }

    private void BuildCombatPopupContent(
        CombatManager combat)
    {
        CombatState state =
            combat.State;

        CombatantRuntimeData player =
            combat.GetPlayerCombatant();

        CombatantRuntimeData enemy =
            combat.GetFirstAliveEnemy();

        List<CombatantRuntimeData> playerSide =
            combat.GetPlayerSideCombatants();

        List<CombatantRuntimeData> enemies =
            combat.GetLivingEnemies();

        AddCombatPopupText(
            GetCombatPhaseLabel(state.Phase),
            36f,
            22f,
            TextAlignmentOptions.Center
        );

        if (enemy != null)
        {
            AddCombatPopupSpriteFrame(
                combat.GetCombatantSprite(enemy),
                90f
            );
        }

        if (player != null)
        {
            string playerBody =
                $"Vida {BuildPopupBar(player.Stats.CurrentHealth, player.Stats.MaxHealth)} " +
                $"{player.Stats.CurrentHealth}/{player.Stats.MaxHealth}\n" +
                $"Energia {BuildPopupBar(player.Stats.CurrentEnergy, player.Stats.MaxEnergy)} " +
                $"{player.Stats.CurrentEnergy}/{player.Stats.MaxEnergy}\n" +
                $"Ataque principal {BuildPopupBar(player.BasicAttackTimer, player.BasicAttackInterval)}";

            if (player.CanUseOffHandAttack)
            {
                playerBody +=
                    $"\nAtaque secundario {BuildPopupBar(player.OffHandAttackTimer, player.OffHandAttackInterval)}";
            }

            AddCombatPopupCard(
                player.DisplayName,
                playerBody
            );

            AddCombatSkillProgress(player);
        }

        if (playerSide.Count > 1)
        {
            string partyBody =
                string.Empty;

            foreach (CombatantRuntimeData ally
                in playerSide)
            {
                if (ally == null ||
                    ally.Type == CombatantType.Player)
                {
                    continue;
                }

                partyBody +=
                    $"{ally.DisplayName}: " +
                    $"{ally.Stats.CurrentHealth}/{ally.Stats.MaxHealth} HP";

                if (ally.IsDefeated)
                {
                    partyBody += " (derrotado)";
                }

                partyBody += "\n";
            }

            if (!string.IsNullOrEmpty(partyBody))
            {
                AddCombatPopupCard(
                    "Companheiros",
                    partyBody.TrimEnd(),
                    104f
                );
            }

            foreach (CombatantRuntimeData ally
                in playerSide)
            {
                if (ally == null ||
                    ally.Type != CombatantType.Ally)
                {
                    continue;
                }

                AddCombatSkillProgress(ally);
            }
        }

        if (enemy != null)
        {
            string enemyBody =
                string.Empty;

            foreach (CombatantRuntimeData enemyEntry
                in enemies)
            {
                enemyBody +=
                    $"{enemyEntry.DisplayName}: " +
                    $"{BuildPopupBar(enemyEntry.Stats.CurrentHealth, enemyEntry.Stats.MaxHealth)} " +
                    $"{enemyEntry.Stats.CurrentHealth}/{enemyEntry.Stats.MaxHealth}\n";
            }

            enemyBody +=
                $"Ataque atual {BuildPopupBar(enemy.BasicAttackTimer, enemy.BasicAttackInterval)}\n" +
                $"Elemento: {GetElementLabel(enemy.Stats.PrimaryElement)}";

            AddCombatPopupCard(
                "Inimigos",
                enemyBody
            );
        }
        else
        {
            AddCombatPopupCard(
                "Inimigos",
                "Nenhum inimigo ativo."
            );
        }

        AddCombatLog(state);

        if (state.Phase == CombatPhase.Victory &&
            !string.IsNullOrEmpty(state.VictorySummary))
        {
            AddCombatPopupCard(
                "Resumo da vitoria",
                state.VictorySummary,
                168f
            );
        }

        if (state.Phase == CombatPhase.Running)
        {
            if (state.CanFlee)
            {
                AddCombatPopupButton(
                    "Fugir",
                    () => CombatManager
                        .GetOrCreate()
                        .FleeCombat(),
                    new Color(0.18f, 0.12f, 0.1f, 1f)
                );
            }
            else
            {
                AddCombatPopupButton(
                    "Fuga indisponivel",
                    () => GameFeedbackUI.ShowNotification(
                        "Nao e possivel fugir deste combate."
                    ),
                    new Color(0.1f, 0.1f, 0.1f, 1f)
                );
            }
        }

        if (state.AwaitingContinue)
        {
            AddCombatPopupButton(
                "Continuar",
                () => CombatManager
                    .GetOrCreate()
                    .ContinueAfterCombat(),
                new Color(0.24f, 0.18f, 0.1f, 1f)
            );
        }
    }

    private void AddCombatSkillProgress(
        CombatantRuntimeData player)
    {
        if (player.SkillRuntimes == null ||
            player.SkillRuntimes.Count == 0)
        {
            return;
        }

        string body =
            string.Empty;

        foreach (CombatSkillRuntimeData runtime
            in player.SkillRuntimes)
        {
            SkillData skill =
                DatabaseManager
                    .Instance
                    .GetData<SkillData>(
                        runtime.SkillID
                    );

            if (skill == null)
                continue;

            float chargeTime =
                Mathf.Max(0.1f, skill.ChargeTime);

            string stateText =
                player.Stats.CurrentEnergy < skill.EnergyCost
                    ? "Sem energia"
                    : runtime.CooldownRemaining > 0f
                        ? "Recarga"
                        : runtime.CurrentCharge >= chargeTime
                            ? "Pronta"
                            : $"{Mathf.RoundToInt(runtime.CurrentCharge / chargeTime * 100f)}%";

            body +=
                $"{skill.DisplayName}: {BuildPopupBar(runtime.CurrentCharge, chargeTime)} {stateText}\n";
        }

        if (string.IsNullOrEmpty(body))
            return;

        string title =
            player.Type == CombatantType.Ally
                ? $"Habilidades - {player.DisplayName}"
                : "Habilidades automaticas";

        AddCombatPopupCard(
            title,
            body.TrimEnd(),
            132f
        );
    }

    private void AddCombatLog(
        CombatState state)
    {
        if (state.Logs == null ||
            state.Logs.Count == 0)
        {
            return;
        }

        AddCombatPopupCard(
            "Acoes recentes",
            string.Join("\n", state.Logs),
            168f
        );
    }

    private void AddCombatPopupText(
        string text,
        float minHeight,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        TMP_Text body =
            CreatePopupText(
                "CombatPopupText",
                combatPopupContentRoot,
                fontSize,
                minHeight,
                alignment
            );

        body.text =
            text;
    }

    private void AddCombatPopupCard(
        string title,
        string body,
        float minHeight = 104f)
    {
        RectTransform cardRoot =
            CreateLayoutRect(
                "CombatPopupCard",
                combatPopupContentRoot,
                minHeight
            );

        AddImage(
            cardRoot.gameObject,
            new Color(0.105f, 0.095f, 0.08f, 1f)
        );

        TMP_Text cardText =
            CreateText(
                "CombatPopupCardText",
                cardRoot,
                new Vector2(0.04f, 0.1f),
                new Vector2(0.96f, 0.9f),
                20f,
                TextAlignmentOptions.TopLeft
            );

        cardText.text =
            $"<b>{title}</b>\n{body}";
    }

    private void AddCombatPopupSpriteFrame(
        Sprite sprite,
        float minHeight)
    {
        RectTransform frame =
            CreateLayoutRect(
                "CombatPopupSpriteFrame",
                combatPopupContentRoot,
                minHeight
            );

        Image image =
            AddImage(
                frame.gameObject,
                Color.white
            );

        image.sprite =
            sprite;

        image.preserveAspect =
            true;
    }

    private void AddCombatPopupButton(
        string label,
        UnityEngine.Events.UnityAction onClick,
        Color color)
    {
        Button button =
            CreateButtonObject(
                label,
                combatPopupActionsRoot,
                color
            );

        button.onClick.AddListener(onClick);
    }

    private string BuildPopupBar(
        int current,
        int max)
    {
        return BuildPopupBar(
            (float)current,
            Mathf.Max(1, max)
        );
    }

    private string BuildPopupBar(
        float current,
        float max)
    {
        int segments = 10;

        float progress =
            max <= 0f
                ? 0f
                : Mathf.Clamp01(current / max);

        int filled =
            Mathf.RoundToInt(progress * segments);

        return "[" +
            new string('#', filled) +
            new string('-', segments - filled) +
            "]";
    }

    private string GetCombatPhaseLabel(
        CombatPhase phase)
    {
        return phase switch
        {
            CombatPhase.Running => "Em combate",

            CombatPhase.Victory => "Vitoria",

            CombatPhase.Defeat => "Derrota",

            CombatPhase.Flee => "Fuga",

            CombatPhase.Starting => "Iniciando combate",

            _ => "Combate"
        };
    }

    private void AddPopupChoiceButton(
        string label,
        UnityEngine.Events.UnityAction onClick)
    {
        Button button =
            CreateButtonObject(
                label,
                travelEventChoicesRoot,
                new Color(0.24f, 0.18f, 0.1f, 1f)
            );

        button.onClick.AddListener(onClick);
    }

    private TMP_Text CreatePopupText(
        string name,
        Transform parent,
        float fontSize,
        float minHeight,
        TextAlignmentOptions alignment)
    {
        RectTransform rect =
            CreateLayoutRect(
                name,
                parent,
                minHeight
            );

        TMP_Text text =
            rect.gameObject
                .AddComponent<TextMeshProUGUI>();

        text.fontSize =
            fontSize;

        text.textWrappingMode =
            TextWrappingModes.Normal;

        text.alignment =
            alignment;

        text.color =
            Color.white;

        return text;
    }

    private void AddTitle(
        string text)
    {
        TMP_Text title =
            CreateTextBlock(
                "Title",
                34f,
                TextAlignmentOptions.Left
            );

        title.text =
            text;
    }

    private void AddBody(
        string text)
    {
        TMP_Text body =
            CreateTextBlock(
                "Body",
                23f,
                TextAlignmentOptions.TopLeft
            );

        body.text =
            text;
    }

    private void AddCard(
        string title,
        string body)
    {
        RectTransform cardRoot =
            CreateLayoutRect(
                "Card",
                contentRoot,
                150f
            );

        AddImage(
            cardRoot.gameObject,
            new Color(0.11f, 0.1f, 0.085f, 1f)
        );

        TMP_Text cardText =
            CreateText(
                "CardText",
                cardRoot,
                new Vector2(0.04f, 0.12f),
                new Vector2(0.96f, 0.88f),
                22f,
                TextAlignmentOptions.TopLeft
            );

        cardText.text =
            $"<b>{title}</b>\n{body}";
    }

    private void AddSpriteFrame(
        Sprite sprite,
        float minHeight)
    {
        RectTransform frame =
            CreateLayoutRect(
                "SpriteFrame",
                contentRoot,
                minHeight
            );

        Image image =
            AddImage(
                frame.gameObject,
                Color.white
            );

        image.sprite =
            sprite;

        image.preserveAspect =
            true;
    }

    private Sprite GetNPCPortrait(
        NPCData npc)
    {
        if (npc == null)
            return null;

        if (npc.Portrait != null)
            return npc.Portrait;

        return npc.IconSprite;
    }

    private void AddRelationshipSummary(
        NPCData npc)
    {
        if (npc == null ||
            (!npc.CanGainFriendship &&
                !npc.CanRomance &&
                !npc.CanMarry))
        {
            return;
        }

        NPCRelationshipSaveData relationship =
            RelationshipManager
                .GetOrCreate()
                .GetRelationship(npc.ID);

        if (relationship == null)
            return;

        string summary =
            $"Amizade: {relationship.FriendshipLevel} ({relationship.FriendshipPoints})\n" +
            $"Conversas: {relationship.TimesTalked}";

        if (npc.CanRomance)
        {
            summary +=
                $"\nRomance: {relationship.RomanceLevel} ({relationship.RomancePoints})";
        }

        if (relationship.IsDating)
        {
            summary += "\nEstado: Namorando";
        }

        if (relationship.IsMarried)
        {
            summary += "\nEstado: Casado";
        }

        AddCard(
            "Relacionamento",
            summary
        );
    }

    private void AddButton(
        string label,
        UnityEngine.Events.UnityAction onClick)
    {
        AddButton(
            label,
            onClick,
            new Color(0.22f, 0.18f, 0.12f, 1f)
        );
    }

    private void AddButton(
        string label,
        UnityEngine.Events.UnityAction onClick,
        Color color)
    {
        Button button =
            CreateButtonObject(
                label,
                contentRoot,
                color
            );

        button.onClick.AddListener(onClick);
    }

    private TMP_Text CreateTextBlock(
        string name,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        RectTransform rect =
            CreateLayoutRect(
                name,
                contentRoot,
                name == "Title"
                    ? 54f
                    : 126f
            );

        TMP_Text text =
            rect.gameObject
                .AddComponent<TextMeshProUGUI>();

        text.fontSize =
            fontSize;

        text.textWrappingMode =
            TextWrappingModes.Normal;

        text.alignment =
            alignment;

        text.color =
            Color.white;

        return text;
    }

    private RectTransform CreateLayoutRect(
        string name,
        Transform parent,
        float minHeight)
    {
        RectTransform rect =
            CreateRect(
                name,
                parent,
                Vector2.zero,
                Vector2.one
            );

        LayoutElement layout =
            rect.gameObject
                .AddComponent<LayoutElement>();

        layout.minHeight =
            minHeight;

        layout.flexibleWidth =
            1f;

        return rect;
    }

    private Button CreateButtonObject(
        string label,
        Transform parent,
        Color color)
    {
        RectTransform rect =
            CreateRect(
                label,
                parent,
                Vector2.zero,
                Vector2.one
            );

        Image image =
            AddImage(
                rect.gameObject,
                color
            );

        Button button =
            rect.gameObject
                .AddComponent<Button>();

        button.targetGraphic =
            image;

        LayoutElement layout =
            rect.gameObject
                .AddComponent<LayoutElement>();

        layout.minHeight =
            72f;

        layout.flexibleWidth =
            1f;

        TMP_Text text =
            CreateText(
                "Label",
                rect,
                new Vector2(0.05f, 0.12f),
                new Vector2(0.95f, 0.88f),
                21f,
                TextAlignmentOptions.Center
            );

        text.text =
            label;

        return button;
    }

    private TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        RectTransform rect =
            CreateRect(
                name,
                parent,
                anchorMin,
                anchorMax
            );

        TMP_Text text =
            rect.gameObject
                .AddComponent<TextMeshProUGUI>();

        text.fontSize =
            fontSize;

        text.textWrappingMode =
            TextWrappingModes.Normal;

        text.alignment =
            alignment;

        text.color =
            Color.white;

        return text;
    }

    private RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject rectObject =
            new GameObject(
                name,
                typeof(RectTransform)
            );

        rectObject.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            rectObject
                .GetComponent<RectTransform>();

        SetAnchors(
            rect,
            anchorMin,
            anchorMax
        );

        return rect;
    }

    private void SetAnchors(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    private Image AddImage(
        GameObject target,
        Color color)
    {
        Image image =
            target.GetComponent<Image>();

        if (image == null)
        {
            image =
                target.AddComponent<Image>();
        }

        image.color =
            color;

        return image;
    }

    private void ClearChildren(
        Transform target)
    {
        for (int i =
            target.childCount - 1;
            i >= 0;
            i--)
        {
            Destroy(
                target
                    .GetChild(i)
                    .gameObject
            );
        }

        if (target == contentRoot)
        {
            VerticalLayoutGroup layout =
                contentRoot
                    .GetComponent<VerticalLayoutGroup>();

            if (layout == null)
            {
                layout =
                    contentRoot
                        .gameObject
                        .AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing =
                14f;

            layout.padding =
                new RectOffset(0, 0, 8, 24);

            layout.childAlignment =
                TextAnchor.UpperCenter;

            layout.childForceExpandWidth =
                true;

            layout.childForceExpandHeight =
                false;

            ContentSizeFitter fitter =
                contentRoot
                    .GetComponent<ContentSizeFitter>();

            if (fitter == null)
            {
                fitter =
                    contentRoot
                        .gameObject
                        .AddComponent<ContentSizeFitter>();
            }

            fitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            RebuildContentLayout();
        }

        if (target == bottomNavRoot)
        {
            HorizontalLayoutGroup layout =
                bottomNavRoot
                    .GetComponent<HorizontalLayoutGroup>();

            if (layout == null)
            {
                layout =
                    bottomNavRoot
                        .gameObject
                        .AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing =
                8f;

            layout.padding =
                new RectOffset(12, 12, 14, 14);

            layout.childForceExpandWidth =
                true;

            layout.childForceExpandHeight =
                true;
        }
    }

    private string GetServiceLabel(
        CityServiceType service)
    {
        return service switch
        {
            CityServiceType.Travel =>
                "Viajar",

            CityServiceType.Guild =>
                "Guilda dos Gatos Negros",

            CityServiceType.Inn =>
                "Estalagem",

            CityServiceType.Shop =>
                "Loja",

            CityServiceType.QuestBoard =>
                "Mural de Missoes",

            CityServiceType.Tavern =>
                "Taverna",

            CityServiceType.Market =>
                "Mercado",

            CityServiceType.Blacksmith =>
                "Ferreiro",

            CityServiceType.Temple =>
                "Templo",

            CityServiceType.Harbor =>
                "Porto",

            _ => service.ToString()
        };
    }

    private string BuildStatLine(
        StatType statType,
        string label)
    {
        CharacterManager character =
            CharacterManager.Instance;

        if (character == null)
            return $"{label}: 0";

        int baseValue =
            character.GetBaseStat(statType);

        int bonusValue =
            character.GetBonusStat(statType);

        int totalValue =
            character.GetTotalStat(statType);

        return bonusValue != 0
            ? $"{label}: {baseValue} + {bonusValue} = {totalValue}"
            : $"{label}: {totalValue}";
    }

    private string GetEquipmentBonusText(
        ItemData itemData)
    {
        if (itemData == null ||
            itemData.StatModifiers == null ||
            itemData.StatModifiers.Count == 0)
        {
            return "Sem bonus.";
        }

        List<string> parts =
            new();

        foreach (StatModifier modifier
            in itemData.StatModifiers)
        {
            if (modifier == null ||
                modifier.Value == 0)
            {
                continue;
            }

            string sign =
                modifier.Value > 0
                    ? "+"
                    : string.Empty;

            parts.Add(
                $"{GetStatLabel(modifier.StatType)} {sign}{modifier.Value}"
            );
        }

        return parts.Count > 0
            ? string.Join(", ", parts)
            : "Sem bonus.";
    }

    private string GetEquipmentSummary(
        ItemData itemData)
    {
        if (itemData == null)
            return "Sem dados.";

        string handText =
            itemData.IsTwoHanded
                ? "Duas maos"
                : "Uma mao";

        string elementText =
            itemData.AttackElement != ElementType.None
                ? $"\nElemento: {GetElementLabel(itemData.AttackElement)}"
                : string.Empty;

        return $"Bonus: {GetEquipmentBonusText(itemData)}\n" +
            $"Uso: {handText}" +
            elementText;
    }

    private string GetEquipmentCompatibilityText(
        ItemData itemData)
    {
        if (itemData == null ||
            !itemData.IsEquipment)
        {
            return "Nao equipavel";
        }

        List<string> parts =
            new();

        if (itemData.CanEquipInSlot(
            EquipmentSlot.MainHand))
        {
            parts.Add("Mao principal");
        }

        if (itemData.CanEquipInSlot(
            EquipmentSlot.OffHand))
        {
            parts.Add("Mao secundaria");
        }

        if (parts.Count == 0)
        {
            parts.Add(
                GetSlotLabel(itemData.EquipmentSlot)
            );
        }

        return string.Join(", ", parts);
    }

    private string GetStatLabel(
        StatType statType)
    {
        return statType switch
        {
            StatType.Strength => "Forca",

            StatType.Dexterity => "Destreza",

            StatType.Intelligence => "Inteligencia",

            StatType.Faith => "Fe",

            StatType.Vitality => "Vitalidade",

            StatType.Charisma => "Carisma",

            _ => statType.ToString()
        };
    }

    private string GetSlotLabel(
        EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.MainHand => "Mao principal",

            EquipmentSlot.OffHand => "Mao secundaria",

            EquipmentSlot.Head => "Cabeca",

            EquipmentSlot.Chest => "Corpo",

            EquipmentSlot.Legs => "Pernas",

            EquipmentSlot.Accessory => "Acessorio",

            _ => slot.ToString()
        };
    }

    private string GetElementLabel(
        ElementType elementType)
    {
        return elementType switch
        {
            ElementType.None => "Nenhum",

            ElementType.Water => "Agua",

            ElementType.Fire => "Fogo",

            ElementType.Electric => "Eletrico",

            ElementType.Earth => "Terra",

            ElementType.Air => "Ar",

            ElementType.Light => "Luz",

            ElementType.Darkness => "Escuridao",

            _ => elementType.ToString()
        };
    }

    private string GetCompanionRaceLabel(
        CompanionData companionData)
    {
        if (companionData == null)
            return "Indefinida";

        RaceData race =
            companionData.Race != null
                ? companionData.Race
                : DatabaseManager.Instance != null
                    ? DatabaseManager
                        .Instance
                        .GetData<RaceData>(
                            companionData.RaceID
                        )
                    : null;

        return race != null &&
            !string.IsNullOrEmpty(race.DisplayName)
                ? race.DisplayName
                : companionData.RaceID;
    }

    private string GetCompanionArchetypeLabel(
        CompanionData companionData)
    {
        if (companionData == null)
            return "Indefinido";

        StartingArchetypeData archetype =
            companionData.Archetype != null
                ? companionData.Archetype
                : DatabaseManager.Instance != null
                    ? DatabaseManager
                        .Instance
                        .GetData<StartingArchetypeData>(
                            companionData.ArchetypeID
                        )
                    : null;

        return archetype != null &&
            !string.IsNullOrEmpty(archetype.DisplayName)
                ? archetype.DisplayName
                : companionData.ArchetypeID;
    }

    private string GetCombatTurnLabel(
        CombatState state)
    {
        if (state == null ||
            state.CurrentCombatant == null)
        {
            return "Indefinido";
        }

        return state.CurrentCombatant.Type ==
            CombatantType.Player
            ? "Jogador"
            : state.CurrentCombatant.DisplayName;
    }

    private string GetItemTypeLabel(
        ItemData itemData)
    {
        if (itemData == null)
            return "Desconhecido";

        return itemData.Type switch
        {
            ItemType.Consumable => "Consumivel",

            ItemType.Material => "Material",

            ItemType.Quest => "Missao",

            ItemType.Equipment => "Equipamento",

            ItemType.Valuable => "Valioso",

            ItemType.Misc => "Diverso",

            _ => itemData.Type.ToString()
        };
    }

    private void RebuildContentLayout()
    {
        if (contentRoot == null)
            return;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            contentRoot
        );
    }

    private void MoveToScene(
        string sceneName)
    {
        Scene scene =
            SceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return;
        }

        SceneManager.MoveGameObjectToScene(
            gameObject,
            scene
        );
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );

        Scene scene =
            SceneManager.GetSceneByName(
                SceneFlowManager.CitySceneName
            );

        if (scene.IsValid() &&
            scene.isLoaded)
        {
            SceneManager.MoveGameObjectToScene(
                eventSystemObject,
                scene
            );
        }
    }
}
