using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public string CharacterID =
        "player";

    public string PlayerName =
        "Hero";

    public string RaceID =
        "race_human";

    public string ArchetypeID =
        "archetype_adventurer";

    public string BodyPresetID =
        "body_default";

    public string PortraitID =
        "portrait_default";

    public CharacterElementData Elements =
        new();

    public List<string> KnownSkillIDs =
        new();

    public AutoCombatSettings AutoCombat =
        new();

    public int Gold = 100;

    public void EnsureRuntimeDefaults()
    {
        if (string.IsNullOrWhiteSpace(RaceID))
        {
            RaceID = "race_human";
        }

        if (string.IsNullOrWhiteSpace(ArchetypeID))
        {
            ArchetypeID = "archetype_adventurer";
        }

        if (Elements == null)
        {
            Elements = new CharacterElementData();
        }

        Elements.EnsureRuntimeDefaults();

        if (KnownSkillIDs == null)
        {
            KnownSkillIDs = new List<string>();
        }

        if (AutoCombat == null)
        {
            AutoCombat = new AutoCombatSettings();
        }

        AutoCombat.EnsureRuntimeDefaults(
            KnownSkillIDs
        );
    }
}
