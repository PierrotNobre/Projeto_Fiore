using System;

[Serializable]
public class GuildMemberState
{
    public string MemberID;

    public string NPCID;

    public bool IsRecruited = true;

    public GuildMemberAssignment Assignment =
        GuildMemberAssignment.Guild;

    public bool IsAvailableForGuildTasks = true;

    public bool IsAvailableForTasks = true;

    public string CurrentTaskID;

    public int RemainingTaskPeriods;

    public int TaskReturnYear;

    public int TaskReturnMonth;

    public int TaskReturnDay;

    public TimeOfDay TaskReturnTimeOfDay;

    public bool IsInFixedParty =>
        Assignment == GuildMemberAssignment.FixedParty;

    public void EnsureRuntimeDefaults()
    {
        if (string.IsNullOrEmpty(MemberID))
        {
            MemberID =
                !string.IsNullOrEmpty(NPCID)
                    ? NPCID
                    : "guild_member_placeholder";
        }

        if (string.IsNullOrEmpty(NPCID))
        {
            NPCID = MemberID;
        }

        if (IsInFixedParty)
        {
            IsAvailableForGuildTasks = false;
            IsAvailableForTasks = false;
        }

        if (!string.IsNullOrEmpty(CurrentTaskID) &&
            RemainingTaskPeriods > 0)
        {
            IsAvailableForGuildTasks = false;
            IsAvailableForTasks = false;
        }
        else if (!IsInFixedParty)
        {
            IsAvailableForTasks =
                IsAvailableForGuildTasks;
        }
    }
}
