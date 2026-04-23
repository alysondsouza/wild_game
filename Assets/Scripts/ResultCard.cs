using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Reusable result card shown at the end of a Puzzle or Classic game.
// Displays win/loss result, rewards earned, secret code, and action buttons.
//
// Sprites are loaded automatically from Resources/Icons/128/.
// No manual sprite wiring needed in the Inspector.
public class ResultCard : MonoBehaviour
{
    [Header("Result")]
    [SerializeField] private GameObject         cardPanel;        // root panel to show/hide
    [SerializeField] private TextMeshProUGUI    resultText;       // "YOU WIN!" / "GAME OVER"
    [SerializeField] private TextMeshProUGUI    secretText;       // "Secret: 3 7 1 2"

    [Header("Rewards")]
    [SerializeField] private GameObject         rewardPanel;      // hidden on loss
    [SerializeField] private Image              lightningIcon;
    [SerializeField] private TextMeshProUGUI    lightningText;    // "+3"
    [SerializeField] private Image              gemIcon;
    [SerializeField] private TextMeshProUGUI    gemText;          // "+1"

    [Header("Buttons")]
    [SerializeField] private Button             playAgainButton;
    [SerializeField] private Button             mainMenuButton;

    // Callbacks wired by PuzzleUI / ClassicUI
    private Action _onPlayAgain;
    private Action _onMainMenu;

    private void Awake()
    {
        // Load sprites from Resources — no manual Inspector wiring needed
        lightningIcon.sprite = Resources.Load<Sprite>("Icons/128/Icon_Resources_Lightning01_Blue");
        gemIcon.sprite       = Resources.Load<Sprite>("Icons/128/Icon_Materials_Gem02_Purple");

        playAgainButton.onClick.AddListener(() => {
            Hide();
            _onPlayAgain?.Invoke();
        });

        mainMenuButton.onClick.AddListener(() => {
            Hide();
            _onMainMenu?.Invoke();
        });

        cardPanel.SetActive(false);
    }

    // Call this from PuzzleUI / ClassicUI once at Start() to register callbacks.
    public void Init(Action onPlayAgain, Action onMainMenu)
    {
        _onPlayAgain = onPlayAgain;
        _onMainMenu  = onMainMenu;
    }

    // Show the card after a win.
    public void ShowWin(int lightningEarned, int gemsEarned, int[] secret)
    {
        resultText.text = "YOU WIN!";
        resultText.color = new Color(0.3f, 0.85f, 0.3f); // green

        secretText.text = "Secret:  " + string.Join("  ", secret);

        rewardPanel.SetActive(true);
        lightningText.text = $"+{lightningEarned}";
        gemText.text       = $"+{gemsEarned}";

        cardPanel.SetActive(true);
    }

    // Show the card after a loss.
    public void ShowLoss(string reason, int[] secret)
    {
        resultText.text  = reason; // e.g. "WRONG!" or "OUT OF GUESSES!"
        resultText.color = new Color(0.85f, 0.3f, 0.3f); // red

        secretText.text = "Secret:  " + string.Join("  ", secret);

        rewardPanel.SetActive(false); // no rewards on loss

        cardPanel.SetActive(true);
    }

    public void Hide()
    {
        cardPanel.SetActive(false);
    }
}