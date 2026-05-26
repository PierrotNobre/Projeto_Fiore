using UnityEngine;
using System.IO;

public class SaveManager : PersistentSingleton<SaveManager>
{
    public SaveData CurrentSave { get; private set; }

    private string savePath;

    protected override void Awake()
    {
        base.Awake();

        savePath = Path.Combine( Application.persistentDataPath,"save.json");
    }

    private void Start()
    {
        LoadGame();
   
        CurrentSave.Player.Gold += 100;

        Debug.Log(CurrentSave.Player.Gold);
    }

    public void NewGame()
    {
        CurrentSave = new SaveData();

        CurrentSave.Stats.CurrentHP = CharacterManager.Instance.MaxHP;

        CurrentSave.Stats.CurrentStamina = CharacterManager.Instance.MaxStamina;

        WorldStateManager.Instance?.LoadFlags();

        SaveGame();
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(CurrentSave,true);

        File.WriteAllText(savePath,json);

        Debug.Log("Game Saved");
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No Save Found");
            NewGame();
            return;
        }

        string json = File.ReadAllText(savePath);

        CurrentSave = JsonUtility.FromJson<SaveData>(json);

        if (CurrentSave == null)
        {
            Debug.LogWarning("Save corrupted.");
            NewGame();
            return;
        }

        WorldStateManager.Instance?.LoadFlags();
        Debug.Log("Game Loaded");
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        NewGame();
    }
}