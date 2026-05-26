using UnityEngine;

[CreateAssetMenu(
    fileName = "Item",
    menuName = "Fiore/Item"
)]
public class ItemData
    : BaseData
{
    [Header("Item")]
    public ItemType Type;

    public int MaxStack = 99;

    public int SellValue = 10;

    [TextArea]
    public string FlavorText;
}