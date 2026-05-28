using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Skill",
    menuName = "Fiore/Skill"
)]
public class SkillData
    : BaseData
{
    public SkillType SkillType =
        SkillType.Physical;

    public ElementType ElementType =
        ElementType.None;

    public int EnergyCost = 5;

    public int Power = 5;

    public float ChargeTime = 5f;

    public float CooldownTime;

    public bool AutoUseInCombat = true;

    public int Priority;

    public SkillTargetType TargetType =
        SkillTargetType.SingleEnemy;

    public List<RequirementData> Requirements =
        new();
}
