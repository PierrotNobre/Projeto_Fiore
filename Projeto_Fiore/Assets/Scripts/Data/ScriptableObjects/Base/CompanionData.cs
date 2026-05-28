using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Companion",
    menuName = "Fiore/Companion"
)]
public class CompanionData
    : BaseData
{
    [Header("Identity")]
    public string NPCID;

    public Sprite PortraitSprite;

    public RaceData Race;

    public string RaceID =
        "race_human";

    public StartingArchetypeData Archetype;

    public string ArchetypeID =
        "archetype_adventurer";

    [Header("Stats")]
    public PlayerStatsData BaseStats =
        new();

    public PlayerVitals BaseVitals =
        new();

    public CharacterElementData ElementalData =
        new();

    [Header("Loadout")]
    public List<string> StartingSkillIDs =
        new();

    public List<string> StartingEquipmentItemIDs =
        new();

    [Header("Rules")]
    public bool CanJoinParty = true;

    public bool CanBeSentOnGuildTasks = true;

    public bool IsUnique = true;

    public List<RequirementData> RecruitmentRequirements =
        new();

    public List<StatType> LevelUpStatPriority =
        new();

    public string ResolvedRaceID =>
        Race != null
            ? Race.ID
            : RaceID;

    public string ResolvedArchetypeID =>
        Archetype != null
            ? Archetype.ID
            : ArchetypeID;

    public string ResolvedNPCID =>
        !string.IsNullOrEmpty(NPCID)
            ? NPCID
            : ID;

    public Sprite ResolvedPortrait =>
        PortraitSprite != null
            ? PortraitSprite
            : GetNPCPortrait();

    private Sprite GetNPCPortrait()
    {
        if (DatabaseManager.Instance == null ||
            string.IsNullOrEmpty(NPCID))
        {
            return null;
        }

        NPCData npc =
            DatabaseManager
                .Instance
                .GetNPCById(NPCID);

        return npc != null
            ? npc.Portrait
            : null;
    }
}
