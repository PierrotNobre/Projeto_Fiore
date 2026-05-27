using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private string persistentSceneName = "Persistent";

    private async void Start()
    {
        await SceneManager.LoadSceneAsync(
            persistentSceneName,
            LoadSceneMode.Additive
        );

        SceneFlowManager
            .GetOrCreate()
            .ShowMainMenu();

        await SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
