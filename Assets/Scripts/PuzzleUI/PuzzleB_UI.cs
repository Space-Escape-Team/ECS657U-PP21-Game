using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 4-button sequence memory puzzle UI.
/// Generates a random sequence of button indices; player must press buttons in order.
/// Correct press flashes green and advances progress (optionally shown via pips); wrong press flashes red and resets progress.
/// Completing the full sequence notifies/marks the puzzle panel complete.
public class PuzzleB_UI : MonoBehaviour, IPuzzleUI, IPuzzlePanelBindable
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("Buttons (size 4)")]
    [SerializeField] private Button[] buttons = new Button[4];

    [Header("Sequence")]
    [SerializeField] private int sequenceLength = 4;

    [Header("Optional Progress Pips (size = sequenceLength)")]
    [SerializeField] private Image[] progressPips;

    [Header("Flash Feedback")]
    [SerializeField] private float flashTime = 0.12f;

    private readonly List<int> sequence = new();
    private int index = 0;

    public void BindPanel(PuzzlePanel_script panel)
    {
        puzzlePanel = panel;
    }

    private void Awake()
    {
        /// Cache panel reference and bind button click handlers once.
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        WireButtons();
    }

    private void OnEnable()
    {
        /// Ensure a sequence exists when the UI becomes active; otherwise only reset progress.
        if (sequence.Count == 0)
            ResetPuzzle();
        else
            ResetProgressOnly();
    }

    private void WireButtons()
    {
        if (buttons == null) return;

        /// Bind each button to Press(buttonIndex) and clear existing listeners to avoid duplicates.
        for (int i = 0; i < buttons.Length; i++)
        {
            int id = i;
            if (buttons[i] == null) continue;

            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => Press(id));
        }
    }

    public void ResetPuzzle()
    {
        /// Regenerate the target sequence and reset player progress back to the start.
        GenerateNewSequence();
        ResetProgressOnly();
    }

    private void GenerateNewSequence()
    {
        /// Create a new random sequence of indices in [0, buttons.Length).
        sequence.Clear();

        for (int i = 0; i < sequenceLength; i++)
            sequence.Add(Random.Range(0, buttons.Length));
    }

    private void ResetProgressOnly()
    {
        /// Reset current input position, re-enable buttons, and refresh pip display.
        index = 0;

        foreach (var b in buttons)
            if (b != null) b.interactable = true;

        UpdatePips();
    }

    private void Press(int id)
    {
        /// Validate state and input, then compare against the next required sequence entry.
        if (sequence.Count == 0) return;
        if (id < 0 || id >= buttons.Length) return;

        if (id == sequence[index])
        {
            StartCoroutine(Flash(buttons[id], Color.green));

            index++;
            UpdatePips();

            /// When the full sequence is matched, mark the puzzle as completed.
            if (index >= sequence.Count)
            {
                PuzzleUIDebugLauncher.Instance?.NotifyPuzzleSolved("PuzzleB");
                puzzlePanel?.MarkCompleted();
            }
        }
        else
        {
            /// Wrong input: flash red and reset progress back to the start (sequence stays the same).
            StartCoroutine(Flash(buttons[id], Color.red));
            ResetProgressOnly();
        }
    }

    private void UpdatePips()
    {
        /// Color pips green for completed steps, otherwise a neutral "off" color.
        if (progressPips == null || progressPips.Length == 0) return;

        Color off = new Color(0.70f, 0.70f, 0.70f, 1f);

        for (int i = 0; i < progressPips.Length; i++)
        {
            if (progressPips[i] == null) continue;
            progressPips[i].color = (i < index) ? Color.green : off;
        }
    }

    private IEnumerator Flash(Button b, Color c)
    {
        /// Temporarily swap the button image color for feedback, then restore it after flashTime.
        if (b == null) yield break;

        var img = b.targetGraphic as Image;
        if (img == null) img = b.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        img.color = c;
        yield return new WaitForSeconds(flashTime);
        img.color = original;
    }
}
