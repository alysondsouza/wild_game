using System;
using UnityEngine;

// Manages player lives — max 3, regenerates 1 every RegenSeconds real-time.
// Persists across sessions via PlayerPrefs.
public class LivesManager : MonoBehaviour
{
    public static LivesManager Instance { get; private set; }

    public const int MaxLives     = 3;
    public const int RegenSeconds = 60; // Change this value to adjust regen time

    public event Action<int> OnLivesChanged;

    public int CurrentLives { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAndRegen();
    }

    // Checks how many lives should have regenerated since last loss.
    public void LoadAndRegen()
    {
        CurrentLives = PlayerPrefs.GetInt("Lives", MaxLives);
        if (CurrentLives >= MaxLives) { Save(); return; }

        string lastLostStr = PlayerPrefs.GetString("LastLifeLostTime", "");
        if (string.IsNullOrEmpty(lastLostStr)) { Save(); return; }

        DateTime lastLost = DateTime.Parse(lastLostStr, null, System.Globalization.DateTimeStyles.RoundtripKind);
        TimeSpan elapsed  = DateTime.UtcNow - lastLost;
        int regenCount    = (int)(elapsed.TotalSeconds / RegenSeconds);

        Debug.Log($"[Lives] elapsed:{elapsed.TotalSeconds:F1}s regenCount:{regenCount} lives:{CurrentLives}");

        if (regenCount > 0)
        {
            CurrentLives = Mathf.Min(CurrentLives + regenCount, MaxLives);

            if (CurrentLives < MaxLives)
            {
                // Advance timestamp by consumed regen cycles
                DateTime newBase = lastLost.AddSeconds(regenCount * RegenSeconds);
                PlayerPrefs.SetString("LastLifeLostTime", newBase.ToString("o"));
            }
            else
            {
                // Full lives — clear timestamp
                PlayerPrefs.DeleteKey("LastLifeLostTime");
            }

            Save();
            OnLivesChanged?.Invoke(CurrentLives);
        }
    }

    public void LoseLife()
    {
        if (CurrentLives <= 0) return;
        CurrentLives--;

        // Only record timestamp on FIRST life lost — don't overwrite if clock is already running
        if (!PlayerPrefs.HasKey("LastLifeLostTime"))
            PlayerPrefs.SetString("LastLifeLostTime", DateTime.UtcNow.ToString("o"));

        Save();
        OnLivesChanged?.Invoke(CurrentLives);
        Debug.Log($"[Lives] Lost a life. Lives remaining: {CurrentLives}");
    }

    public bool HasLives() => CurrentLives > 0;

    public TimeSpan TimeUntilNextLife()
    {
        if (CurrentLives >= MaxLives) return TimeSpan.Zero;
        string s = PlayerPrefs.GetString("LastLifeLostTime", "");
        if (string.IsNullOrEmpty(s)) return TimeSpan.Zero;

        DateTime lastLost  = DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);
        DateTime nextRegen = lastLost.AddSeconds(RegenSeconds);
        TimeSpan remaining = nextRegen - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private void Save()
    {
        PlayerPrefs.SetInt("Lives", CurrentLives);
        PlayerPrefs.Save();
    }
}