using System;
using UnityEngine;

[Serializable]
public class CityConnection
{
    public CityData ConnectedCity;

    [Range(1, 20)]
    public int TravelHours;
}