using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "Region",
    menuName = "Fiore/Region"
)]
public class RegionData : BaseData
{
    [Header("Region Info")]
    public bool IsDangerous;

    [Range(0, 100)]
    public int DangerLevel;

    public List<CityData> Cities;
}