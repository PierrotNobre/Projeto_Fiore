using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Shop",
    menuName = "Fiore/Shop"
)]
public class ShopData
    : ScriptableObject
{
    public string ShopID;

    public string DisplayName;

    public List<ShopItemEntry> Items =
        new();
}
