using UnityEngine;
using System;

public class GameManager : PersistentSingleton<GameManager>
{
    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    protected override void Awake()
    {
        base.Awake();

        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;

        Debug.Log($"Game State Changed To: {newState}");

        OnGameStateChanged?.Invoke(newState);
    }

    private void Start()
    {
        var city =
        DatabaseManager.Instance
        .GetData<CityData>(
            "city_aguasclaras"
        );

        Debug.Log(city.DisplayName);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveManager.Instance.SaveGame();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            SaveManager.Instance.LoadGame();
        }


        if (Input.GetKeyDown(KeyCode.P))
        {
            SaveManager.Instance.DeleteSave();
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            NPCManager
                .Instance
                .PrintCurrentCityNPCs();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NPCManager
                .Instance
                .TalkToNPCByIndex(0);
        }
    }

}