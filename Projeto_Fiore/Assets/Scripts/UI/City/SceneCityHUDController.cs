using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneCityHUDController : MonoBehaviour
{
    private const int DefaultOpenHour = 8;
    private const int DefaultCloseHour = 21;

    private GameObject hudRoot;

    private RectTransform servicesContainer;
    private RectTransform locationsContainer;
    private RectTransform socialContainer;

    private TMP_Text playerNameText;
    private TMP_Text playerLevelText;
    private TMP_Text playerHealthText;
    private TMP_Text playerEnergyText;

    private TMP_Text strengthText;
    private TMP_Text dexterityText;
    private TMP_Text intelligenceText;
    private TMP_Text charismaText;
    private TMP_Text faithText;
    private TMP_Text vitalityText;

    private Image playerPortraitImage;
    private Image playerHealthFill;
    private Image playerEnergyFill;

    private RectTransform partyRoot;
    private readonly List<RectTransform> partySlots = new();

    private Image cityImage;
    private TMP_Text cityNameText;
    private TMP_Text kingdomNameText;
    private TMP_Text timeOfDayText;
    private TMP_Text dateText;
    private TMP_Text hourText;
    private TMP_Text seasonText;

    private RectTransform bottomMenuRoot;

    private bool hierarchyBound;
    private bool subscribed;

    public static SceneCityHUDController GetOrCreateFromScene()
    {
        SceneCityHUDController existing =
            FindLoadedController();

        if (existing != null)
            return existing;

        GameObject cityArea =
            GameObject.Find("CityArea");

        if (cityArea == null)
            return null;

        Canvas canvas =
            cityArea.GetComponentInParent<Canvas>();

        GameObject host =
            canvas != null
                ? canvas.gameObject
                : cityArea;

        SceneCityHUDController controller =
            host.GetComponent<SceneCityHUDController>();

        if (controller == null)
        {
            controller =
                host.AddComponent<SceneCityHUDController>();
        }

        controller.hudRoot =
            cityArea;

        controller.BindHierarchy();

        return controller;
    }

    public void ShowAndRefresh()
    {
        BindHierarchy();
        Subscribe();
        SetVisible(true);
        RefreshAll();
    }

    public void SetVisible(
        bool visible)
    {
        BindHierarchy();

        if (hudRoot != null)
        {
            hudRoot.SetActive(visible);
        }
    }

    public void RefreshAll()
    {
        if (!isActiveAndEnabled ||
            SaveManager.Instance == null ||
            !SaveManager.Instance.HasActiveGame)
        {
            return;
        }

        BindHierarchy();

        RefreshPlayerPanel();
        RefreshCityInfoPanel();
        RefreshServicesPanel();
        RefreshLocationsPanel();
        RefreshSocialPanel();
        BindBottomMenu();
    }

    private static SceneCityHUDController FindLoadedController()
    {
        SceneCityHUDController[] controllers =
            Resources
                .FindObjectsOfTypeAll<SceneCityHUDController>();

        foreach (SceneCityHUDController controller
            in controllers)
        {
            if (controller == null ||
                controller.gameObject == null ||
                !controller.gameObject.scene.IsValid())
            {
                continue;
            }

            if (controller.gameObject.scene.name ==
                SceneFlowManager.CitySceneName)
            {
                return controller;
            }
        }

        return null;
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        GameEvents.OnTimeAdvanced += RefreshAll;
        GameEvents.OnTimeOfDayChanged += RefreshTimeOfDay;
        GameEvents.OnTravelFinished += RefreshAll;
        GameEvents.OnTravelStarted += RefreshAll;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        GameEvents.OnTimeAdvanced -= RefreshAll;
        GameEvents.OnTimeOfDayChanged -= RefreshTimeOfDay;
        GameEvents.OnTravelFinished -= RefreshAll;
        GameEvents.OnTravelStarted -= RefreshAll;

        subscribed = false;
    }

    private void RefreshTimeOfDay(
        TimeOfDay timeOfDay)
    {
        RefreshAll();
    }

    private void BindHierarchy()
    {
        if (hierarchyBound &&
            hudRoot != null)
        {
            return;
        }

        if (hudRoot == null)
        {
            Transform cityArea =
                FindTransformByName(
                    transform.root,
                    "CityArea"
                );

            hudRoot =
                cityArea != null
                    ? cityArea.gameObject
                    : gameObject;
        }

        Transform root =
            hudRoot.transform;

        Transform playerPanel =
            FindTransformByName(
                root,
                "PlayerPanel"
            );

        Transform cityPanel =
            FindTransformByName(
                root,
                "CityPanel"
            );

        Transform servicesPanel =
            FindTransformByName(
                root,
                "ServicesPanel"
            );

        Transform placesPanel =
            FindTransformByName(
                root,
                "PlacesPanel"
            );

        Transform socialPanel =
            FindTransformByName(
                root,
                "SocialPanel"
            );

        bottomMenuRoot =
            FindTransformByName(
                root,
                "PlayerMenu"
            ) as RectTransform;

        BindPlayerPanel(playerPanel);
        BindCityPanel(cityPanel);

        servicesContainer =
            ResolveHorizontalContainer(servicesPanel);

        locationsContainer =
            ResolveHorizontalContainer(placesPanel);

        socialContainer =
            ResolveHorizontalContainer(socialPanel);

        SetTitleText(
            servicesPanel,
            "ServicesTitleTxt",
            "Servicos"
        );

        SetTitleText(
            placesPanel,
            "PlacesTitleTxt",
            "Locais"
        );

        SetTitleText(
            socialPanel,
            "SocialTitleTxt",
            "Social"
        );

        BindBottomMenu();

        hierarchyBound = true;
    }

    private void BindPlayerPanel(
        Transform playerPanel)
    {
        playerNameText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "PlayerNameTXT"
            );

        playerLevelText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "PlayerLevelTXT"
            );

        playerHealthText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "LifeQuantityTXT"
            );

        playerEnergyText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "EnergyQuantityTXT"
            );

        strengthText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "StrengthTxt"
            );

        dexterityText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "DexterityTxt"
            );

        intelligenceText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "InteligenceTxt"
            );

        charismaText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "CharismaTxt"
            );

        faithText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "FaithTxt"
            );

        vitalityText =
            FindComponentByName<TMP_Text>(
                playerPanel,
                "VitalityTxt"
            );

        Transform playerButton =
            FindTransformByName(
                playerPanel,
                "PlayerButton"
            );

        if (playerButton != null)
        {
            Button button =
                playerButton.GetComponent<Button>();

            playerPortraitImage =
                playerButton.GetComponent<Image>();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(
                    () =>
                    {
                        MobileHUDManager.TryShowScreen(
                            UIScreenType.Character
                        );
                    }
                );
            }
        }

        Transform healthBar =
            FindTransformByName(
                playerPanel,
                "PlayerHealthBar"
            );

        playerHealthFill =
            FindComponentByName<Image>(
                healthBar,
                "HealthBarFill"
            ) ??
            FindComponentByName<Image>(
                healthBar,
                "HealthBarImage"
            );

        Transform energyBar =
            FindTransformByName(
                playerPanel,
                "PlayerEnergyBar"
            );

        playerEnergyFill =
            FindComponentByName<Image>(
                energyBar,
                "EnergyBarFill"
            ) ??
            FindComponentByName<Image>(
                energyBar,
                "EnergyBarImage"
            );

        partyRoot =
            FindTransformByName(
                playerPanel,
                "PlayerParty"
            ) as RectTransform;

        partySlots.Clear();

        if (partyRoot != null)
        {
            foreach (RectTransform slot
                in partyRoot
                    .GetComponentsInChildren<RectTransform>(
                        true
                    )
                    .Where(rect =>
                        rect != partyRoot &&
                        rect.name.StartsWith(
                            "PartyNPC#",
                            StringComparison.Ordinal
                        ))
                    .OrderBy(rect => rect.name))
            {
                partySlots.Add(slot);
            }
        }
    }

    private void BindCityPanel(
        Transform cityPanel)
    {
        cityNameText =
            FindComponentByName<TMP_Text>(
                cityPanel,
                "CityNameTxt"
            );

        kingdomNameText =
            FindComponentByName<TMP_Text>(
                cityPanel,
                "KingdomNameTxt"
            );

        timeOfDayText =
            FindComponentByName<TMP_Text>(
                cityPanel,
                "DayTimeTxt"
            );

        dateText =
            FindComponentByName<TMP_Text>(
                cityPanel,
                "DateTxt"
            );

        hourText =
            FindComponentByName<TMP_Text>(
                cityPanel,
                "HourTxt"
            );

        seasonText =
            FindComponentByName<TMP_Text>(
                cityPanel,
                "SeasonTxt"
            );

        cityImage =
            FindComponentByName<Image>(
                cityPanel,
                "CityImage"
            );
    }

    private void RefreshPlayerPanel()
    {
        SaveData save =
            SaveManager.Instance.CurrentSave;

        PlayerData player =
            save.Player;

        PlayerStatsData stats =
            save.Stats;

        stats.EnsureRuntimeDefaults();
        player.EnsureRuntimeDefaults();

        CharacterManager character =
            CharacterManager.GetOrCreate();

        character.ClampVitalsToCurrentMaximum();

        SetText(
            playerNameText,
            string.IsNullOrEmpty(player.PlayerName)
                ? "Player"
                : player.PlayerName
        );

        SetText(
            playerLevelText,
            $"level: {stats.Level}"
        );

        SetText(
            playerHealthText,
            $"{stats.CurrentHP}/{character.MaxHP}"
        );

        SetText(
            playerEnergyText,
            $"{stats.CurrentStamina}/{character.MaxStamina}"
        );

        SetBar(
            playerHealthFill,
            stats.CurrentHP,
            character.MaxHP,
            new Color(0.62f, 0.08f, 0.08f, 1f)
        );

        SetBar(
            playerEnergyFill,
            stats.CurrentStamina,
            character.MaxStamina,
            new Color(0.05f, 0.32f, 0.66f, 1f)
        );

        SetImagePreservingManualSprite(
            playerPortraitImage,
            GetPlayerPortrait(player),
            new Color(1f, 1f, 1f, 1f)
        );

        SetText(
            strengthText,
            $"Strength {character.GetTotalStat(StatType.Strength)}"
        );

        SetText(
            dexterityText,
            $"Dexterity {character.GetTotalStat(StatType.Dexterity)}"
        );

        SetText(
            intelligenceText,
            $"Intelligence {character.GetTotalStat(StatType.Intelligence)}"
        );

        SetText(
            charismaText,
            $"Charisma {character.GetTotalStat(StatType.Charisma)}"
        );

        SetText(
            faithText,
            $"Fe {character.GetTotalStat(StatType.Faith)}"
        );

        SetText(
            vitalityText,
            $"Vitalidade {character.GetTotalStat(StatType.Vitality)}"
        );

        RefreshPartySlots();
    }

    private void RefreshPartySlots()
    {
        CompanionManager companionManager =
            CompanionManager.GetOrCreate();

        List<CompanionState> activeCompanions =
            companionManager.GetActivePartyCompanions();

        for (int i = 0;
            i < partySlots.Count;
            i++)
        {
            RectTransform slot =
                partySlots[i];

            bool hasCompanion =
                i < activeCompanions.Count;

            slot.gameObject.SetActive(hasCompanion);

            if (!hasCompanion)
                continue;

            CompanionState state =
                activeCompanions[i];

            CompanionData data =
                companionManager.GetCompanionById(
                    state.CompanionID
                );

            state.EnsureRuntimeDefaults();

            string displayName =
                data != null &&
                !string.IsNullOrEmpty(data.DisplayName)
                    ? data.DisplayName
                    : state.CompanionID;

            TMP_Text label =
                slot.GetComponentInChildren<TMP_Text>(
                    true
                );

            SetText(
                label,
                $"{displayName}\nlvl: {state.Level}\n" +
                $"{state.CurrentVitals.CurrentHealth}/{state.CurrentVitals.MaxHealth} HP\n" +
                $"{state.CurrentVitals.CurrentEnergy}/{state.CurrentVitals.MaxEnergy} EN"
            );

            Image portrait =
                FindComponentByName<Image>(
                    slot,
                    "PartyNPCImage"
                );

            SetImagePreservingManualSprite(
                portrait,
                companionManager.GetCompanionPortrait(
                    state.CompanionID
                ),
                new Color(1f, 1f, 1f, 1f)
            );

            Button button =
                slot.GetComponent<Button>() ??
                slot.gameObject.AddComponent<Button>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () =>
                {
                    MobileHUDManager.TryShowScreen(
                        UIScreenType.Party
                    );
                }
            );
        }
    }

    private void RefreshCityInfoPanel()
    {
        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        TimeManager time =
            TimeManager.Instance;

        if (city == null)
        {
            SetText(cityNameText, "Cidade");
            SetText(kingdomNameText, string.Empty);
            return;
        }

        SetText(cityNameText, city.DisplayName);
        SetText(kingdomNameText, city.Kingdom.ToString());

        if (time != null)
        {
            TimeData currentTime =
                time.CurrentTime;

            SetText(
                timeOfDayText,
                time.GetCurrentTimeOfDayDisplayName()
            );

            SetText(
                dateText,
                $"{currentTime.Day:00}/{currentTime.Month:00}/{currentTime.Year:0000}"
            );

            SetText(
                hourText,
                $"{currentTime.Hour:00}:{currentTime.Minute:00}"
            );

            SetText(
                seasonText,
                time.GetCurrentSeasonDisplayName()
            );
        }

        SetImagePreservingManualSprite(
            cityImage,
            city.Icon,
            new Color(0.82f, 0.82f, 0.82f, 1f)
        );
    }

    private void RefreshServicesPanel()
    {
        ClearContainer(servicesContainer);

        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        if (city == null)
        {
            CreateMessageCard(
                servicesContainer,
                "Cidade nao encontrada."
            );

            return;
        }

        List<ServiceEntry> services =
            CollectServiceEntries(city);

        if (services.Count == 0)
        {
            CreateMessageCard(
                servicesContainer,
                "Nenhum servico disponivel."
            );

            return;
        }

        foreach (ServiceEntry entry
            in services)
        {
            bool availableNow =
                IsServiceOpen(entry.Service) &&
                entry.LocationAccessible;

            string subtitle =
                $"{GetServiceLabel(entry.Service)}\n" +
                $"{GetServiceHoursText(entry.Service)}\n" +
                (availableNow
                    ? "Disponivel"
                    : !entry.LocationAccessible
                        ? "Bloqueado"
                        : "Fechado agora");

            CreateCard(
                servicesContainer,
                $"Service_{entry.Service}_{entry.LocationID}",
                entry.Sprite,
                entry.DisplayName,
                subtitle,
                availableNow,
                () =>
                {
                    if (!entry.LocationAccessible)
                    {
                        GameFeedbackUI.ShowNotification(
                            "Local bloqueado."
                        );

                        return;
                    }

                    if (!IsServiceOpen(entry.Service))
                    {
                        GameFeedbackUI.ShowNotification(
                            "Este servico esta fechado agora."
                        );

                        return;
                    }

                    MobileHUDManager.OpenCityService(
                        entry.Service,
                        entry.Location
                    );
                },
                new Vector2(230f, 250f)
            );
        }
    }

    private void RefreshLocationsPanel()
    {
        ClearContainer(locationsContainer);

        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        if (city == null ||
            city.Locations == null ||
            city.Locations.Count == 0)
        {
            CreateMessageCard(
                locationsContainer,
                "Nenhum local disponivel."
            );

            return;
        }

        bool anyLocation =
            false;

        foreach (CityLocationData location
            in city.Locations)
        {
            if (location == null ||
                !location.ShowInCityScreen)
            {
                continue;
            }

            anyLocation =
                true;

            bool accessible =
                RequirementChecker.AreRequirementsMet(
                    location.UnlockRequirements
                );

            CreateCard(
                locationsContainer,
                $"Location_{location.LocationID}",
                location.LocationSprite,
                location.DisplayName,
                accessible
                    ? location.Description
                    : "Bloqueado",
                accessible,
                () =>
                {
                    if (!accessible)
                    {
                        GameFeedbackUI.ShowNotification(
                            "Local bloqueado."
                        );

                        return;
                    }

                    MobileHUDManager.OpenLocation(
                        location
                    );
                },
                new Vector2(300f, 210f)
            );
        }

        if (!anyLocation)
        {
            CreateMessageCard(
                locationsContainer,
                "Nenhum local disponivel."
            );
        }
    }

    private void RefreshSocialPanel()
    {
        ClearContainer(socialContainer);

        List<NPCData> npcs =
            NPCManager.Instance != null
                ? NPCManager
                    .Instance
                    .GetPublicNPCsInCurrentCity()
                : new List<NPCData>();

        if (npcs.Count == 0)
        {
            CreateMessageCard(
                socialContainer,
                "Nao ha ninguem nas ruas agora."
            );

            return;
        }

        foreach (NPCData npc
            in npcs)
        {
            if (npc == null)
                continue;

            string subtitle =
                BuildNPCSubtitle(npc);

            CreateCard(
                socialContainer,
                $"NPC_{npc.ID}",
                GetNPCPortrait(npc),
                npc.DisplayName,
                subtitle,
                true,
                () =>
                {
                    MobileHUDManager.OpenNPCInteraction(
                        npc
                    );
                },
                new Vector2(210f, 250f)
            );
        }
    }

    private void BindBottomMenu()
    {
        if (bottomMenuRoot == null)
            return;

        List<Button> buttons =
            bottomMenuRoot
                .GetComponentsInChildren<Button>(
                    true
                )
                .OrderBy(button =>
                    button
                        .GetComponent<RectTransform>()
                        .anchoredPosition
                        .x)
                .ToList();

        if (buttons.Count == 0)
            return;

        BindBottomButton(
            buttons[0],
            "Cidade",
            UIScreenType.City,
            true
        );

        if (buttons.Count > 1)
        {
            BindBottomButton(
                buttons[1],
                "Missoes",
                UIScreenType.Quests,
                false
            );
        }

        if (buttons.Count > 2)
        {
            BindBottomButton(
                buttons[2],
                "Mapa",
                UIScreenType.Travel,
                false
            );
        }

        if (buttons.Count > 3)
        {
            BindBottomButton(
                buttons[3],
                "Inventario",
                UIScreenType.Inventory,
                false
            );
        }
    }

    private void BindBottomButton(
        Button button,
        string label,
        UIScreenType screen,
        bool active)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(
            () =>
            {
                MobileHUDManager.TryShowScreen(screen);
            }
        );

        TMP_Text text =
            button.GetComponentInChildren<TMP_Text>(
                true
            );

        if (text != null)
        {
            text.text = label;
        }

        Image image =
            button.GetComponent<Image>();

        if (image != null)
        {
            image.color =
                active
                    ? new Color(1f, 0.92f, 0.62f, 1f)
                    : Color.white;
        }
    }

    private List<ServiceEntry> CollectServiceEntries(
        CityData city)
    {
        List<ServiceEntry> result =
            new();

        HashSet<CityServiceType> locationServices =
            new();

        if (city.Locations != null)
        {
            foreach (CityLocationData location
                in city.Locations)
            {
                if (location == null ||
                    !location.ShowInCityScreen ||
                    location.Services == null ||
                    location.Services.Count == 0)
                {
                    continue;
                }

                CityServiceType primaryService =
                    GetPrimaryService(location);

                if (primaryService == CityServiceType.None ||
                    primaryService == CityServiceType.Travel)
                {
                    continue;
                }

                foreach (CityServiceType service
                    in location.Services)
                {
                    if (service != CityServiceType.None &&
                        service != CityServiceType.Travel)
                    {
                        locationServices.Add(service);
                    }
                }

                result.Add(
                    new ServiceEntry
                    {
                        Service = primaryService,
                        DisplayName = location.DisplayName,
                        Location = location,
                        LocationID = location.LocationID,
                        Sprite = location.LocationSprite,
                        LocationAccessible =
                            RequirementChecker
                                .AreRequirementsMet(
                                    location.UnlockRequirements
                                )
                    }
                );
            }
        }

        if (city.Services != null)
        {
            foreach (CityServiceType service
                in city.Services)
            {
                if (service == CityServiceType.None ||
                    service == CityServiceType.Travel ||
                    locationServices.Contains(service) ||
                    result.Any(entry =>
                        entry.Location == null &&
                        entry.Service == service))
                {
                    continue;
                }

                result.Add(
                    new ServiceEntry
                    {
                        Service = service,
                        DisplayName = GetServiceLabel(service),
                        Location = null,
                        LocationID = "city",
                        Sprite = null,
                        LocationAccessible = true
                    }
                );
            }
        }

        return result;
    }

    private static CityServiceType GetPrimaryService(
        CityLocationData location)
    {
        if (location == null ||
            location.Services == null)
        {
            return CityServiceType.None;
        }

        CityServiceType[] priority =
        {
            CityServiceType.Guild,
            CityServiceType.Shop,
            CityServiceType.Market,
            CityServiceType.Inn,
            CityServiceType.Tavern,
            CityServiceType.QuestBoard,
            CityServiceType.Blacksmith,
            CityServiceType.Temple,
            CityServiceType.Harbor
        };

        foreach (CityServiceType service
            in priority)
        {
            if (location.Services.Contains(service))
                return service;
        }

        return location.Services
            .FirstOrDefault(service =>
                service != CityServiceType.None &&
                service != CityServiceType.Travel);
    }

    private static bool IsServiceOpen(
        CityServiceType service)
    {
        if (TimeManager.Instance == null)
            return true;

        if (service == CityServiceType.None ||
            service == CityServiceType.Travel)
        {
            return false;
        }

        int hour =
            TimeManager
                .Instance
                .CurrentTime
                .Hour;

        return hour >= DefaultOpenHour &&
            hour < DefaultCloseHour;
    }

    private static string GetServiceHoursText(
        CityServiceType service)
    {
        if (service == CityServiceType.None ||
            service == CityServiceType.Travel)
        {
            return "Indisponivel";
        }

        return $"{DefaultOpenHour:00}:00 - {DefaultCloseHour:00}:00";
    }

    private static string BuildNPCSubtitle(
        NPCData npc)
    {
        string subtitle =
            string.Empty;

        if (RelationshipManager.Instance != null)
        {
            NPCRelationshipLevel level =
                RelationshipManager
                    .Instance
                    .GetFriendshipLevel(npc.ID);

            subtitle =
                $"Amizade: {level}";
        }

        CompanionData companion =
            CompanionManager
                .GetOrCreate()
                .GetCompanionByNPCId(
                    npc.ID
                );

        if (companion != null)
        {
            subtitle =
                string.IsNullOrEmpty(subtitle)
                    ? "Recrutavel"
                    : $"{subtitle}\nRecrutavel";
        }

        return string.IsNullOrEmpty(subtitle)
            ? "Disponivel"
            : subtitle;
    }

    private static Sprite GetPlayerPortrait(
        PlayerData player)
    {
        if (player == null ||
            string.IsNullOrEmpty(player.PortraitID))
        {
            return null;
        }

        return Resources.Load<Sprite>(
            player.PortraitID
        );
    }

    private static Sprite GetNPCPortrait(
        NPCData npc)
    {
        if (npc == null)
            return null;

        if (npc.Portrait != null)
            return npc.Portrait;

        if (npc.IconSprite != null)
            return npc.IconSprite;

        return npc.FullBodySprite;
    }

    private static void SetText(
        TMP_Text text,
        string value)
    {
        if (text == null)
            return;

        text.text = value ?? string.Empty;
    }

    private static void SetTitleText(
        Transform root,
        string childName,
        string value)
    {
        TMP_Text text =
            FindComponentByName<TMP_Text>(
                root,
                childName
            );

        SetText(
            text,
            value
        );
    }

    private static void SetImagePreservingManualSprite(
        Image image,
        Sprite sprite,
        Color fallbackColor)
    {
        if (image == null)
            return;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else if (image.sprite == null)
        {
            image.color = fallbackColor;
        }

        image.preserveAspect = true;
    }

    private static void SetBar(
        Image fill,
        int current,
        int max,
        Color color)
    {
        if (fill == null)
            return;

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount =
            max <= 0
                ? 0f
                : Mathf.Clamp01(
                    (float)current / max
                );

        fill.color = color;
    }

    private static RectTransform ResolveHorizontalContainer(
        Transform panel)
    {
        if (panel == null)
            return null;

        ScrollRect scroll =
            panel.GetComponentInChildren<ScrollRect>(
                true
            );

        RectTransform container =
            scroll != null
                ? scroll.content
                : null;

        if (container == null)
        {
            Transform containerTransform =
                FindTransformByName(
                    panel,
                    "Container"
                );

            container =
                containerTransform as RectTransform;
        }

        if (container == null)
        {
            GameObject containerObject =
                new GameObject(
                    "Container",
                    typeof(RectTransform)
                );

            container =
                containerObject
                    .GetComponent<RectTransform>();

            container.SetParent(
                panel,
                false
            );
        }

        if (scroll != null)
        {
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.content = container;
        }

        HorizontalLayoutGroup layout =
            container.GetComponent<HorizontalLayoutGroup>();

        if (layout == null)
        {
            layout =
                container
                    .gameObject
                    .AddComponent<HorizontalLayoutGroup>();
        }

        layout.spacing = 14f;
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        ContentSizeFitter fitter =
            container.GetComponent<ContentSizeFitter>();

        if (fitter == null)
        {
            fitter =
                container
                    .gameObject
                    .AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        fitter.verticalFit =
            ContentSizeFitter.FitMode.Unconstrained;

        return container;
    }

    private static void ClearContainer(
        Transform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1;
            i >= 0;
            i--)
        {
            GameObject child =
                container
                    .GetChild(i)
                    .gameObject;

            child.SetActive(false);
            Destroy(child);
        }
    }

    private static void CreateMessageCard(
        Transform container,
        string message)
    {
        CreateCard(
            container,
            "MessageCard",
            null,
            message,
            string.Empty,
            false,
            null,
            new Vector2(360f, 140f)
        );
    }

    private static Button CreateCard(
        Transform container,
        string objectName,
        Sprite sprite,
        string title,
        string subtitle,
        bool available,
        Action onClick,
        Vector2 size)
    {
        if (container == null)
            return null;

        GameObject card =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement)
            );

        RectTransform rect =
            card.GetComponent<RectTransform>();

        rect.SetParent(
            container,
            false
        );

        rect.sizeDelta = size;

        LayoutElement layout =
            card.GetComponent<LayoutElement>();

        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.minWidth = size.x;
        layout.minHeight = size.y;

        Image background =
            card.GetComponent<Image>();

        background.color =
            available
                ? new Color(0.12f, 0.1f, 0.075f, 0.96f)
                : new Color(0.08f, 0.08f, 0.08f, 0.72f);

        Button button =
            card.GetComponent<Button>();

        button.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            button.onClick.AddListener(
                () => onClick()
            );
        }

        VerticalLayoutGroup layoutGroup =
            card.AddComponent<VerticalLayoutGroup>();

        layoutGroup.padding =
            new RectOffset(10, 10, 10, 10);

        layoutGroup.spacing = 6f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        Image portrait =
            CreateImage(
                "Image",
                rect,
                sprite,
                available
                    ? new Color(0.86f, 0.86f, 0.86f, 1f)
                    : new Color(0.46f, 0.46f, 0.46f, 1f),
                size.y * 0.52f
            );

        portrait.preserveAspect = true;

        TMP_Text titleText =
            CreateText(
                "Title",
                rect,
                title,
                25f,
                42f,
                TextAlignmentOptions.Center
            );

        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 14f;
        titleText.fontSizeMax = 25f;

        if (!string.IsNullOrEmpty(subtitle))
        {
            TMP_Text subtitleText =
                CreateText(
                    "Subtitle",
                    rect,
                    subtitle,
                    17f,
                    Mathf.Max(45f, size.y * 0.22f),
                    TextAlignmentOptions.Center
                );

            subtitleText.enableAutoSizing = true;
            subtitleText.fontSizeMin = 11f;
            subtitleText.fontSizeMax = 17f;
        }

        return button;
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Sprite sprite,
        Color color,
        float height)
    {
        GameObject imageObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement)
            );

        imageObject.transform.SetParent(
            parent,
            false
        );

        Image image =
            imageObject.GetComponent<Image>();

        image.sprite = sprite;
        image.color =
            sprite != null
                ? Color.white
                : color;

        LayoutElement layout =
            imageObject.GetComponent<LayoutElement>();

        layout.preferredHeight = height;
        layout.minHeight = height;

        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        float height,
        TextAlignmentOptions alignment)
    {
        GameObject textObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement)
            );

        textObject.transform.SetParent(
            parent,
            false
        );

        TMP_Text text =
            textObject.GetComponent<TMP_Text>();

        text.text = value ?? string.Empty;
        text.fontSize = fontSize;
        text.color = new Color(0.96f, 0.86f, 0.66f, 1f);
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement layout =
            textObject.GetComponent<LayoutElement>();

        layout.preferredHeight = height;
        layout.minHeight = height;

        return text;
    }

    private static Transform FindTransformByName(
        Transform root,
        string objectName)
    {
        if (root == null ||
            string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        if (root.name == objectName)
            return root;

        for (int i = 0;
            i < root.childCount;
            i++)
        {
            Transform found =
                FindTransformByName(
                    root.GetChild(i),
                    objectName
                );

            if (found != null)
                return found;
        }

        return null;
    }

    private static T FindComponentByName<T>(
        Transform root,
        string objectName)
        where T : Component
    {
        Transform transform =
            FindTransformByName(
                root,
                objectName
            );

        return transform != null
            ? transform.GetComponent<T>()
            : null;
    }

    private static string GetServiceLabel(
        CityServiceType service)
    {
        return service switch
        {
            CityServiceType.Tavern => "Taverna",
            CityServiceType.Market => "Mercado",
            CityServiceType.Guild => "Guilda dos Gatos Negros",
            CityServiceType.Blacksmith => "Ferreiro",
            CityServiceType.Temple => "Templo",
            CityServiceType.Harbor => "Porto",
            CityServiceType.Inn => "Estalagem",
            CityServiceType.Shop => "Loja Geral",
            CityServiceType.QuestBoard => "Mural de Missoes",
            _ => service.ToString()
        };
    }

    private class ServiceEntry
    {
        public CityServiceType Service;
        public string DisplayName;
        public CityLocationData Location;
        public string LocationID;
        public Sprite Sprite;
        public bool LocationAccessible;
    }
}
