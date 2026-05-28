using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CompanionManager
    : PersistentSingleton<CompanionManager>
{
    private const int AttributePointsPerLevel = 2;

    public static CompanionManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject companionObject =
            new GameObject(
                "CompanionManager"
            );

        return companionObject
            .AddComponent<CompanionManager>();
    }

    private SaveData Save =>
        SaveManager
            .Instance
            .CurrentSave;

    public CompanionData GetCompanionById(
        string companionID)
    {
        if (string.IsNullOrEmpty(companionID) ||
            DatabaseManager.Instance == null)
        {
            return null;
        }

        return DatabaseManager
            .Instance
            .GetData<CompanionData>(
                companionID
            );
    }

    public CompanionData GetCompanionByNPCId(
        string npcID)
    {
        if (string.IsNullOrEmpty(npcID) ||
            DatabaseManager.Instance == null)
        {
            return null;
        }

        return DatabaseManager
            .Instance
            .GetAllData<CompanionData>()
            .FirstOrDefault(companion =>
                companion != null &&
                companion.ResolvedNPCID == npcID);
    }

    public bool HasCompanion(
        string companionID)
    {
        return GetCompanionById(companionID) != null;
    }

    public List<CompanionData> GetRecruitableCompanions()
    {
        if (DatabaseManager.Instance == null)
            return new List<CompanionData>();

        return DatabaseManager
            .Instance
            .GetAllData<CompanionData>()
            .Where(companion =>
                companion != null &&
                !IsCompanionRecruited(companion.ID) &&
                RequirementChecker.AreRequirementsMet(
                    companion.RecruitmentRequirements,
                    companion.ResolvedNPCID
                ))
            .ToList();
    }

    public List<CompanionData> GetCompanionsForGuild()
    {
        if (DatabaseManager.Instance == null)
            return new List<CompanionData>();

        return DatabaseManager
            .Instance
            .GetAllData<CompanionData>()
            .Where(companion =>
                companion != null &&
                companion.CanBeSentOnGuildTasks)
            .ToList();
    }

    public List<CompanionState> GetRecruitedCompanions()
    {
        EnsureCompanionList();

        return Save
            .CompanionStates
            .Where(state =>
                state != null &&
                state.IsRecruited)
            .ToList();
    }

    public List<CompanionState> GetActivePartyCompanions()
    {
        EnsureCompanionList();
        Save.Party.EnsureRuntimeDefaults();

        List<CompanionState> result =
            new();

        foreach (string companionID
            in Save.Party.ActivePartyMemberIDs)
        {
            CompanionState state =
                GetCompanionState(
                    companionID,
                    false
                );

            if (state != null &&
                state.IsRecruited &&
                state.IsInActiveParty)
            {
                result.Add(state);
            }
        }

        return result;
    }

    public CompanionState GetCompanionState(
        string companionID,
        bool createIfMissing)
    {
        if (string.IsNullOrEmpty(companionID) ||
            SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null)
        {
            return null;
        }

        EnsureCompanionList();

        CompanionState state =
            Save.CompanionStates.Find(
                item => item != null &&
                    item.CompanionID == companionID
            );

        if (state == null &&
            createIfMissing)
        {
            state =
                CreateStateFromData(
                    companionID
                );

            Save.CompanionStates.Add(
                state
            );
        }

        state?.EnsureRuntimeDefaults();

        return state;
    }

    public bool RecruitCompanion(
        string companionID,
        bool addToActiveParty = false)
    {
        CompanionData companion =
            GetCompanionById(companionID);

        if (companion == null)
        {
            GameFeedbackUI.ShowNotification(
                "Companheiro nao encontrado."
            );

            return false;
        }

        if (!RequirementChecker.AreRequirementsMet(
            companion.RecruitmentRequirements,
            companion.ResolvedNPCID))
        {
            GameFeedbackUI.ShowNotification(
                "Requisitos de recrutamento nao cumpridos."
            );

            return false;
        }

        CompanionState state =
            GetCompanionState(
                companion.ID,
                true
            );

        if (state.IsRecruited)
        {
            return false;
        }

        state.IsRecruited = true;
        state.IsUnavailable = false;
        state.CurrentGuildTaskID = string.Empty;
        state.IsAvailableForGuildTasks =
            companion.CanBeSentOnGuildTasks;

        EnsureGuildMember(
            companion,
            state
        );

        bool addedToParty =
            addToActiveParty &&
            AddToParty(
                companion.ID,
                saveAfterChange: false
            );

        SaveManager.Instance.SaveGame();

        string companionName =
            GetCompanionDisplayName(
                companion.ID
            );

        GameFeedbackUI.ShowNotification(
            addedToParty
                ? $"{companionName} entrou para o grupo."
                : $"{companionName} entrou para a Guilda dos Gatos Negros."
        );

        Debug.Log(
            $"Companion recruited: {companion.ID}"
        );

        return true;
    }

    public bool RecruitCompanionFromNPC(
        string npcID,
        bool addToActiveParty = false)
    {
        CompanionData companion =
            GetCompanionByNPCId(npcID);

        return companion != null &&
            RecruitCompanion(
                companion.ID,
                addToActiveParty
            );
    }

    public bool AddToParty(
        string companionID)
    {
        return AddToParty(
            companionID,
            true
        );
    }

    public bool AddToParty(
        string companionID,
        bool saveAfterChange)
    {
        CompanionData companion =
            GetCompanionById(companionID);

        CompanionState state =
            GetCompanionState(
                companionID,
                false
            );

        if (companion == null ||
            state == null ||
            !state.IsRecruited ||
            !companion.CanJoinParty)
        {
            GameFeedbackUI.ShowNotification(
                "Companheiro indisponivel para o grupo."
            );

            return false;
        }

        if (!string.IsNullOrEmpty(state.CurrentGuildTaskID) ||
            state.IsUnavailable)
        {
            GameFeedbackUI.ShowNotification(
                "Companheiro indisponivel no momento."
            );

            return false;
        }

        Save.Party.EnsureRuntimeDefaults();

        if (Save.Party.IsInParty(companionID))
            return true;

        if (!Save.Party.HasPartySpace())
        {
            GameFeedbackUI.ShowNotification(
                "Grupo cheio."
            );

            return false;
        }

        Save.Party.AddToParty(
            companionID
        );

        state.IsInActiveParty = true;
        state.IsAvailableForGuildTasks = false;

        UpdateGuildMemberAssignment(
            companionID,
            GuildMemberAssignment.FixedParty
        );

        if (saveAfterChange)
        {
            SaveManager.Instance.SaveGame();
        }

        GameFeedbackUI.ShowNotification(
            $"{GetCompanionDisplayName(companionID)} entrou para o grupo."
        );

        return true;
    }

    public bool RemoveFromParty(
        string companionID)
    {
        CompanionState state =
            GetCompanionState(
                companionID,
                false
            );

        if (state == null)
            return false;

        Save.Party.RemoveFromParty(
            companionID
        );

        state.IsInActiveParty = false;
        state.IsAvailableForGuildTasks =
            CanCompanionDoGuildTasks(companionID);

        UpdateGuildMemberAssignment(
            companionID,
            GuildMemberAssignment.Guild
        );

        SaveManager.Instance.SaveGame();

        GameFeedbackUI.ShowNotification(
            $"{GetCompanionDisplayName(companionID)} voltou para a guilda."
        );

        return true;
    }

    public bool IsCompanionRecruited(
        string companionID)
    {
        CompanionState state =
            GetCompanionState(
                companionID,
                false
            );

        return state != null &&
            state.IsRecruited;
    }

    public bool IsCompanionInParty(
        string companionID)
    {
        if (string.IsNullOrEmpty(companionID) ||
            SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null ||
            SaveManager.Instance.CurrentSave.Party == null)
        {
            return false;
        }

        Save.Party.EnsureRuntimeDefaults();

        return Save.Party.IsInParty(
            companionID
        );
    }

    public bool CanCompanionDoGuildTasks(
        string companionID)
    {
        CompanionData companion =
            GetCompanionById(companionID);

        CompanionState state =
            GetCompanionState(
                companionID,
                false
            );

        return companion != null &&
            companion.CanBeSentOnGuildTasks &&
            state != null &&
            state.IsRecruited &&
            !state.IsInActiveParty &&
            !state.IsUnavailable &&
            string.IsNullOrEmpty(
                state.CurrentGuildTaskID
            );
    }

    public void MarkGuildTaskStarted(
        string companionID,
        string taskID)
    {
        CompanionState state =
            GetCompanionState(
                companionID,
                false
            );

        if (state == null)
            return;

        state.CurrentGuildTaskID = taskID;
        state.IsAvailableForGuildTasks = false;
        state.IsInActiveParty = false;

        Save.Party.RemoveFromParty(
            companionID
        );
    }

    public void MarkGuildTaskCompleted(
        string companionID)
    {
        CompanionState state =
            GetCompanionState(
                companionID,
                false
            );

        if (state == null)
            return;

        state.CurrentGuildTaskID = string.Empty;
        state.IsAvailableForGuildTasks =
            CanCompanionDoGuildTasks(companionID);
    }

    public string AddExperienceToActiveCompanions(
        int amount)
    {
        if (amount <= 0)
            return string.Empty;

        List<string> summaries =
            new();

        foreach (CompanionState state
            in GetActivePartyCompanions())
        {
            int levels =
                AddExperience(
                    state.CompanionID,
                    amount,
                    saveAfterChange: false
                );

            if (levels > 0)
            {
                summaries.Add(
                    $"{GetCompanionDisplayName(state.CompanionID)} nivel {state.Level}"
                );
            }
        }

        SaveManager.Instance.SaveGame();

        return string.Join(
            ", ",
            summaries
        );
    }

    public int AddExperience(
        string companionID,
        int amount,
        bool saveAfterChange = true)
    {
        CompanionState state =
            GetCompanionState(
                companionID,
                false
            );

        if (state == null ||
            amount <= 0)
        {
            return 0;
        }

        state.CurrentExperience +=
            Mathf.Max(0, amount);

        int levelsGained = 0;

        while (state.CurrentExperience >=
            state.ExperienceToNextLevel)
        {
            state.CurrentExperience -=
                state.ExperienceToNextLevel;

            state.Level++;
            levelsGained++;

            state.UnspentAttributePoints +=
                AttributePointsPerLevel;

            state.ExperienceToNextLevel =
                PlayerStatsData
                    .GetExperienceRequiredForLevel(
                        state.Level
                    );

            ApplyAutomaticAttributePoints(
                state
            );

            RecalculateVitals(
                state
            );

            CheckSkillUnlocks(
                state
            );
        }

        if (saveAfterChange)
        {
            SaveManager.Instance.SaveGame();
        }

        if (levelsGained > 0)
        {
            GameFeedbackUI.ShowNotification(
                $"{GetCompanionDisplayName(companionID)} subiu para o nivel {state.Level}."
            );
        }

        return levelsGained;
    }

    public CombatStats BuildCombatStats(
        CompanionState state)
    {
        CompanionData companion =
            GetCompanionById(
                state.CompanionID
            );

        RecalculateVitals(
            state
        );

        CombatStats stats =
            new CombatStats
            {
                MaxHealth =
                    state.CurrentVitals.MaxHealth,
                CurrentHealth =
                    Mathf.Clamp(
                        state.CurrentVitals.CurrentHealth,
                        1,
                        state.CurrentVitals.MaxHealth
                    ),
                MaxEnergy =
                    state.CurrentVitals.MaxEnergy,
                CurrentEnergy =
                    Mathf.Clamp(
                        state.CurrentVitals.CurrentEnergy,
                        0,
                        state.CurrentVitals.MaxEnergy
                    ),
                PhysicalAttack =
                    GetTotalStat(
                        state,
                        StatType.Strength
                    ),
                MagicalAttack =
                    GetTotalStat(
                        state,
                        StatType.Intelligence
                    ),
                Defense =
                    GetTotalStat(
                        state,
                        StatType.Vitality
                    ),
                Speed =
                    GetTotalStat(
                        state,
                        StatType.Dexterity
                    ),
                PrimaryElement =
                    companion != null &&
                    companion.ElementalData != null
                        ? companion.ElementalData.PrimaryElement
                        : ElementType.None
            };

        foreach (ElementType elementType
            in System.Enum.GetValues(
                typeof(ElementType)))
        {
            int resistance =
                GetElementResistance(
                    state,
                    elementType
                );

            if (resistance != 0)
            {
                stats.Resistances.Add(
                    new ElementModifier
                    {
                        ElementType = elementType,
                        Value = resistance
                    }
                );
            }

            int power =
                GetElementPowerBonus(
                    state,
                    elementType
                );

            if (power != 0)
            {
                stats.PowerBonuses.Add(
                    new ElementModifier
                    {
                        ElementType = elementType,
                        Value = power
                    }
                );
            }
        }

        return stats;
    }

    public int GetTotalStat(
        CompanionState state,
        StatType statType)
    {
        if (state == null)
            return 0;

        CompanionData companion =
            GetCompanionById(
                state.CompanionID
            );

        int total =
            companion != null &&
            companion.BaseStats != null
                ? companion.BaseStats.GetStat(statType)
                : 5;

        RaceData race =
            GetCompanionRace(companion);

        if (race != null)
        {
            total += race.GetStatBonus(statType);
        }

        StartingArchetypeData archetype =
            GetCompanionArchetype(companion);

        if (archetype != null)
        {
            total += archetype.GetStatBonus(statType);
        }

        total += state.InvestedStats != null
            ? state.InvestedStats.GetStat(statType)
            : 0;

        total += GetEquipmentStatBonus(
            state,
            statType
        );

        return total;
    }

    public ItemData GetCompanionEquippedItemData(
        CompanionState state,
        EquipmentSlot slot)
    {
        if (state == null ||
            state.EquipmentState == null)
        {
            return null;
        }

        string itemID =
            state
                .EquipmentState
                .GetItemID(slot);

        return !string.IsNullOrEmpty(itemID) &&
            DatabaseManager.Instance != null
                ? DatabaseManager
                    .Instance
                    .GetItemById(itemID)
                : null;
    }

    public bool CanUseOffHandAttack(
        CompanionState state)
    {
        if (state == null ||
            state.AutoCombatSettings == null ||
            !state.AutoCombatSettings.AllowOffHandAttack)
        {
            return false;
        }

        ItemData offHand =
            GetCompanionEquippedItemData(
                state,
                EquipmentSlot.OffHand
            );

        return offHand != null &&
            offHand.IsEquipment &&
            !offHand.IsTwoHanded &&
            offHand.CanEquipInMainHand &&
            offHand.CanEquipInOffHand;
    }

    public string GetCompanionDisplayName(
        string companionID)
    {
        CompanionData companion =
            GetCompanionById(companionID);

        if (companion != null &&
            !string.IsNullOrEmpty(companion.DisplayName))
        {
            return companion.DisplayName;
        }

        return string.IsNullOrEmpty(companionID)
            ? "Companheiro"
            : companionID;
    }

    public Sprite GetCompanionPortrait(
        string companionID)
    {
        CompanionData companion =
            GetCompanionById(companionID);

        return companion != null
            ? companion.ResolvedPortrait
            : null;
    }

    private CompanionState CreateStateFromData(
        string companionID)
    {
        CompanionData companion =
            GetCompanionById(
                companionID
            );

        CompanionState state =
            new CompanionState
            {
                CompanionID = companionID,
                Level = 1,
                ExperienceToNextLevel =
                    PlayerStatsData
                        .GetExperienceRequiredForLevel(1)
            };

        if (companion != null)
        {
            if (companion.BaseVitals != null)
            {
                companion.BaseVitals.EnsureRuntimeDefaults();

                state.CurrentVitals =
                    new PlayerVitals
                    {
                        MaxHealth =
                            companion.BaseVitals.MaxHealth,
                        CurrentHealth =
                            companion.BaseVitals.MaxHealth,
                        MaxEnergy =
                            companion.BaseVitals.MaxEnergy,
                        CurrentEnergy =
                            companion.BaseVitals.MaxEnergy
                    };
            }

            AddStartingSkills(
                state,
                companion
            );

            ApplyStartingEquipment(
                state,
                companion
            );
        }

        state.EnsureRuntimeDefaults();
        RecalculateVitals(state);

        return state;
    }

    private void AddStartingSkills(
        CompanionState state,
        CompanionData companion)
    {
        if (companion.StartingSkillIDs != null)
        {
            foreach (string skillID
                in companion.StartingSkillIDs)
            {
                AddSkillIfMissing(
                    state,
                    skillID
                );
            }
        }

        StartingArchetypeData archetype =
            GetCompanionArchetype(companion);

        if (archetype != null &&
            archetype.StartingSkillIDs != null)
        {
            foreach (string skillID
                in archetype.StartingSkillIDs)
            {
                AddSkillIfMissing(
                    state,
                    skillID
                );
            }
        }

        state.AutoCombatSettings
            .EnsureRuntimeDefaults(
                state.LearnedSkillIDs
            );
    }

    private void ApplyStartingEquipment(
        CompanionState state,
        CompanionData companion)
    {
        if (companion.StartingEquipmentItemIDs == null)
            return;

        foreach (string itemID
            in companion.StartingEquipmentItemIDs)
        {
            ItemData item =
                DatabaseManager.Instance != null
                    ? DatabaseManager
                        .Instance
                        .GetItemById(itemID)
                    : null;

            if (item == null ||
                !item.IsEquipment)
            {
                continue;
            }

            EquipmentSlot slot =
                item.EquipmentSlot;

            if (item.CanEquipInMainHand)
            {
                slot = EquipmentSlot.MainHand;
            }
            else if (item.CanEquipInOffHand)
            {
                slot = EquipmentSlot.OffHand;
            }

            state.EquipmentState.SetItemID(
                slot,
                item.ID
            );

            if (slot == EquipmentSlot.MainHand &&
                item.IsTwoHanded)
            {
                state.EquipmentState.SetItemID(
                    EquipmentSlot.OffHand,
                    null
                );
            }
        }
    }

    private void CheckSkillUnlocks(
        CompanionState state)
    {
        CompanionData companion =
            GetCompanionById(
                state.CompanionID
            );

        if (companion == null ||
            DatabaseManager.Instance == null)
        {
            return;
        }

        foreach (SkillData skill
            in DatabaseManager
                .Instance
                .GetAllData<SkillData>())
        {
            if (skill == null ||
                string.IsNullOrEmpty(skill.ID) ||
                state.LearnedSkillIDs.Contains(skill.ID) ||
                state.Level < Mathf.Max(1, skill.RequiredLevel))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(skill.ArchetypeID) &&
                skill.ArchetypeID != companion.ResolvedArchetypeID)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(skill.RaceID) &&
                skill.RaceID != companion.ResolvedRaceID)
            {
                continue;
            }

            AddSkillIfMissing(
                state,
                skill.ID
            );

            GameFeedbackUI.ShowNotification(
                $"{GetCompanionDisplayName(state.CompanionID)} aprendeu {skill.DisplayName}."
            );
        }

        state.AutoCombatSettings
            .EnsureRuntimeDefaults(
                state.LearnedSkillIDs
            );
    }

    private void AddSkillIfMissing(
        CompanionState state,
        string skillID)
    {
        if (string.IsNullOrEmpty(skillID) ||
            state.LearnedSkillIDs.Contains(skillID))
        {
            return;
        }

        state.LearnedSkillIDs.Add(
            skillID
        );
    }

    private void ApplyAutomaticAttributePoints(
        CompanionState state)
    {
        while (state.UnspentAttributePoints > 0)
        {
            StatType stat =
                ResolveNextLevelUpStat(
                    state
                );

            state.InvestedStats.AddStat(
                stat,
                1
            );

            state.UnspentAttributePoints--;
        }
    }

    private StatType ResolveNextLevelUpStat(
        CompanionState state)
    {
        CompanionData companion =
            GetCompanionById(
                state.CompanionID
            );

        if (companion != null &&
            companion.LevelUpStatPriority != null &&
            companion.LevelUpStatPriority.Count > 0)
        {
            int index =
                Mathf.Abs(state.Level +
                    state.UnspentAttributePoints) %
                companion.LevelUpStatPriority.Count;

            return companion
                .LevelUpStatPriority[index];
        }

        StartingArchetypeData archetype =
            GetCompanionArchetype(companion);

        if (archetype != null &&
            archetype.GetStatBonus(StatType.Intelligence) >
            archetype.GetStatBonus(StatType.Strength))
        {
            return state.UnspentAttributePoints % 2 == 0
                ? StatType.Intelligence
                : StatType.Faith;
        }

        return state.UnspentAttributePoints % 2 == 0
            ? StatType.Strength
            : StatType.Vitality;
    }

    private void RecalculateVitals(
        CompanionState state)
    {
        if (state == null)
            return;

        state.CurrentVitals.MaxHealth =
            100 +
            GetTotalStat(
                state,
                StatType.Vitality
            ) * 5;

        state.CurrentVitals.MaxEnergy =
            50 +
            GetTotalStat(
                state,
                StatType.Intelligence
            ) * 3;

        state.CurrentVitals.EnsureRuntimeDefaults();
    }

    private RaceData GetCompanionRace(
        CompanionData companion)
    {
        if (companion == null ||
            DatabaseManager.Instance == null)
        {
            return null;
        }

        if (companion.Race != null)
            return companion.Race;

        return DatabaseManager
            .Instance
            .GetData<RaceData>(
                companion.RaceID
            );
    }

    private StartingArchetypeData GetCompanionArchetype(
        CompanionData companion)
    {
        if (companion == null ||
            DatabaseManager.Instance == null)
        {
            return null;
        }

        if (companion.Archetype != null)
            return companion.Archetype;

        return DatabaseManager
            .Instance
            .GetData<StartingArchetypeData>(
                companion.ArchetypeID
            );
    }

    private int GetEquipmentStatBonus(
        CompanionState state,
        StatType statType)
    {
        int total = 0;

        foreach (EquipmentSlot slot
            in EquipmentManager.GetEquipmentSlots())
        {
            ItemData item =
                GetCompanionEquippedItemData(
                    state,
                    slot
                );

            if (item != null)
            {
                total += item.GetStatBonus(
                    statType
                );
            }
        }

        return total;
    }

    private int GetElementPowerBonus(
        CompanionState state,
        ElementType elementType)
    {
        CompanionData companion =
            GetCompanionById(
                state.CompanionID
            );

        int total =
            companion != null &&
            companion.ElementalData != null
                ? companion.ElementalData.GetPowerBonus(elementType)
                : 0;

        foreach (EquipmentSlot slot
            in EquipmentManager.GetEquipmentSlots())
        {
            ItemData item =
                GetCompanionEquippedItemData(
                    state,
                    slot
                );

            if (item != null)
            {
                total += item.GetElementPowerBonus(
                    elementType
                );
            }
        }

        return total;
    }

    private int GetElementResistance(
        CompanionState state,
        ElementType elementType)
    {
        CompanionData companion =
            GetCompanionById(
                state.CompanionID
            );

        int total =
            companion != null &&
            companion.ElementalData != null
                ? companion.ElementalData.GetResistance(elementType)
                : 0;

        foreach (EquipmentSlot slot
            in EquipmentManager.GetEquipmentSlots())
        {
            ItemData item =
                GetCompanionEquippedItemData(
                    state,
                    slot
                );

            if (item != null)
            {
                total += item.GetElementResistanceBonus(
                    elementType
                );
            }
        }

        return total;
    }

    private void EnsureGuildMember(
        CompanionData companion,
        CompanionState state)
    {
        GuildStateData guild =
            Save.Guild;

        guild.EnsureRuntimeDefaults();

        GuildMemberState member =
            guild.Members.Find(existing =>
                existing != null &&
                (existing.MemberID == companion.ID ||
                    existing.NPCID == companion.ResolvedNPCID));

        if (member == null)
        {
            member =
                new GuildMemberState();

            guild.Members.Add(member);
        }

        member.MemberID = companion.ID;
        member.NPCID = companion.ResolvedNPCID;
        member.IsRecruited = true;
        member.Assignment = state.IsInActiveParty
            ? GuildMemberAssignment.FixedParty
            : GuildMemberAssignment.Guild;
        member.IsAvailableForGuildTasks =
            CanCompanionDoGuildTasks(
                companion.ID
            );
        member.IsAvailableForTasks =
            member.IsAvailableForGuildTasks;
        member.CurrentTaskID =
            state.CurrentGuildTaskID;

        if (!guild.RecruitedMemberIDs.Contains(
            companion.ID))
        {
            guild.RecruitedMemberIDs.Add(
                companion.ID
            );
        }
    }

    private void UpdateGuildMemberAssignment(
        string companionID,
        GuildMemberAssignment assignment)
    {
        GuildStateData guild =
            Save.Guild;

        guild.EnsureRuntimeDefaults();

        GuildMemberState member =
            guild.Members.Find(existing =>
                existing != null &&
                existing.MemberID == companionID);

        if (member == null)
            return;

        member.Assignment = assignment;
        member.IsAvailableForGuildTasks =
            assignment == GuildMemberAssignment.Guild &&
            CanCompanionDoGuildTasks(companionID);
        member.IsAvailableForTasks =
            member.IsAvailableForGuildTasks;

        if (assignment == GuildMemberAssignment.FixedParty)
        {
            if (!guild.FixedPartyMemberIDs.Contains(
                companionID))
            {
                guild.FixedPartyMemberIDs.Add(
                    companionID
                );
            }
        }
        else
        {
            guild.FixedPartyMemberIDs.Remove(
                companionID
            );
        }
    }

    private void EnsureCompanionList()
    {
        if (Save.CompanionStates == null)
        {
            Save.CompanionStates =
                new List<CompanionState>();
        }

        Save.CompanionStates.RemoveAll(
            state => state == null ||
                string.IsNullOrEmpty(
                    state.CompanionID
                )
        );

        foreach (CompanionState state
            in Save.CompanionStates)
        {
            state.EnsureRuntimeDefaults();
        }
    }
}
