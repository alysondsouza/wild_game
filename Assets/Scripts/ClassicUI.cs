using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Drives the Classic Mode scene.
// Player has MaxGuesses attempts to guess the secret code.
// After each guess: feedback is shown as a history row.
// Long press on digit button paints all matching digits in history rows.
// Same lives/lightning/currency system as PuzzleUI.
public class ClassicUI : MonoBehaviour
{
    private const int MaxGuesses = 10;

    [Header("Settings")]
    [SerializeField] private int codeLength = 4;

    [Header("History UI")]
    [SerializeField] private Transform          historyContainer;
    [SerializeField] private ScrollRect         historyScrollRect;
    [SerializeField] private GameObject         historyRowPrefab;
    [SerializeField] private GameObject         slotCellPrefab;

    [Header("Input UI")]
    [SerializeField] private Transform          inputSlotsGroup;
    [SerializeField] private TextMeshProUGUI    feedbackText;
    [SerializeField] private TextMeshProUGUI    answerText;
    [SerializeField] private TextMeshProUGUI    guessCounterText;

    [Header("HUD — Lives")]
    [SerializeField] private Image[]            heartImages;

    [Header("HUD — Lightning")]
    [SerializeField] private Image[]            lightningImages;
    [SerializeField] private TextMeshProUGUI    timerText;

    [Header("HUD — Currency")]
    [SerializeField] private TextMeshProUGUI    totalLightningText;
    [SerializeField] private TextMeshProUGUI    diamondText;

    [Header("No Lives Panel")]
    [SerializeField] private GameObject         noLivesPanel;
    [SerializeField] private TextMeshProUGUI    noLivesTimerText;

    [Header("Regen")]
    [SerializeField] private TextMeshProUGUI    regenTimerText;

    [Header("Icon Colors")]
    [SerializeField] private Color iconActiveColor   = Color.white;
    [SerializeField] private Color iconInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Input Slot Colors")]
    [SerializeField] private Color slotNormalColor   = new Color(0.2f, 0.2f, 0.4f);
    [SerializeField] private Color slotSelectedColor = new Color(0.9f, 0.7f, 0.1f);
    [SerializeField] private Color slotFilledColor   = new Color(0.2f, 0.5f, 0.8f);
    [SerializeField] private Color slotLockedColor   = new Color(0.4f, 0.4f, 0.5f);

    private List<int>           _secret           = new List<int>();
    private int[]               _slotValues;
    private int                 _selectedSlot     = 0;
    private bool                _submitted        = false;
    private int                 _guessCount       = 0;

    private List<SlotCell>          _inputSlots       = new List<SlotCell>();
    private List<GuessHistoryRow>   _historyRows      = new List<GuessHistoryRow>();

    // Cached long press buttons — suppresses digit insert on long press release.
    private LongPressButton[]   _longPressButtons = new LongPressButton[0];

    // Tracks annotation color index per digit (0–9) across all history rows.
    private int[] _digitColorIndex = new int[10];

    // -------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------

    void Start()
    {
        codeLength = PlayerPrefs.GetInt("CodeLength", codeLength);

        if (LivesManager.Instance != null)
            LivesManager.Instance.OnLivesChanged += UpdateHeartsHUD;

        if (LightningManager.Instance != null)
        {
            LightningManager.Instance.OnLightningChanged += UpdateLightningHUD;
            LightningManager.Instance.OnTimerTick        += UpdateTimerHUD;
        }

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += UpdateCurrencyHUD;

        RegisterLongPressHandlers();

        noLivesPanel.SetActive(false);
        CheckLivesAndStart();
        StartCoroutine(RegenTimerLoop());
    }

    void OnDestroy()
    {
        if (LivesManager.Instance != null)
            LivesManager.Instance.OnLivesChanged -= UpdateHeartsHUD;

        if (LightningManager.Instance != null)
        {
            LightningManager.Instance.OnLightningChanged -= UpdateLightningHUD;
            LightningManager.Instance.OnTimerTick        -= UpdateTimerHUD;
        }

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateCurrencyHUD;
    }

    // -------------------------------------------------------------------
    // Lives gate
    // -------------------------------------------------------------------

    private void CheckLivesAndStart()
    {
        if (LivesManager.Instance != null && !LivesManager.Instance.HasLives())
        {
            noLivesPanel.SetActive(true);
            StartCoroutine(NoLivesCountdown());
            return;
        }

        noLivesPanel.SetActive(false);
        UpdateHeartsHUD(LivesManager.Instance?.CurrentLives ?? LivesManager.MaxLives);
        UpdateCurrencyHUD(
            CurrencyManager.Instance?.TotalLightning ?? 0,
            CurrencyManager.Instance?.TotalDiamonds  ?? 0);

        StartGame();
    }

    // -------------------------------------------------------------------
    // Game setup
    // -------------------------------------------------------------------

    private void StartGame()
    {
        _submitted  = false;
        _guessCount = 0;

        feedbackText.text = "";
        answerText.text   = "";

        System.Array.Clear(_digitColorIndex, 0, _digitColorIndex.Length);

        foreach (Transform c in historyContainer) Destroy(c.gameObject);
        _historyRows.Clear();

        foreach (Transform c in inputSlotsGroup) Destroy(c.gameObject);
        _inputSlots.Clear();

        var maker = new CodeMaker(codeLength);
        _secret   = new List<int>(maker.Secret);

        Debug.Log($"[ClassicUI] Secret: {string.Join(" ", _secret)}");

        _slotValues = new int[codeLength];
        for (int i = 0; i < codeLength; i++)
        {
            _slotValues[i] = -1;
            var obj  = Instantiate(slotCellPrefab, inputSlotsGroup);
            var slot = obj.GetComponent<SlotCell>();
            slot.Label.text = "_";
            int idx = i;
            slot.Button.onClick.AddListener(() => SelectSlot(idx));
            _inputSlots.Add(slot);
        }

        SelectSlot(0);
        RefreshInputSlots();
        UpdateGuessCounter();
        LightningManager.Instance?.StartPuzzle(codeLength);
    }

    // -------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------

    public void SelectSlot(int index)
    {
        if (_submitted) return;
        _selectedSlot = index;
        RefreshInputSlots();
    }

    public void AddDigit(int digit)
    {
        if (_submitted) return;

        // Suppress insert if this was a long press release.
        foreach (var lpb in _longPressButtons)
            if (lpb.LongPressConsumed) return;

        _slotValues[_selectedSlot] = digit;
        _selectedSlot = (_selectedSlot + 1) % codeLength;
        RefreshInputSlots();
    }

    public void ClearAll()
    {
        if (_submitted) return;
        for (int i = 0; i < codeLength; i++) _slotValues[i] = -1;
        feedbackText.text = "";
        SelectSlot(0);
        RefreshInputSlots();
    }

    public void SubmitGuess()
    {
        if (_submitted) return;

        for (int i = 0; i < codeLength; i++)
        {
            if (_slotValues[i] == -1)
            {
                feedbackText.text = $"Fill all {codeLength} slots first!";
                SelectSlot(i);
                StartCoroutine(ClearAfterDelay(feedbackText, 2f));
                return;
            }
        }

        _guessCount++;

        var guess = new List<int>(_slotValues);
        var (correct, misplaced, wrong) = Evaluate(guess, _secret);

        AddHistoryRow(guess, correct, misplaced, wrong);
        StartCoroutine(ScrollToBottom());

        bool won          = correct == codeLength;
        bool outOfGuesses = _guessCount >= MaxGuesses;

        if (won)
        {
            _submitted = true;
            int earned = LightningManager.Instance?.StopAndCollect() ?? 0;
            CurrencyManager.Instance?.AddLightning(earned);
            CurrencyManager.Instance?.AddDiamonds(1);
            answerText.text   = "Secret:  " + string.Join("  ", _secret);
            feedbackText.text = $"YOU WIN!  +{earned} lightning  +1 gem";
            RefreshInputSlots();
        }
        else if (outOfGuesses)
        {
            _submitted = true;
            LightningManager.Instance?.StopNoReward();
            LivesManager.Instance?.LoseLife();
            CurrencyManager.Instance?.SpendLightning(1);
            answerText.text   = "Secret:  " + string.Join("  ", _secret);
            feedbackText.text = "Out of guesses!  -1 lightning";
            RefreshInputSlots();
        }
        else
        {
            for (int i = 0; i < codeLength; i++) _slotValues[i] = -1;
            SelectSlot(0);
            RefreshInputSlots();
            UpdateGuessCounter();
        }
    }

    public void NewGame()
    {
        if (!_submitted)
        {
            LightningManager.Instance?.StopNoReward();
            LivesManager.Instance?.LoseLife();
            CurrencyManager.Instance?.SpendLightning(1);
        }
        CheckLivesAndStart();
    }

    public void GoToMainMenu()
    {
        LightningManager.Instance?.StopNoReward();
        if (!_submitted)
            LivesManager.Instance?.LoseLife();
        SceneManager.LoadScene("MainMenu");
    }

    // -------------------------------------------------------------------
    // History row
    // -------------------------------------------------------------------

    private void AddHistoryRow(List<int> guess, int correct, int misplaced, int wrong)
    {
        var rowObj = Instantiate(historyRowPrefab, historyContainer);
        var row    = rowObj.GetComponent<GuessHistoryRow>();
        row.Populate(guess, correct, misplaced, wrong, slotCellPrefab);
        _historyRows.Add(row);
    }

    // -------------------------------------------------------------------
    // Bulk paint — long press on digit button
    // -------------------------------------------------------------------

    private void RegisterLongPressHandlers()
    {
        _longPressButtons = FindObjectsByType<LongPressButton>(FindObjectsSortMode.None);

        foreach (var lpb in _longPressButtons)
        {
            var label = lpb.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) continue;
            if (!int.TryParse(label.text, out int digit)) continue;

            int captured = digit;
            lpb.OnLongPress += () => BulkPaintDigit(captured);
        }
    }

    // Advances the annotation color of ALL history row slots showing this digit.
    private void BulkPaintDigit(int digit)
    {
        if (_submitted) return;

        _digitColorIndex[digit] = (_digitColorIndex[digit] + 1) % 5;
        int targetIndex = _digitColorIndex[digit];

        foreach (var row in _historyRows)
            foreach (var slot in row.GetSlotsWithDigit(digit))
                slot.SetColorIndex(targetIndex);
    }

    // -------------------------------------------------------------------
    // Evaluation
    // -------------------------------------------------------------------

    private (int correct, int misplaced, int wrong) Evaluate(List<int> guess, List<int> target)
    {
        int correct = 0;
        var secretLeft = new List<int>(codeLength);
        var guessLeft  = new List<int>(codeLength);

        for (int i = 0; i < codeLength; i++)
        {
            if (guess[i] == target[i]) correct++;
            else { secretLeft.Add(target[i]); guessLeft.Add(guess[i]); }
        }

        int misplaced = 0;
        foreach (int g in guessLeft)
            if (secretLeft.Remove(g)) misplaced++;

        return (correct, misplaced, codeLength - correct - misplaced);
    }

    // -------------------------------------------------------------------
    // HUD
    // -------------------------------------------------------------------

    private void UpdateGuessCounter()
    {
        if (guessCounterText != null)
            guessCounterText.text = $"Guess {_guessCount + 1} / {MaxGuesses}";
    }

    private void UpdateHeartsHUD(int lives)
    {
        for (int i = 0; i < heartImages.Length; i++)
            heartImages[i].color = i < lives ? iconActiveColor : iconInactiveColor;
    }

    private void UpdateLightningHUD(int lightning)
    {
        for (int i = 0; i < lightningImages.Length; i++)
            lightningImages[i].color = i < lightning ? iconActiveColor : iconInactiveColor;
    }

    private void UpdateTimerHUD(float elapsed, float total)
    {
        if (timerText == null) return;
        float remaining = Mathf.Max(0f, total - elapsed);
        if (remaining <= 0f) { timerText.text = "No Bonus"; return; }
        int mins = Mathf.FloorToInt(remaining / 60f);
        int secs = Mathf.FloorToInt(remaining % 60f);
        timerText.text = mins + ":" + secs.ToString("D2");
    }

    private void UpdateCurrencyHUD(int lightning, int diamonds)
    {
        if (totalLightningText != null) totalLightningText.text = lightning.ToString();
        if (diamondText != null)        diamondText.text        = diamonds.ToString();
    }

    // -------------------------------------------------------------------
    // Slot visuals
    // -------------------------------------------------------------------

    private void RefreshInputSlots()
    {
        for (int i = 0; i < _inputSlots.Count; i++)
        {
            bool isSelected = (i == _selectedSlot) && !_submitted;
            bool isFilled   = _slotValues[i] != -1;

            _inputSlots[i].Label.text = isFilled ? _slotValues[i].ToString() : "_";

            _inputSlots[i].SetColor(
                _submitted  ? slotLockedColor   :
                isSelected  ? slotSelectedColor  :
                isFilled    ? slotFilledColor    :
                              slotNormalColor);
        }
    }

    // -------------------------------------------------------------------
    // Coroutines
    // -------------------------------------------------------------------

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        if (historyScrollRect != null)
            historyScrollRect.verticalNormalizedPosition = 0f;
    }

    private IEnumerator RegenTimerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            LivesManager.Instance?.LoadAndRegen();

            if (LivesManager.Instance != null &&
                LivesManager.Instance.CurrentLives < LivesManager.MaxLives)
            {
                var t = LivesManager.Instance.TimeUntilNextLife();
                if (regenTimerText != null)
                    regenTimerText.text = $"Next Life: {t.Minutes:D2}:{t.Seconds:D2}";
            }
            else
            {
                if (regenTimerText != null) regenTimerText.text = "";
            }
        }
    }

    private IEnumerator NoLivesCountdown()
    {
        while (LivesManager.Instance != null && !LivesManager.Instance.HasLives())
        {
            var t = LivesManager.Instance.TimeUntilNextLife();

            if (t.TotalSeconds <= 0)
            {
                LivesManager.Instance.LoadAndRegen();
                SceneManager.LoadScene("MainMenu");
                yield break;
            }

            if (noLivesTimerText != null)
                noLivesTimerText.text = $"Next Life in {t.Minutes:D2}:{t.Seconds:D2}";

            yield return new WaitForSeconds(1f);
            LivesManager.Instance.LoadAndRegen();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator ClearAfterDelay(TextMeshProUGUI label, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        label.text = "";
    }
}