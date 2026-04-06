using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int codeLength = 4;
    [SerializeField] private int digitMin   = 0;
    [SerializeField] private int digitMax   = 9;

    [Header("Clue UI")]
    [SerializeField] private Transform cluesContainer;  // Vertical layout parent
    [SerializeField] private GameObject clueRowPrefab;  // ClueRow prefab
    [SerializeField] private GameObject slotCellPrefab; // SlotCell prefab

    [Header("Input UI")]
    [SerializeField] private Transform inputSlotsGroup; // Horizontal layout parent for input slots
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI answerText;

    [Header("Input Slot Colors")]
    [SerializeField] private Color slotNormalColor   = new Color(0.2f, 0.2f, 0.4f);
    [SerializeField] private Color slotSelectedColor = new Color(0.9f, 0.7f, 0.1f);
    [SerializeField] private Color slotFilledColor   = new Color(0.2f, 0.5f, 0.8f);
    [SerializeField] private Color slotLockedColor   = new Color(0.4f, 0.4f, 0.5f);

    private List<int> _secret;
    private int[] _slotValues;
    private int _selectedSlot = 0;
    private bool _submitted   = false;

    // Runtime references
    private List<SlotCell> _inputSlots = new List<SlotCell>();
    private List<ClueRow>  _clueRows   = new List<ClueRow>();

    void Start()
    {
        codeLength = PlayerPrefs.GetInt("CodeLength", codeLength);
        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        _submitted = false;
        feedbackText.text = "";
        answerText.text   = "";

        // Destroy old clue rows
        foreach (Transform child in cluesContainer) Destroy(child.gameObject);
        _clueRows.Clear();

        // Destroy old input slots
        foreach (Transform child in inputSlotsGroup) Destroy(child.gameObject);
        _inputSlots.Clear();

        // Generate puzzle
        var maker   = new CodeMaker(codeLength, digitMin, digitMax);
        var builder = new ClueBuilder(maker, codeLength, digitMin, digitMax);
        _secret     = new List<int>(maker.Secret);
        List<ClueData> clues = builder.Build(out _);

        Debug.Log($"[PuzzleUI] Secret: {string.Join(" ", _secret)} | Clues: {clues.Count}");

        // Instantiate clue rows
        foreach (ClueData clue in clues)
        {
            GameObject rowObj = Instantiate(clueRowPrefab, cluesContainer);
            ClueRow row       = rowObj.GetComponent<ClueRow>();
            row.Populate(clue, slotCellPrefab);
            _clueRows.Add(row);
        }

        // Instantiate input slots dynamically
        _slotValues = new int[codeLength];
        for (int i = 0; i < codeLength; i++)
        {
            _slotValues[i] = -1;

            GameObject obj = Instantiate(slotCellPrefab, inputSlotsGroup);
            SlotCell slot  = obj.GetComponent<SlotCell>();

            slot.Label.text = "_";

            // Wire click to SelectSlot — capture index for lambda
            int index = i;
            slot.Button.onClick.AddListener(() => SelectSlot(index));

            _inputSlots.Add(slot);
        }

        SelectSlot(0);
        RefreshInputSlots();
    }

    public void SelectSlot(int index)
    {
        if (_submitted) return;
        _selectedSlot = index;
        RefreshInputSlots();
    }

    public void AddDigit(int digit)
    {
        if (_submitted) return;
        _slotValues[_selectedSlot] = digit;
        _selectedSlot = (_selectedSlot + 1) % codeLength;
        RefreshInputSlots();
    }

    // Clears input slots AND resets all clue slot colors.
    public void ClearAll()
    {
        if (_submitted) return;

        // Reset input
        for (int i = 0; i < codeLength; i++) _slotValues[i] = -1;
        feedbackText.text = "";
        SelectSlot(0);
        RefreshInputSlots();

        // Reset clue slot annotation colors
        foreach (ClueRow row in _clueRows)
            row.ResetColors();
    }

    public void SubmitGuess()
    {
        if (_submitted) return;

        // Block if any slot empty
        for (int i = 0; i < codeLength; i++)
        {
            if (_slotValues[i] == -1)
            {
                feedbackText.text = $"Fill all {codeLength} slots first!";
                SelectSlot(i);
                StopAllCoroutines();
                StartCoroutine(ClearMessageAfterDelay(feedbackText, 2f));
                return;
            }
        }

        _submitted = true;

        bool correct = true;
        for (int i = 0; i < codeLength; i++)
            if (_slotValues[i] != _secret[i]) { correct = false; break; }

        answerText.text   = "Secret:  " + string.Join("  ", _secret);
        feedbackText.text = correct ? "** YOU WIN! **" : "** WRONG! Better luck next time. **";

        RefreshInputSlots();
    }

    public void NewPuzzle()
    {
        GeneratePuzzle();
    }

    private void RefreshInputSlots()
    {
        for (int i = 0; i < _inputSlots.Count; i++)
        {
            bool isSelected = (i == _selectedSlot) && !_submitted;
            bool isFilled   = (_slotValues[i] != -1);

            _inputSlots[i].Label.text = isFilled ? _slotValues[i].ToString() : "_";

            Color targetColor = _submitted ? slotLockedColor
                              : isSelected ? slotSelectedColor
                              : isFilled   ? slotFilledColor
                              : slotNormalColor;

            _inputSlots[i].SetColor(targetColor);
        }
    }

    private System.Collections.IEnumerator ClearMessageAfterDelay(TextMeshProUGUI label, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        label.text = "";
    }

    // Called by Main Menu button.
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}