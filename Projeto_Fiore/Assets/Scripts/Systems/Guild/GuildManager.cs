using UnityEngine;

public class GuildManager
    : PersistentSingleton<GuildManager>
{
    private const string PlaceholderMemberID =
        "guild_member_placeholder";

    private const string DefaultTaskID =
        "simple_guild_errand";

    public static GuildManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject guildObject =
            new GameObject(
                "GuildManager"
            );

        return guildObject
            .AddComponent<GuildManager>();
    }

    public GuildStateData Guild =>
        SaveManager
            .Instance
            .CurrentSave
            .Guild;

    public bool RecruitMember(
        NPCData npc)
    {
        if (npc == null)
            return false;

        EnsureGuild();

        if (FindMember(npc.ID) != null)
            return false;

        Guild.Members.Add(
            new GuildMemberState
            {
                MemberID = npc.ID,
                NPCID = npc.ID,
                Assignment =
                    GuildMemberAssignment.Guild,
                IsAvailableForGuildTasks = true,
                IsAvailableForTasks = true
            }
        );

        if (!Guild.RecruitedMemberIDs.Contains(npc.ID))
        {
            Guild.RecruitedMemberIDs.Add(npc.ID);
        }

        SaveManager.Instance.SaveGame();

        return true;
    }

    public bool AssignToFixedParty(
        NPCData npc)
    {
        if (npc == null)
            return false;

        return AssignToFixedParty(
            npc.ID
        );
    }

    public bool AssignToFixedParty(
        string npcID)
    {
        EnsureGuild();

        GuildMemberState member =
            FindMember(npcID);

        if (member == null)
            return false;

        if (member.IsInFixedParty)
            return true;

        if (Guild.FixedPartyMemberIDs.Count >=
            GuildStateData.MaxFixedPartyMembers)
        {
            Debug.LogWarning(
                "Fixed party is already full."
            );

            return false;
        }

        member.Assignment =
            GuildMemberAssignment.FixedParty;

        member.IsAvailableForGuildTasks =
            false;

        member.IsAvailableForTasks =
            false;

        if (!Guild.FixedPartyMemberIDs.Contains(npcID))
        {
            Guild.FixedPartyMemberIDs.Add(npcID);
        }

        SaveManager.Instance.SaveGame();

        return true;
    }

    public bool SendToGuild(
        string npcID)
    {
        EnsureGuild();

        GuildMemberState member =
            FindMember(npcID);

        if (member == null)
            return false;

        member.Assignment =
            GuildMemberAssignment.Guild;

        member.IsAvailableForGuildTasks =
            true;

        member.IsAvailableForTasks =
            true;

        Guild.FixedPartyMemberIDs.Remove(npcID);

        SaveManager.Instance.SaveGame();

        return true;
    }

    public void AddReputation(
        int amount)
    {
        EnsureGuild();

        Guild.Reputation =
            Mathf.Max(
                0,
                Guild.Reputation + amount
            );

        TryLevelUpGuild();

        SaveManager.Instance.SaveGame();

        Debug.Log(
            $"Guild reputation changed: {Guild.Reputation}"
        );

        GameFeedbackUI.ShowGuildUpdated(
            $"Reputacao {Guild.Reputation}"
        );
    }

    public void SetGuildLevel(
        int level)
    {
        EnsureGuild();

        Guild.Level =
            Mathf.Max(
                1,
                level
            );

        SaveManager.Instance.SaveGame();

        Debug.Log(
            $"Guild level changed: {Guild.Level}"
        );

        GameFeedbackUI.ShowGuildUpdated(
            $"Nivel {Guild.Level}"
        );
    }

    public void AddResources(
        int amount)
    {
        EnsureGuild();

        Guild.Resources =
            Mathf.Max(
                0,
                Guild.Resources + amount
            );

        SaveManager.Instance.SaveGame();
    }

    public void AddFunds(
        int amount)
    {
        EnsureGuild();

        Guild.Funds =
            Mathf.Max(
                0,
                Guild.Funds + amount
            );

        SaveManager.Instance.SaveGame();
    }

    public int GetReputationRequiredForNextLevel()
    {
        EnsureGuild();

        return Mathf.Max(
            50,
            Guild.Level * 50
        );
    }

    public void RegisterGuildMissionCompleted()
    {
        EnsureGuild();

        Guild.CompletedGuildMissions++;

        SaveManager.Instance.SaveGame();

        Debug.Log(
            $"Guild missions completed: {Guild.CompletedGuildMissions}"
        );
    }

    public GuildMemberState EnsurePlaceholderMember()
    {
        EnsureGuild();

        GuildMemberState member =
            FindMember(PlaceholderMemberID);

        if (member != null)
            return member;

        member =
            new GuildMemberState
            {
                MemberID = PlaceholderMemberID,
                NPCID = PlaceholderMemberID,
                IsRecruited = true,
                Assignment =
                    GuildMemberAssignment.Guild,
                IsAvailableForGuildTasks = true,
                IsAvailableForTasks = true
            };

        Guild.Members.Add(member);

        if (!Guild.RecruitedMemberIDs
            .Contains(PlaceholderMemberID))
        {
            Guild.RecruitedMemberIDs
                .Add(PlaceholderMemberID);
        }

        SaveManager.Instance.SaveGame();

        return member;
    }

    public bool SendPlaceholderMemberToDefaultTask()
    {
        return SendMemberToTask(
            PlaceholderMemberID,
            DefaultTaskID
        );
    }

    public bool SendMemberToTask(
        string memberID,
        string taskID)
    {
        EnsureGuild();

        GuildMemberState member =
            FindMember(memberID);

        if (member == null &&
            memberID == PlaceholderMemberID)
        {
            member =
                EnsurePlaceholderMember();
        }

        GuildTaskData task =
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetData<GuildTaskData>(
                        taskID)
                : null;

        if (member == null ||
            task == null)
        {
            GameFeedbackUI.ShowNotification(
                "Tarefa da guilda indisponivel."
            );

            return false;
        }

        member.EnsureRuntimeDefaults();

        if (!member.IsAvailableForGuildTasks ||
            !member.IsAvailableForTasks ||
            !string.IsNullOrEmpty(member.CurrentTaskID))
        {
            GameFeedbackUI.ShowNotification(
                "Membro indisponivel para tarefas."
            );

            return false;
        }

        if (Guild.Level < task.RequiredGuildLevel ||
            !RequirementChecker.AreRequirementsMet(
                task.Requirements))
        {
            GameFeedbackUI.ShowNotification(
                "Requisitos da tarefa nao cumpridos."
            );

            return false;
        }

        member.CurrentTaskID =
            task.ID;

        member.RemainingTaskPeriods =
            Mathf.Max(
                1,
                task.DurationInPeriods
            );

        member.IsAvailableForGuildTasks =
            false;

        member.IsAvailableForTasks =
            false;

        TimeData time =
            TimeManager.Instance != null
                ? TimeManager.Instance.CurrentTime
                : null;

        if (time != null)
        {
            member.TaskReturnYear =
                time.Year;

            member.TaskReturnMonth =
                time.Month;

            member.TaskReturnDay =
                time.Day;

            member.TaskReturnTimeOfDay =
                time.TimeOfDay;
        }

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"Membro enviado: {task.DisplayName}"
        );

        Debug.Log(
            $"Guild task started: {memberID} -> {taskID}"
        );

        return true;
    }

    public void AdvanceGuildTasks(
        int periods)
    {
        if (periods <= 0 ||
            SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null)
        {
            return;
        }

        EnsureGuild();

        foreach (GuildMemberState member
            in Guild.Members)
        {
            if (member == null ||
                string.IsNullOrEmpty(
                    member.CurrentTaskID))
            {
                continue;
            }

            member.RemainingTaskPeriods -=
                periods;

            if (member.RemainingTaskPeriods <= 0)
            {
                CompleteGuildTask(member);
            }
        }

        SaveManager.Instance.SaveGame();
    }

    private GuildMemberState FindMember(
        string npcID)
    {
        if (string.IsNullOrEmpty(npcID))
            return null;

        foreach (var member
            in Guild.Members)
        {
            if (member != null &&
                (member.NPCID == npcID ||
                    member.MemberID == npcID))
            {
                return member;
            }
        }

        return null;
    }

    private void CompleteGuildTask(
        GuildMemberState member)
    {
        GuildTaskData task =
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetData<GuildTaskData>(
                        member.CurrentTaskID)
                : null;

        string taskName =
            task != null
                ? task.DisplayName
                : member.CurrentTaskID;

        if (task != null)
        {
            RewardManager.ApplyReward(
                task.Reward,
                task.ID
            );
        }

        member.CurrentTaskID =
            string.Empty;

        member.RemainingTaskPeriods =
            0;

        member.IsAvailableForGuildTasks =
            true;

        member.IsAvailableForTasks =
            true;

        GameFeedbackUI.ShowGuildUpdated(
            $"Tarefa concluida: {taskName}"
        );

        Debug.Log(
            $"Guild task completed: {taskName}"
        );
    }

    private void TryLevelUpGuild()
    {
        int required =
            GetReputationRequiredForNextLevel();

        while (Guild.Reputation >= required)
        {
            Guild.Level++;

            GameFeedbackUI.ShowGuildUpdated(
                $"Guilda nivel {Guild.Level}"
            );

            Debug.Log(
                $"Guild level changed: {Guild.Level}"
            );

            required =
                GetReputationRequiredForNextLevel();
        }
    }

    private void EnsureGuild()
    {
        if (SaveManager
            .Instance
            .CurrentSave
            .Guild == null)
        {
            SaveManager
                .Instance
                .CurrentSave
                .Guild =
                new GuildStateData();
        }

        SaveManager
            .Instance
            .CurrentSave
            .Guild
            .EnsureRuntimeDefaults();
    }

    [ContextMenu("Debug/Add Guild Reputation")]
    public void DebugAddReputation()
    {
        AddReputation(10);
    }

    [ContextMenu("Debug/List Guild State")]
    public void LogGuildState()
    {
        EnsureGuild();

        Debug.Log(
            $"Guild State | Name: {Guild.GuildName} | " +
            $"Level: {Guild.Level} | Reputation: {Guild.Reputation} | " +
            $"Members: {Guild.RecruitedMemberIDs.Count} | " +
            $"Completed Missions: {Guild.CompletedGuildMissions}"
        );
    }
}
