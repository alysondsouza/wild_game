using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attached to the ClueRow prefab root.
// Instantiates SlotCells dynamically based on code length.
public class ClueRow : MonoBehaviour
{
    [Header("References (assign in prefab)")]
    [SerializeField] private Transform slotsGroup;          // Parent for slot cells
    [SerializeField] private TextMeshProUGUI feedbackLabel; // "[O] 1  [-] 2  [X] 1"
    [SerializeField] private GameObject slotCellPrefab;     // SlotCell prefab

    private List<SlotCell> _slots = new List<SlotCell>();

    // Called by PuzzleUI after instantiating this prefab.
    public void Populate(ClueData data, GameObject slotPrefab)
    {
        // Instantiate one SlotCell per digit
        for (int i = 0; i < data.Guess.Count; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsGroup);
            SlotCell slot  = obj.GetComponent<SlotCell>();

            slot.Label.text = data.Guess[i].ToString();
            slot.EnableColorCycling(); // Clue slots are annotatable
            slot.ResetColor();

            _slots.Add(slot);
        }

        feedbackLabel.text = $"[O] {data.Correct}   [-] {data.Misplaced}   [X] {data.Wrong}";
    }

    // Resets all slot colors back to default blue.
    // Called when player presses Clear.
    public void ResetColors()
    {
        foreach (SlotCell slot in _slots)
            slot.ResetColor();
    }
}