using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CalendarEvent",
    menuName = "Fiore/Calendar Event"
)]
public class CalendarEventData : BaseData
{
    public CalendarEventTriggerType TriggerType;

    public int Year;

    public int Month = 1;

    public int Day = 1;

    public FioreSeason Season;

    public TimeOfDay TimeOfDay;

    public bool UseYear;

    public bool UseTimeOfDay;

    public bool IsUnique = true;

    public List<RequirementData> Requirements =
        new();

    public List<DialogueActionData> Actions =
        new();

    public RewardData Reward = new();

    public string NotificationMessage;
}
