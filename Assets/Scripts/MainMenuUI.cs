using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Drives the Main Menu scene.
// Saves CodeLength to PlayerPrefs before loading PuzzleScene.
public class MainMenuUI : MonoBehaviour
{
    [Header("Mode Buttons")]
    [SerializeField] private Button puzzleButton;   // Active
    [SerializeField] private Button classicButton;  // Greyed out — coming soon

    [Header("Length Slider")]
    [SerializeField] private Slider lengthSlider;
    [SerializeField] private TextMeshProUGUI lengthLabel; // Shows current value e.g. "4"

    [Header("Play Button")]
    [SerializeField] private Button playButton;

    [Header("Colors")]
    [SerializeField] private Color modeActiveColor   = new Color(0.15f, 0.25f, 0.7f);
    [SerializeField] private Color modeDisabledColor = new Color(0.4f,  0.4f,  0.4f);

    private void Start()
    {
        // Slider range: 3–7
        lengthSlider.minValue = 3;
        lengthSlider.maxValue = 7;
        lengthSlider.wholeNumbers = true;

        // Default to last used length, fallback to 4
        lengthSlider.value = PlayerPrefs.GetInt("CodeLength", 4);
        UpdateLengthLabel(lengthSlider.value);

        // Wire slider change
        lengthSlider.onValueChanged.AddListener(UpdateLengthLabel);

        // Grey out Classic — not implemented yet
        classicButton.interactable = false;
        SetButtonColor(classicButton, modeDisabledColor);
        SetButtonColor(puzzleButton, modeActiveColor);

        // Wire play button
        playButton.onClick.AddListener(StartGame);
    }

    // Updates the label next to the slider.
    private void UpdateLengthLabel(float value)
    {
        lengthLabel.text = ((int)value).ToString();
    }

    // Saves settings and loads PuzzleScene.
    private void StartGame()
    {
        PlayerPrefs.SetInt("CodeLength", (int)lengthSlider.value);
        PlayerPrefs.SetString("GameMode", "Puzzle");
        PlayerPrefs.Save();
        SceneManager.LoadScene("PuzzleScene");
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var colors = btn.colors;
        colors.normalColor      = color;
        colors.disabledColor    = modeDisabledColor;
        btn.colors = colors;
    }
}