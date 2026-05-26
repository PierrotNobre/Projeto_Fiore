using UnityEngine;

public class TravelManager
    : PersistentSingleton<TravelManager>
{
    private const int TRAVEL_TICK = 6;

    public bool IsTraveling =>
        SaveManager.Instance
        .CurrentSave
        .Travel
        .IsTraveling;

    public CityData CurrentCity
    {
        get
        {
            return DatabaseManager.Instance
                .GetData<CityData>(
                    SaveManager
                    .Instance
                    .CurrentSave
                    .Location
                    .CurrentCityID
                );
        }
    }

    public void TravelTo(
        CityData destination
    )
    {
        if (destination == null)
            return;

        int travelHours =
            GetTravelTime(destination);

        SaveManager.Instance
            .CurrentSave
            .Travel =
            new TravelSession
            {
                OriginCityID =
                    CurrentCity.ID,

                DestinationCityID =
                    destination.ID,

                RemainingHours =
                    travelHours,

                IsTraveling = true
            };

        GameManager.Instance
            .ChangeState(
                GameState.Travel
            );
        GameEvents.OnTravelStarted?.Invoke();

        Debug.Log(
            $"Started travel to " +
            $"{destination.DisplayName}"
        );

        //ContinueTravel();
    }

    public void ContinueTravel()
    {

        if (!IsTraveling)
            return;

        if (TravelEventManager.Instance.HasActiveEvent)
        {
            Debug.Log(
                "Cannot continue travel while an event is active."
            );

            return;
        }

        var travel =
            SaveManager.Instance
            .CurrentSave
            .Travel;

        int tick =
            Mathf.Min(
                TRAVEL_TICK,
                travel.RemainingHours
            );

        travel.RemainingHours -= tick;

        TimeManager.Instance
            .AdvanceHours(tick);

        Debug.Log(
            $"Travel Tick: " +
            $"{tick}h " +
            $"Remaining: " +
            $"{travel.RemainingHours}"
        );

        TravelEventManager.Instance.TryTriggerEvent();

        if (travel.RemainingHours <= 0)
        {
            ArriveAtDestination();
        }
    }

    private void CheckTravelEvent()
    {
        Debug.Log(
            "Checking travel event..."
        );
    }

    private void ArriveAtDestination()
    {
        var travel =
            SaveManager.Instance
            .CurrentSave
            .Travel;

        SaveManager.Instance
            .CurrentSave
            .Location
            .CurrentCityID =
                travel.DestinationCityID;

        SaveManager.Instance
            .CurrentSave
            .Location
            .IsTraveling =
                false;

        travel.IsTraveling =
            false;

        var city =
            DatabaseManager.Instance
            .GetData<CityData>(
                travel.DestinationCityID
            );

        GameManager.Instance.ChangeState(GameState.Cityhub);

        CityManager.Instance.EnterCity();

        GameEvents.OnTravelFinished?.Invoke();

        SaveManager.Instance.SaveGame();

        Debug.Log(
            $"Arrived at " +
            $"{city.DisplayName}"
        );
    }

    private int GetTravelTime(
        CityData destination
    )
    {
        foreach (var connection
            in CurrentCity.Connections)
        {
            if (connection
                .ConnectedCity
                == destination)
            {
                return connection
                    .TravelHours;
            }
        }

        return 12;
    }

    private void OnEnable()
    {
        GameEvents.OnTravelEventResolved += HandleTravelEventResolved;
    }

    private void OnDisable()
    {
        GameEvents.OnTravelEventResolved -= HandleTravelEventResolved;
    }

    private void HandleTravelEventResolved()
    {
        if (!IsTraveling)
            return;

        var travel =
            SaveManager
                .Instance
                .CurrentSave
                .Travel;

        if (travel.RemainingHours <= 0)
        {
            ArriveAtDestination();
        }
    }
}