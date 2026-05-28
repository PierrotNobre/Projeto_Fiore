using System;
using System.Collections.Generic;

[Serializable]
public class PartyState
{
    public int MaxPartySize = 4;

    public List<string> ActivePartyMemberIDs = new();

    public void EnsureRuntimeDefaults()
    {
        if (MaxPartySize <= 0)
        {
            MaxPartySize = 4;
        }

        if (ActivePartyMemberIDs == null)
        {
            ActivePartyMemberIDs = new List<string>();
        }

        ActivePartyMemberIDs.RemoveAll(
            string.IsNullOrEmpty
        );

        for (int i = ActivePartyMemberIDs.Count - 1;
            i >= 0;
            i--)
        {
            if (ActivePartyMemberIDs.IndexOf(
                ActivePartyMemberIDs[i]) != i)
            {
                ActivePartyMemberIDs.RemoveAt(i);
            }
        }
    }

    public bool HasPartySpace()
    {
        EnsureRuntimeDefaults();

        return ActivePartyMemberIDs.Count <
            MaxPartySize;
    }

    public bool IsInParty(
        string companionID)
    {
        EnsureRuntimeDefaults();

        return !string.IsNullOrEmpty(companionID) &&
            ActivePartyMemberIDs.Contains(
                companionID
            );
    }

    public bool AddToParty(
        string companionID)
    {
        EnsureRuntimeDefaults();

        if (string.IsNullOrEmpty(companionID))
            return false;

        if (IsInParty(companionID))
            return true;

        if (!HasPartySpace())
            return false;

        ActivePartyMemberIDs.Add(
            companionID
        );

        return true;
    }

    public bool RemoveFromParty(
        string companionID)
    {
        EnsureRuntimeDefaults();

        if (string.IsNullOrEmpty(companionID))
            return false;

        return ActivePartyMemberIDs.Remove(
            companionID
        );
    }
}
