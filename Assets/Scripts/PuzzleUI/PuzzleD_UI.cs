using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// 3x3 toggle puzzle UI.
/// Generates a hidden target bool[9]; player must match it exactly.
/// When a cell is turned ON it flashes green/red depending on correctness, then auto-resets after resetDelay.
/// Solving locks input, cancels timers, and notifies/marks the puzzle panel complete.
public class PuzzleD_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("Cells (Toggles) ")]
    [SerializeField] private Toggle[] cells = new Toggle[9];

    [Header("How many cells in the hidden target should be ON")]
    [Range(1, 9)]
    [SerializeField] private int targetOnCount = 4;

    [Header("Timed reset")]
    [SerializeField] private float resetDelay = 1.25f;

    private bool[] target;
    private static readonly Color BaseColor = HexToColor("585858");
    private bool solved = false;
    private Coroutine[] resetCoroutines;

    private void Awake()
    {
        /// Cache panel reference, allocate timers, and bind toggle callbacks.
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        resetCoroutines = new Coroutine[9];
        HookCells();
    }

    private void HookCells()
    {
        if (cells == null) return;

        /// Disable toggle transitions and bind each toggle to OnCellChanged(index, isOn).
        for (int i = 0; i < cells.Length; i++)
        {
            int idx = i;
            if (cells[idx] == null) continue;

            cells[idx].transition = Selectable.Transition.None;
            cells[idx].onValueChanged.RemoveAllListeners();
            cells[idx].onValueChanged.AddListener(isOn => OnCellChanged(idx, isOn));
        }
    }

    public void ResetPuzzle()
    {
        /// Stop all pending per-cell reset timers, generate a new random target, and reset UI to all OFF.
        solved = false;

        if (cells == null || cells.Length != 9)
        {
            Debug.LogWarning("PuzzleD_UI: Cells must be size 9.");
            return;
        }

        if (resetCoroutines == null || resetCoroutines.Length != 9)
            resetCoroutines = new Coroutine[9];

        for (int i = 0; i < 9; i++)
        {
            if (resetCoroutines[i] != null)
            {
                StopCoroutine(resetCoroutines[i]);
                resetCoroutines[i] = null;
            }
        }

        target = new bool[9];

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

        for (int i = 0; i < 9; i++)
        {
            cells[i].SetIsOnWithoutNotify(false);
            SetCellColor(i, BaseColor);
            cells[i].interactable = true;
        }
    }

    private void OnCellChanged(int idx, bool isOn)
    {
        /// OFF: cancel timer + restore base color. ON: color by correctness + start timer. If solved: lock and notify.
        if (solved) return;
        if (target == null || target.Length != 9) return;

        if (!isOn)
        {
            CancelTimer(idx);
            SetCellColor(idx, BaseColor);
            return;
        }

        SetCellColor(idx, target[idx] ? Color.green : Color.red);

        CancelTimer(idx);
        resetCoroutines[idx] = StartCoroutine(AutoResetCell(idx));

        if (IsSolved())
        {
            solved = true;

            for (int i = 0; i < 9; i++)
            {
                cells[i].interactable = false;
                CancelTimer(i);
            }

            PuzzleUIDebugLauncher.Instance?.NotifyPuzzleSolved("PuzzleD");
            puzzlePanel?.MarkCompleted();
        }
    }

    private IEnumerator AutoResetCell(int idx)
    {
        /// After resetDelay, force the cell back to OFF unless the puzzle was solved.
        yield return new WaitForSeconds(resetDelay);

        if (solved) yield break;
        if (idx < 0 || idx >= cells.Length || cells[idx] == null) yield break;

        cells[idx].SetIsOnWithoutNotify(false);
        SetCellColor(idx, BaseColor);
        resetCoroutines[idx] = null;
    }

    private void CancelTimer(int idx)
    {
        if (idx < 0 || idx >= resetCoroutines.Length) return;

        if (resetCoroutines[idx] != null)
        {
            StopCoroutine(resetCoroutines[idx]);
            resetCoroutines[idx] = null;
        }
    }

    private bool IsSolved()
    {
        for (int i = 0; i < 9; i++)
        {
            if (cells[i] == null) return false;
            if (cells[i].isOn != target[i]) return false;
        }
        return true;
    }

    private void SetCellColor(int idx, Color c)
    {
        /// Apply color to Toggle targetGraphic if present, otherwise fallback to an Image on the Toggle.
        if (idx < 0 || idx >= cells.Length) return;
        var t = cells[idx];
        if (t == null) return;

        if (t.targetGraphic is Graphic g)
        {
            g.color = c;
            return;
        }

        var img = t.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private static Color HexToColor(string hex)
    {
        /// Convert 6-digit RGB hex to Color; returns gray on invalid input.
        if (string.IsNullOrEmpty(hex)) return Color.gray;
        if (hex[0] == '#') hex = hex.Substring(1);
        if (hex.Length != 6) return Color.gray;

        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
