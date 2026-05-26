using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private string persistentSceneName = "Persistent";
    [SerializeField] private string firstSceneName = "MainMenu";

    private async void Start()
    {
        // Carrega cena persistente
        await SceneManager.LoadSceneAsync(
            persistentSceneName,
            LoadSceneMode.Additive
        );

        // Carrega primeira cena
        await SceneManager.LoadSceneAsync(
            firstSceneName,
            LoadSceneMode.Additive
        );

        // Remove bootstrap
        await SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}