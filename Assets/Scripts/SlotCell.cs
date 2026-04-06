using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A single reusable slot cell.
// Used in both ClueRow (display) and PuzzleUI (player input).
// Handles its own color cycling when clicked.
public class SlotCell : MonoBehaviour
{
    [HideInInspector] public Button Button;
    [HideInInspector] public TextMeshProUGUI Label;

    // Annotation colors — cycles on each click
    private static readonly Color[] CycleColors = new Color[]
    {
        new Color(0.15f, 0.25f, 0.7f),  // 0 — blue (default)
        new Color(0.15f, 0.65f, 0.25f), // 1 — green
        new Color(0.75f, 0.15f, 0.15f), // 2 — red
        new Color(0.85f, 0.75f, 0.1f),  // 3 — yellow
        new Color(0.05f, 0.05f, 0.05f), // 4 — black
    };

    private static readonly Color[] LabelColors = new Color[]
    {
        Color.white, // blue bg
        Color.white, // green bg
        Color.white, // red bg
        Color.white, // yellow bg
        Color.white, // black bg
    };

    private int _colorIndex = 0;
    private bool _cyclingEnabled = false; // Only clue slots cycle colors

    void Awake()
    {
        Button = GetComponent<Button>();
        Label  = GetComponentInChildren<TextMeshProUGUI>();
    }

    // Call this to enable color cycling (clue slots only).
    public void EnableColorCycling()
    {
        _cyclingEnabled = true;
        Button.onClick.AddListener(CycleColor);
    }

    // Resets color back to default blue.
    public void ResetColor()
    {
        _colorIndex = 0;
        ApplyColor();
    }

    // Sets the slot to a specific non-cycling color (used for input slots).
    public void SetColor(Color color)
    {
        var colors = Button.colors;
        colors.normalColor      = color;
        colors.highlightedColor = color * 1.1f;
        colors.selectedColor    = color;
        Button.colors = colors;
    }

    private void CycleColor()
    {
        if (!_cyclingEnabled) return;
        _colorIndex = (_colorIndex + 1) % CycleColors.Length;
        ApplyColor();
    }

    private void ApplyColor()
    {
        var colors = Button.colors;
        colors.normalColor      = CycleColors[_colorIndex];
        colors.highlightedColor = CycleColors[_colorIndex] * 1.1f;
        colors.selectedColor    = CycleColors[_colorIndex];
        Button.colors = colors;
        Label.color   = LabelColors[_colorIndex];
    }
}