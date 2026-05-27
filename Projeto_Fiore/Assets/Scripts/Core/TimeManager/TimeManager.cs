using UnityEngine;

public class TimeManager
    : PersistentSingleton<TimeManager>
{
    public const int HoursPerDay = 24;
    public const int DaysPerMonth = 20;
    public const int MonthsPerYear = 8;
    public const int MonthsPerSeason = 2;

    public TimeData CurrentTime
        => SaveManager
        .Instance
        .CurrentSave
        .Time;

    public FioreSeason CurrentSeason =>
        GetSeasonForMonth(CurrentTime.Month);

    public void AdvanceHours(int hours)
    {
        if (hours <= 0)
            return;

        FioreSeason previousSeason =
            CurrentSeason;

        TimeOfDay previousTimeOfDay =
            CurrentTime.TimeOfDay;

        for (int i = 0; i < hours; i++)
        {
            AdvanceSingleHour();
        }

        SyncTimeOfDayFromHour();

        NotifyTimeOfDayChangedIfNeeded(
            previousTimeOfDay
        );

        NotifySeasonChangedIfNeeded(previousSeason);

        NotifyTimeAdvanced(
            Mathf.CeilToInt(
                hours / 6f
            ),
            "Horas"
        );
    }

    public void AdvanceDays(int days)
    {
        AdvanceDays(
            days,
            "Dias"
        );
    }

    public void AdvanceDays(
        int days,
        string reason)
    {
        AdvanceHours(days * HoursPerDay);
    }

    public void AdvanceMonths(int months)
    {
        if (months <= 0)
            return;

        FioreSeason previousSeason =
            CurrentSeason;

        TimeOfDay previousTimeOfDay =
            CurrentTime.TimeOfDay;

        for (int i = 0; i < months; i++)
        {
            AdvanceMonth();
        }

        NotifyTimeOfDayChangedIfNeeded(
            previousTimeOfDay
        );

        NotifySeasonChangedIfNeeded(previousSeason);

        NotifyTimeAdvanced(
            months * DaysPerMonth * 4,
            "Meses"
        );
    }

    public void AdvancePeriod(
        string reason = "Tempo")
    {
        AdvancePeriods(
            1,
            reason
        );
    }

    public void AdvancePeriods(
        int amount,
        string reason = "Tempo")
    {
        if (amount <= 0)
            return;

        FioreSeason previousSeason =
            CurrentSeason;

        TimeOfDay previousTimeOfDay =
            CurrentTime.TimeOfDay;

        for (int i = 0; i < amount; i++)
        {
            AdvanceSinglePeriod();
        }

        NotifyTimeOfDayChangedIfNeeded(
            previousTimeOfDay
        );

        NotifySeasonChangedIfNeeded(previousSeason);

        NotifyTimeAdvanced(
            amount,
            reason
        );
    }

    public void SetTimeOfDay(
        TimeOfDay timeOfDay)
    {
        TimeOfDay previousTimeOfDay =
            CurrentTime.TimeOfDay;

        CurrentTime.TimeOfDay =
            timeOfDay;

        CurrentTime.Hour =
            GetHourForTimeOfDay(timeOfDay);

        NotifyTimeOfDayChangedIfNeeded(
            previousTimeOfDay
        );

        NotifyTimeAdvanced(
            0,
            "Periodo definido"
        );
    }

    public void RestUntilNextMorning(
        string reason = "Descanso")
    {
        FioreSeason previousSeason =
            CurrentSeason;

        TimeOfDay previousTimeOfDay =
            CurrentTime.TimeOfDay;

        AdvanceDay();

        CurrentTime.TimeOfDay =
            TimeOfDay.Morning;

        CurrentTime.Hour =
            GetHourForTimeOfDay(
                TimeOfDay.Morning
            );

        NotifyTimeOfDayChangedIfNeeded(
            previousTimeOfDay
        );

        NotifySeasonChangedIfNeeded(previousSeason);

        NotifyTimeAdvanced(
            4,
            reason
        );
    }

    public TimeOfDay GetCurrentTimeOfDay()
    {
        SyncTimeOfDayFromHour();

        return CurrentTime.TimeOfDay;
    }

    private void AdvanceSingleHour()
    {
        CurrentTime.Hour++;

        if (CurrentTime.Hour >= HoursPerDay)
        {
            CurrentTime.Hour = 0;

            AdvanceDay();
        }

        GameEvents
            .OnHourPassed
            ?.Invoke(CurrentTime.Hour);
    }

    private void AdvanceSinglePeriod()
    {
        TimeOfDay next =
            CurrentTime.TimeOfDay switch
            {
                TimeOfDay.Morning =>
                    TimeOfDay.Afternoon,

                TimeOfDay.Afternoon =>
                    TimeOfDay.Evening,

                TimeOfDay.Evening =>
                    TimeOfDay.Night,

                _ =>
                    TimeOfDay.Morning
            };

        if (CurrentTime.TimeOfDay ==
            TimeOfDay.Night)
        {
            AdvanceDay();
        }

        CurrentTime.TimeOfDay =
            next;

        CurrentTime.Hour =
            GetHourForTimeOfDay(next);
    }

    private void AdvanceDay()
    {
        CurrentTime.Day++;

        if (CurrentTime.Day > DaysPerMonth)
        {
            CurrentTime.Day = 1;
            
            AdvanceMonth();
        }

        GameEvents
            .OnDayPassed
            ?.Invoke(CurrentTime.Day);

        SaveManager.Instance.SaveGame();
    }

    private void AdvanceMonth()
    {
        CurrentTime.Month++;

        if (CurrentTime.Month > MonthsPerYear)
        {
            CurrentTime.Month = 1;

            AdvanceYear();
        }

        GameEvents
            .OnMonthPassed
            ?.Invoke(CurrentTime.Month);
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
            $"Dia {CurrentTime.Day}, " +
            $"Mes {CurrentTime.Month}, " +
            $"Ano {CurrentTime.Year} - " +
            $"{GetCurrentTimeOfDayDisplayName()} - " +
            GetCurrentSeasonDisplayName();
    }

    public string GetCurrentTimeOfDayDisplayName()
    {
        return GetCurrentTimeOfDay() switch
        {
            TimeOfDay.Morning => "Manha",

            TimeOfDay.Afternoon => "Tarde",

            TimeOfDay.Evening => "Noite",

            TimeOfDay.Night => "Madrugada",

            _ => "Manha"
        };
    }

    public string GetCurrentSeasonDisplayName()
    {
        return CurrentSeason switch
        {
            FioreSeason.Spring => "Primavera",

            FioreSeason.Summer => "Verao",

            FioreSeason.Autumn => "Outono",

            FioreSeason.Winter => "Inverno",

            _ => "Primavera"
        };
    }

    public FioreSeason GetSeasonForMonth(int month)
    {
        int normalizedMonth =
            Mathf.Clamp(
                month,
                1,
                MonthsPerYear
            );

        int seasonIndex =
            (normalizedMonth - 1)
            / MonthsPerSeason;

        return (FioreSeason)seasonIndex;
    }

    private void NotifySeasonChangedIfNeeded(
        FioreSeason previousSeason)
    {
        if (previousSeason == CurrentSeason)
            return;

        GameEvents
            .OnSeasonChanged
            ?.Invoke(CurrentSeason);
    }

    private void NotifyTimeOfDayChangedIfNeeded(
        TimeOfDay previousTimeOfDay)
    {
        if (previousTimeOfDay ==
            CurrentTime.TimeOfDay)
        {
            return;
        }

        GameEvents
            .OnTimeOfDayChanged
            ?.Invoke(CurrentTime.TimeOfDay);
    }

    private void NotifyTimeAdvanced(
        int advancedPeriods,
        string reason)
    {
        SaveManager.Instance.SaveGame();

        CalendarEventManager
            .GetOrCreate()
            .CheckCalendarEvents();

        GuildManager
            .GetOrCreate()
            .AdvanceGuildTasks(
                advancedPeriods
            );

        if (!string.IsNullOrEmpty(reason))
        {
            Debug.Log(
                $"Time advanced: {reason}"
            );
        }

        GameEvents.OnTimeAdvanced?.Invoke();
    }

    private void SyncTimeOfDayFromHour()
    {
        CurrentTime.TimeOfDay =
            GetTimeOfDayForHour(CurrentTime.Hour);
    }

    private static TimeOfDay GetTimeOfDayForHour(
        int hour)
    {
        if (hour >= 6 && hour < 12)
            return TimeOfDay.Morning;

        if (hour >= 12 && hour < 18)
            return TimeOfDay.Afternoon;

        if (hour >= 18 && hour < 22)
            return TimeOfDay.Evening;

        return TimeOfDay.Night;
    }

    private static int GetHourForTimeOfDay(
        TimeOfDay timeOfDay)
    {
        return timeOfDay switch
        {
            TimeOfDay.Morning => 8,

            TimeOfDay.Afternoon => 13,

            TimeOfDay.Evening => 18,

            TimeOfDay.Night => 22,

            _ => 8
        };
    }
}
