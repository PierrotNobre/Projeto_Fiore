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
        Debug.Log(
            $"Entered city: {CurrentCity.DisplayName}"
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
    }
}