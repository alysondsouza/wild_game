using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PuzzleUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int codeLength = 4;
    [SerializeField] private int digitMin   = 0;
    [SerializeField] private int digitMax   = 9;

    [Header("Clue UI")]
    [SerializeField] private Transform  cluesContainer;
    [SerializeField] private GameObject clueRowPrefab;
    [SerializeField] private GameObject slotCellPrefab;

    [Header("Input UI")]
    [SerializeField] private Transform          inputSlotsGroup;
    [SerializeField] private TextMeshProUGUI    feedbackText;

    [Header("Result Card")]
    [SerializeField] private ResultCard resultCard;

    [Header("Regen")]
    [SerializeField] private TextMeshProUGUI regenTimerText;

    [Header("HUD — Lives (3 heart images)")]
    [SerializeField] private Image[] heartImages;

    [Header("HUD — Lightning (3 bolt images + timer)")]
    [SerializeField] private Image[]         lightningImages;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("HUD — Currency")]
    [SerializeField] private TextMeshProUGUI totalLightningText;
    [SerializeField] private TextMeshProUGUI diamondText;

    [Header("No Lives Panel")]
    [SerializeField] private GameObject      noLivesPanel;
    [SerializeField] private TextMeshProUGUI noLivesTimerText;

    [Header("Icon Colors")]
    [SerializeField] private Color iconActiveColor   = Color.white;
    [SerializeField] private Color iconInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Input Slot Colors")]
    [SerializeField] private Color slotNormalColor   = new Color(0.2f, 0.2f, 0.4f);
    [SerializeField] private Color slotSelectedColor = new Color(0.9f, 0.7f, 0.1f);
    [SerializeField] private Color slotFilledColor   = new Color(0.2f, 0.5f, 0.8f);
    [SerializeField] private Color slotLockedColor   = new Color(0.4f, 0.4f, 0.5f);

    private List<int>  _secret       = new List<int>();
    private int[]      _slotValues;
    private int        _selectedSlot = 0;
    private bool       _submitted    = false;
    private bool       _skipPending  = false;

    private List<SlotCell>    _inputSlots       = new List<SlotCell>();
    private List<ClueRow>     _clueRows         = new List<ClueRow>();
    private LongPressButton[] _longPressButtons = new LongPressButton[0];
    private int[]             _digitColorIndex  = new int[10];

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

        // Wire result card callbacks — Play Again = new puzzle, Main Menu = no life penalty
        resultCard?.Init(
            onPlayAgain: () => CheckLivesAndStart(),
            onMainMenu:  () => SceneManager.LoadScene("MainMenu"));

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

        GeneratePuzzle();
    }

    // -------------------------------------------------------------------
    // Puzzle generation
    // -------------------------------------------------------------------

    private void GeneratePuzzle()
    {
        _submitted   = false;
        _skipPending = false;
        feedbackText.text = "";

        System.Array.Clear(_digitColorIndex, 0, _digitColorIndex.Length);

        foreach (Transform c in cluesContainer) Destroy(c.gameObject);
        _clueRows.Clear();

        foreach (Transform c in inputSlotsGroup) Destroy(c.gameObject);
        _inputSlots.Clear();

        var maker   = new CodeMaker(codeLength, digitMin, digitMax);
        var builder = new ClueBuilder(maker, codeLength, digitMin, digitMax);
        _secret     = new List<int>(maker.Secret);
        var clues   = builder.Build(out _);

        foreach (ClueData clue in clues)
        {
            var rowObj = Instantiate(clueRowPrefab, cluesContainer);
            var row    = rowObj.GetComponent<ClueRow>();
            row.Populate(clue, slotCellPrefab);
            _clueRows.Add(row);
        }

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
        foreach (var row in _clueRows) row.ResetColors();
        System.Array.Clear(_digitColorIndex, 0, _digitColorIndex.Length);
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
                StopAllCoroutines();
                StartCoroutine(ClearAfterDelay(feedbackText, 2f));
                return;
            }
        }

        _submitted = true;

        bool correct = true;
        for (int i = 0; i < codeLength; i++)
            if (_slotValues[i] != _secret[i]) { correct = false; break; }

        RefreshInputSlots();

        if (correct)
        {
            int earned = LightningManager.Instance?.StopAndCollect() ?? 0;
            CurrencyManager.Instance?.AddLightning(earned);
            CurrencyManager.Instance?.AddDiamonds(1);
            resultCard?.ShowWin(earned, 1, _secret.ToArray());
        }
        else
        {
            LightningManager.Instance?.StopNoReward();
            LivesManager.Instance?.LoseLife();
            CurrencyManager.Instance?.SpendLightning(1);
            resultCard?.ShowLoss("WRONG!", _secret.ToArray());
        }
    }

    public void NewPuzzle()
    {
        if (_skipPending) return;

        if (!_submitted)
        {
            _skipPending = true;
            LightningManager.Instance?.StopNoReward();
            LivesManager.Instance?.LoseLife();
            CurrencyManager.Instance?.SpendLightning(1);
            feedbackText.text = "Skipped! -1 life";
            StartCoroutine(DelayThen(1.5f, CheckLivesAndStart));
        }
        else
        {
            CheckLivesAndStart();
        }
    }

    public void GoToMainMenu()
    {
        LightningManager.Instance?.StopNoReward();
        if (!_submitted)
            LivesManager.Instance?.LoseLife();
        SceneManager.LoadScene("MainMenu");
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

    private void BulkPaintDigit(int digit)
    {
        if (_submitted) return;

        _digitColorIndex[digit] = (_digitColorIndex[digit] + 1) % 5;
        int targetIndex = _digitColorIndex[digit];

        foreach (var row in _clueRows)
            foreach (var slot in row.GetSlotsWithDigit(digit))
                slot.SetColorIndex(targetIndex);
    }

    // -------------------------------------------------------------------
    // HUD Updates
    // -------------------------------------------------------------------

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
    // Coroutines
    // -------------------------------------------------------------------

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

    private IEnumerator DelayThen(float seconds, System.Action callback)
    {
        yield return new WaitForSeconds(seconds);
        callback?.Invoke();
    }

    private IEnumerator ClearAfterDelay(TextMeshProUGUI label, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        label.text = "";
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
                _submitted  ? slotLockedColor  :
                isSelected  ? slotSelectedColor :
                isFilled    ? slotFilledColor   :
                              slotNormalColor);
        }
    }
}