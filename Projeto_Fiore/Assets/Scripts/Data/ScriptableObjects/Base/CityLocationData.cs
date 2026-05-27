using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CityLocationData
{
    public string LocationID;

    public string DisplayName;

    [TextArea]
    public string Description;

    public CityLocationType LocationType;

    public Sprite LocationSprite;

    public List<NPCData> NPCs = new();

    public List<string> NPCIDs = new();

    public List<CityServiceType> Services = new();

    public ShopData ShopData;

    public bool ShowInCityScreen = true;

    public List<RequirementData> UnlockRequirements =
        new();
}
