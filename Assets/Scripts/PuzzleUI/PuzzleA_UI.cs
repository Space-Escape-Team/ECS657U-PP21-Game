using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PuzzleA_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("UI Parents (under this PuzzleA_UI root)")]
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;

    [Header("Pairs")]
    [SerializeField] private int pairCount = 4;

    private readonly List<Button> leftButtons = new();
    private readonly List<Button> rightButtons = new();
    private readonly Dictionary<Button, int> leftId = new();
    private readonly Dictionary<Button, int> rightId = new();

    private Button selectedLeft;
    private int correctConnections;

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();
    }

    public void ResetPuzzle()
    {
        selectedLeft = null;
        correctConnections = 0;

        ClearChildren(leftPanel);
        ClearChildren(rightPanel);

        leftButtons.Clear();
        rightButtons.Clear();
        leftId.Clear();
        rightId.Clear();

        BuildNodes();
    }

    private void BuildNodes()
    {
        // IDs: 0..pairCount-1
        List<int> ids = new();
        for (int i = 0; i < pairCount; i++) ids.Add(i);

        // Shuffle right-side ids
        List<int> shuffled = new List<int>(ids);
        Shuffle(shuffled);

        for (int i = 0; i < pairCount; i++)
        {
            Button lb = MakeUIButton(leftPanel, $"L{i + 1}");
            Button rb = MakeUIButton(rightPanel, $"R{i + 1}");

            leftButtons.Add(lb);
            rightButtons.Add(rb);

            leftId[lb] = ids[i];
            rightId[rb] = shuffled[i];

            int li = i;
            lb.onClick.AddListener(() => OnLeftClicked(leftButtons[li]));

            int ri = i;
            rb.onClick.AddListener(() => OnRightClicked(rightButtons[ri]));
        }
    }

    private Button MakeUIButton(Transform parent, string label)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 50);

        Image img = go.GetComponent<Image>();
        img.color = Color.white;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(go.transform, false);

        Text txt = textGO.GetComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.color = Color.black;

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }

    private void OnLeftClicked(Button b) => selectedLeft = b;

    private void OnRightClicked(Button b)
    {
        if (selectedLeft == null) return;

        bool correct = leftId[selectedLeft] == rightId[b];
        selectedLeft = null;

        if (correct)
        {
            correctConnections++;
            if (correctConnections >= pairCount)
                puzzlePanel?.MarkCompleted();
        }
        else
        {
            // wrong resets puzzle
            ResetPuzzle();
        }
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
