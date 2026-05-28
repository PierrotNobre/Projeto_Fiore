using System;
using System.Collections.Generic;

[Serializable]
public class AutoCombatSettings
{
    public bool AllowAutoSkills = true;

    public bool AllowOffHandAttack = true;

    public List<string> EnabledSkillIDs = new();

    public void EnsureRuntimeDefaults(
        List<string> knownSkillIDs)
    {
        if (EnabledSkillIDs == null)
        {
            EnabledSkillIDs = new List<string>();
        }

        if (knownSkillIDs == null)
            return;

        foreach (string skillID in knownSkillIDs)
        {
            if (string.IsNullOrEmpty(skillID) ||
                EnabledSkillIDs.Contains(skillID))
            {
                continue;
            }

            EnabledSkillIDs.Add(skillID);
        }
    }

    public bool IsSkillEnabled(
        string skillID)
    {
        if (!AllowAutoSkills)
            return false;

        return !string.IsNullOrEmpty(skillID) &&
            EnabledSkillIDs != null &&
            EnabledSkillIDs.Contains(skillID);
    }

    public void SetSkillEnabled(
        string skillID,
        bool enabled)
    {
        if (string.IsNullOrEmpty(skillID))
            return;

        if (EnabledSkillIDs == null)
        {
            EnabledSkillIDs = new List<string>();
        }

        if (enabled)
        {
            if (!EnabledSkillIDs.Contains(skillID))
            {
                EnabledSkillIDs.Add(skillID);
            }

            return;
        }

        EnabledSkillIDs.Remove(skillID);
    }
}
