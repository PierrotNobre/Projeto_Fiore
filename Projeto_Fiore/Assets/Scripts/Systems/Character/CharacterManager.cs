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

        return type switch
        {
            StatType.Strength =>
                race.StrengthBonus,

            StatType.Dexterity =>
                race.DexterityBonus,

            StatType.Intelligence =>
                race.IntelligenceBonus,

            StatType.Faith =>
                race.FaithBonus,

            StatType.Vitality =>
                race.VitalityBonus,

            StatType.Charisma =>
                race.CharismaBonus,

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
