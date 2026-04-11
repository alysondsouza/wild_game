using System;
using UnityEngine;

// Manages the per-puzzle lightning timer.
// Durations: 30s/45s/60s/90s/120s for code lengths 3-7.
// Loses 1 lightning at each 1/3 of total time elapsed.
public class LightningManager : MonoBehaviour
{
    public static LightningManager Instance { get; private set; }

    private static readonly float[] TimerByLength = { 30f, 45f, 60f, 90f, 120f };

    public event Action<int>         OnLightningChanged; // current puzzle lightning (0-3)
    public event Action<float, float> OnTimerTick;       // (elapsed, total)

    public int   CurrentLightning { get; private set; }
    public bool  IsRunning        { get; private set; }

    private float _totalTime;
    private float _elapsed;
    private int   _lastThreshold;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartPuzzle(int codeLength)
    {
        int index      = Mathf.Clamp(codeLength - 3, 0, TimerByLength.Length - 1);
        _totalTime     = TimerByLength[index];
        _elapsed       = 0f;
        _lastThreshold = 0;
        CurrentLightning = 3;
        IsRunning        = true;

        OnLightningChanged?.Invoke(CurrentLightning);
        OnTimerTick?.Invoke(0f, _totalTime);
    }

    // Call on win — returns lightning earned.
    public int StopAndCollect()
    {
        IsRunning = false;
        return CurrentLightning;
    }

    // Call on skip or wrong — no reward.
    public void StopNoReward()
    {
        IsRunning = false;
    }

    private void Update()
    {
        if (!IsRunning) return;

        _elapsed += Time.deltaTime;
        OnTimerTick?.Invoke(_elapsed, _totalTime);

        float third    = _totalTime / 3f;
        int threshold  = Mathf.Clamp(Mathf.FloorToInt(_elapsed / third), 0, 3);

        if (threshold > _lastThreshold)
        {
            _lastThreshold   = threshold;
            CurrentLightning = Mathf.Max(0, 3 - threshold);
            OnLightningChanged?.Invoke(CurrentLightning);
        }

        if (_elapsed >= _totalTime) IsRunning = false;
    }
}