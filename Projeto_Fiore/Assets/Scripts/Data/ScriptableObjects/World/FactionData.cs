using UnityEngine;

[CreateAssetMenu(
    fileName = "Faction",
    menuName = "Fiore/Faction"
)]
public class FactionData : BaseData
{
    [Header("Faction")]
    public FactionType Type;

    [Header("Political Identity")]
    public RegionData MainRegion;

    public bool IsMajorFaction;

    [TextArea]
    public string Ideology;

    [Header("Relations")]
    public FactionData ParentFaction;
}