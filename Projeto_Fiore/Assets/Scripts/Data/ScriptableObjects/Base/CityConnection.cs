using System;
using UnityEngine;

[Serializable]
public class CityConnection
{
    public CityData ConnectedCity;

    [TextArea]
    public string RouteDescription;

    [Range(1, 20)]
    public int TravelHours;

    [Min(0)]
    public int TravelCost;

    [Range(0, 10)]
    public int RiskLevel;
}
