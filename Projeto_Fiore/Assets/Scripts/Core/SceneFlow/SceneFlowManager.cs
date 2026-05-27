using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager
    : PersistentSingleton<SceneFlowManager>
{
    public const string PersistentSceneName = "Persistent";
    public const string MainMenuSceneName = "MainMenu";
    public const string CitySceneName = "City";
    public const string WorldMapSceneName = "WorldMap";
    public const string CombatSceneName = "Combat";

    [SerializeField]
    private string mainMenuSceneName = MainMenuSceneName;

    [SerializeField]
    private string citySceneName = CitySceneName;

    [SerializeField]
    private string worldMapSceneName = WorldMapSceneName;

    [SerializeField]
    private string combatSceneName = CombatSceneName;

    private Coroutine activeTransition;

    private bool loadMenuForActiveGame;

    public static SceneFlowManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject flowObject =
            new GameObject(
                "SceneFlowManager"
            );

        SceneFlowManager manager =
            flowObject
            .AddComponent<SceneFlowManager>();

        manager.MoveToScene(
            PersistentSceneName
        );

        return manager;
    }

    public void ShowMainMenu()
    {
        loadMenuForActiveGame =
            false;

        StartManagedTransition(
            mainMenuSceneName,
            GameState.MainMenu,
            () =>
            {
                MobileHUDManager.HideActiveHUD();
                MainMenuManager.OpenOrCreate();
            }
        );
    }

    public void ShowLoadMenuFromGame()
    {
        loadMenuForActiveGame =
            true;

        StartManagedTransition(
            mainMenuSceneName,
            GameState.MainMenu,
            () =>
            {
                MobileHUDManager.HideActiveHUD();
                MainMenuManager.OpenLoadFromGame();
            }
        );
    }

    public void EnterGameFromCurrentSave()
    {
        StartManagedTransition(
            citySceneName,
            GetCityStateForCurrentSave(),
            () =>
            {
                MainMenuManager.HideActiveMenu();

                UIScreenType targetScreen =
                    SaveManager.Instance.CurrentSave.Travel.IsTraveling
                        ? UIScreenType.Travel
                        : SaveManager.Instance.CurrentSave.Exploration.IsExploring
                            ? UIScreenType.Exploration
                        : UIScreenType.City;

                MobileHUDManager
                    .OpenOrCreate()
                    ?.ShowScreen(targetScreen);
            }
        );
    }

    public void ShowCity()
    {
        StartManagedTransition(
            citySceneName,
            GetCityStateForCurrentSave(),
            () =>
            {
                MobileHUDManager
                    .OpenOrCreate()
                    ?.ShowScreen(UIScreenType.City);
            }
        );
    }

    public void ShowWorldMap()
    {
        StartManagedTransition(
            worldMapSceneName,
            GameState.WorldMap,
            () =>
            {
                MobileHUDManager.HideActiveHUD();
                WorldMapHUDManager.OpenOrCreate();
            }
        );
    }

    public void ShowCombat()
    {
        StartManagedTransition(
            combatSceneName,
            GameState.Combat,
            () =>
            {
                MobileHUDManager.HideActiveHUD();
            }
        );
    }

    public void ReturnToGameFromMenu()
    {
        if (!SaveManager.Instance.HasActiveGame)
        {
            ShowMainMenu();
            return;
        }

        EnterGameFromCurrentSave();
    }

    public bool IsLoadMenuForActiveGame()
    {
        return loadMenuForActiveGame;
    }

    private GameState GetCityStateForCurrentSave()
    {
        if (SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null ||
            SaveManager.Instance.CurrentSave.Travel == null)
        {
            return GameState.Cityhub;
        }

        return SaveManager.Instance.CurrentSave.Travel.IsTraveling
            ? GameState.Travel
            : SaveManager.Instance.CurrentSave.Exploration.IsExploring
                ? GameState.Exploration
            : GameState.Cityhub;
    }

    private void StartManagedTransition(
        string targetSceneName,
        GameState targetState,
        System.Action onLoaded)
    {
        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
        }

        activeTransition =
            StartCoroutine(
                SwitchManagedScene(
                    targetSceneName,
                    targetState,
                    onLoaded
                )
            );
    }

    private IEnumerator SwitchManagedScene(
        string targetSceneName,
        GameState targetState,
        System.Action onLoaded)
    {
        yield return LoadSceneIfNeeded(targetSceneName);

        Scene targetScene =
            SceneManager.GetSceneByName(targetSceneName);

        if (targetScene.IsValid() &&
            targetScene.isLoaded)
        {
            SceneManager.SetActiveScene(targetScene);
        }

        yield return UnloadOtherManagedScenes(targetSceneName);

        GameManager
            .Instance
            ?.ChangeState(targetState);

        onLoaded?.Invoke();

        activeTransition =
            null;
    }

    private IEnumerator LoadSceneIfNeeded(
        string sceneName)
    {
        Scene scene =
            SceneManager.GetSceneByName(sceneName);

        if (scene.IsValid() &&
            scene.isLoaded)
        {
            yield break;
        }

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Additive
            );

        while (loadOperation != null &&
            !loadOperation.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator UnloadOtherManagedScenes(
        string targetSceneName)
    {
        string[] managedScenes =
        {
            mainMenuSceneName,
            citySceneName,
            worldMapSceneName,
            combatSceneName
        };

        foreach (string sceneName
            in managedScenes)
        {
            if (sceneName == targetSceneName)
                continue;

            Scene scene =
                SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                continue;
            }

            AsyncOperation unloadOperation =
                SceneManager.UnloadSceneAsync(scene);

            while (unloadOperation != null &&
                !unloadOperation.isDone)
            {
                yield return null;
            }
        }
    }

    private void MoveToScene(
        string sceneName)
    {
        Scene scene =
            SceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return;
        }

        SceneManager.MoveGameObjectToScene(
            gameObject,
            scene
        );
    }
}
