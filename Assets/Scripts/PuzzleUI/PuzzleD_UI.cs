using UnityEngine;
using UnityEngine.UI;

public class PuzzleD_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("3x3 Toggles (size 9)")]
    [SerializeField] private Toggle[] cells;

    [Header("How many are ON in the target pattern")]
    [Range(1, 9)]
    [SerializeField] private int targetOnCount = 4;

    private bool[] target;

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        if (cells != null)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                int idx = i;
                if (cells[i] == null) continue;
                cells[i].onValueChanged.AddListener(_ => OnCellChanged(idx));
            }
        }
    }

    public void ResetPuzzle()
    {
        if (cells == null || cells.Length != 9)
        {
            Debug.LogWarning("PuzzleD_UI: Cells must be size 9.");
            return;
        }

        target = new bool[9];

        // Clear pattern
        for (int i = 0; i < 9; i++)
            target[i] = false;

        // Pick random ON cells
        int placed = 0;
        while (placed < targetOnCount)
        {
            int r = Random.Range(0, 9);
            if (!target[r])
            {
                target[r] = true;
                placed++;
            }
        }

        // Reset player toggles to OFF (without firing events)
        for (int i = 0; i < 9; i++)
            cells[i].SetIsOnWithoutNotify(false);
    }

    private void OnCellChanged(int _)
    {
        if (target == null || target.Length != 9) return;

        for (int i = 0; i < 9; i++)
        {
            if (cells[i].isOn != target[i])
                return;
        }

        puzzlePanel?.MarkCompleted();
    }
}
