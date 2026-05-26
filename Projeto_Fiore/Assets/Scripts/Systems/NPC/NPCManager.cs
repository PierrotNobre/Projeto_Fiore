using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NPCManager
    : PersistentSingleton<NPCManager>
{
    public List<NPCData> GetNPCsInCurrentCity()
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

        if (npc.AlwaysAvailable)
            return true;

        int currentHour =
            TimeManager
            .Instance
            .CurrentTime
            .Hour;

        if (npc.AvailableFromHour
            <= npc.AvailableUntilHour)
        {
            return currentHour >= npc.AvailableFromHour
                &&
                currentHour < npc.AvailableUntilHour;
        }

        // Caso atravesse meia-noite.
        // Exemplo: 18h até 2h.
        return currentHour >= npc.AvailableFromHour
            ||
            currentHour < npc.AvailableUntilHour;
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
                npc.DefaultDialogue.ID
            );
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