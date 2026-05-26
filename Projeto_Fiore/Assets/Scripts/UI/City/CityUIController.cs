using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CityUIController : MonoBehaviour
{
    [Header("Header")]
    [SerializeField]
    private TMP_Text cityNameText;

    [SerializeField]
    private TMP_Text dateText;

    [SerializeField]
    private TMP_Text goldText;

    [Header("Containers")]
    [SerializeField]
    private Transform servicesContainer;

    [SerializeField]
    private Transform npcContainer;

    [Header("Prefabs")]
    [SerializeField]
    private UIButtonEntry buttonPrefab;

    [Header("Travel UI")]
    [SerializeField]
    private TravelUIController travelUIController;

    public void Refresh()
    {
        UpdateHeader();

        BuildServices();

        BuildNPCs();
    }

    private void UpdateHeader()
    {
        CityData city =
            CityManager
            .Instance
            .CurrentCity;

        cityNameText.text =
            city.DisplayName;

        dateText.text =
            TimeManager
            .Instance
            .GetFormattedDate();

        goldText.text =
            $"Ouro: {SaveManager.Instance.CurrentSave.Player.Gold}";
    }

    private void BuildServices()
    {
        ClearContainer(
            servicesContainer
        );

        CityData city =
            CityManager
            .Instance
            .CurrentCity;

        foreach (var service
            in city.Services)
        {
            if (service == CityServiceType.None)
                continue;

            UIButtonEntry entry =
                Instantiate(
                    buttonPrefab,
                    servicesContainer
                );

            entry.Setup(
                GetServiceLabel(service),
                () => OpenService(service)
            );
        }
    }

    private void BuildNPCs()
    {
        ClearContainer(
            npcContainer
        );

        List<NPCData> npcs =
            NPCManager
            .Instance
            .GetNPCsInCurrentCity();

        foreach (var npc in npcs)
        {
            UIButtonEntry entry =
                Instantiate(
                    buttonPrefab,
                    npcContainer
                );

            entry.Setup(
                npc.DisplayName,
                () => TalkToNPC(npc)
            );
        }
    }

    private void OpenService(
        CityServiceType service)
    {
        Debug.Log(
            $"Opening service: {service}"
        );

        switch (service)
        {
            case CityServiceType.Guild:
                CityManager
                    .Instance
                    .OpenGuild();
                break;

            case CityServiceType.Tavern:
                Debug.Log("Tavern opened.");
                break;

            case CityServiceType.Market:
                Debug.Log("Market opened.");
                break;

            case CityServiceType.Blacksmith:
                Debug.Log("Blacksmith opened.");
                break;

            case CityServiceType.Temple:
                Debug.Log("Temple opened.");
                break;

            case CityServiceType.Harbor:
                Debug.Log("Harbor opened.");
                break;
        }

        Refresh();
    }

    private void TalkToNPC(
        NPCData npc)
    {
        NPCManager
            .Instance
            .TalkToNPC(npc);
    }

    public void Rest()
    {
        CityManager
            .Instance
            .Rest(8);

        Refresh();
    }

    public void OpenTravel()
    {
        travelUIController.Open();
    }

    public void OpenInventory()
    {
        Debug.Log(
            "Inventory UI will open here."
        );
    }

    private void ClearContainer(
        Transform container)
    {
        for (int i =
            container.childCount - 1;
            i >= 0;
            i--)
        {
            Destroy(
                container
                    .GetChild(i)
                    .gameObject
            );
        }
    }

    private string GetServiceLabel(
        CityServiceType service)
    {
        return service switch
        {
            CityServiceType.Tavern =>
                "Taverna",

            CityServiceType.Market =>
                "Mercado",

            CityServiceType.Guild =>
                "Guilda",

            CityServiceType.Blacksmith =>
                "Ferreiro",

            CityServiceType.Temple =>
                "Templo",

            CityServiceType.Harbor =>
                "Porto",

            _ => service.ToString()
        };
    }

    private void OnEnable()
    {
        GameEvents.OnTravelFinished += Refresh;
        GameEvents.OnTimeAdvanced += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnTravelFinished -= Refresh;
        GameEvents.OnTimeAdvanced -= Refresh;
    }
}