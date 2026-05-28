using System;
using System.Collections.Generic;

[Serializable]
public class CompanionState
{
    public string CompanionID;

    public bool IsRecruited;

    public bool IsInActiveParty;

    public bool IsAvailableForGuildTasks = true;

    public int Level = 1;

    public int CurrentExperience;

    public int ExperienceToNextLevel = 100;

    public int UnspentAttributePoints;

    public CompanionStatAllocation InvestedStats =
        new();

    public PlayerVitals CurrentVitals =
        new();

    public List<string> LearnedSkillIDs =
        new();

    public EquipmentState EquipmentState =
        new();

    public AutoCombatSettings AutoCombatSettings =
        new();

    public string CurrentGuildTaskID;

    public bool IsUnavailable;

    public void EnsureRuntimeDefaults()
    {
        if (Level <= 0)
        {
            Level = 1;
        }

        if (CurrentExperience < 0)
        {
            CurrentExperience = 0;
        }

        ExperienceToNextLevel =
            PlayerStatsData
                .GetExperienceRequiredForLevel(
                    Level
                );

        if (UnspentAttributePoints < 0)
        {
            UnspentAttributePoints = 0;
        }

        if (InvestedStats == null)
        {
            InvestedStats =
                new CompanionStatAllocation();
        }

        if (CurrentVitals == null)
        {
            CurrentVitals =
                new PlayerVitals();
        }

        CurrentVitals.EnsureRuntimeDefaults();

        if (LearnedSkillIDs == null)
        {
            LearnedSkillIDs =
                new List<string>();
        }

        if (EquipmentState == null)
        {
            EquipmentState =
                new EquipmentState();
        }

        EquipmentState.EnsureRuntimeDefaults();

        if (AutoCombatSettings == null)
        {
            AutoCombatSettings =
                new AutoCombatSettings();
        }

        AutoCombatSettings.EnsureRuntimeDefaults(
            LearnedSkillIDs
        );

        if (!string.IsNullOrEmpty(CurrentGuildTaskID) ||
            IsInActiveParty ||
            IsUnavailable)
        {
            IsAvailableForGuildTasks = false;
        }
    }
}

[Serializable]
public class CompanionStatAllocation
{
    public int Strength;

    public int Dexterity;

    public int Intelligence;

    public int Faith;

    public int Vitality;

    public int Charisma;

    public int GetStat(
        StatType type)
    {
        return type switch
        {
            StatType.Strength => Strength,

            StatType.Dexterity => Dexterity,

            StatType.Intelligence => Intelligence,

            StatType.Faith => Faith,

            StatType.Vitality => Vitality,

            StatType.Charisma => Charisma,

            _ => 0
        };
    }

    public void AddStat(
        StatType type,
        int amount)
    {
        switch (type)
        {
            case StatType.Strength:
                Strength += amount;
                break;

            case StatType.Dexterity:
                Dexterity += amount;
                break;

            case StatType.Intelligence:
                Intelligence += amount;
                break;

            case StatType.Faith:
                Faith += amount;
                break;

            case StatType.Vitality:
                Vitality += amount;
                break;

            case StatType.Charisma:
                Charisma += amount;
                break;
        }
    }
}
