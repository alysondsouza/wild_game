using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Drives the Main Menu scene.
// Shows lives, regen timer, currency in TopBar.
// Saves CodeLength to PlayerPrefs before loading PuzzleScene.
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
    [SerializeField] private Color modeActiveColor   = new Color(0.15f, 0.25f, 0.7f);
    [SerializeField] private Color modeDisabledColor = new Color(0.4f,  0.4f,  0.4f);
    [SerializeField] private Color iconActiveColor   = Color.white;
    [SerializeField] private Color iconInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private void Start()
    {
        // Slider setup
        lengthSlider.minValue     = 3;
        lengthSlider.maxValue     = 7;
        lengthSlider.wholeNumbers = true;
        lengthSlider.value        = PlayerPrefs.GetInt("CodeLength", 4);
        UpdateLengthLabel(lengthSlider.value);
        lengthSlider.onValueChanged.AddListener(UpdateLengthLabel);

        // Mode buttons
        classicButton.interactable = false;
        SetButtonColor(classicButton, modeDisabledColor);
        SetButtonColor(puzzleButton, modeActiveColor);

        // Play button
        playButton.onClick.AddListener(StartGame);

        // Subscribe to live events
        if (LivesManager.Instance != null)
            LivesManager.Instance.OnLivesChanged += OnLivesChanged;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;

        // Initial HUD refresh
        RefreshHUD();

        // Update regen timer every second
        StartCoroutine(HUDUpdateLoop());
    }

    private void OnDestroy()
    {
        if (LivesManager.Instance != null)
            LivesManager.Instance.OnLivesChanged -= OnLivesChanged;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
    }

    private void OnLivesChanged(int lives) => RefreshHUD();
    private void OnCurrencyChanged(int lightning, int diamonds) => RefreshHUD();

    private void RefreshHUD()
    {
        if (LivesManager.Instance == null) return;

        int lives = LivesManager.Instance.CurrentLives;

        // Hearts
        for (int i = 0; i < heartImages.Length; i++)
            heartImages[i].color = i < lives ? iconActiveColor : iconInactiveColor;

        // Regen timer
        if (lives < LivesManager.MaxLives)
        {
            var t = LivesManager.Instance.TimeUntilNextLife();
            if (regenTimerText != null)
                regenTimerText.text = $"Next Life: {t.Minutes:D2}:{t.Seconds:D2}";
        }
        else
        {
            if (regenTimerText != null)
                regenTimerText.text = "";
        }

        // Currency
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

    private void UpdateLengthLabel(float value)
    {
        lengthLabel.text = ((int)value).ToString();
    }

    private void StartGame()
    {
        if (LivesManager.Instance != null && !LivesManager.Instance.HasLives())
        {
            Debug.Log("[MainMenu] No lives — can't start.");
            return;
        }

        PlayerPrefs.SetInt("CodeLength", (int)lengthSlider.value);
        PlayerPrefs.SetString("GameMode", "Puzzle");
        PlayerPrefs.Save();
        SceneManager.LoadScene("PuzzleScene");
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var colors           = btn.colors;
        colors.normalColor   = color;
        colors.disabledColor = modeDisabledColor;
        btn.colors           = colors;
    }
}