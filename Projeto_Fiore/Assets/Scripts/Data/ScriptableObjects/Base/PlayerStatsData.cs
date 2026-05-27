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

    [Header("Resources")]
    public int CurrentHP = 100;

    public int CurrentStamina = 100;

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
