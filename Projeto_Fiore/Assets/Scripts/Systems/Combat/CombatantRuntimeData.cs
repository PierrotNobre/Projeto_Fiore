using System;
using System.Collections.Generic;

[Serializable]
public class CombatantRuntimeData
{
    public string CombatantID;

    public string DisplayName;

    public CombatantType Type;

    public string SourceDataID;

    public CombatStats Stats = new();

    public bool IsDefeated;

    public float BasicAttackTimer;

    public float BasicAttackInterval = 4f;

    public float OffHandAttackTimer;

    public float OffHandAttackInterval = 5.25f;

    public bool CanUseOffHandAttack;

    public List<CombatSkillRuntimeData> SkillRuntimes =
        new();
}
