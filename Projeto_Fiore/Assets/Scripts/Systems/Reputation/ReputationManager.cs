using UnityEngine;
using System.Linq;

public class ReputationManager
    : PersistentSingleton<ReputationManager>
{
    private const int MIN_REPUTATION = -100;
    private const int MAX_REPUTATION = 100;

    public int GetReputation(
        string factionID)
    {
        var list =
            SaveManager
            .Instance
            .CurrentSave
            .Reputation;

        var reputation =
            list.FirstOrDefault(
                x => x.FactionID == factionID
            );

        return reputation?.Value ?? 0;
    }

    public void SetReputation(
        string factionID,
        int value)
    {
        var clampedValue =
            Mathf.Clamp(
                value,
                MIN_REPUTATION,
                MAX_REPUTATION
            );

        var list =
            SaveManager
            .Instance
            .CurrentSave
            .Reputation;

        var reputation =
            list.FirstOrDefault(
                x => x.FactionID == factionID
            );

        if (reputation == null)
        {
            list.Add(
                new ReputationData
                {
                    FactionID = factionID,
                    Value = clampedValue
                }
            );
        }
        else
        {
            reputation.Value =
                clampedValue;
        }

        SaveManager
            .Instance
            .SaveGame();

        Debug.Log(
            $"Reputation set: {factionID} = {clampedValue}"
        );
    }

    public void AddReputation(
        string factionID,
        int amount)
    {
        int current =
            GetReputation(factionID);

        SetReputation(
            factionID,
            current + amount
        );
    }

    public ReputationTier GetTier(
        string factionID)
    {
        int value =
            GetReputation(factionID);

        if (value <= -60)
            return ReputationTier.Hostile;

        if (value <= -20)
            return ReputationTier.Suspicious;

        if (value < 20)
            return ReputationTier.Neutral;

        if (value < 60)
            return ReputationTier.Friendly;

        return ReputationTier.Allied;
    }

    public bool HasMinimumReputation(
        string factionID,
        int minimumValue)
    {
        return GetReputation(factionID)
            >= minimumValue;
    }
}