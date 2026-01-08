using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleB_UI : MonoBehaviour, IPuzzleUI
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

        for (int i = 0; i < sequenceLength; i++)
            sequence.Add(Random.Range(0, buttons.Length));

        foreach (var b in buttons)
            if (b != null) b.interactable = true;

        UpdatePips();
    }

    private void Press(int id)
    {
        if (sequence.Count == 0) return;
        if (id < 0 || id >= buttons.Length) return;

        if (id == sequence[index])
        {
            StartCoroutine(Flash(buttons[id], Color.green));

            index++;
            UpdatePips();

            if (index >= sequence.Count)
            {
                puzzlePanel?.MarkCompleted();
            }
        }
        else
        {
            StartCoroutine(Flash(buttons[id], Color.red));
            ResetPuzzle();
        }
    }

    private void UpdatePips()
    {
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
        if (b == null) yield break;

        var img = b.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        img.color = c;
        yield return new WaitForSeconds(flashTime);
        img.color = original;
    }
}
