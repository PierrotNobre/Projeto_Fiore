using System;
using System.IO;
using UnityEngine;

public class SaveManager : PersistentSingleton<SaveManager>
{
    private const int MinSaveSlot = 1;
    private const int MaxSaveSlot = 3;

    public SaveData CurrentSave { get; private set; }

    public int CurrentSaveSlot { get; private set; } =
        MinSaveSlot;

    public bool HasActiveGame { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        EnsureRuntimeSave();
    }

    public void NewGame()
    {
        NewGame(
            CurrentSaveSlot,
            "Hero",
            "body_default",
            "portrait_default",
            "race_human"
        );
    }

    public void NewGame(
        int slotID,
        string characterName,
        string bodyPresetID,
        string portraitID,
        string raceID = "race_human")
    {
        CurrentSaveSlot =
            NormalizeSlot(slotID);

        CurrentSave =
            new SaveData();

        CurrentSave.EnsureRuntimeDefaults();

        CurrentSave.CurrentSaveSlot =
            CurrentSaveSlot;

        CurrentSave.Player.CharacterID =
            "player";

        CurrentSave.Player.PlayerName =
            string.IsNullOrWhiteSpace(characterName)
                ? "Hero"
                : characterName.Trim();

        CurrentSave.Player.BodyPresetID =
            string.IsNullOrWhiteSpace(bodyPresetID)
                ? "body_default"
                : bodyPresetID;

        CurrentSave.Player.PortraitID =
            string.IsNullOrWhiteSpace(portraitID)
                ? "portrait_default"
                : portraitID;

        CurrentSave.Player.RaceID =
            string.IsNullOrWhiteSpace(raceID)
                ? "race_human"
                : raceID;

        CurrentSave.Location.CurrentCityID =
            "city_lunaris";

        CurrentSave.Location.IsTraveling =
            false;

        CurrentSave.Time =
            new TimeData();

        CurrentSave.Travel =
            new TravelSession();

        CurrentSave.Exploration =
            new ExplorationStateData();

        CurrentSave.WorldState =
            new WorldStateData();

        CurrentSave.WorldState.EnsureRuntimeDefaults();

        CurrentSave.Guild =
            new GuildStateData();

        CurrentSave.Guild.EnsureRuntimeDefaults();

        CurrentSave.Wallet =
            new WalletState
            {
                Coins = 100
            };

        CurrentSave.Player.Gold =
            CurrentSave.Wallet.Coins;

        CurrentSave.Equipment =
            new EquipmentState();

        CurrentSave.QuestStates.Clear();
        CurrentSave.ActiveQuests.Clear();
        CurrentSave.Inventory.Clear();
        CurrentSave.Reputation.Clear();

        ApplyCharacterRuntimeDefaults();
        AddInitialInventoryItems();

        TravelEventManager
            .Instance
            ?.ClearActiveEvent();

        HasActiveGame =
            true;

        WorldStateManager.Instance?.LoadFlags();

        SaveGame(CurrentSaveSlot);

        Debug.Log(
            $"Character created: {CurrentSave.Player.PlayerName}"
        );
    }

    [ContextMenu("Debug/Save Game")]
    public void SaveGame()
    {
        SaveGame(CurrentSaveSlot);
    }

    public void SaveGame(
        int slotID)
    {
        EnsureRuntimeSave();

        CurrentSaveSlot =
            NormalizeSlot(slotID);

        CurrentSave.CurrentSaveSlot =
            CurrentSaveSlot;

        CurrentSave.LastSavedAt =
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );

        string json =
            JsonUtility.ToJson(
                CurrentSave,
                true
            );

        File.WriteAllText(
            GetSavePath(CurrentSaveSlot),
            json
        );

        Debug.Log(
            $"Game saved successfully. Slot: {CurrentSaveSlot}"
        );
    }

    [ContextMenu("Debug/Load Game")]
    public void LoadGame()
    {
        LoadGame(CurrentSaveSlot);
    }

    public bool LoadGame(
        int slotID)
    {
        int normalizedSlot =
            NormalizeSlot(slotID);

        string path =
            GetSavePath(normalizedSlot);

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                $"Save slot {normalizedSlot} is empty."
            );

            return false;
        }

        string json =
            File.ReadAllText(path);

        CurrentSave =
            JsonUtility.FromJson<SaveData>(json);

        if (CurrentSave == null)
        {
            Debug.LogWarning(
                $"Save slot {normalizedSlot} is corrupted."
            );

            EnsureRuntimeSave();

            return false;
        }

        CurrentSave.EnsureRuntimeDefaults();

        CurrentSaveSlot =
            normalizedSlot;

        CurrentSave.CurrentSaveSlot =
            normalizedSlot;

        HasActiveGame =
            true;

        TravelEventManager
            .Instance
            ?.ClearActiveEvent();

        WorldStateManager.Instance?.LoadFlags();

        Debug.Log(
            $"Game loaded successfully. Slot: {CurrentSaveSlot}"
        );

        Debug.Log(
            $"Current city loaded: {CurrentSave.Location.CurrentCityID}"
        );

        GameEvents.OnTimeAdvanced?.Invoke();

        return true;
    }

    public bool HasSave(
        int slotID)
    {
        return File.Exists(
            GetSavePath(
                NormalizeSlot(slotID)
            )
        );
    }

    [ContextMenu("Debug/Reset Save")]
    public void DeleteSave()
    {
        DeleteSave(CurrentSaveSlot);
    }

    public void DeleteSave(
        int slotID)
    {
        int normalizedSlot =
            NormalizeSlot(slotID);

        string path =
            GetSavePath(normalizedSlot);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        if (CurrentSaveSlot == normalizedSlot)
        {
            CurrentSave =
                new SaveData();

            CurrentSave.EnsureRuntimeDefaults();

            CurrentSave.CurrentSaveSlot =
                normalizedSlot;

            HasActiveGame =
                false;
        }

        Debug.Log(
            $"Save reset. Slot: {normalizedSlot}"
        );
    }

    public SaveSlotInfo GetSaveSlotInfo(
        int slotID)
    {
        int normalizedSlot =
            NormalizeSlot(slotID);

        SaveSlotInfo info =
            new SaveSlotInfo
            {
                SlotID =
                    normalizedSlot,

                HasSave =
                    false
            };

        string path =
            GetSavePath(normalizedSlot);

        if (!File.Exists(path))
        {
            return info;
        }

        try
        {
            string json =
                File.ReadAllText(path);

            SaveData saveData =
                JsonUtility.FromJson<SaveData>(json);

            if (saveData == null)
            {
                return info;
            }

            saveData.EnsureRuntimeDefaults();

            info.HasSave =
                true;

            info.CharacterName =
                saveData.Player.PlayerName;

            info.CurrentCityID =
                saveData.Location.CurrentCityID;

            info.Year =
                saveData.Time.Year;

            info.Month =
                saveData.Time.Month;

            info.Day =
                saveData.Time.Day;

            info.Hour =
                saveData.Time.Hour;

            info.LastSavedAt =
                saveData.LastSavedAt;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Could not read save slot {normalizedSlot}: {exception.Message}"
            );
        }

        return info;
    }

    public void ReturnToMainMenu()
    {
        HasActiveGame =
            false;

        TravelEventManager
            .Instance
            ?.ClearActiveEvent();

        SceneFlowManager
            .GetOrCreate()
            .ShowMainMenu();
    }

    [ContextMenu("Debug/List Save Summary")]
    public void LogSaveSummary()
    {
        if (CurrentSave == null)
        {
            Debug.Log(
                "No save loaded."
            );

            return;
        }

        Debug.Log(
            $"Save Summary | Slot: {CurrentSaveSlot} | " +
            $"Character: {CurrentSave.Player.PlayerName} | " +
            $"City: {CurrentSave.Location.CurrentCityID} | " +
            $"Date: {CurrentSave.Time.Day}/{CurrentSave.Time.Month}/{CurrentSave.Time.Year} " +
            $"{CurrentSave.Time.Hour:00}:00 | " +
            $"Events: {CurrentSave.WorldState.EventHistory.OccurredEventIds.Count} | " +
            $"QuestStates: {CurrentSave.QuestStates.Count} | " +
            $"GuildLevel: {CurrentSave.Guild.Level} | " +
            $"InventoryStacks: {CurrentSave.Inventory.Count} | " +
            $"Coins: {CurrentSave.Wallet.Coins}"
        );
    }

    [ContextMenu("Debug/Create New Game Slot 1")]
    public void DebugCreateNewGameSlot1()
    {
        NewGame(
            1,
            "Hero",
            "body_default",
            "portrait_default"
        );
    }

    private void EnsureRuntimeSave()
    {
        if (CurrentSave == null)
        {
            CurrentSave =
                new SaveData();
        }

        CurrentSave.EnsureRuntimeDefaults();

        if (CurrentSave.CurrentSaveSlot < MinSaveSlot ||
            CurrentSave.CurrentSaveSlot > MaxSaveSlot)
        {
            CurrentSave.CurrentSaveSlot =
                CurrentSaveSlot;
        }
    }

    private void ApplyCharacterRuntimeDefaults()
    {
        if (CharacterManager.Instance == null)
            return;

        CurrentSave.Stats.CurrentHP =
            CharacterManager.Instance.MaxHP;

        CurrentSave.Stats.CurrentStamina =
            CharacterManager.Instance.MaxStamina;
    }

    private void AddInitialInventoryItems()
    {
        AddInitialItem(
            "potion_simple",
            2
        );

        AddInitialItem(
            "bread",
            2
        );

        AddInitialItem(
            "simple_sword",
            1
        );

        AddInitialItem(
            "rough_stone",
            2
        );
    }

    private void AddInitialItem(
        string itemID,
        int quantity)
    {
        CurrentSave.Inventory.Add(
            new InventoryItem
            {
                ItemID =
                    itemID,

                Quantity =
                    quantity
            }
        );
    }

    private string GetSavePath(
        int slotID)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"save_slot_{NormalizeSlot(slotID)}.json"
        );
    }

    private int NormalizeSlot(
        int slotID)
    {
        return Mathf.Clamp(
            slotID,
            MinSaveSlot,
            MaxSaveSlot
        );
    }
}
