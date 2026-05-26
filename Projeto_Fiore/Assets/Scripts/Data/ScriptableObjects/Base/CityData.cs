using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "City",
    menuName = "Fiore/City"
)]
public class CityData : BaseData
{
    [Header("Region")]
    public RegionData Region;

    [Header("Travel")]
    public bool HasPort;

    public bool HasGuild;

    [Range(1, 10)]
    public int Prosperity;

    [Range(1, 10)]
    public int Security;

    [Header("Map")]
    public Vector2 MapPosition;

    public List<CityConnection> Connections;

    [Header("Services")]
    public List<CityServiceType> Services;
}