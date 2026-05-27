using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuildUIController
    : MonoBehaviour
{
    private const string MeetGuildQuestID =
        "meet_black_cats_guild";

    private const string ReturnToLunarisQuestID =
        "return_to_lunaris";

    [SerializeField]
    private GameObject guildPanel;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text stateText;

    public static void OpenOrCreate()
    {
        EnsureGuildManager();

        GuildUIController controller =
            FindAnyObjectByType<GuildUIController>();

        if (controller == null)
        {
            GameObject controllerObject =
                new GameObject(
                    "GuildUIController"
                );

            controller =
                controllerObject
                    .AddComponent<GuildUIController>();

            controller.BuildRuntimeUI();
        }

        controller.Open();
    }

    public void Open()
    {
        if (guildPanel == null)
        {
            BuildRuntimeUI();
        }

        guildPanel.SetActive(true);

        Refresh();

        CompleteMeetGuildQuestIfNeeded();
    }

    public void Close()
    {
        if (guildPanel == null)
            return;

        guildPanel.SetActive(false);
    }

    public void Refresh()
    {
        GuildStateData guild =
            GuildManager
                .Instance
                .Guild;

        guild.EnsureRuntimeDefaults();

        titleText.text =
            "Guilda dos Gatos Negros";

        stateText.text =
            $"Nivel: {guild.Level}\n" +
            $"Reputacao: {guild.Reputation}\n" +
            $"Membros recrutados: {guild.RecruitedMemberIDs.Count}\n\n" +
            "Estado atual: a guilda esta em decadencia e precisa recuperar forca.";
    }

    private void CompleteMeetGuildQuestIfNeeded()
    {
        if (QuestManager.Instance == null)
            return;

        if (QuestManager
            .Instance
            .GetQuestState(MeetGuildQuestID)
            != QuestStatus.Active)
        {
            return;
        }

        QuestManager
            .Instance
            .CompleteQuest(MeetGuildQuestID);

        QuestManager
            .Instance
            .StartQuest(ReturnToLunarisQuestID);
    }

    private void BuildRuntimeUI()
    {
        Canvas canvas =
            gameObject.AddComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            700;

        CanvasScaler scaler =
            gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(720f, 1280f);

        gameObject.AddComponent<GraphicRaycaster>();

        guildPanel =
            new GameObject(
                "GuildPanel",
                typeof(RectTransform),
                typeof(Image)
            );

        guildPanel.transform.SetParent(
            transform,
            false
        );

        RectTransform panelRect =
            guildPanel.GetComponent<RectTransform>();

        panelRect.anchorMin =
            Vector2.zero;

        panelRect.anchorMax =
            Vector2.one;

        panelRect.offsetMin =
            Vector2.zero;

        panelRect.offsetMax =
            Vector2.zero;

        guildPanel
            .GetComponent<Image>()
            .color =
            new Color(0.08f, 0.07f, 0.06f, 0.96f);

        titleText =
            CreateText(
                "TitleText",
                guildPanel.transform,
                new Vector2(0.08f, 0.76f),
                new Vector2(0.92f, 0.92f),
                34f,
                TextAlignmentOptions.Center
            );

        stateText =
            CreateText(
                "StateText",
                guildPanel.transform,
                new Vector2(0.1f, 0.32f),
                new Vector2(0.9f, 0.74f),
                24f,
                TextAlignmentOptions.TopLeft
            );

        CreateButton(
            "Voltar",
            guildPanel.transform,
            new Vector2(0.12f, 0.08f),
            new Vector2(0.88f, 0.18f),
            Close
        );

        CreateButton(
            "Concluir missao",
            guildPanel.transform,
            new Vector2(0.12f, 0.2f),
            new Vector2(0.88f, 0.3f),
            CompleteMeetGuildQuestIfNeeded
        );
    }

    private static void EnsureGuildManager()
    {
        if (GuildManager.Instance != null)
            return;

        GameObject managerObject =
            new GameObject(
                "GuildManager"
            );

        managerObject
            .AddComponent<GuildManager>();
    }

    private TMP_Text CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        textObject.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        TMP_Text text =
            textObject.GetComponent<TMP_Text>();

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

    private void CreateButton(
        string label,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject =
            new GameObject(
                label,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        buttonObject.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        buttonObject
            .GetComponent<Image>()
            .color =
            new Color(0.18f, 0.16f, 0.12f, 1f);

        Button button =
            buttonObject.GetComponent<Button>();

        button.onClick.AddListener(onClick);

        TMP_Text text =
            CreateText(
                "Label",
                buttonObject.transform,
                new Vector2(0.04f, 0.12f),
                new Vector2(0.96f, 0.88f),
                24f,
                TextAlignmentOptions.Center
            );

        text.text =
            label;
    }
}
