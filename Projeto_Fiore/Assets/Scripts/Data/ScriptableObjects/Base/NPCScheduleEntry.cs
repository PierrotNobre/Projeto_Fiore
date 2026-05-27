using System;
using System.Collections.Generic;

[Serializable]
public class NPCScheduleEntry
{
    public TimeOfDay TimeOfDay;

    public string CityID;

    public string LocationID;

    public bool IsAvailable = true;

    public List<RequirementData> Requirements =
        new();
}
