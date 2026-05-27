using System;

[Serializable]
public class WalletState
{
    public int Coins = -1;

    public void EnsureRuntimeDefaults()
    {
        if (Coins < 0)
        {
            Coins = 0;
        }
    }
}
