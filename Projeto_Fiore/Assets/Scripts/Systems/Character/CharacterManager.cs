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
