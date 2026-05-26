using UnityEngine;
using System.Collections.Generic;

public class WorldStateManager
    : PersistentSingleton<
        WorldStateManager>
{
    private Dictionary<
        string,
        bool> flags =
            new();

    protected override void Awake()
    {
        base.Awake();
    }

    public void LoadFlags()
    {
        flags.Clear();

        if (SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null)
        {
            Debug.LogWarning(
                "Save not loaded yet."
            );

            return;
        }

        foreach (var flag
            in SaveManager.Instance
                .CurrentSave
                .WorldState
                .Flags)
        {
            flags[flag.Key] =
                flag.Value;
        }

        Debug.Log(
            $"Loaded {flags.Count} flags."
        );
    }

    public bool HasFlag(
        string key)
    {
        return flags.TryGetValue(
            key,
            out bool value)
            && value;
    }

    public void SetFlag(
        string key,
        bool value = true)
    {
        flags[key] = value;

        SaveFlag(
            key,
            value
        );

        Debug.Log(
            $"Flag Set: " +
            $"{key} = {value}"
        );
    }

    private void SaveFlag(
        string key,
        bool value)
    {
        var saveFlags =
            SaveManager
            .Instance
            .CurrentSave
            .WorldState
            .Flags;

        var existing =
            saveFlags.Find(
                x => x.Key == key
            );

        if (existing != null)
        {
            existing.Value =
                value;
        }
        else
        {
            saveFlags.Add(
                new WorldFlag
                {
                    Key = key,
                    Value = value
                });
        }

        SaveManager
            .Instance
            .SaveGame();
    }
}