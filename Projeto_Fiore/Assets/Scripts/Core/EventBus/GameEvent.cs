using System;

public static class GameEvents
{
    public static Action OnTravelStarted;
    public static Action OnTravelFinished;
    public static Action OnAutoSave;

    public static Action<string> OnLocationEntered;

    public static Action<GameState> OnGameStateChanged;

    public static Action<int> OnHourPassed;
    public static Action<int> OnDayPassed;
    public static Action<int> OnMonthPassed;
    public static Action<int> OnYearPassed;
    public static Action<FioreSeason> OnSeasonChanged;
    public static Action<TimeOfDay> OnTimeOfDayChanged;
    public static Action OnTimeAdvanced;

    public static Action<TravelEventData> OnTravelEventTriggered;
    public static Action OnTravelEventResolved;
    public static Action<WorldEventData> OnWorldEventTriggered;
}
