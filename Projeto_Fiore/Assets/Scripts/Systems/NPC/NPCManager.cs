using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NPCManager
    : PersistentSingleton<NPCManager>
{
    public NPCData GetNPCById(
        string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
            return null;

        return DatabaseManager
            .Instance
            .GetNPCById(npcId);
    }

    public bool HasNPC(
        string npcId)
    {
        return GetNPCById(npcId) != null;
    }

    public List<NPCData> GetNPCsInCurrentCity()
    {
        CityData currentCity =
            CityManager
            .Instance
            .CurrentCity;

        if (currentCity == null)
            return new List<NPCData>();

        return GetNPCsForCity(currentCity.ID);
    }

    public List<NPCData> GetPublicNPCsInCurrentCity()
    {
        CityData currentCity =
            CityManager
                .Instance
                .CurrentCity;

        if (currentCity == null)
            return new List<NPCData>();

        return GetNPCsForCity(currentCity.ID)
            .Where(npc =>
                npc != null &&
                npc.AppearsInCityOverview)
            .ToList();
    }

    public List<NPCData> GetNPCsForLocation(
        CityData city,
        CityLocationData location)
    {
        List<NPCData> result =
            new();

        if (city == null ||
            location == null)
        {
            return result;
        }

        if (location.NPCs != null)
        {
            foreach (NPCData npc
                in location.NPCs)
            {
                TryAddNPCForLocation(
                    result,
                    npc,
                    city,
                    location
                );
            }
        }

        if (location.NPCIDs != null)
        {
            foreach (string npcId
                in location.NPCIDs)
            {
                TryAddNPCForLocation(
                    result,
                    GetNPCById(npcId),
                    city,
                    location
                );
            }
        }

        foreach (NPCData npc
            in GetNPCsForCity(city.ID))
        {
            if (npc == null ||
                !IsNPCPresentAtLocation(
                    npc,
                    city,
                    location))
            {
                continue;
            }

            TryAddNPC(result, npc, city);
        }

        return result;
    }

    public List<NPCData> GetNPCsForCity(
        string cityId)
    {
        CityData city =
            DatabaseManager
                .Instance
                .GetData<CityData>(cityId);

        if (city == null)
            return new List<NPCData>();

        List<NPCData> result =
            new();

        if (city.NPCs != null)
        {
            foreach (NPCData npc
                in city.NPCs)
            {
                TryAddNPC(result, npc, city);
            }
        }

        foreach (NPCData npc
            in DatabaseManager
                .Instance
                .GetAllData<NPCData>())
        {
            TryAddNPC(result, npc, city);
        }

        return result;
    }

    private void TryAddNPC(
        List<NPCData> result,
        NPCData npc,
        CityData city)
    {
        if (npc == null ||
            city == null ||
            npc.HomeCity != city ||
            result.Contains(npc) ||
            !IsNPCAvailable(npc))
        {
            return;
        }

        result.Add(npc);
    }

    private void TryAddNPCForLocation(
        List<NPCData> result,
        NPCData npc,
        CityData city,
        CityLocationData location)
    {
        if (npc == null ||
            city == null ||
            location == null ||
            npc.HomeCity != city ||
            result.Contains(npc) ||
            !IsNPCAvailable(npc) ||
            !IsNPCPresentAtLocation(
                npc,
                city,
                location))
        {
            return;
        }

        result.Add(npc);
    }

    public bool IsNPCPresentAtLocation(
        NPCData npc,
        CityData city,
        CityLocationData location)
    {
        if (npc == null ||
            city == null ||
            location == null ||
            npc.HomeCity != city)
        {
            return false;
        }

        NPCScheduleEntry scheduleEntry =
            GetCurrentScheduleEntry(
                npc,
                city.ID
            );

        if (scheduleEntry != null)
        {
            return scheduleEntry.IsAvailable &&
                scheduleEntry.LocationID ==
                    location.LocationID &&
                RequirementChecker
                    .AreRequirementsMet(
                        scheduleEntry.Requirements,
                        npc.ID
                    );
        }

        return npc.DefaultLocationID ==
            location.LocationID;
    }

    public List<NPCData> GetNPCsInCurrentCityLegacy()
    {
        CityData currentCity =
            CityManager
            .Instance
            .CurrentCity;

        return DatabaseManager
            .Instance
            .GetAllData<NPCData>()
            .Where(npc =>
                npc.HomeCity == currentCity
                &&
                IsNPCAvailable(npc)
            )
            .ToList();
    }

    public bool IsNPCAvailable(NPCData npc)
    {
        if (npc == null)
            return false;

        NPCScheduleEntry scheduleEntry =
            GetCurrentScheduleEntry(
                npc,
                npc.HomeCity != null
                    ? npc.HomeCity.ID
                    : null
            );

        if (scheduleEntry != null)
        {
            return scheduleEntry.IsAvailable &&
                RequirementChecker
                    .AreRequirementsMet(
                        npc.AppearanceRequirements
                    ) &&
                RequirementChecker
                    .AreRequirementsMet(
                        scheduleEntry.Requirements,
                        npc.ID
                    );
        }

        if (npc.AlwaysAvailable)
            return RequirementChecker
                .AreRequirementsMet(
                    npc.AppearanceRequirements
                );

        int currentHour =
            TimeManager
            .Instance
            .CurrentTime
            .Hour;

        if (npc.AvailableFromHour
            <= npc.AvailableUntilHour)
        {
            bool isWithinRegularHours =
                currentHour >= npc.AvailableFromHour
                &&
                currentHour < npc.AvailableUntilHour;

            return isWithinRegularHours &&
                RequirementChecker
                    .AreRequirementsMet(
                        npc.AppearanceRequirements
                    );
        }

        // Caso atravesse meia-noite.
        // Exemplo: 18h até 2h.
        bool isWithinHours =
            currentHour >= npc.AvailableFromHour
            ||
            currentHour < npc.AvailableUntilHour;

        return isWithinHours &&
            RequirementChecker
                .AreRequirementsMet(
                    npc.AppearanceRequirements
                );
    }

    private NPCScheduleEntry GetCurrentScheduleEntry(
        NPCData npc,
        string cityId)
    {
        if (npc == null ||
            npc.ScheduleEntries == null ||
            npc.ScheduleEntries.Count == 0 ||
            TimeManager.Instance == null)
        {
            return null;
        }

        TimeOfDay currentTimeOfDay =
            TimeManager
                .Instance
                .GetCurrentTimeOfDay();

        foreach (NPCScheduleEntry entry
            in npc.ScheduleEntries)
        {
            if (entry == null ||
                entry.TimeOfDay != currentTimeOfDay)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(entry.CityID) &&
                !string.IsNullOrEmpty(cityId) &&
                entry.CityID != cityId)
            {
                continue;
            }

            return entry;
        }

        return null;
    }

    public void TalkToNPC(NPCData npc)
    {
        if (npc == null)
        {
            Debug.LogWarning("NPC is null.");
            return;
        }

        if (!IsNPCAvailable(npc))
        {
            Debug.Log(
                $"{npc.DisplayName} is not available right now."
            );

            return;
        }

        if (npc.DefaultDialogue == null)
        {
            Debug.Log(
                $"{npc.DisplayName} has nothing to say."
            );

            return;
        }

        Debug.Log(
            $"Talking to {npc.DisplayName}"
        );

        DialogueManager
            .Instance
            .StartDialogue(
                npc.DefaultDialogue.ID,
                npc
            );
    }

    public void OpenNPCById(
        string npcId)
    {
        NPCData npc =
            GetNPCById(npcId);

        if (npc == null)
        {
            Debug.LogWarning(
                $"NPC not found: {npcId}"
            );

            return;
        }

        MobileHUDManager.OpenNPCInteraction(npc);
    }

    public void PrintCurrentCityNPCs()
    {
        List<NPCData> npcs =
            GetNPCsInCurrentCity();

        Debug.Log(
            $"NPCs in {CityManager.Instance.CurrentCity.DisplayName}:"
        );

        for (int i = 0; i < npcs.Count; i++)
        {
            Debug.Log(
                $"{i}: {npcs[i].DisplayName} ({npcs[i].Role})"
            );
        }
    }

    public void TalkToNPCByIndex(int index)
    {
        List<NPCData> npcs =
            GetNPCsInCurrentCity();

        if (index < 0 || index >= npcs.Count)
        {
            Debug.LogWarning(
                "Invalid NPC index."
            );

            return;
        }

        TalkToNPC(npcs[index]);
    }
}
