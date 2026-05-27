using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int CurrentSaveSlot = 1;

    public string LastSavedAt;

    public TimeData Time = new();

    public PlayerData Player = new();

    public WalletState Wallet = new();

    public WorldStateData WorldState = new();

    public CurrentLocationData Location = new();

    public TravelSession Travel = new();

    public ExplorationStateData Exploration = new();

    public GuildStateData Guild = new();

    public List<QuestStateData> QuestStates = new();

    public List<ActiveQuest> ActiveQuests = new();

    public List<InventoryItem> Inventory = new();

    public EquipmentState Equipment = new();

    public PlayerStatsData Stats = new();

    public List<ReputationData> Reputation = new();

    public List<NPCRelationshipSaveData> NPCRelationships = new();

    public void EnsureRuntimeDefaults()
    {
        if (Time == null)
        {
            Time = new TimeData();
        }

        Time.EnsureRuntimeDefaults();

        if (Player == null)
        {
            Player = new PlayerData();
        }

        if (Wallet == null)
        {
            Wallet = new WalletState();
        }

        if (Wallet.Coins < 0)
        {
            Wallet.Coins =
                Math.Max(
                    0,
                    Player.Gold
                );
        }

        Wallet.EnsureRuntimeDefaults();

        Player.Gold =
            Wallet.Coins;

        if (WorldState == null)
        {
            WorldState = new WorldStateData();
        }

        WorldState.EnsureRuntimeDefaults();

        if (Location == null)
        {
            Location = new CurrentLocationData();
        }

        if (Travel == null)
        {
            Travel = new TravelSession();
        }

        if (Exploration == null)
        {
            Exploration = new ExplorationStateData();
        }

        Exploration.EnsureRuntimeDefaults();

        if (Guild == null)
        {
            Guild = new GuildStateData();
        }

        Guild.EnsureRuntimeDefaults();

        if (QuestStates == null)
        {
            QuestStates = new List<QuestStateData>();
        }

        if (ActiveQuests == null)
        {
            ActiveQuests = new List<ActiveQuest>();
        }

        if (Inventory == null)
        {
            Inventory = new List<InventoryItem>();
        }

        if (Equipment == null)
        {
            Equipment = new EquipmentState();
        }

        if (Stats == null)
        {
            Stats = new PlayerStatsData();
        }

        if (Reputation == null)
        {
            Reputation = new List<ReputationData>();
        }

        if (NPCRelationships == null)
        {
            NPCRelationships =
                new List<NPCRelationshipSaveData>();
        }

        foreach (NPCRelationshipSaveData relationship
            in NPCRelationships)
        {
            relationship?.EnsureRuntimeDefaults();
        }

        foreach (QuestStateData questState
            in QuestStates)
        {
            questState?.EnsureRuntimeDefaults();
        }
    }
}
