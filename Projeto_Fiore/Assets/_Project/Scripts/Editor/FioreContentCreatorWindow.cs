using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class FioreContentCreatorWindow : EditorWindow
{
    private const string DatabaseRootFolder = "Assets/_Project/Database";
    private const string MainDatabasePath = "Assets/Scripts/Data/ScriptableObjects/Database/MainDatabase.asset";

    private static readonly ContentConfig[] ContentConfigs =
        BuildContentConfigsFromGameDatabase();

    private readonly List<UnityEngine.Object> loadedAssets = new();

    private int selectedContentIndex;
    private UnityEngine.Object selectedAsset;
    private SerializedObject selectedSerializedObject;
    private GameDatabase targetDatabase;
    private string searchText = string.Empty;
    private string databaseMessage = string.Empty;
    private Vector2 contentTabsScroll;
    private Vector2 listScroll;
    private Vector2 editorScroll;

    private sealed class ContentConfig
    {
        public ContentConfig(
            string label,
            Type assetType,
            string databaseProperty,
            string idPrefix,
            string displayNameSeed)
        {
            Label = label;
            AssetType = assetType;
            DatabaseProperty = databaseProperty;
            IdPrefix = idPrefix;
            DisplayNameSeed = displayNameSeed;
        }

        public string Label { get; }
        public Type AssetType { get; }
        public string DatabaseProperty { get; }
        public string IdPrefix { get; }
        public string DisplayNameSeed { get; }

        public string FolderPath =>
            $"{DatabaseRootFolder}/{DatabaseProperty}";
    }

    private ContentConfig CurrentConfig =>
        ContentConfigs[Mathf.Clamp(
            selectedContentIndex,
            0,
            ContentConfigs.Length - 1)];

    [MenuItem("Tools/Fiore/Content Creator")]
    public static void OpenWindow()
    {
        FioreContentCreatorWindow window =
            GetWindow<FioreContentCreatorWindow>();

        window.titleContent =
            new GUIContent("Fiore Content Creator");
        window.minSize =
            new Vector2(920f, 540f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent =
            new GUIContent("Fiore Content Creator");
        minSize =
            new Vector2(920f, 540f);

        targetDatabase =
            FindMainDatabase();
        LoadAssets();
    }

    private void OnFocus()
    {
        LoadAssets();
    }

    private void OnGUI()
    {
        DrawHeader();

        if (ContentConfigs.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "No ScriptableObject lists were found in GameDatabase.",
                MessageType.Warning);
            return;
        }

        DrawDatabaseControls();
        DrawToolbar();
        EditorGUILayout.Space(6f);
        DrawSelectedContent();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(
            "Fiore Content Creator",
            EditorStyles.boldLabel);
    }

    private void DrawDatabaseControls()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "Game Database",
            EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        targetDatabase =
            (GameDatabase)EditorGUILayout.ObjectField(
                "Target",
                targetDatabase,
                typeof(GameDatabase),
                false);

        if (GUILayout.Button(
                "Use MainDatabase",
                GUILayout.Width(130f)))
        {
            targetDatabase =
                FindMainDatabase();
            databaseMessage =
                targetDatabase != null
                    ? "MainDatabase selected."
                    : "MainDatabase.asset was not found.";
        }

        if (GUILayout.Button(
                "Add All To Database",
                GUILayout.Width(150f)))
        {
            AddAllAssetsToDatabase();
        }

        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrWhiteSpace(databaseMessage))
        {
            EditorGUILayout.HelpBox(
                databaseMessage,
                MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        contentTabsScroll =
            EditorGUILayout.BeginScrollView(
                contentTabsScroll,
                false,
                false,
                GUILayout.Height(36f));

        EditorGUILayout.BeginHorizontal(
            EditorStyles.toolbar);

        for (int i = 0; i < ContentConfigs.Length; i++)
        {
            bool isSelected =
                selectedContentIndex == i;

            if (GUILayout.Toggle(
                    isSelected,
                    ContentConfigs[i].Label,
                    EditorStyles.toolbarButton,
                    GUILayout.Width(GetTabWidth(ContentConfigs[i].Label))) &&
                !isSelected)
            {
                SelectContentTab(i);
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectedContent()
    {
        EditorGUILayout.BeginHorizontal();
        DrawAssetList();
        DrawAssetEditor();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAssetList()
    {
        ContentConfig config =
            CurrentConfig;

        EditorGUILayout.BeginVertical(
            GUILayout.Width(305f));

        EditorGUILayout.LabelField(
            config.Label,
            EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        searchText =
            EditorGUILayout.TextField(
                searchText,
                GUI.skin.FindStyle("ToolbarSearchTextField") ??
                EditorStyles.textField);

        if (GUILayout.Button(
                "Refresh",
                EditorStyles.miniButton,
                GUILayout.Width(64f)))
        {
            LoadAssets();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4f);

        listScroll =
            EditorGUILayout.BeginScrollView(
                listScroll,
                GUI.skin.box);

        foreach (UnityEngine.Object asset in loadedAssets)
        {
            if (asset == null ||
                !AssetMatchesSearch(asset))
            {
                continue;
            }

            DrawAssetListItem(asset);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(6f);

        if (GUILayout.Button(
                $"Create New {config.AssetType.Name}",
                GUILayout.Height(30f)))
        {
            CreateNewAsset();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAssetListItem(
        UnityEngine.Object asset)
    {
        bool isSelected =
            selectedAsset == asset;
        Color previousColor =
            GUI.backgroundColor;

        if (isSelected)
        {
            GUI.backgroundColor =
                new Color(0.55f, 0.75f, 1f);
        }

        if (GUILayout.Button(
                GetAssetListLabel(asset),
                EditorStyles.miniButton))
        {
            SelectAsset(asset);
        }

        GUI.backgroundColor =
            previousColor;
    }

    private void DrawAssetEditor()
    {
        EditorGUILayout.BeginVertical(
            GUILayout.ExpandWidth(true));

        if (selectedAsset == null)
        {
            EditorGUILayout.HelpBox(
                "Select an asset or create a new one to edit its data.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EnsureSerializedObject();

        EditorGUILayout.ObjectField(
            "Selected Asset",
            selectedAsset,
            CurrentConfig.AssetType,
            false);

        selectedSerializedObject.Update();

        editorScroll =
            EditorGUILayout.BeginScrollView(
                editorScroll,
                GUILayout.ExpandHeight(true));

        DrawSerializedProperties(
            selectedSerializedObject);

        EditorGUILayout.EndScrollView();

        if (selectedSerializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(selectedAsset);
        }

        EditorGUILayout.Space(6f);
        DrawEditorActions();
        DrawValidationMessages();

        EditorGUILayout.EndVertical();
    }

    private void DrawEditorActions()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "Save",
                GUILayout.Height(28f)))
        {
            SaveSelectedAsset();
        }

        if (GUILayout.Button(
                "Ping Asset",
                GUILayout.Height(28f)))
        {
            PingSelectedAsset();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSerializedProperties(
        SerializedObject serializedObject)
    {
        SerializedProperty property =
            serializedObject.GetIterator();
        bool enterChildren =
            true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren =
                false;

            if (property.name == "m_Script")
            {
                continue;
            }

            EditorGUILayout.PropertyField(
                property,
                true);
        }
    }

    private void DrawValidationMessages()
    {
        if (selectedSerializedObject == null)
        {
            return;
        }

        List<string> warnings =
            GetValidationWarnings(selectedSerializedObject);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(
            "Validation",
            EditorStyles.boldLabel);

        if (warnings.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No validation warnings.",
                MessageType.Info);
            return;
        }

        foreach (string warning in warnings)
        {
            EditorGUILayout.HelpBox(
                warning,
                MessageType.Warning);
        }
    }

    private void SelectContentTab(
        int index)
    {
        selectedContentIndex =
            index;
        selectedAsset =
            null;
        selectedSerializedObject =
            null;
        searchText =
            string.Empty;
        LoadAssets();
    }

    private void LoadAssets()
    {
        loadedAssets.Clear();

        if (ContentConfigs.Length == 0)
        {
            return;
        }

        foreach (UnityEngine.Object asset in FindAssetsOfType(CurrentConfig.AssetType))
        {
            loadedAssets.Add(asset);
        }

        loadedAssets.Sort(CompareAssetsByName);

        if (selectedAsset != null &&
            !loadedAssets.Contains(selectedAsset))
        {
            selectedAsset =
                null;
            selectedSerializedObject =
                null;
        }
    }

    private List<UnityEngine.Object> FindAssetsOfType(
        Type assetType)
    {
        List<UnityEngine.Object> assets =
            new();
        string[] guids =
            AssetDatabase.FindAssets(
                $"t:{assetType.Name}");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object asset =
                AssetDatabase.LoadAssetAtPath(
                    path,
                    assetType);

            if (asset != null &&
                !assets.Contains(asset))
            {
                assets.Add(asset);
            }
        }

        return assets;
    }

    private void CreateNewAsset()
    {
        ContentConfig config =
            CurrentConfig;

        EnsureFolderExists(config.FolderPath);

        ScriptableObject asset =
            ScriptableObject.CreateInstance(config.AssetType);
        string id =
            GenerateNewId(config.IdPrefix);

        asset.name =
            id;

        SerializedObject serializedObject =
            new(asset);

        SetStringProperty(
            serializedObject,
            id,
            "ID",
            "id");
        SetStringProperty(
            serializedObject,
            config.DisplayNameSeed,
            "DisplayName",
            "displayName");
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        string assetPath =
            AssetDatabase.GenerateUniqueAssetPath(
                $"{config.FolderPath}/{MakeSafeFileName(id)}.asset");

        AssetDatabase.CreateAsset(
            asset,
            assetPath);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LoadAssets();

        UnityEngine.Object createdAsset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                assetPath);

        SelectAsset(
            createdAsset != null
                ? createdAsset
                : asset);
        PingSelectedAsset();
    }

    private void SaveSelectedAsset()
    {
        if (selectedAsset == null)
        {
            return;
        }

        string assetPath =
            AssetDatabase.GetAssetPath(selectedAsset);

        selectedSerializedObject?.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(assetPath))
        {
            UnityEngine.Object refreshedAsset =
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    assetPath);

            if (refreshedAsset != null)
            {
                SelectAsset(refreshedAsset);
            }
        }

        ShowNotification(
            new GUIContent("Asset saved"));
        LoadAssets();
    }

    private void AddAllAssetsToDatabase()
    {
        if (targetDatabase == null)
        {
            databaseMessage =
                "Choose a GameDatabase asset first.";
            return;
        }

        SerializedObject databaseObject =
            new(targetDatabase);
        int addedCount =
            0;
        int alreadyPresentCount =
            0;
        int missingListCount =
            0;

        foreach (ContentConfig config in ContentConfigs)
        {
            SerializedProperty listProperty =
                databaseObject.FindProperty(config.DatabaseProperty);

            if (listProperty == null ||
                !listProperty.isArray)
            {
                missingListCount++;
                continue;
            }

            foreach (UnityEngine.Object asset in FindAssetsOfType(config.AssetType))
            {
                if (ArrayContainsObject(
                        listProperty,
                        asset))
                {
                    alreadyPresentCount++;
                    continue;
                }

                AddObjectToArray(
                    listProperty,
                    asset);
                addedCount++;
            }
        }

        databaseObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(targetDatabase);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        databaseMessage =
            $"Database updated. Added: {addedCount}. Already present: {alreadyPresentCount}. Missing lists: {missingListCount}.";
        ShowNotification(
            new GUIContent("Database updated"));
    }

    private void AddObjectToArray(
        SerializedProperty arrayProperty,
        UnityEngine.Object asset)
    {
        int index =
            arrayProperty.arraySize;

        arrayProperty.InsertArrayElementAtIndex(index);

        SerializedProperty element =
            arrayProperty.GetArrayElementAtIndex(index);
        element.objectReferenceValue =
            asset;
    }

    private bool ArrayContainsObject(
        SerializedProperty arrayProperty,
        UnityEngine.Object asset)
    {
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            SerializedProperty element =
                arrayProperty.GetArrayElementAtIndex(i);

            if (element.objectReferenceValue == asset)
            {
                return true;
            }
        }

        return false;
    }

    private GameDatabase FindMainDatabase()
    {
        GameDatabase mainDatabase =
            AssetDatabase.LoadAssetAtPath<GameDatabase>(
                MainDatabasePath);

        if (mainDatabase != null)
        {
            return mainDatabase;
        }

        string[] guids =
            AssetDatabase.FindAssets("t:GameDatabase MainDatabase");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);
            GameDatabase database =
                AssetDatabase.LoadAssetAtPath<GameDatabase>(
                    path);

            if (database != null)
            {
                return database;
            }
        }

        return null;
    }

    private void EnsureFolderExists(
        string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) ||
            !folderPath.StartsWith("Assets", StringComparison.Ordinal))
        {
            return;
        }

        string[] parts =
            folderPath.Split('/');
        string currentPath =
            parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath =
                $"{currentPath}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(
                    currentPath,
                    parts[i]);
            }

            currentPath =
                nextPath;
        }
    }

    private string GenerateNewId(
        string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}".Substring(
            0,
            prefix.Length + 7);
    }

    private void PingSelectedAsset()
    {
        if (selectedAsset == null)
        {
            return;
        }

        Selection.activeObject =
            selectedAsset;
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(selectedAsset);
    }

    private void SelectAsset(
        UnityEngine.Object asset)
    {
        selectedAsset =
            asset;
        selectedSerializedObject =
            asset != null
                ? new SerializedObject(asset)
                : null;
        editorScroll =
            Vector2.zero;

        if (asset != null)
        {
            Selection.activeObject =
                asset;
        }
    }

    private void EnsureSerializedObject()
    {
        if (selectedSerializedObject == null ||
            selectedSerializedObject.targetObject != selectedAsset)
        {
            selectedSerializedObject =
                new SerializedObject(selectedAsset);
        }
    }

    private List<string> GetValidationWarnings(
        SerializedObject serializedObject)
    {
        List<string> warnings =
            new();

        SerializedProperty idProperty =
            FindProperty(
                serializedObject,
                "ID",
                "id");
        SerializedProperty displayNameProperty =
            FindProperty(
                serializedObject,
                "DisplayName",
                "displayName");

        if (IsEmptyStringProperty(idProperty))
        {
            warnings.Add("ID is empty.");
        }

        if (IsEmptyStringProperty(displayNameProperty))
        {
            warnings.Add("DisplayName is empty.");
        }

        if (CurrentConfig.AssetType == typeof(NPCData))
        {
            AddNpcValidationWarnings(
                serializedObject,
                warnings);
        }
        else if (CurrentConfig.AssetType == typeof(QuestData))
        {
            AddQuestValidationWarnings(
                serializedObject,
                warnings);
        }

        return warnings;
    }

    private void AddNpcValidationWarnings(
        SerializedObject serializedObject,
        List<string> warnings)
    {
        SerializedProperty homeCity =
            FindProperty(
                serializedObject,
                "HomeCity",
                "homeCity");
        SerializedProperty defaultLocationId =
            FindProperty(
                serializedObject,
                "DefaultLocationID",
                "defaultLocationID",
                "defaultLocationId");

        if (IsEmptyObjectReference(homeCity) &&
            IsEmptyStringProperty(defaultLocationId))
        {
            warnings.Add(
                "NPC has no HomeCity or DefaultLocationID set.");
        }
    }

    private void AddQuestValidationWarnings(
        SerializedObject serializedObject,
        List<string> warnings)
    {
        SerializedProperty objectives =
            FindProperty(
                serializedObject,
                "objectives",
                "Objectives");

        if (objectives != null &&
            objectives.isArray &&
            objectives.arraySize == 0)
        {
            warnings.Add("Quest has no objectives.");
        }
    }

    private bool AssetMatchesSearch(
        UnityEngine.Object asset)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        string label =
            GetAssetListLabel(asset);
        string id =
            GetStringPropertyValue(
                asset,
                "ID",
                "id");

        return ContainsIgnoreCase(
                   asset.name,
                   searchText) ||
               ContainsIgnoreCase(
                   label,
                   searchText) ||
               ContainsIgnoreCase(
                   id,
                   searchText);
    }

    private string GetAssetListLabel(
        UnityEngine.Object asset)
    {
        string displayName =
            GetStringPropertyValue(
                asset,
                "DisplayName",
                "displayName");

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return asset.name;
        }

        if (string.Equals(
                displayName,
                asset.name,
                StringComparison.Ordinal))
        {
            return displayName;
        }

        return $"{displayName} ({asset.name})";
    }

    private string GetStringPropertyValue(
        UnityEngine.Object asset,
        params string[] propertyNames)
    {
        if (asset == null)
        {
            return string.Empty;
        }

        SerializedObject serializedObject =
            new(asset);
        SerializedProperty property =
            FindProperty(
                serializedObject,
                propertyNames);

        return property != null &&
               property.propertyType == SerializedPropertyType.String
            ? property.stringValue
            : string.Empty;
    }

    private void SetStringProperty(
        SerializedObject serializedObject,
        string value,
        params string[] propertyNames)
    {
        SerializedProperty property =
            FindProperty(
                serializedObject,
                propertyNames);

        if (property == null ||
            property.propertyType != SerializedPropertyType.String)
        {
            return;
        }

        property.stringValue =
            value;
    }

    private SerializedProperty FindProperty(
        SerializedObject serializedObject,
        params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            if (property != null)
            {
                return property;
            }
        }

        SerializedProperty iterator =
            serializedObject.GetIterator();

        while (iterator.NextVisible(true))
        {
            foreach (string propertyName in propertyNames)
            {
                if (string.Equals(
                        iterator.name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return iterator.Copy();
                }
            }
        }

        return null;
    }

    private bool IsEmptyStringProperty(
        SerializedProperty property)
    {
        return property != null &&
               property.propertyType == SerializedPropertyType.String &&
               string.IsNullOrWhiteSpace(property.stringValue);
    }

    private bool IsEmptyObjectReference(
        SerializedProperty property)
    {
        return property != null &&
               property.propertyType ==
               SerializedPropertyType.ObjectReference &&
               property.objectReferenceValue == null;
    }

    private int CompareAssetsByName(
        UnityEngine.Object left,
        UnityEngine.Object right)
    {
        return string.Compare(
            GetAssetListLabel(left),
            GetAssetListLabel(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private bool ContainsIgnoreCase(
        string source,
        string value)
    {
        return !string.IsNullOrEmpty(source) &&
               source.IndexOf(
                   value,
                   StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private float GetTabWidth(
        string label)
    {
        return Mathf.Clamp(
            (label.Length * 8f) + 28f,
            72f,
            170f);
    }

    private string MakeSafeFileName(
        string value)
    {
        char[] invalidChars =
            Path.GetInvalidFileNameChars();
        char[] chars =
            value.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(
                    invalidChars,
                    chars[i]) >= 0 ||
                char.IsWhiteSpace(chars[i]))
            {
                chars[i] =
                    '_';
            }
        }

        string safeName =
            new string(chars).Trim('_');

        return string.IsNullOrEmpty(safeName)
            ? "NewAsset"
            : safeName;
    }

    private static ContentConfig[] BuildContentConfigsFromGameDatabase()
    {
        List<ContentConfig> configs =
            new();
        FieldInfo[] fields =
            typeof(GameDatabase).GetFields(
                BindingFlags.Instance |
                BindingFlags.Public);

        Array.Sort(
            fields,
            (left, right) =>
                left.MetadataToken.CompareTo(right.MetadataToken));

        foreach (FieldInfo field in fields)
        {
            if (!TryGetDatabaseAssetType(
                    field,
                    out Type assetType))
            {
                continue;
            }

            configs.Add(
                new ContentConfig(
                    ObjectNames.NicifyVariableName(field.Name),
                    assetType,
                    field.Name,
                    BuildIdPrefix(assetType),
                    $"New {BuildDisplayName(assetType)}"));
        }

        return configs.ToArray();
    }

    private static bool TryGetDatabaseAssetType(
        FieldInfo field,
        out Type assetType)
    {
        assetType =
            null;

        if (!field.FieldType.IsGenericType ||
            field.FieldType.GetGenericTypeDefinition() != typeof(List<>))
        {
            return false;
        }

        Type candidateType =
            field.FieldType.GetGenericArguments()[0];

        if (!typeof(ScriptableObject).IsAssignableFrom(candidateType))
        {
            return false;
        }

        assetType =
            candidateType;
        return true;
    }

    private static string BuildDisplayName(
        Type assetType)
    {
        string typeName =
            StripDataSuffix(assetType.Name);

        return ObjectNames.NicifyVariableName(typeName);
    }

    private static string BuildIdPrefix(
        Type assetType)
    {
        string typeName =
            StripDataSuffix(assetType.Name);
        List<char> chars =
            new();

        for (int i = 0; i < typeName.Length; i++)
        {
            char current =
                typeName[i];

            if (char.IsUpper(current))
            {
                bool shouldSeparate =
                    i > 0 &&
                    chars.Count > 0 &&
                    chars[^1] != '_' &&
                    (char.IsLower(typeName[i - 1]) ||
                     i + 1 < typeName.Length &&
                     char.IsLower(typeName[i + 1]));

                if (shouldSeparate)
                {
                    chars.Add('_');
                }

                chars.Add(
                    char.ToLowerInvariant(current));
            }
            else if (char.IsLetterOrDigit(current))
            {
                chars.Add(
                    char.ToLowerInvariant(current));
            }
            else if (chars.Count > 0 &&
                     chars[^1] != '_')
            {
                chars.Add('_');
            }
        }

        string prefix =
            new string(chars.ToArray()).Trim('_');

        return string.IsNullOrEmpty(prefix)
            ? "asset"
            : prefix;
    }

    private static string StripDataSuffix(
        string typeName)
    {
        return typeName.EndsWith(
            "Data",
            StringComparison.Ordinal)
            ? typeName.Substring(
                0,
                typeName.Length - 4)
            : typeName;
    }
}
