using UnityEngine;

public class CharacterManager
    : PersistentSingleton<
        CharacterManager>
{
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

    public int GetStat(
        StatType type)
    {
        return type switch
        {
            StatType.Strength =>
                Stats.Strength +
                PlayerRace
                .StrengthBonus,

            StatType.Dexterity =>
                Stats.Dexterity +
                PlayerRace
                .DexterityBonus,

            StatType.Intelligence =>
                Stats.Intelligence +
                PlayerRace
                .IntelligenceBonus,

            StatType.Faith =>
                Stats.Faith +
                PlayerRace
                .FaithBonus,

            StatType.Vitality =>
                Stats.Vitality +
                PlayerRace
                .VitalityBonus,

            StatType.Charisma =>
                Stats.Charisma +
                PlayerRace
                .CharismaBonus,

            _ => 0
        };
    }

    public int MaxHP =>
        GetStat(
            StatType.Vitality
        ) * 20;

    public int MaxStamina =>
        GetStat(
            StatType.Dexterity
        ) * 15;

    public void GainExperience(
        int amount)
    {
        Stats.Experience +=
            amount;

        Debug.Log(
            $"+{amount} XP"
        );

        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        int neededXP =
            Stats.Level * 100;

        if (Stats.Experience
            < neededXP)
        {
            return;
        }

        Stats.Experience -=
            neededXP;

        Stats.Level++;

        Debug.Log(
            $"LEVEL UP: " +
            $"{Stats.Level}"
        );
    }
}