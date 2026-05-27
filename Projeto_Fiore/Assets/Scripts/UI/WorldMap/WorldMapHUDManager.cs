using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldMapHUDManager : MonoBehaviour
{
    private static WorldMapHUDManager instance;

    private Canvas canvas;
    private RectTransform root;
    private TMP_Text titleText;
    private TMP_Text bodyText;

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance =
            this;

        BuildRuntimeUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static WorldMapHUDManager OpenOrCreate()
    {
        if (instance != null)
        {
            instance.gameObject.SetActive(true);
            instance.MoveToScene(
                SceneFlowManager.WorldMapSceneName
            );
            instance.Refresh();
            return instance;
        }

        GameObject mapObject =
            new GameObject(
                "WorldMapHUDManager"
            );

        WorldMapHUDManager manager =
            mapObject.AddComponent<WorldMapHUDManager>();

        manager.MoveToScene(
            SceneFlowManager.WorldMapSceneName
        );

        manager.Refresh();

        return manager;
    }

    private void Refresh()
    {
        CityData city =
            CityManager.Instance != null
                ? CityManager.Instance.CurrentCity
                : null;

        TimeData time =
            TimeManager.Instance != null
                ? TimeManager.Instance.CurrentTime
                : null;

        titleText.text =
            "Mapa de Fiore";

        bodyText.text =
            $"{(city != null ? city.DisplayName : "Cidade desconhecida")}\n" +
            $"{(time != null ? $"Dia {time.Day} / Mes {time.Month} / Ano {time.Year}" : "Data desconhecida")}";
    }

    private void BuildRuntimeUI()
    {
        if (canvas != null)
            return;

        canvas =
            gameObject.AddComponent<Canvas>();

        EnsureEventSystem();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            450;

        CanvasScaler scaler =
            gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(720f, 1280f);

        gameObject.AddComponent<GraphicRaycaster>();

        root =
            CreateRect(
                "Root",
                transform,
                Vector2.zero,
                Vector2.one
            );

        AddImage(
            root.gameObject,
            new Color(0.045f, 0.05f, 0.045f, 0.95f)
        );

        titleText =
            CreateText(
                "Title",
                root,
                new Vector2(0.08f, 0.82f),
                new Vector2(0.92f, 0.94f),
                40f,
                TextAlignmentOptions.Center
            );

        bodyText =
            CreateText(
                "Body",
                root,
                new Vector2(0.08f, 0.62f),
                new Vector2(0.92f, 0.8f),
                25f,
                TextAlignmentOptions.Center
            );

        Button cityButton =
            CreateButton(
                "Voltar para cidade",
                root,
                new Vector2(0.08f, 0.14f),
                new Vector2(0.92f, 0.22f)
            );

        cityButton.onClick.AddListener(
            () =>
            {
                SceneFlowManager
                    .GetOrCreate()
                    .ShowCity();
            }
        );
    }

    private Button CreateButton(
        string label,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        RectTransform rect =
            CreateRect(
                label,
                parent,
                anchorMin,
                anchorMax
            );

        Image image =
            AddImage(
                rect.gameObject,
                new Color(0.22f, 0.18f, 0.12f, 1f)
            );

        Button button =
            rect.gameObject
                .AddComponent<Button>();

        button.targetGraphic =
            image;

        TMP_Text text =
            CreateText(
                "Label",
                rect,
                new Vector2(0.04f, 0.12f),
                new Vector2(0.96f, 0.88f),
                23f,
                TextAlignmentOptions.Center
            );

        text.text =
            label;

        return button;
    }

    private TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        RectTransform rect =
            CreateRect(
                name,
                parent,
                anchorMin,
                anchorMax
            );

        TMP_Text text =
            rect.gameObject
                .AddComponent<TextMeshProUGUI>();

        text.fontSize =
            fontSize;

        text.textWrappingMode =
            TextWrappingModes.Normal;

        text.alignment =
            alignment;

        text.color =
            Color.white;

        return text;
    }

    private RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject rectObject =
            new GameObject(
                name,
                typeof(RectTransform)
            );

        rectObject.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            rectObject.GetComponent<RectTransform>();

        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        return rect;
    }

    private Image AddImage(
        GameObject target,
        Color color)
    {
        Image image =
            target.GetComponent<Image>();

        if (image == null)
        {
            image =
                target.AddComponent<Image>();
        }

        image.color =
            color;

        return image;
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

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject =
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );

        Scene scene =
            SceneManager.GetSceneByName(
                SceneFlowManager.WorldMapSceneName
            );

        if (scene.IsValid() &&
            scene.isLoaded)
        {
            SceneManager.MoveGameObjectToScene(
                eventSystemObject,
                scene
            );
        }
    }
}
