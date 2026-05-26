using UnityEngine;

public abstract class BaseData : ScriptableObject
{
    [Header("Basic Info")]
    public string ID;

    public string DisplayName;

    [TextArea]
    public string Description;
}