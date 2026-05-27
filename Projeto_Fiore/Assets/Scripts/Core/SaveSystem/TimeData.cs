using System;

[Serializable]
public class TimeData
{
    public int Year = 235;

    public int Month = 1;

    public int Day = 1;

    public int Hour = 8;

    public int Minute = 0;

    public TimeOfDay TimeOfDay =
        TimeOfDay.Morning;

    public void EnsureRuntimeDefaults()
    {
        if (Year <= 0)
        {
            Year = 235;
        }

        if (Month < 1)
        {
            Month = 1;
        }

        if (Day < 1)
        {
            Day = 1;
        }

        if (Hour < 0 || Hour >= TimeManager.HoursPerDay)
        {
            Hour = 8;
        }
    }
}
