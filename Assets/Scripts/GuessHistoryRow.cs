using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Displays one guess attempt in the Classic Mode history scroll view.
// Same visual as ClueRow: digit slots + colored circle feedback.
// Slots support color cycling so the player can annotate guesses.
public class GuessHistoryRow : MonoBehaviour
{
    [Header("References (assign in prefab)")]
    [SerializeField] private Transform          slotsGroup;
    [SerializeField] private TextMeshProUGUI    feedbackLabel;

    private List<SlotCell> _slots = new List<SlotCell>();

    // Called by ClassicUI after instantiating this prefab.
    public void Populate(List<int> guess, int correct, int misplaced, int wrong, GameObject slotPrefab)
    {
        foreach (int digit in guess)
        {
            var obj  = Instantiate(slotPrefab, slotsGroup);
            var slot = obj.GetComponent<SlotCell>();
            slot.Label.text = digit.ToString();
            slot.EnableColorCycling(); // allow individual click-to-cycle
            slot.ResetColor();
            _slots.Add(slot);
        }

        feedbackLabel.text =
            $"<color=#4CAF50>●</color> {correct}  " +
            $"<color=#FFC107>●</color> {misplaced}  " +
            $"<color=#F44336>●</color> {wrong}";
    }

    // Returns all slots whose label matches the given digit.
    // Used by ClassicUI for bulk painting on long press.
    public List<SlotCell> GetSlotsWithDigit(int digit)
    {
        string target = digit.ToString();
        var result = new List<SlotCell>();
        foreach (SlotCell slot in _slots)
            if (slot.Label.text == target)
                result.Add(slot);
        return result;
    }

    // Resets all slot colors back to default blue.
    public void ResetColors()
    {
        foreach (SlotCell slot in _slots)
            slot.ResetColor();
    }
}