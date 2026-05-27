using System;
using System.Collections.Generic;

[Serializable]
public class GuildStateData
{
    public const int MaxFixedPartyMembers = 4;

    public string GuildID =
        "faction_blackcatguild";

    public string GuildName =
        "Gatos Negros";

    public GuildDevelopmentState DevelopmentState =
        GuildDevelopmentState.Decadent;

    public int Level = 1;

    public int Reputation;

    public int Resources;

    public int Funds;

    public int CompletedGuildMissions;

    public List<GuildMemberState> Members = new();

    public List<string> RecruitedMemberIDs = new();

    public List<string> FixedPartyMemberIDs = new();

    public List<string> UnlockedGuildUpgradeIDs = new();

    public void EnsureRuntimeDefaults()
    {
        if (string.IsNullOrEmpty(GuildID))
        {
            GuildID =
                "faction_blackcatguild";
        }

        if (string.IsNullOrEmpty(GuildName))
        {
            GuildName =
                "Gatos Negros";
        }

        if (Level < 1)
        {
            Level = 1;
        }

        if (Members == null)
        {
            Members =
                new List<GuildMemberState>();
        }

        if (RecruitedMemberIDs == null)
        {
            RecruitedMemberIDs =
                new List<string>();
        }

        foreach (var member
            in Members)
        {
            member?.EnsureRuntimeDefaults();

            if (member != null &&
                !string.IsNullOrEmpty(member.NPCID) &&
                !RecruitedMemberIDs.Contains(member.NPCID))
            {
                RecruitedMemberIDs.Add(member.NPCID);
            }
        }

        if (FixedPartyMemberIDs == null)
        {
            FixedPartyMemberIDs =
                new List<string>();
        }

        if (UnlockedGuildUpgradeIDs == null)
        {
            UnlockedGuildUpgradeIDs =
                new List<string>();
        }
    }
}
