using System;
using UnityEngine;

// Ví Fruit Coin: lưu bền vững bằng PlayerPrefs. Chuyển đổi: 100 điểm gameplay = 1 Fruit Coin (xem ScoreManager.FruitCoins)
public class CurrencyManager : MonoBehaviour
{
    private const string BalanceKey = "FruitCoins";

    public static CurrencyManager Instance { get; private set; }

    public int Balance { get; private set; }

    public event Action<int> OnBalanceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Balance = PlayerPrefs.GetInt(BalanceKey, 0);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Balance += amount;
        Save();
    }

    // Trả về false nếu không đủ tiền (dùng cho Gacha sau này)
    public bool TrySpend(int amount)
    {
        if (amount < 0 || Balance < amount) return false;

        Balance -= amount;
        Save();
        return true;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(BalanceKey, Balance);
        PlayerPrefs.Save();
        OnBalanceChanged?.Invoke(Balance);
    }

    [ContextMenu("Reset Wallet")]
    private void ResetWallet()
    {
        Balance = 0;
        Save();
    }
}
