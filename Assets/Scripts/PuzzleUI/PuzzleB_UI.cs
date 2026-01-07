using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PuzzleB_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("Buttons (size 4)")]
    [SerializeField] private Button[] buttons = new Button[4];

    [Header("Sequence")]
    [SerializeField] private int sequenceLength = 4;

    private List<int> sequence = new();
    private int index = 0;

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        WireButtons();
    }

    private void WireButtons()
    {
        if (buttons == null) return;

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
        index = 0;
        sequence.Clear();

        // New sequence each open/close (reset spec)
        for (int i = 0; i < sequenceLength; i++)
            sequence.Add(Random.Range(0, buttons.Length));

        foreach (var b in buttons)
            if (b != null) b.interactable = true;
    }

    private void Press(int id)
    {
        if (sequence.Count == 0) return;

        if (id == sequence[index])
        {
            index++;
            if (index >= sequence.Count)
                puzzlePanel?.MarkCompleted();
        }
        else
        {
            // wrong resets
            ResetPuzzle();
        }
    }
}
