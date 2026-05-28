using System;
using System.Collections.Generic;

[Serializable]
public class CombatState
{
    public bool IsInCombat;

    public string CombatID;

    public CombatPhase Phase =
        CombatPhase.None;

    public List<CombatantRuntimeData> Combatants =
        new();

    public int CurrentTurnIndex;

    public string SourceEncounterID;

    public string SourceExplorationEventID;

    public string SourceAreaID;

    public bool CanFlee = true;

    public UIScreenType ReturnScreenAfterCombat =
        UIScreenType.Exploration;

    public string LastLog;

    public string VictorySummary;

    public string EncounterDisplayName;

    public float ElapsedCombatTime;

    public float CombatSpeedMultiplier = 1f;

    public bool IsCombatPaused;

    public bool AwaitingContinue;

    public List<string> Logs =
        new();

    public CombatantRuntimeData CurrentCombatant
    {
        get
        {
            if (Combatants == null ||
                Combatants.Count == 0 ||
                CurrentTurnIndex < 0 ||
                CurrentTurnIndex >= Combatants.Count)
            {
                return null;
            }

            return Combatants[CurrentTurnIndex];
        }
    }

    public void Reset()
    {
        IsInCombat = false;
        CombatID = string.Empty;
        Phase = CombatPhase.None;
        Combatants.Clear();
        CurrentTurnIndex = 0;
        SourceEncounterID = string.Empty;
        SourceExplorationEventID = string.Empty;
        SourceAreaID = string.Empty;
        CanFlee = true;
        ReturnScreenAfterCombat = UIScreenType.Exploration;
        LastLog = string.Empty;
        VictorySummary = string.Empty;
        EncounterDisplayName = string.Empty;
        ElapsedCombatTime = 0f;
        CombatSpeedMultiplier = 1f;
        IsCombatPaused = false;
        AwaitingContinue = false;
        Logs.Clear();
    }

    public void AddLog(
        string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        LastLog = message;
        Logs.Add(message);

        while (Logs.Count > 6)
        {
            Logs.RemoveAt(0);
        }
    }
}
