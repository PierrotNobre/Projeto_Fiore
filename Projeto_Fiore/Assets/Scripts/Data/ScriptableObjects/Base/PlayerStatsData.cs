using System;
using UnityEngine;

[Serializable]
public class PlayerStatsData
{
    public int Strength = 5;

    public int Dexterity = 5;

    public int Intelligence = 5;

    public int Faith = 5;

    public int Vitality = 5;

    public int Charisma = 5;

    [Header("Progression")]
    public int Level = 1;

    public int Experience = 0;

    [Header("Resources")]
    public int CurrentHP = 100;

    public int CurrentStamina = 100;
}