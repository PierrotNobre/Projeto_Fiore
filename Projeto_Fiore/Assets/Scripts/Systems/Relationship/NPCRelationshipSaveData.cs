using System;
using System.Collections.Generic;

[Serializable]
public class NPCRelationshipSaveData
{
    public string NPCID;

    public int FriendshipPoints;

    public int RomancePoints;

    public NPCRelationshipLevel FriendshipLevel =
        NPCRelationshipLevel.Stranger;

    public NPCRomanceLevel RomanceLevel =
        NPCRomanceLevel.None;

    public bool RomanceAvailable;

    public bool IsDating;

    public bool IsMarried;

    public int TimesTalked;

    public int GiftsGivenToday;

    public int LastInteractionDay;

    public int LastGiftYear;

    public int LastGiftMonth;

    public int LastGiftDay;

    public List<string> UnlockedRelationshipEventIDs =
        new();

    public void EnsureRuntimeDefaults()
    {
        if (UnlockedRelationshipEventIDs == null)
        {
            UnlockedRelationshipEventIDs =
                new List<string>();
        }
    }
}
