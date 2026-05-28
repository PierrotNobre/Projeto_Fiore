using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatManager
    : PersistentSingleton<CombatManager>
{
    [SerializeField]
    private float combatTickRate = 0.1f;

    [SerializeField]
    private float baseAttackInterval = 4f;

    [SerializeField]
    private float minimumAttackInterval = 1.1f;

    [SerializeField]
    private float maximumAttackInterval = 6f;

    [SerializeField]
    private float offHandIntervalMultiplier = 1.35f;

    [SerializeField]
    private float actionVisualDelay = 0.45f;

    private Coroutine combatRoutine;

    private bool resolvingCombatEnd;

    private float actionDelayRemaining;

    public CombatState State { get; private set; } =
        new();

    public static CombatManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject combatObject =
            new GameObject(
                "CombatManager"
            );

        return combatObject
            .AddComponent<CombatManager>();
    }

    public bool IsInCombat =>
        State != null &&
        State.IsInCombat;

    public void ClearCombat()
    {
        StopCombatLoop();
        actionDelayRemaining = 0f;
        resolvingCombatEnd = false;
        State.Reset();
        MobileHUDManager.HideCombatPopup();
    }

    public bool StartCombat(
        CombatEncounterData encounter,
        string sourceAreaID = null,
        string sourceExplorationEventID = null)
    {
        if (encounter == null)
            return false;

        if (State.IsInCombat)
        {
            GameFeedbackUI.ShowNotification(
                "Ja existe um combate ativo."
            );

            return false;
        }

        if (!RequirementChecker
            .AreRequirementsMet(
                encounter.Requirements))
        {
            GameFeedbackUI.ShowNotification(
                "Requisitos do encontro nao cumpridos."
            );

            return false;
        }

        StopCombatLoop();
        actionDelayRemaining = 0f;
        resolvingCombatEnd = false;

        State.Reset();
        State.IsInCombat = true;
        State.CombatID = encounter.ID;
        State.SourceEncounterID = encounter.ID;
        State.SourceAreaID = sourceAreaID;
        State.SourceExplorationEventID =
            sourceExplorationEventID;
        State.CanFlee = encounter.CanFlee;
        State.ReturnScreenAfterCombat =
            UIScreenType.Exploration;
        State.EncounterDisplayName =
            !string.IsNullOrEmpty(encounter.DisplayName)
                ? encounter.DisplayName
                : encounter.ID;
        State.Phase = CombatPhase.Starting;

        State.Combatants.Add(
            BuildPlayerCombatant()
        );

        int index = 1;

        foreach (EnemyEncounterEntry entry
            in encounter.Enemies)
        {
            if (entry == null ||
                string.IsNullOrEmpty(entry.EnemyID))
            {
                continue;
            }

            EnemyData enemy =
                DatabaseManager
                    .Instance
                    .GetData<EnemyData>(
                        entry.EnemyID
                    );

            if (enemy == null)
                continue;

            int quantity =
                Mathf.Max(1, entry.Quantity);

            for (int i = 0; i < quantity; i++)
            {
                State.Combatants.Add(
                    BuildEnemyCombatant(
                        enemy,
                        index
                    )
                );

                index++;
            }
        }

        State.Combatants =
            State.Combatants
                .OrderByDescending(
                    combatant => combatant
                        .Stats
                        .Speed
                )
                .ToList();

        State.CurrentTurnIndex = 0;
        State.Phase = CombatPhase.Running;
        AddLog(
            $"Combate iniciado: {State.EncounterDisplayName}"
        );

        combatRoutine =
            StartCoroutine(
                RunCombatLoop()
            );

        MobileHUDManager.TryShowCombatPopup();

        Debug.Log(
            $"Combat started: {encounter.ID}"
        );

        return true;
    }

    public CombatantRuntimeData GetCurrentCombatant()
    {
        return State.CurrentCombatant;
    }

    public List<CombatantRuntimeData> GetLivingEnemies()
    {
        return State.Combatants
            .Where(combatant =>
                combatant != null &&
                combatant.Type == CombatantType.Enemy &&
                !combatant.IsDefeated)
            .ToList();
    }

    public CombatantRuntimeData GetFirstAliveEnemy()
    {
        return State.Combatants
            .FirstOrDefault(
                combatant =>
                    combatant != null &&
                    combatant.Type == CombatantType.Enemy &&
                    !combatant.IsDefeated
            );
    }

    public CombatantRuntimeData GetPlayerCombatant()
    {
        return State.Combatants
            .FirstOrDefault(
                combatant =>
                    combatant != null &&
                    combatant.Type == CombatantType.Player
            );
    }

    public IReadOnlyList<string> GetCombatLogs()
    {
        return State.Logs;
    }

    public bool ExecuteBasicAttack(
        string targetID,
        bool offHand = false)
    {
        if (!CanRunCombatAction())
            return false;

        CombatantRuntimeData player =
            GetPlayerCombatant();

        CombatantRuntimeData target =
            FindCombatant(targetID);

        return ExecutePlayerBasicAttack(
            player,
            target,
            offHand
        );
    }

    public bool ExecuteSkill(
        string skillID,
        string targetID)
    {
        if (!CanRunCombatAction())
            return false;

        SkillData skill =
            DatabaseManager
                .Instance
                .GetData<SkillData>(skillID);

        CombatantRuntimeData player =
            GetPlayerCombatant();

        CombatantRuntimeData target =
            skill != null &&
            skill.TargetType == SkillTargetType.Self
                ? player
                : FindCombatant(targetID);

        return ExecutePlayerSkill(
            player,
            skill,
            target
        );
    }

    public bool UseConsumable(
        string itemID)
    {
        if (!CanRunCombatAction())
            return false;

        ItemData item =
            DatabaseManager
                .Instance
                .GetItemById(itemID);

        if (item == null ||
            !item.IsConsumable)
        {
            GameFeedbackUI.ShowNotification(
                "Item indisponivel em combate."
            );

            return false;
        }

        if (!InventoryManager
            .Instance
            .RemoveItem(
                itemID,
                1,
                syncEquipment: true,
                saveAfterChange: false))
        {
            GameFeedbackUI.ShowNotification(
                "Item indisponivel."
            );

            return false;
        }

        int healAmount =
            Mathf.Max(1, item.EffectValue);

        CombatantRuntimeData player =
            GetPlayerCombatant();

        player.Stats.CurrentHealth =
            Mathf.Min(
                player.Stats.MaxHealth,
                player.Stats.CurrentHealth +
                healAmount
            );

        SaveManager
            .Instance
            .CurrentSave
            .Stats
            .CurrentHP =
            player.Stats.CurrentHealth;

        AddLog(
            $"{item.DisplayName} recuperou {healAmount} de vida."
        );

        MobileHUDManager.RefreshCombatPopup();

        return true;
    }

    public bool FleeCombat()
    {
        if (!State.IsInCombat ||
            !State.CanFlee)
        {
            GameFeedbackUI.ShowNotification(
                "Nao e possivel fugir."
            );

            return false;
        }

        StopCombatLoop();
        State.Phase = CombatPhase.Flee;
        State.IsInCombat = false;
        State.AwaitingContinue = true;
        AddLog(
            "Voce fugiu do combate."
        );

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.FleeCombat,
                    State.SourceEncounterID,
                    1,
                    State.SourceAreaID
                )
            );

        SaveManager.Instance.SaveGame();
        MobileHUDManager.RefreshCombatPopup();

        return true;
    }

    public void ContinueAfterCombat()
    {
        UIScreenType returnScreen =
            State.Phase == CombatPhase.Defeat
                ? UIScreenType.City
                : State.ReturnScreenAfterCombat;

        State.Phase =
            CombatPhase.Ended;

        MobileHUDManager.HideCombatPopup();
        MobileHUDManager.TryShowScreen(returnScreen);
        State.Reset();
    }

    public void EndCombatVictory()
    {
        if (resolvingCombatEnd)
            return;

        resolvingCombatEnd = true;
        StopCombatLoop();

        State.Phase =
            CombatPhase.Victory;

        State.IsInCombat =
            false;

        CombatEncounterData encounter =
            DatabaseManager
                .Instance
                .GetData<CombatEncounterData>(
                    State.SourceEncounterID
                );

        if (encounter != null)
        {
            RewardManager.ApplyReward(
                encounter.VictoryReward,
                encounter.ID
            );

            if (!string.IsNullOrEmpty(
                encounter.VictoryEventID))
            {
                WorldStateManager
                    .Instance
                    .MarkEventOccurred(
                        encounter.VictoryEventID
                    );
            }
        }

        foreach (CombatantRuntimeData enemy
            in State.Combatants)
        {
            if (enemy == null ||
                enemy.Type != CombatantType.Enemy)
            {
                continue;
            }

            QuestManager
                .Instance
                ?.ReportObjectiveProgress(
                    new QuestObjectiveContext(
                        QuestStepObjectiveType.DefeatEnemy,
                        enemy.SourceDataID,
                        1,
                        State.SourceEncounterID
                    )
                );
        }

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.CompleteCombatEncounter,
                    State.SourceEncounterID,
                    1,
                    State.SourceAreaID
                )
            );

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.WinCombat,
                    State.SourceEncounterID,
                    1,
                    State.SourceAreaID
                )
            );

        if (!string.IsNullOrEmpty(
            State.SourceExplorationEventID))
        {
            QuestManager
                .Instance
                ?.ReportObjectiveProgress(
                    new QuestObjectiveContext(
                        QuestStepObjectiveType.CompleteExplorationEvent,
                        State.SourceExplorationEventID,
                        1,
                        State.SourceAreaID
                    )
                );
        }

        AddLog(
            "Vitoria em combate."
        );

        State.AwaitingContinue = true;
        SaveManager.Instance.SaveGame();
        GameFeedbackUI.ShowNotification(
            State.LastLog
        );
        MobileHUDManager.RefreshCombatPopup();
    }

    public void EndCombatDefeat()
    {
        if (resolvingCombatEnd)
            return;

        resolvingCombatEnd = true;
        StopCombatLoop();

        State.Phase =
            CombatPhase.Defeat;

        State.IsInCombat =
            false;

        CombatEncounterData encounter =
            DatabaseManager
                .Instance
                .GetData<CombatEncounterData>(
                    State.SourceEncounterID
                );

        if (encounter != null &&
            !string.IsNullOrEmpty(
                encounter.DefeatEventID))
        {
            WorldStateManager
                .Instance
                .MarkEventOccurred(
                    encounter.DefeatEventID
                );
        }

        CharacterManager
            .Instance
            ?.RecoverHealth(1);

        if (ExplorationManager.Instance != null &&
            ExplorationManager.Instance.State.IsExploring)
        {
            ExplorationManager
                .Instance
                .ReturnToOriginCity();
        }

        AddLog(
            "Derrota em combate. Voce retornou a um estado seguro."
        );

        State.AwaitingContinue = true;
        SaveManager.Instance.SaveGame();
        GameFeedbackUI.ShowNotification(
            State.LastLog
        );
        MobileHUDManager.RefreshCombatPopup();
    }

    public Sprite GetCombatantSprite(
        CombatantRuntimeData combatant)
    {
        if (combatant == null ||
            combatant.Type != CombatantType.Enemy)
        {
            return null;
        }

        EnemyData enemy =
            DatabaseManager
                .Instance
                .GetData<EnemyData>(
                    combatant.SourceDataID
                );

        return enemy != null
            ? enemy.Sprite
            : null;
    }

    private IEnumerator RunCombatLoop()
    {
        WaitForSeconds wait =
            new WaitForSeconds(combatTickRate);

        while (State.IsInCombat)
        {
            if (State.Phase == CombatPhase.Running &&
                !State.IsCombatPaused)
            {
                ProcessCombatTick(
                    combatTickRate *
                    Mathf.Max(
                        0.1f,
                        State.CombatSpeedMultiplier
                    )
                );
            }

            yield return wait;
        }

        combatRoutine = null;
    }

    private void ProcessCombatTick(
        float deltaTime)
    {
        if (!CanRunCombatAction())
            return;

        State.ElapsedCombatTime +=
            Mathf.Max(0f, deltaTime);

        if (actionDelayRemaining > 0f)
        {
            actionDelayRemaining =
                Mathf.Max(
                    0f,
                    actionDelayRemaining - deltaTime
                );

            MobileHUDManager.RefreshCombatPopup();
            return;
        }

        UpdateCombatantTimers(deltaTime);

        foreach (CombatantRuntimeData combatant
            in State.Combatants)
        {
            if (combatant == null ||
                combatant.IsDefeated)
            {
                continue;
            }

            if (combatant.Type == CombatantType.Player)
            {
                if (TryExecutePlayerAutoAction(combatant))
                    return;
            }
            else if (TryExecuteEnemyAutoAction(combatant))
            {
                return;
            }
        }

        MobileHUDManager.RefreshCombatPopup();
    }

    private void UpdateCombatantTimers(
        float deltaTime)
    {
        foreach (CombatantRuntimeData combatant
            in State.Combatants)
        {
            if (combatant == null ||
                combatant.IsDefeated)
            {
                continue;
            }

            combatant.BasicAttackTimer +=
                deltaTime;

            if (combatant.CanUseOffHandAttack)
            {
                combatant.OffHandAttackTimer +=
                    deltaTime;
            }

            if (combatant.SkillRuntimes == null)
                continue;

            foreach (CombatSkillRuntimeData skillRuntime
                in combatant.SkillRuntimes)
            {
                if (skillRuntime == null)
                    continue;

                if (skillRuntime.CooldownRemaining > 0f)
                {
                    skillRuntime.CooldownRemaining =
                        Mathf.Max(
                            0f,
                            skillRuntime.CooldownRemaining -
                            deltaTime
                        );

                    continue;
                }

                SkillData skill =
                    DatabaseManager
                        .Instance
                        .GetData<SkillData>(
                            skillRuntime.SkillID
                        );

                if (skill == null ||
                    !skill.AutoUseInCombat)
                {
                    continue;
                }

                skillRuntime.CurrentCharge =
                    Mathf.Min(
                        Mathf.Max(0.1f, skill.ChargeTime),
                        skillRuntime.CurrentCharge +
                        deltaTime
                    );
            }
        }
    }

    private bool TryExecutePlayerAutoAction(
        CombatantRuntimeData player)
    {
        if (player == null ||
            player.IsDefeated)
        {
            return false;
        }

        if (TryExecuteReadyPlayerSkill(player))
            return true;

        CombatantRuntimeData target =
            GetFirstAliveEnemy();

        if (target == null)
            return false;

        if (player.BasicAttackTimer >=
            player.BasicAttackInterval)
        {
            player.BasicAttackTimer =
                Mathf.Max(
                    0f,
                    player.BasicAttackTimer -
                    player.BasicAttackInterval
                );

            ExecutePlayerBasicAttack(
                player,
                target,
                offHand: false
            );

            return true;
        }

        if (player.CanUseOffHandAttack &&
            player.OffHandAttackTimer >=
            player.OffHandAttackInterval)
        {
            player.OffHandAttackTimer =
                Mathf.Max(
                    0f,
                    player.OffHandAttackTimer -
                    player.OffHandAttackInterval
                );

            ExecutePlayerBasicAttack(
                player,
                target,
                offHand: true
            );

            return true;
        }

        return false;
    }

    private bool TryExecuteReadyPlayerSkill(
        CombatantRuntimeData player)
    {
        if (player.SkillRuntimes == null ||
            player.SkillRuntimes.Count == 0)
        {
            return false;
        }

        List<CombatSkillRuntimeData> readySkills =
            player
                .SkillRuntimes
                .Where(runtime =>
                {
                    SkillData skill =
                        DatabaseManager
                            .Instance
                            .GetData<SkillData>(
                                runtime.SkillID
                            );

                    return skill != null &&
                        skill.AutoUseInCombat &&
                        runtime.CooldownRemaining <= 0f &&
                        runtime.CurrentCharge >=
                        Mathf.Max(0.1f, skill.ChargeTime);
                })
                .OrderByDescending(runtime =>
                {
                    SkillData skill =
                        DatabaseManager
                            .Instance
                            .GetData<SkillData>(
                                runtime.SkillID
                            );

                    return skill != null
                        ? skill.Priority
                        : 0;
                })
                .ToList();

        foreach (CombatSkillRuntimeData runtime
            in readySkills)
        {
            SkillData skill =
                DatabaseManager
                    .Instance
                    .GetData<SkillData>(
                        runtime.SkillID
                    );

            if (skill == null)
                continue;

            if (player.Stats.CurrentEnergy <
                skill.EnergyCost)
            {
                runtime.CooldownRemaining = 1f;
                continue;
            }

            CombatantRuntimeData target =
                skill.TargetType == SkillTargetType.Self
                    ? player
                    : GetFirstAliveEnemy();

            if (target == null)
                continue;

            if (!ExecutePlayerSkill(
                player,
                skill,
                target))
            {
                continue;
            }

            runtime.CurrentCharge = 0f;
            runtime.CooldownRemaining =
                Mathf.Max(0f, skill.CooldownTime);

            return true;
        }

        return false;
    }

    private bool TryExecuteEnemyAutoAction(
        CombatantRuntimeData enemy)
    {
        if (enemy.BasicAttackTimer <
            enemy.BasicAttackInterval)
        {
            return false;
        }

        CombatantRuntimeData player =
            GetPlayerCombatant();

        if (player == null ||
            player.IsDefeated)
        {
            return false;
        }

        enemy.BasicAttackTimer =
            Mathf.Max(
                0f,
                enemy.BasicAttackTimer -
                enemy.BasicAttackInterval
            );

        int damage =
            CalculateDamage(
                enemy.Stats.PhysicalAttack,
                player,
                enemy.Stats.PrimaryElement
            );

        ApplyDamage(
            player,
            damage
        );

        AddLog(
            $"{enemy.DisplayName} atacou causando {damage} de dano."
        );

        MarkActionDelay();

        CheckCombatEnd();
        MobileHUDManager.RefreshCombatPopup();

        return true;
    }

    private bool ExecutePlayerBasicAttack(
        CombatantRuntimeData player,
        CombatantRuntimeData target,
        bool offHand)
    {
        if (player == null ||
            target == null ||
            target.IsDefeated)
        {
            return false;
        }

        EquipmentManager equipment =
            EquipmentManager.GetOrCreate();

        if (offHand &&
            !equipment.CanUseOffHandAttack())
        {
            return false;
        }

        ItemData weapon =
            equipment.GetEquippedItemData(
                offHand
                    ? EquipmentSlot.OffHand
                    : EquipmentSlot.MainHand
            );

        int attack =
            player.Stats.PhysicalAttack +
            GetWeaponAttackBonus(weapon);

        if (offHand)
        {
            attack =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        attack * 0.55f
                    )
                );
        }

        ElementType element =
            weapon != null
                ? weapon.AttackElement
                : ElementType.None;

        int damage =
            CalculateDamage(
                attack,
                target,
                element
            );

        ApplyDamage(
            target,
            damage
        );

        AddLog(
            offHand
                ? $"Ataque secundario causou {damage} de dano em {target.DisplayName}."
                : $"Ataque principal causou {damage} de dano em {target.DisplayName}."
        );

        MarkActionDelay();

        CheckCombatEnd();
        MobileHUDManager.RefreshCombatPopup();

        return true;
    }

    private bool ExecutePlayerSkill(
        CombatantRuntimeData player,
        SkillData skill,
        CombatantRuntimeData target)
    {
        if (player == null ||
            skill == null ||
            target == null ||
            target.IsDefeated)
        {
            return false;
        }

        List<string> knownSkillIDs =
            SaveManager
                .Instance
                .CurrentSave
                .Player
                .KnownSkillIDs;

        if (knownSkillIDs == null ||
            !knownSkillIDs.Contains(skill.ID))
        {
            return false;
        }

        if (!RequirementChecker
            .AreRequirementsMet(
                skill.Requirements))
        {
            return false;
        }

        if (player.Stats.CurrentEnergy <
            skill.EnergyCost)
        {
            return false;
        }

        player.Stats.CurrentEnergy -=
            Mathf.Max(0, skill.EnergyCost);

        int attack =
            skill.SkillType == SkillType.Magical
                ? player.Stats.MagicalAttack
                : player.Stats.PhysicalAttack;

        attack +=
            Mathf.Max(0, skill.Power);

        attack +=
            player
                .Stats
                .GetPowerBonus(
                    skill.ElementType
                );

        int damage =
            CalculateDamage(
                attack,
                target,
                skill.ElementType
            );

        ApplyDamage(
            target,
            damage
        );

        SaveManager
            .Instance
            .CurrentSave
            .Stats
            .CurrentStamina =
            player.Stats.CurrentEnergy;

        QuestManager
            .Instance
            ?.ReportObjectiveProgress(
                new QuestObjectiveContext(
                    QuestStepObjectiveType.UseSkill,
                    skill.ID,
                    1,
                    State.SourceEncounterID
                )
            );

        string elementText =
            skill.ElementType == ElementType.None
                ? string.Empty
                : $" {GetElementLabel(skill.ElementType)}";

        AddLog(
            $"{skill.DisplayName} causou {damage} de dano{elementText} em {target.DisplayName}."
        );

        MarkActionDelay();

        CheckCombatEnd();
        MobileHUDManager.RefreshCombatPopup();

        return true;
    }

    private CombatantRuntimeData BuildPlayerCombatant()
    {
        SaveData save =
            SaveManager.Instance.CurrentSave;

        CharacterManager character =
            CharacterManager.Instance;

        CombatStats stats =
            new CombatStats
            {
                MaxHealth = character.MaxHP,
                CurrentHealth = Mathf.Clamp(
                    save.Stats.CurrentHP,
                    1,
                    character.MaxHP
                ),
                MaxEnergy = character.MaxStamina,
                CurrentEnergy = Mathf.Clamp(
                    save.Stats.CurrentStamina,
                    0,
                    character.MaxStamina
                ),
                PhysicalAttack =
                    character.GetTotalStat(
                        StatType.Strength
                    ),
                MagicalAttack =
                    character.GetTotalStat(
                        StatType.Intelligence
                    ),
                Defense =
                    character.GetTotalStat(
                        StatType.Vitality
                    ),
                Speed =
                    character.GetTotalStat(
                        StatType.Dexterity
                    ),
                PrimaryElement =
                    character.PrimaryElement
            };

        foreach (ElementType elementType
            in System.Enum.GetValues(
                typeof(ElementType)))
        {
            int resistance =
                character.GetElementResistance(
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
                character.GetElementPowerBonus(
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

        EquipmentManager equipment =
            EquipmentManager.GetOrCreate();

        CombatantRuntimeData combatant =
            new CombatantRuntimeData
            {
                CombatantID = "player",
                DisplayName = save.Player.PlayerName,
                Type = CombatantType.Player,
                SourceDataID = "player",
                Stats = stats,
                BasicAttackInterval =
                    CalculateBasicAttackInterval(stats.Speed),
                OffHandAttackInterval =
                    CalculateBasicAttackInterval(stats.Speed) *
                    offHandIntervalMultiplier,
                CanUseOffHandAttack =
                    equipment.CanUseOffHandAttack()
            };

        combatant.BasicAttackTimer =
            combatant.BasicAttackInterval * 0.25f;

        combatant.OffHandAttackTimer =
            combatant.OffHandAttackInterval * 0.15f;

        AddPlayerSkillRuntimes(combatant);

        return combatant;
    }

    private CombatantRuntimeData BuildEnemyCombatant(
        EnemyData enemy,
        int index)
    {
        CombatStats stats =
            new CombatStats
            {
                MaxHealth = Mathf.Max(1, enemy.MaxHealth),
                CurrentHealth = Mathf.Max(1, enemy.MaxHealth),
                MaxEnergy = 0,
                CurrentEnergy = 0,
                PhysicalAttack = Mathf.Max(1, enemy.Attack),
                MagicalAttack = Mathf.Max(1, enemy.Attack),
                Defense = Mathf.Max(0, enemy.Defense),
                Speed = Mathf.Max(1, enemy.Speed),
                PrimaryElement = enemy.PrimaryElement,
                Resistances = enemy.Resistances != null
                    ? new List<ElementModifier>(enemy.Resistances)
                    : new List<ElementModifier>(),
                Weaknesses = enemy.Weaknesses != null
                    ? new List<ElementModifier>(enemy.Weaknesses)
                    : new List<ElementModifier>()
            };

        CombatantRuntimeData combatant =
            new CombatantRuntimeData
            {
                CombatantID =
                    $"{enemy.ID}_{index}",
                DisplayName =
                    string.IsNullOrEmpty(enemy.DisplayName)
                        ? enemy.ID
                        : enemy.DisplayName,
                Type = CombatantType.Enemy,
                SourceDataID = enemy.ID,
                Stats = stats,
                BasicAttackInterval =
                    CalculateBasicAttackInterval(stats.Speed)
            };

        combatant.BasicAttackTimer =
            combatant.BasicAttackInterval * 0.35f;

        return combatant;
    }

    private void AddPlayerSkillRuntimes(
        CombatantRuntimeData combatant)
    {
        List<string> skillIDs =
            SaveManager
                .Instance
                .CurrentSave
                .Player
                .KnownSkillIDs;

        if (skillIDs == null)
            return;

        foreach (string skillID in skillIDs)
        {
            SkillData skill =
                DatabaseManager
                    .Instance
                    .GetData<SkillData>(
                        skillID
                    );

            if (skill == null ||
                !skill.AutoUseInCombat)
            {
                continue;
            }

            combatant.SkillRuntimes.Add(
                new CombatSkillRuntimeData
                {
                    SkillID = skill.ID,
                    CurrentCharge = 0f,
                    CooldownRemaining = 0f
                }
            );
        }
    }

    private bool CanRunCombatAction()
    {
        return State.IsInCombat &&
            State.Phase == CombatPhase.Running &&
            !resolvingCombatEnd;
    }

    private CombatantRuntimeData FindCombatant(
        string combatantID)
    {
        return State.Combatants
            .FirstOrDefault(
                combatant =>
                    combatant != null &&
                    combatant.CombatantID == combatantID
            );
    }

    private int GetWeaponAttackBonus(
        ItemData weapon)
    {
        if (weapon == null)
            return 0;

        return Mathf.Max(
            0,
            weapon.GetStatBonus(
                StatType.Strength
            ) +
            weapon.GetStatBonus(
                StatType.Intelligence
            )
        );
    }

    private float CalculateBasicAttackInterval(
        int speed)
    {
        return Mathf.Clamp(
            baseAttackInterval -
            Mathf.Max(0, speed) * 0.18f,
            minimumAttackInterval,
            maximumAttackInterval
        );
    }

    private int CalculateDamage(
        int attack,
        CombatantRuntimeData target,
        ElementType element)
    {
        int finalDamage =
            attack -
            target.Stats.Defense;

        if (element != ElementType.None)
        {
            finalDamage -=
                target.Stats.GetResistance(element);

            finalDamage +=
                target.Stats.GetWeakness(element);
        }

        return Mathf.Max(
            1,
            finalDamage
        );
    }

    private void ApplyDamage(
        CombatantRuntimeData target,
        int damage)
    {
        target.Stats.CurrentHealth =
            Mathf.Max(
                0,
                target.Stats.CurrentHealth -
                Mathf.Max(0, damage)
            );

        if (target.Type == CombatantType.Player)
        {
            SaveManager
                .Instance
                .CurrentSave
                .Stats
                .CurrentHP =
                target.Stats.CurrentHealth;
        }

        if (target.Stats.CurrentHealth <= 0)
        {
            target.IsDefeated = true;
            AddLog(
                $"{target.DisplayName} foi derrotado."
            );
        }
    }

    private bool CheckCombatEnd()
    {
        if (resolvingCombatEnd)
            return true;

        CombatantRuntimeData player =
            GetPlayerCombatant();

        if (player == null ||
            player.IsDefeated)
        {
            EndCombatDefeat();
            return true;
        }

        if (GetLivingEnemies().Count == 0)
        {
            EndCombatVictory();
            return true;
        }

        return false;
    }

    private void AddLog(
        string message)
    {
        State.AddLog(message);
        Debug.Log(message);
    }

    private void StopCombatLoop()
    {
        if (combatRoutine == null)
            return;

        StopCoroutine(combatRoutine);
        combatRoutine = null;
    }

    private void MarkActionDelay()
    {
        actionDelayRemaining =
            Mathf.Max(0f, actionVisualDelay);
    }

    private static string GetElementLabel(
        ElementType elementType)
    {
        return elementType switch
        {
            ElementType.Water => "Agua",

            ElementType.Fire => "Fogo",

            ElementType.Electric => "Eletrico",

            ElementType.Earth => "Terra",

            ElementType.Air => "Ar",

            ElementType.Light => "Luz",

            ElementType.Darkness => "Escuridao",

            _ => "neutro"
        };
    }
}
