using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameDatabase",
    menuName = "Fiore/Game Database"
)]
public class GameDatabase
    : ScriptableObject
{
    [Header("World")]
    public List<RaceData> Races;

    public List<RegionData> Regions;

    public List<CityData> Cities;

    [Header("Events")]
    public List<TravelEventData> TravelEvents;

    [Header("Quests")]
    public List<QuestData> Quests;

    [Header("Items")]
    public List<ItemData> Items;

    [Header("Factions")]
    public List<FactionData> Factions;

    [Header("Dialogues")]
    public List<DialogueData> Dialogues;

    [Header("NPCs")]
    public List<NPCData> NPCs;
}