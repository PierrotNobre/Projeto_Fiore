using UnityEngine;

public class WalletManager
    : PersistentSingleton<WalletManager>
{
    public static WalletManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject walletObject =
            new GameObject(
                "WalletManager"
            );

        return walletObject
            .AddComponent<WalletManager>();
    }

    public int GetCoins()
    {
        WalletState wallet =
            GetWallet();

        return wallet != null
            ? wallet.Coins
            : 0;
    }

    public void AddCoins(
        int amount)
    {
        if (amount <= 0)
            return;

        WalletState wallet =
            GetWallet();

        if (wallet == null)
            return;

        wallet.Coins += amount;

        SyncLegacyGold();

        SaveManager.Instance.SaveGame();

        Debug.Log(
            $"Coins changed: +{amount}"
        );

        GameFeedbackUI.ShowNotification(
            $"Recebeu {amount} moedas."
        );
    }

    public bool CanAfford(
        int amount)
    {
        return amount <= 0 ||
            GetCoins() >= amount;
    }

    public bool SpendCoins(
        int amount)
    {
        if (amount <= 0)
            return true;

        WalletState wallet =
            GetWallet();

        if (wallet == null ||
            wallet.Coins < amount)
        {
            GameFeedbackUI.ShowNotification(
                "Moedas insuficientes."
            );

            return false;
        }

        wallet.Coins -= amount;

        if (wallet.Coins < 0)
        {
            wallet.Coins = 0;
        }

        SyncLegacyGold();

        SaveManager.Instance.SaveGame();

        Debug.Log(
            $"Coins changed: -{amount}"
        );

        GameFeedbackUI.ShowNotification(
            $"Gastou {amount} moedas."
        );

        return true;
    }

    private WalletState GetWallet()
    {
        if (SaveManager.Instance == null ||
            SaveManager.Instance.CurrentSave == null)
        {
            return null;
        }

        SaveManager
            .Instance
            .CurrentSave
            .EnsureRuntimeDefaults();

        return SaveManager
            .Instance
            .CurrentSave
            .Wallet;
    }

    private void SyncLegacyGold()
    {
        SaveData save =
            SaveManager.Instance.CurrentSave;

        save.Player.Gold =
            save.Wallet.Coins;
    }
}
