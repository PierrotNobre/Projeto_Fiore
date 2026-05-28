using System;
using UnityEngine;

[Serializable]
public class PlayerVitals
{
    public int MaxHealth = 100;

    public int CurrentHealth = 100;

    public int MaxEnergy = 50;

    public int CurrentEnergy = 50;

    public void EnsureRuntimeDefaults()
    {
        MaxHealth =
            Mathf.Max(
                1,
                MaxHealth
            );

        MaxEnergy =
            Mathf.Max(
                0,
                MaxEnergy
            );

        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth <= 0 ? MaxHealth : CurrentHealth,
                0,
                MaxHealth
            );

        CurrentEnergy =
            Mathf.Clamp(
                CurrentEnergy < 0 ? MaxEnergy : CurrentEnergy,
                0,
                MaxEnergy
            );
    }
}
