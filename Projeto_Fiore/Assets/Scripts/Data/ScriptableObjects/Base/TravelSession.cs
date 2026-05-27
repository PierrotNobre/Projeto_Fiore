using System;

[Serializable]
public class TravelSession
{
    public string OriginCityID;

    public string DestinationCityID;

    public int TotalHours;

    public int RemainingHours;

    public bool IsTraveling;

    public int ElapsedHours =>
        Math.Max(
            0,
            TotalHours - RemainingHours
        );
}
