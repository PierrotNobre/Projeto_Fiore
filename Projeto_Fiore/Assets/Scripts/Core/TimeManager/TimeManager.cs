using UnityEngine;
using System;

public class TimeManager
    : PersistentSingleton<TimeManager>
{
    public TimeData CurrentTime
        => SaveManager
        .Instance
        .CurrentSave
        .Time;

    private const int HOURS_PER_DAY = 24;
    private const int DAYS_PER_MONTH = 20;
    private const int MONTHS_PER_YEAR = 8;

    public void AdvanceHours(int hours)
    {
        for (int i = 0; i < hours; i++)
        {
            AdvanceSingleHour();
        }

        GameEvents.OnTimeAdvanced?.Invoke();
    }

    private void AdvanceSingleHour()
    {
        CurrentTime.Hour++;

        GameEvents
            .OnHourPassed
            ?.Invoke(CurrentTime.Hour);

        if (CurrentTime.Hour >= HOURS_PER_DAY)
        {
            CurrentTime.Hour = 0;

            AdvanceDay();
        }
    }

    private void AdvanceDay()
    {
        CurrentTime.Day++;

        GameEvents
            .OnDayPassed
            ?.Invoke(CurrentTime.Day);
        SaveManager.Instance.SaveGame();
        if (CurrentTime.Day > DAYS_PER_MONTH)
        {
            CurrentTime.Day = 1;
            
            AdvanceMonth();
        }
    }

    private void AdvanceMonth()
    {
        CurrentTime.Month++;

        GameEvents
            .OnMonthPassed
            ?.Invoke(CurrentTime.Month);

        if (CurrentTime.Month > MONTHS_PER_YEAR)
        {
            CurrentTime.Month = 1;

            AdvanceYear();
        }
    }

    private void AdvanceYear()
    {
        CurrentTime.Year++;

        GameEvents
            .OnYearPassed
            ?.Invoke(CurrentTime.Year);
    }

    public string GetFormattedDate()
    {
        return
            $"Day {CurrentTime.Day}, " +
            $"Month {CurrentTime.Month}, " +
            $"Year {CurrentTime.Year} " +
            $"{CurrentTime.Hour:00}:00";
    }
}