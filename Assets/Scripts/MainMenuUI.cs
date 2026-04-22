using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Drives the Main Menu scene.
// Shows lives, regen timer, currency in TopBar.
// Saves CodeLength and GameMode to PlayerPrefs before loading the correct scene.
public class MainMenuUI : MonoBehaviour
{
    [Header("Mode Buttons")]
    [SerializeField] private Button puzzleButton;
    [SerializeField] private Button classicButton;

    [Header("Length Slider")]
    [SerializeField] private Slider          lengthSlider;
    [SerializeField] private TextMeshProUGUI lengthLabel;

    [Header("Play Button")]
    [SerializeField] private Button playButton;

    [Header("HUD — Lives")]
    [SerializeField] private Image[]         heartImages;
    [SerializeField] private TextMeshProUGUI regenTimerText;

    [Header("HUD — Currency")]
    [SerializeField] private TextMeshProUGUI totalLightningText;
    [SerializeField] private TextMeshProUGUI diamondText;

    [Header("Colors")]
    [SerializeField] private Color modeActiveColor   = new Color(0.78f, 0.86f, 1f);
    [SerializeField] private Color modeInactiveColor = new Color(0.63f, 0.63f, 0.63f);
    [SerializeField] private Color iconActiveColor   = Color.white;
    [SerializeField] private Color iconInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    // "Puzzle" or "Classic"
    private string _selectedMode = "Puzzle";

    private void Start()
    {
        // Slider setup
        lengthSlider.minValue     = 3;
        lengthSlider.maxValue     = 7;
        lengthSlider.wholeNumbers = true;
        lengthSlider.value        = PlayerPrefs.GetInt("CodeLength", 4);
        UpdateLengthLabel(lengthSlider.value);
        lengthSlider.onValueChanged.AddListener(UpdateLengthLabel);

        // Mode buttons — both enabled, highlight current selection
        puzzleButton.onClick.AddListener(() => SelectMode("Puzzle"));
        classicButton.onClick.AddListener(() => SelectMode("Classic"));

        // Restore last selected mode
        _selectedMode = PlayerPrefs.GetString("GameMode", "Puzzle");
        HighlightModeButtons();

        // Play button
        playButton.onClick.AddListener(StartGame);

        // Subscribe to live events
        if (LivesManager.Instance != null)
            LivesManager.Instance.OnLivesChanged += OnLivesChanged;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;

        RefreshHUD();
        StartCoroutine(HUDUpdateLoop());
    }

    private void OnDestroy()
    {
        if (LivesManager.Instance != null)
            LivesManager.Instance.OnLivesChanged -= OnLivesChanged;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
    }

    // -------------------------------------------------------------------
    // Mode selection
    // -------------------------------------------------------------------

    private void SelectMode(string mode)
    {
        _selectedMode = mode;
        HighlightModeButtons();
    }

    private void HighlightModeButtons()
    {
        SetButtonColor(puzzleButton,  _selectedMode == "Puzzle"  ? modeActiveColor : modeInactiveColor);
        SetButtonColor(classicButton, _selectedMode == "Classic" ? modeActiveColor : modeInactiveColor);
    }

    // -------------------------------------------------------------------
    // Start game
    // -------------------------------------------------------------------

    private void StartGame()
    {
        if (LivesManager.Instance != null && !LivesManager.Instance.HasLives())
        {
            Debug.Log("[MainMenu] No lives — can't start.");
            return;
        }

        PlayerPrefs.SetInt("CodeLength", (int)lengthSlider.value);
        PlayerPrefs.SetString("GameMode", _selectedMode);
        PlayerPrefs.Save();

        string scene = _selectedMode == "Classic" ? "ClassicScene" : "PuzzleScene";
        SceneManager.LoadScene(scene);
    }

    // -------------------------------------------------------------------
    // HUD
    // -------------------------------------------------------------------

    private void OnLivesChanged(int lives)    => RefreshHUD();
    private void OnCurrencyChanged(int l, int d) => RefreshHUD();

    private void RefreshHUD()
    {
        if (LivesManager.Instance == null) return;

        int lives = LivesManager.Instance.CurrentLives;

        for (int i = 0; i < heartImages.Length; i++)
            heartImages[i].color = i < lives ? iconActiveColor : iconInactiveColor;

        if (lives < LivesManager.MaxLives)
        {
            var t = LivesManager.Instance.TimeUntilNextLife();
            if (regenTimerText != null)
                regenTimerText.text = $"Next Life: {t.Minutes:D2}:{t.Seconds:D2}";
        }
        else
        {
            if (regenTimerText != null) regenTimerText.text = "";
        }

        if (CurrencyManager.Instance != null)
        {
            if (totalLightningText != null)
                totalLightningText.text = CurrencyManager.Instance.TotalLightning.ToString();
            if (diamondText != null)
                diamondText.text = CurrencyManager.Instance.TotalDiamonds.ToString();
        }
    }

    private IEnumerator HUDUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            LivesManager.Instance?.LoadAndRegen();
            RefreshHUD();
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private void UpdateLengthLabel(float value)
    {
        lengthLabel.text = ((int)value).ToString();
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var colors         = btn.colors;
        colors.normalColor = color;
        btn.colors         = colors;
    }
}