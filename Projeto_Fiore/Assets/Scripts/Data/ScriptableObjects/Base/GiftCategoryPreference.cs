using System;

[Serializable]
public class GiftCategoryPreference
{
    public GiftCategory Category =
        GiftCategory.Misc;

    public GiftReaction Reaction =
        GiftReaction.Neutral;

    public int FriendshipAmount = 1;

    public int RomanceAmount;
}
