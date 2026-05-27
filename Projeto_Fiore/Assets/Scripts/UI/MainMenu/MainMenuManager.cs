using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager
    : PersistentSingleton<MainMenuManager>
{
    private enum MainMenuScreen
    {
        Home,
        NewSlotSelect,
        LoadSlotSelect,
        OverwriteConfirm,
        CharacterCreation
    }

    private readonly string[] bodyPresets =
    {
        "body_default",
        "body_light",
        "body_strong"
    };

    private Canvas canvas;
    private RectTransform root;
    private RectTransform contentRoot;
    private TMP_InputField characterNameInput;
    private TMP_Text bodyPresetText;

    private int pendingSlotID = 1;
    private int selectedBodyPresetIndex;
    private bool loadOpenedFromGame;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        BuildRuntimeUI();
    }

    public static MainMenuManager OpenOrCreate()
    {
        MainMenuManager manager =
            Instance;

        if (manager == null)
        {
            GameObject menuObject =
                new GameObject(
                    "MainMenuManager"
                );

            manager =
                menuObject
                    .AddComponent<MainMenuManager>();
        }

        manager.gameObject.SetActive(true);
        manager.MoveToScene(
            SceneFlowManager.MainMenuSceneName
        );
        manager.loadOpenedFromGame = false;
        manager.ShowHome();

        return manager;
    }

    public static void OpenLoadFromGame()
    {
        Scene menuScene =
            SceneManager.GetSceneByName(
                SceneFlowManager.MainMenuSceneName
            );

        if ((!menuScene.IsValid() ||
            !menuScene.isLoaded) &&
            SceneFlowManager.Instance != null)
        {
            SceneFlowManager
                .Instance
                .ShowLoadMenuFromGame();

            return;
        }

        MainMenuManager manager =
            OpenOrCreate();

        manager.loadOpenedFromGame =
            true;

        manager.ShowLoadSlotSelect();
    }

    public static void HideActiveMenu()
    {
        if (Instance == null)
            return;

        Instance.Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ShowHome()
    {
        ClearContent();

        AddTitle("Mundo de Fiore");
        AddBody("Escolha como deseja iniciar.");

        AddButton(
            "Novo Jogo",
            ShowNewSlotSelect
        );

        AddButton(
            "Carregar Jogo",
            () =>
            {
                loadOpenedFromGame =
                    false;

                ShowLoadSlotSelect();
            }
        );

        AddButton(
            "Sair",
            () =>
            {
                Application.Quit();
            },
            new Color(0.19f, 0.13f, 0.1f, 1f)
        );
    }

    private void ShowNewSlotSelect()
    {
        ClearContent();

        AddTitle("Novo Jogo");
        AddBody("Escolha um dos 3 slots para criar a campanha.");

        for (int i = 1; i <= 3; i++)
        {
            int slotID =
                i;

            SaveSlotInfo info =
                SaveManager
                    .Instance
                    .GetSaveSlotInfo(slotID);

            AddSlotCard(info);

            AddButton(
                info.HasSave
                    ? $"Sobrescrever Slot {slotID}"
                    : $"Usar Slot {slotID}",
                () =>
                {
                    if (info.HasSave)
                    {
                        ShowOverwriteConfirm(slotID);
                        return;
                    }

                    ShowCharacterCreation(slotID);
                }
            );
        }

        AddButton(
            "Voltar",
            ShowHome
        );
    }

    private void ShowLoadSlotSelect()
    {
        ClearContent();

        AddTitle("Carregar Jogo");
        AddBody("Escolha o slot salvo.");

        for (int i = 1; i <= 3; i++)
        {
            int slotID =
                i;

            SaveSlotInfo info =
                SaveManager
                    .Instance
                    .GetSaveSlotInfo(slotID);

            AddSlotCard(info);

            AddButton(
                info.HasSave
                    ? $"Carregar Slot {slotID}"
                    : $"Slot {slotID} vazio",
                () =>
                {
                    if (!SaveManager
                        .Instance
                        .LoadGame(slotID))
                    {
                        GameFeedbackUI.ShowNotification(
                            "Slot vazio."
                        );

                        return;
                    }

                    SceneFlowManager
                        .GetOrCreate()
                        .EnterGameFromCurrentSave();

                    GameFeedbackUI.ShowNotification(
                        "Jogo carregado."
                    );
                },
                info.HasSave
                    ? new Color(0.22f, 0.18f, 0.12f, 1f)
                    : new Color(0.12f, 0.11f, 0.1f, 1f)
            );

            if (info.HasSave)
            {
                AddButton(
                    $"Apagar Slot {slotID}",
                    () => ShowDeleteConfirm(slotID),
                    new Color(0.28f, 0.1f, 0.08f, 1f)
                );
            }
        }

        AddButton(
            "Voltar",
            () =>
            {
                if (loadOpenedFromGame &&
                    SaveManager.Instance.HasActiveGame)
                {
                    SceneFlowManager
                        .GetOrCreate()
                        .ReturnToGameFromMenu();

                    return;
                }

                ShowHome();
            }
        );
    }

    private void ShowOverwriteConfirm(
        int slotID)
    {
        pendingSlotID =
            slotID;

        ClearContent();

        AddTitle("Sobrescrever Slot");
        AddBody(
            $"O Slot {slotID} ja possui um save. Confirmar vai apagar os dados desse slot."
        );

        AddButton(
            "Confirmar sobrescrita",
            () => ShowCharacterCreation(pendingSlotID),
            new Color(0.36f, 0.12f, 0.1f, 1f)
        );

        AddButton(
            "Voltar",
            ShowNewSlotSelect
        );
    }

    private void ShowDeleteConfirm(
        int slotID)
    {
        pendingSlotID =
            slotID;

        ClearContent();

        AddTitle("Apagar Slot");
        AddBody(
            $"Tem certeza que deseja apagar o Slot {slotID}? Essa acao nao pode ser desfeita."
        );

        AddButton(
            "Apagar definitivamente",
            () =>
            {
                SaveManager
                    .Instance
                    .DeleteSave(pendingSlotID);

                GameFeedbackUI.ShowNotification(
                    "Save apagado."
                );

                ShowLoadSlotSelect();
            },
            new Color(0.36f, 0.12f, 0.1f, 1f)
        );

        AddButton(
            "Voltar",
            ShowLoadSlotSelect
        );
    }

    private void ShowCharacterCreation(
        int slotID)
    {
        pendingSlotID =
            slotID;

        selectedBodyPresetIndex =
            0;

        ClearContent();

        AddTitle("Criar Personagem");
        AddBody($"Slot escolhido: {pendingSlotID}");

        characterNameInput =
            AddInputField(
                "Nome do personagem",
                "Hero"
            );

        bodyPresetText =
            AddBodyText(
                $"Preset visual: {bodyPresets[selectedBodyPresetIndex]}"
            );

        AddButton(
            "Trocar preset visual",
            CycleBodyPreset
        );

        AddButton(
            "Confirmar personagem",
            ConfirmCharacterCreation
        );

        AddButton(
            "Voltar",
            ShowNewSlotSelect
        );
    }

    private void CycleBodyPreset()
    {
        selectedBodyPresetIndex =
            (selectedBodyPresetIndex + 1)
            % bodyPresets.Length;

        if (bodyPresetText != null)
        {
            bodyPresetText.text =
                $"Preset visual: {bodyPresets[selectedBodyPresetIndex]}";
        }
    }

    private void ConfirmCharacterCreation()
    {
        string characterName =
            characterNameInput != null
                ? characterNameInput.text
                : "Hero";

        SaveManager
            .Instance
            .NewGame(
                pendingSlotID,
                characterName,
                bodyPresets[selectedBodyPresetIndex],
                "portrait_default",
                "race_human"
            );

        SceneFlowManager
            .GetOrCreate()
            .EnterGameFromCurrentSave();

        GameFeedbackUI.ShowNotification(
            "Novo jogo iniciado."
        );
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

    private void AddSlotCard(
        SaveSlotInfo info)
    {
        if (info == null ||
            !info.HasSave)
        {
            AddCard(
                $"Slot {info?.SlotID ?? 0}",
                "Slot vazio."
            );

            return;
        }

        AddCard(
            $"Slot {info.SlotID}",
            $"Personagem: {info.CharacterName}\n" +
            $"Cidade: {info.CurrentCityID}\n" +
            $"Data: {info.GetWorldDateLabel()}\n" +
            $"Ultimo save: {info.LastSavedAt}"
        );
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
            900;

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
            new Color(0.05f, 0.045f, 0.04f, 1f)
        );

        RectTransform viewport =
            CreateRect(
                "ContentViewport",
                root,
                new Vector2(0.07f, 0.06f),
                new Vector2(0.93f, 0.94f)
            );

        viewport
            .gameObject
            .AddComponent<RectMask2D>();

        ScrollRect scrollRect =
            viewport
                .gameObject
                .AddComponent<ScrollRect>();

        scrollRect.horizontal =
            false;

        scrollRect.vertical =
            true;

        scrollRect.viewport =
            viewport;

        contentRoot =
            CreateRect(
                "Content",
                viewport,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            );

        contentRoot.pivot =
            new Vector2(0.5f, 1f);

        contentRoot.sizeDelta =
            new Vector2(0f, 1600f);

        scrollRect.content =
            contentRoot;

        PrepareContentLayout();
    }

    private void AddTitle(
        string text)
    {
        TMP_Text title =
            AddTextBlock(
                "Title",
                42f,
                72f,
                TextAlignmentOptions.Center
            );

        title.text =
            text;
    }

    private void AddBody(
        string text)
    {
        TMP_Text body =
            AddBodyText(text);

        body.alignment =
            TextAlignmentOptions.Center;
    }

    private TMP_Text AddBodyText(
        string text)
    {
        TMP_Text body =
            AddTextBlock(
                "Body",
                23f,
                78f,
                TextAlignmentOptions.TopLeft
            );

        body.text =
            text;

        return body;
    }

    private void AddCard(
        string title,
        string body)
    {
        RectTransform cardRoot =
            CreateLayoutRect(
                "Card",
                contentRoot,
                142f
            );

        AddImage(
            cardRoot.gameObject,
            new Color(0.11f, 0.1f, 0.085f, 1f)
        );

        TMP_Text cardText =
            CreateText(
                "CardText",
                cardRoot,
                new Vector2(0.04f, 0.1f),
                new Vector2(0.96f, 0.9f),
                22f,
                TextAlignmentOptions.TopLeft
            );

        cardText.text =
            $"<b>{title}</b>\n{body}";
    }

    private void AddButton(
        string label,
        UnityEngine.Events.UnityAction onClick)
    {
        AddButton(
            label,
            onClick,
            new Color(0.22f, 0.18f, 0.12f, 1f)
        );
    }

    private void AddButton(
        string label,
        UnityEngine.Events.UnityAction onClick,
        Color color)
    {
        Button button =
            CreateButtonObject(
                label,
                contentRoot,
                color
            );

        button.onClick.AddListener(onClick);
    }

    private TMP_InputField AddInputField(
        string placeholder,
        string defaultValue)
    {
        RectTransform rect =
            CreateLayoutRect(
                "InputField",
                contentRoot,
                84f
            );

        Image image =
            AddImage(
                rect.gameObject,
                new Color(0.1f, 0.09f, 0.075f, 1f)
            );

        TMP_InputField input =
            rect.gameObject
                .AddComponent<TMP_InputField>();

        input.targetGraphic =
            image;

        TMP_Text text =
            CreateText(
                "Text",
                rect,
                new Vector2(0.05f, 0.18f),
                new Vector2(0.95f, 0.82f),
                25f,
                TextAlignmentOptions.Left
            );

        TMP_Text placeholderText =
            CreateText(
                "Placeholder",
                rect,
                new Vector2(0.05f, 0.18f),
                new Vector2(0.95f, 0.82f),
                25f,
                TextAlignmentOptions.Left
            );

        placeholderText.text =
            placeholder;

        placeholderText.color =
            new Color(1f, 1f, 1f, 0.46f);

        input.textComponent =
            text;

        input.placeholder =
            placeholderText;

        input.text =
            defaultValue;

        return input;
    }

    private TMP_Text AddTextBlock(
        string name,
        float fontSize,
        float minHeight,
        TextAlignmentOptions alignment)
    {
        RectTransform rect =
            CreateLayoutRect(
                name,
                contentRoot,
                minHeight
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

    private Button CreateButtonObject(
        string label,
        Transform parent,
        Color color)
    {
        RectTransform rect =
            CreateLayoutRect(
                label,
                parent,
                76f
            );

        Image image =
            AddImage(
                rect.gameObject,
                color
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
                new Vector2(0.05f, 0.14f),
                new Vector2(0.95f, 0.86f),
                22f,
                TextAlignmentOptions.Center
            );

        text.text =
            label;

        return button;
    }

    private RectTransform CreateLayoutRect(
        string name,
        Transform parent,
        float minHeight)
    {
        RectTransform rect =
            CreateRect(
                name,
                parent,
                Vector2.zero,
                Vector2.one
            );

        LayoutElement layout =
            rect.gameObject
                .AddComponent<LayoutElement>();

        layout.minHeight =
            minHeight;

        layout.flexibleWidth =
            1f;

        return rect;
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
            rectObject
                .GetComponent<RectTransform>();

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

    private void ClearContent()
    {
        for (int i = contentRoot.childCount - 1;
            i >= 0;
            i--)
        {
            Destroy(
                contentRoot
                    .GetChild(i)
                    .gameObject
            );
        }

        PrepareContentLayout();
    }

    private void PrepareContentLayout()
    {
        VerticalLayoutGroup layout =
            contentRoot
                .GetComponent<VerticalLayoutGroup>();

        if (layout == null)
        {
            layout =
                contentRoot
                    .gameObject
                    .AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing =
            14f;

        layout.padding =
            new RectOffset(0, 0, 18, 24);

        layout.childAlignment =
            TextAnchor.UpperCenter;

        layout.childForceExpandWidth =
            true;

        layout.childForceExpandHeight =
            false;

        ContentSizeFitter fitter =
            contentRoot
                .GetComponent<ContentSizeFitter>();

        if (fitter == null)
        {
            fitter =
                contentRoot
                    .gameObject
                    .AddComponent<ContentSizeFitter>();
        }

        fitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            contentRoot
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
                SceneFlowManager.MainMenuSceneName
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
