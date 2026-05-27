using System;

[Serializable]
public class NPCGiftPreference
{
    public string ItemID;

    public GiftReaction Reaction =
        GiftReaction.Neutral;

    public int FriendshipAmount = 1;

    public int RomanceAmount;
}
