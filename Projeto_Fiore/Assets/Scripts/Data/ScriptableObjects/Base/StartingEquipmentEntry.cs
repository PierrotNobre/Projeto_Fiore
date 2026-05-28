using System;

[Serializable]
public class StartingEquipmentEntry
{
    public string ItemID;

    public EquipmentSlot TargetSlot =
        EquipmentSlot.MainHand;
}
