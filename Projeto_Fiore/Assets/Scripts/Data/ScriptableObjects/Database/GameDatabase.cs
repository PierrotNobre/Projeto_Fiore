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

    public List<StartingArchetypeData> StartingArchetypes;

    public List<RegionData> Regions;

    public List<CityData> Cities;

    public List<ExplorationAreaData> ExplorationAreas;

    [Header("Events")]
    public List<TravelEventData> TravelEvents;

    public List<WorldEventData> WorldEvents;

    public List<NPCRelationshipEventData> RelationshipEvents;

    public List<CalendarEventData> CalendarEvents;

    public List<ExplorationEventData> ExplorationEvents;

    [Header("Quests")]
    public List<QuestData> Quests;

    [Header("Guild")]
    public List<GuildTaskData> GuildTasks;

    public List<EnemyData> Enemies;

    public List<CombatEncounterData> CombatEncounters;

    public List<SkillData> Skills;

    [Header("Items")]
    public List<ItemData> Items;

    [Header("Factions")]
    public List<FactionData> Factions;

    [Header("Dialogues")]
    public List<DialogueData> Dialogues;

    [Header("NPCs")]
    public List<NPCData> NPCs;
}
