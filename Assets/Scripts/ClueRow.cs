using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attached to the ClueRow prefab root.
// Instantiates SlotCells dynamically based on code length.
public class ClueRow : MonoBehaviour
{
    [Header("References (assign in prefab)")]
    [SerializeField] private Transform slotsGroup;
    [SerializeField] private TextMeshProUGUI feedbackLabel;
    [SerializeField] private GameObject slotCellPrefab;

    private List<SlotCell> _slots = new List<SlotCell>();

    // Called by PuzzleUI after instantiating this prefab.
    public void Populate(ClueData data, GameObject slotPrefab)
    {
        for (int i = 0; i < data.Guess.Count; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsGroup);
            SlotCell slot  = obj.GetComponent<SlotCell>();

            slot.Label.text = data.Guess[i].ToString();
            slot.EnableColorCycling();
            slot.ResetColor();

            _slots.Add(slot);
        }

        // Colored circle icons via TMP rich text:
        // green ● = correct position, yellow ● = misplaced, red ● = wrong
        feedbackLabel.text =
            $"<color=#4CAF50>●</color> {data.Correct}  " +
            $"<color=#FFC107>●</color> {data.Misplaced}  " +
            $"<color=#F44336>●</color> {data.Wrong}";
    }

    // Returns all slots whose label matches the given digit.
    // Used by PuzzleUI for bulk painting on long press.
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