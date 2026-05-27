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

    public TravelSession CurrentTravel =>
        SaveManager.Instance
        .CurrentSave
        .Travel;

    public void TravelTo(
        CityData destination
    )
    {
        if (destination == null)
            return;

        if (IsTraveling)
        {
            Debug.LogWarning(
                "Cannot start a new travel while another travel is active."
            );

            return;
        }

        CityData origin =
            CurrentCity;

        if (origin == null)
        {
            Debug.LogWarning(
                "Cannot start travel without a current city."
            );

            return;
        }

        if (!TryGetConnection(
            origin,
            destination,
            out CityConnection connection))
        {
            Debug.LogWarning(
                $"No travel route from {origin.DisplayName} " +
                $"to {destination.DisplayName}."
            );

            return;
        }

        int travelHours =
            Mathf.Max(
                1,
                connection.TravelHours
            );

        SaveManager.Instance
            .CurrentSave
            .Travel =
            new TravelSession
            {
                OriginCityID =
                    origin.ID,

                DestinationCityID =
                    destination.ID,

                TotalHours =
                    travelHours,

                RemainingHours =
                    travelHours,

                IsTraveling = true
            };

        SaveManager.Instance
            .CurrentSave
            .Location
            .IsTraveling =
            true;

        SaveManager.Instance
            .SaveGame();

        WorldStateManager
            .Instance
            ?.TriggerWorldEvents(
                EventTriggerType.OnTravelStart,
                origin,
                origin,
                destination
            );

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

        if (TravelEventManager.Instance != null &&
            TravelEventManager.Instance.HasActiveEvent)
        {
            Debug.Log(
                "Cannot continue travel while an event is active."
            );

            return;
        }

        var travel =
            CurrentTravel;

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

        if (travel.RemainingHours <= 0)
        {
            ArriveAtDestination();
            return;
        }

        if (TravelEventManager.Instance != null)
        {
            TravelEventManager
                .Instance
                .TryTriggerEvent();
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
            CurrentTravel;

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

        travel.RemainingHours =
            0;

        var city =
            DatabaseManager.Instance
            .GetData<CityData>(
                travel.DestinationCityID
            );

        var originCity =
            DatabaseManager.Instance
            .GetData<CityData>(
                travel.OriginCityID
            );

        WorldStateManager
            .Instance
            ?.TriggerWorldEvents(
                EventTriggerType.OnTravelEnd,
                city,
                originCity,
                city
            );

        GameManager.Instance.ChangeState(GameState.Cityhub);

        CityManager.Instance.EnterCity();

        GameEvents.OnTravelFinished?.Invoke();

        SaveManager.Instance.SaveGame();

        if (city != null)
        {
            Debug.Log(
                $"Arrived at " +
                $"{city.DisplayName}"
            );
        }
    }

    public bool TryGetConnection(
        CityData destination
    )
    {
        return TryGetConnection(
            CurrentCity,
            destination,
            out _
        );
    }

    public bool TryGetConnection(
        CityData origin,
        CityData destination,
        out CityConnection matchingConnection)
    {
        matchingConnection = null;

        if (origin == null ||
            destination == null ||
            origin.Connections == null)
        {
            return false;
        }

        foreach (var connection
            in origin.Connections)
        {
            if (connection == null ||
                connection.ConnectedCity == null)
            {
                continue;
            }

            if (connection.ConnectedCity == destination)
            {
                matchingConnection = connection;
                return true;
            }
        }

        return false;
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
            CurrentTravel;

        if (travel.RemainingHours <= 0)
        {
            ArriveAtDestination();
        }
    }
}
