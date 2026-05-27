using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameFeedbackUI
    : PersistentSingleton<GameFeedbackUI>
{
    [Header("Runtime UI")]
    [SerializeField]
    private GameObject notificationPanel;

    [SerializeField]
    private TMP_Text notificationText;

    [SerializeField]
    private float notificationDuration = 3f;

    private float hideAtTime;

    protected override void Awake()
    {
        base.Awake();

        if (notificationPanel == null ||
            notificationText == null)
        {
            BuildRuntimeUI();
        }

        HideNotification();
    }

    private void Update()
    {
        if (notificationPanel == null ||
            !notificationPanel.activeSelf)
        {
            return;
        }

        if (Time.unscaledTime >= hideAtTime)
        {
            HideNotification();
        }
    }

    public static void ShowNotification(
        string message)
    {
        GameFeedbackUI feedback =
            GetOrCreate();

        feedback.Show(message);
    }

    public static void ShowEventNotification(
        string eventName)
    {
        ShowNotification(
            $"Evento iniciado: {eventName}"
        );
    }

    public static void ShowQuestStarted(
        string questName)
    {
        ShowNotification(
            $"Missao iniciada: {questName}"
        );
    }

    public static void ShowQuestCompleted(
        string questName)
    {
        ShowNotification(
            $"Missao concluida: {questName}"
        );
    }

    public static void ShowGuildUpdated(
        string message)
    {
        ShowNotification(
            $"Guilda: {message}"
        );
    }

    private static GameFeedbackUI GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject feedbackObject =
            new GameObject(
                "GameFeedbackUI"
            );

        return feedbackObject
            .AddComponent<GameFeedbackUI>();
    }

    private void Show(
        string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        notificationText.text =
            message;

        notificationPanel.SetActive(true);

        hideAtTime =
            Time.unscaledTime +
            notificationDuration;

        Debug.Log(message);
    }

    private void HideNotification()
    {
        if (notificationPanel == null)
            return;

        notificationPanel.SetActive(false);
    }

    private void BuildRuntimeUI()
    {
        Canvas canvas =
            gameObject.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            1000;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        notificationPanel =
            new GameObject(
                "NotificationPanel",
                typeof(RectTransform),
                typeof(Image)
            );

        notificationPanel.transform.SetParent(
            transform,
            false
        );

        RectTransform panelRect =
            notificationPanel
                .GetComponent<RectTransform>();

        panelRect.anchorMin =
            new Vector2(0.08f, 0.86f);

        panelRect.anchorMax =
            new Vector2(0.92f, 0.98f);

        panelRect.offsetMin =
            Vector2.zero;

        panelRect.offsetMax =
            Vector2.zero;

        Image panelImage =
            notificationPanel.GetComponent<Image>();

        panelImage.color =
            new Color(0.06f, 0.05f, 0.04f, 0.92f);

        GameObject textObject =
            new GameObject(
                "NotificationText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        textObject.transform.SetParent(
            notificationPanel.transform,
            false
        );

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin =
            new Vector2(0.05f, 0.12f);

        textRect.anchorMax =
            new Vector2(0.95f, 0.88f);

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;

        notificationText =
            textObject.GetComponent<TMP_Text>();

        notificationText.alignment =
            TextAlignmentOptions.Center;

        notificationText.fontSize =
            24f;

        notificationText.textWrappingMode =
            TextWrappingModes.Normal;

        notificationText.color =
            Color.white;
    }
}
