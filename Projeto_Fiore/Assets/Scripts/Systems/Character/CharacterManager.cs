using System.Collections.Generic;
using UnityEngine;

public class CharacterManager
    : PersistentSingleton<
        CharacterManager>
{
    private const int AttributePointsPerLevel = 2;

    public static CharacterManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject characterObject =
            new GameObject(
                "CharacterManager"
            );

        return characterObject
            .AddComponent<CharacterManager>();
    }

    private PlayerStatsData Stats =>
        SaveManager
        .Instance
        .CurrentSave
        .Stats;

    private RaceData PlayerRace =>
        DatabaseManager
        .Instance
        .GetData<RaceData>(
            SaveManager
            .Instance
            .CurrentSave
            .Player
            .RaceID
        );

    private StartingArchetypeData PlayerArchetype =>
        DatabaseManager
        .Instance
        .GetData<StartingArchetypeData>(
            SaveManager
            .Instance
            .CurrentSave
            .Player
            .ArchetypeID
        );

    public int GetStat(
        StatType type)
    {
        return GetTotalStat(type);
    }

    public int GetBaseStat(
        StatType type)
    {
        return Stats.GetStat(type);
    }

    public int GetBonusStat(
        StatType type)
    {
        return GetRaceBonus(type) +
            GetArchetypeBonus(type) +
            EquipmentManager
                .GetOrCreate()
                .GetTotalStatBonus(type);
    }

    public int GetTotalStat(
        StatType type)
    {
        return GetBaseStat(type) +
            GetBonusStat(type);
    }

    public bool MeetsRequirement(
        StatType type,
        int requiredValue)
    {
        return GetTotalStat(type) >=
            requiredValue;
    }

    private int GetRaceBonus(
        StatType type)
    {
        RaceData race =
            PlayerRace;

        if (race == null)
            return 0;

        return race.GetStatBonus(type);
    }

    private int GetArchetypeBonus(
        StatType type)
    {
        StartingArchetypeData archetype =
            PlayerArchetype;

        if (archetype == null)
            return 0;

        return archetype.GetStatBonus(type);
    }

    public int MaxHP =>
        100 +
        GetStat(
            StatType.Vitality
        ) * 5;

    public int MaxStamina =>
        50 +
        GetStat(
            StatType.Intelligence
        ) * 3;

    public ElementType PrimaryElement =>
        SaveManager
            .Instance
            .CurrentSave
            .Player
            .Elements
            .PrimaryElement;

    public int GetElementPowerBonus(
        ElementType elementType)
    {
        CharacterElementData elements =
            SaveManager
                .Instance
                .CurrentSave
                .Player
                .Elements;

        elements.EnsureRuntimeDefaults();

        return elements.GetPowerBonus(elementType) +
            EquipmentManager
                .GetOrCreate()
                .GetTotalElementPowerBonus(elementType);
    }

    public int GetElementResistance(
        ElementType elementType)
    {
        CharacterElementData elements =
            SaveManager
                .Instance
                .CurrentSave
                .Player
                .Elements;

        elements.EnsureRuntimeDefaults();

        return elements.GetResistance(elementType) +
            EquipmentManager
                .GetOrCreate()
                .GetTotalElementResistanceBonus(elementType);
    }

    public void ClampVitalsToCurrentMaximum()
    {
        Stats.CurrentHP =
            Mathf.Clamp(
                Stats.CurrentHP,
                0,
                MaxHP
            );

        Stats.CurrentStamina =
            Mathf.Clamp(
                Stats.CurrentStamina,
                0,
                MaxStamina
            );
    }

    public void TakeDamage(
        int amount)
    {
        Stats.CurrentHP =
            Mathf.Max(
                0,
                Stats.CurrentHP -
                Mathf.Max(0, amount)
            );
    }

    public void RecoverHealth(
        int amount)
    {
        Stats.CurrentHP =
            Mathf.Clamp(
                Stats.CurrentHP +
                Mathf.Max(0, amount),
                0,
                MaxHP
            );
    }

    public void SpendEnergy(
        int amount)
    {
        Stats.CurrentStamina =
            Mathf.Max(
                0,
                Stats.CurrentStamina -
                Mathf.Max(0, amount)
            );
    }

    public void RecoverEnergy(
        int amount)
    {
        Stats.CurrentStamina =
            Mathf.Clamp(
                Stats.CurrentStamina +
                Mathf.Max(0, amount),
                0,
                MaxStamina
        );
    }

    public int GetExperienceToNextLevel()
    {
        Stats.EnsureRuntimeDefaults();
        return Stats.ExperienceToNextLevel;
    }

    public float GetExperienceProgressPercent()
    {
        Stats.EnsureRuntimeDefaults();

        if (Stats.ExperienceToNextLevel <= 0)
            return 0f;

        return Mathf.Clamp01(
            (float)Stats.Experience /
            Stats.ExperienceToNextLevel
        );
    }

    public bool SpendAttributePoint(
        StatType statType)
    {
        Stats.EnsureRuntimeDefaults();

        if (Stats.UnspentAttributePoints <= 0)
        {
            GameFeedbackUI.ShowNotification(
                "Nenhum ponto de atributo disponivel."
            );

            return false;
        }

        Stats.AddStat(
            statType,
            1
        );

        Stats.UnspentAttributePoints--;

        ClampVitalsToCurrentMaximum();

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"Atributo aumentado: {statType}"
        );

        Debug.Log(
            $"Stat changed: {statType} -> {Stats.GetStat(statType)}"
        );

        return true;
    }

    public bool HasSkill(
        string skillID)
    {
        List<string> knownSkills =
            SaveManager
                .Instance
                .CurrentSave
                .Player
                .KnownSkillIDs;

        return !string.IsNullOrEmpty(skillID) &&
            knownSkills != null &&
            knownSkills.Contains(skillID);
    }

    public bool LearnSkill(
        string skillID)
    {
        if (string.IsNullOrEmpty(skillID) ||
            HasSkill(skillID))
        {
            return false;
        }

        PlayerData player =
            SaveManager
                .Instance
                .CurrentSave
                .Player;

        player.KnownSkillIDs.Add(skillID);

        player.AutoCombat
            ?.SetSkillEnabled(
                skillID,
                true
            );

        SkillData skill =
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetData<SkillData>(skillID)
                : null;

        string skillName =
            skill != null &&
            !string.IsNullOrEmpty(skill.DisplayName)
                ? skill.DisplayName
                : skillID;

        GameFeedbackUI.ShowNotification(
            $"Nova habilidade aprendida: {skillName}."
        );

        Debug.Log(
            $"Skill learned: {skillID}"
        );

        return true;
    }

    public List<string> CheckSkillUnlocks()
    {
        List<string> learnedSkills =
            new();

        if (DatabaseManager.Instance == null)
            return learnedSkills;

        SaveData save =
            SaveManager.Instance.CurrentSave;

        List<SkillData> skills =
            DatabaseManager
                .Instance
                .GetAllData<SkillData>();

        foreach (SkillData skill in skills)
        {
            if (skill == null ||
                string.IsNullOrEmpty(skill.ID) ||
                HasSkill(skill.ID))
            {
                continue;
            }

            if (Stats.Level <
                Mathf.Max(1, skill.RequiredLevel))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(skill.ArchetypeID) &&
                skill.ArchetypeID != save.Player.ArchetypeID)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(skill.RaceID) &&
                skill.RaceID != save.Player.RaceID)
            {
                continue;
            }

            if (!RequirementChecker.AreRequirementsMet(
                skill.UnlockRequirements))
            {
                continue;
            }

            if (LearnSkill(skill.ID))
            {
                learnedSkills.Add(
                    !string.IsNullOrEmpty(skill.DisplayName)
                        ? skill.DisplayName
                        : skill.ID
                );
            }
        }

        return learnedSkills;
    }

    public void RestoreVitals()
    {
        Stats.CurrentHP =
            MaxHP;

        Stats.CurrentStamina =
            MaxStamina;

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            "Vida e energia recuperadas."
        );
    }

    public CharacterProgressionResult AddExperience(
        int amount)
    {
        CharacterProgressionResult result =
            new CharacterProgressionResult
            {
                ExperienceGained = Mathf.Max(0, amount),
                StartingLevel = Stats.Level,
                FinalLevel = Stats.Level
            };

        if (amount <= 0)
            return result;

        Stats.EnsureRuntimeDefaults();

        Stats.Experience +=
            Mathf.Max(0, amount);

        Debug.Log(
            $"+{amount} XP"
        );

        ApplyLevelUps(result);

        List<string> learnedSkills =
            CheckSkillUnlocks();

        if (learnedSkills.Count > 0)
        {
            result.LearnedSkillSummary =
                string.Join(
                    ", ",
                    learnedSkills
                );
        }

        SaveManager
            .Instance
            .CurrentSave
            .Player
            .EnsureRuntimeDefaults();

        return result;
    }

    public CharacterProgressionResult GainExperience(
        int amount)
    {
        return AddExperience(amount);
    }

    private void ApplyLevelUps(
        CharacterProgressionResult result)
    {
        Stats.EnsureRuntimeDefaults();

        while (Stats.Experience >=
            Stats.ExperienceToNextLevel)
        {
            Stats.Experience -=
                Stats.ExperienceToNextLevel;

            Stats.Level++;

            Stats.UnspentAttributePoints +=
                AttributePointsPerLevel;

            result.AttributePointsGained +=
                AttributePointsPerLevel;

            Stats.ExperienceToNextLevel =
                PlayerStatsData
                    .GetExperienceRequiredForLevel(
                        Stats.Level
                    );

            Stats.CurrentHP =
                MaxHP;

            Stats.CurrentStamina =
                MaxStamina;

            Debug.Log(
                $"LEVEL UP: {Stats.Level}"
            );
        }

        result.FinalLevel =
            Stats.Level;

        if (result.LeveledUp)
        {
            GameFeedbackUI.ShowNotification(
                $"Nivel aumentado! Voce chegou ao nivel {Stats.Level}."
            );

            GameFeedbackUI.ShowNotification(
                $"Voce recebeu {result.AttributePointsGained} pontos de atributo."
            );
        }
    }
}
