using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DatabaseManager
    : PersistentSingleton<DatabaseManager>
{
    [SerializeField]
    private GameDatabase database;

    private Dictionary<string, BaseData>
        dataLookup = new();

    protected override void Awake()
    {
        base.Awake();

        BuildDatabase();
    }

    private void BuildDatabase()
    {
        AddCollection(database.Races);

        AddCollection(database.Regions);

        AddCollection(database.Cities);

        AddCollection(database.ExplorationAreas);

        AddCollection(database.TravelEvents);

        AddCollection(database.WorldEvents);

        AddCollection(database.RelationshipEvents);

        AddCollection(database.CalendarEvents);

        AddCollection(database.ExplorationEvents);

        AddCollection(database.Quests);

        AddCollection(database.GuildTasks);

        AddCollection(database.Enemies);

        AddCollection(database.Items);

        AddCollection(database.Factions);

        AddCollection(database.Dialogues);

        AddCollection(database.NPCs);

        Debug.Log(
            $"Database Loaded: {dataLookup.Count}"
        );
    }

    private void AddCollection<T>(
        List<T> collection
    ) where T : BaseData
    {
        if (collection == null)
            return;

        foreach (var item in collection)
        {
            if (item == null)
                continue;

            if (dataLookup.ContainsKey(item.ID))
            {
                Debug.LogWarning(
                    $"Duplicate ID: {item.ID}"
                );

                continue;
            }

            dataLookup.Add(
                item.ID,
                item
            );
        }
    }

    public T GetData<T>(
        string id
    ) where T : BaseData
    {
        if (dataLookup.TryGetValue(
            id,
            out BaseData data))
        {
            return data as T;
        }

        Debug.LogWarning(
            $"Data not found: {id}"
        );

        return null;
    }

    public List<T>
    GetAllData<T>()
    where T : BaseData
    {
        List<T> result =
            new();

        foreach (var data
            in dataLookup.Values)
        {
            if (data is T typed)
            {
                result.Add(typed);
            }
        }

        return result;
    }

    public ItemData GetItemById(
        string itemId)
    {
        return GetData<ItemData>(itemId);
    }

    public bool HasItem(
        string itemId)
    {
        return GetItemById(itemId) != null;
    }

    public NPCData GetNPCById(
        string npcId)
    {
        return GetData<NPCData>(npcId);
    }

    public bool HasNPC(
        string npcId)
    {
        return GetNPCById(npcId) != null;
    }
}
