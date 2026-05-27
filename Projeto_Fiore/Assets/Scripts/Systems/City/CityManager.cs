using UnityEngine;

public class CityManager
    : PersistentSingleton<
        CityManager>
{
    public CityData CurrentCity
    {
        get
        {
            return TravelManager
                .Instance
                .CurrentCity;
        }
    }

    public void EnterCity()
    {
        CityData city =
            CurrentCity;

        if (city == null)
        {
            Debug.LogWarning(
                "Cannot enter city because current city was not found."
            );

            return;
        }

        Debug.Log(
            $"Entered city: {city.DisplayName}"
        );

        WorldStateManager
            .Instance
            ?.TriggerWorldEvents(
                EventTriggerType.OnEnterCity,
                city
            );

        QuestManager
            .Instance
            .CheckQuestProgress();
    }

    private void ShowAvailableServices()
    {
        Debug.Log(
            "Available Services:"
        );

        foreach (var service
            in CurrentCity.Services)
        {
            Debug.Log(service);
        }
    }

    public void Rest(int hours = 8)
    {
        Debug.Log(
            $"Resting for " +
            $"{hours}h"
        );

        TimeManager.Instance
            .AdvanceHours(hours);

        SaveManager.Instance
            .SaveGame();
    }

    public bool HasService(
        CityServiceType type)
    {
        return CurrentCity
            .Services
            .Contains(type);
    }

    public void OpenGuild()
    {
        if (!HasService(
            CityServiceType.Guild))
        {
            Debug.Log(
                "No Guild Here"
            );

            return;
        }

        Debug.Log(
            "Guild Opened"
        );

        if (!MobileHUDManager.TryShowScreen(
            UIScreenType.Guild))
        {
            GuildUIController.OpenOrCreate();
        }
    }
}
