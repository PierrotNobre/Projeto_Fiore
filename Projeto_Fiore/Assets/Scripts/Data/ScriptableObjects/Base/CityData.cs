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

    public FioreKingdom Kingdom;

    [Header("Travel")]
    public bool HasPort;

    public bool HasGuild;

    [Range(1, 10)]
    public int Prosperity;

    [Range(1, 10)]
    public int Security;

    [Header("Map")]
    public Vector2 MapPosition;

    public Sprite Icon;

    public List<CityConnection> Connections;

    [Header("Services")]
    public List<CityServiceType> Services;

    [Header("Shop")]
    public ShopData Shop;

    [Header("NPCs")]
    public List<NPCData> NPCs;

    [Header("Locations")]
    public List<CityLocationData> Locations;

    [Header("Exploration")]
    public List<ExplorationAreaData> ExplorationAreas;
}
