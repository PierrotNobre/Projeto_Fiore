using System;
using UnityEngine;

[Serializable]
public class PlayerStatsData
{
    public int Strength = 5;

    public int Dexterity = 5;

    public int Intelligence = 5;

    public int Faith = 5;

    public int Vitality = 5;

    public int Charisma = 5;

    [Header("Progression")]
    public int Level = 1;

    public int Experience = 0;

    public int ExperienceToNextLevel = 100;

    public int UnspentAttributePoints;

    [Header("Resources")]
    public int CurrentHP = 100;

    public int CurrentStamina = 100;

    public void EnsureRuntimeDefaults()
    {
        if (Level <= 0)
        {
            Level = 1;
        }

        if (Experience < 0)
        {
            Experience = 0;
        }

        ExperienceToNextLevel =
            GetExperienceRequiredForLevel(Level);

        if (UnspentAttributePoints < 0)
        {
            UnspentAttributePoints = 0;
        }
    }

    public static int GetExperienceRequiredForLevel(
        int level)
    {
        return 100 +
            (Math.Max(1, level) - 1) * 50;
    }

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

    public void SetStat(
        StatType type,
        int value)
    {
        switch (type)
        {
            case StatType.Strength:
                Strength = value;
                break;

            case StatType.Dexterity:
                Dexterity = value;
                break;

            case StatType.Intelligence:
                Intelligence = value;
                break;

            case StatType.Faith:
                Faith = value;
                break;

            case StatType.Vitality:
                Vitality = value;
                break;

            case StatType.Charisma:
                Charisma = value;
                break;
        }
    }

    public void AddStat(
        StatType type,
        int amount)
    {
        SetStat(
            type,
            GetStat(type) + amount
        );
    }

    public bool MeetsRequirement(
        StatType type,
        int requiredValue)
    {
        return GetStat(type)
            >= requiredValue;
    }
}
