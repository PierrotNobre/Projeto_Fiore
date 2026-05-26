using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public TimeData Time = new();

    public PlayerData Player = new();

    public WorldStateData WorldState = new();

    public CurrentLocationData Location = new();

    public TravelSession Travel = new();

    public List<ActiveQuest> ActiveQuests = new();

    public List<InventoryItem> Inventory = new();

    public PlayerStatsData Stats = new();

    public List<ReputationData> Reputation = new();
}