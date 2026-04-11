using System;
using UnityEngine;

// Manages total lightning ⚡ and diamonds 💎.
// Every 10 ⚡ auto-converts to 1 💎.
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private const int LightningPerDiamond = 10;

    public event Action<int, int> OnCurrencyChanged; // (lightning, diamonds)

    public int TotalLightning { get; private set; }
    public int TotalDiamonds  { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        TotalLightning = PlayerPrefs.GetInt("Lightning", 0);
        TotalDiamonds  = PlayerPrefs.GetInt("Diamonds",  0);
    }

    // Call on win — adds lightning and auto-converts every 10 ⚡ → 1 💎.
    public void AddLightning(int amount)
    {
        if (amount <= 0) return;
        TotalLightning += amount;

        int newDiamonds = TotalLightning / LightningPerDiamond;
        if (newDiamonds > 0)
        {
            TotalDiamonds  += newDiamonds;
            TotalLightning %= LightningPerDiamond;
        }

        Save();
        OnCurrencyChanged?.Invoke(TotalLightning, TotalDiamonds);
    }

    // Call on win — always grants 1 gem directly.
    public void AddDiamonds(int amount)
    {
        if (amount <= 0) return;
        TotalDiamonds += amount;
        Save();
        OnCurrencyChanged?.Invoke(TotalLightning, TotalDiamonds);
    }

    // Call on loss/skip — removes lightning if available, never goes below 0.
    public void SpendLightning(int amount)
    {
        if (amount <= 0) return;
        TotalLightning = Mathf.Max(0, TotalLightning - amount);
        Save();
        OnCurrencyChanged?.Invoke(TotalLightning, TotalDiamonds);
    }

    private void Save()
    {
        PlayerPrefs.SetInt("Lightning", TotalLightning);
        PlayerPrefs.SetInt("Diamonds",  TotalDiamonds);
        PlayerPrefs.Save();
    }
}