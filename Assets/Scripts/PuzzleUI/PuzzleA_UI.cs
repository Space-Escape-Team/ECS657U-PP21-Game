using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleA_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("Panels (parents containing the node buttons)")]
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;

    [Header("Wires")]
    [SerializeField] private RectTransform wiresParent;
    [SerializeField] private RectTransform wirePrefab;

    [Header("Pairs")]
    [SerializeField] private int pairCount = 4;

    [SerializeField]
    private Color[] idColors =
    {
        new Color(0.95f, 0.30f, 0.30f),
        new Color(0.30f, 0.60f, 0.95f),
        new Color(0.35f, 0.90f, 0.35f),
        new Color(0.95f, 0.85f, 0.30f),
    };

    private readonly List<Button> leftNodes = new();
    private readonly List<Button> rightNodes = new();

    private readonly Dictionary<Button, int> leftId = new();
    private readonly Dictionary<Button, int> rightId = new();

    private readonly Dictionary<Button, Button> connections = new();
    private readonly Dictionary<Button, RectTransform> wires = new();

    private Button selectedLeft;

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();
    }

    public void ResetPuzzle()
    {
        selectedLeft = null;
        CacheNodes();
        AssignIdsAndColors();
        ShuffleRightSidePositions();
        ClearConnections();
    }

    private void OnDisable()
    {
        selectedLeft = null;
        ClearConnections();
    }

    private void Update()
    {
        foreach (var kv in connections)
        {
            Button left = kv.Key;
            Button right = kv.Value;

            if (left == null || right == null) continue;
            if (!wires.TryGetValue(left, out var wire) || wire == null) continue;

            UpdateWireTransform(wire, left.GetComponent<RectTransform>(), right.GetComponent<RectTransform>());
        }
    }

    private void CacheNodes()
    {
        leftNodes.Clear();
        rightNodes.Clear();
        leftId.Clear();
        rightId.Clear();

        for (int i = 0; i < leftPanel.childCount; i++)
        {
            var btn = leftPanel.GetChild(i).GetComponent<Button>();
            if (btn != null) leftNodes.Add(btn);
        }

        for (int i = 0; i < rightPanel.childCount; i++)
        {
            var btn = rightPanel.GetChild(i).GetComponent<Button>();
            if (btn != null) rightNodes.Add(btn);
        }

        pairCount = Mathf.Min(pairCount, leftNodes.Count, rightNodes.Count);

        for (int i = 0; i < leftNodes.Count; i++)
        {
            int idx = i;
            leftNodes[i].onClick.RemoveAllListeners();
            leftNodes[i].onClick.AddListener(() => OnLeftClicked(leftNodes[idx]));
        }

        for (int i = 0; i < rightNodes.Count; i++)
        {
            int idx = i;
            rightNodes[i].onClick.RemoveAllListeners();
            rightNodes[i].onClick.AddListener(() => OnRightClicked(rightNodes[idx]));
        }
    }

    private void AssignIdsAndColors()
    {

        for (int i = 0; i < leftNodes.Count; i++)
        {
            var btn = leftNodes[i];
            int id = i < pairCount ? i : -1;
            leftId[btn] = id;
            SetNodeColor(btn, GetColorForId(id));
        }

        for (int i = 0; i < rightNodes.Count; i++)
        {
            var btn = rightNodes[i];
            int id = i < pairCount ? i : -1;
            rightId[btn] = id;
            SetNodeColor(btn, GetColorForId(id));
        }
    }

    private void ShuffleRightSidePositions()
    {

        for (int i = 0; i < rightPanel.childCount; i++)
        {
            int j = Random.Range(i, rightPanel.childCount);
            rightPanel.GetChild(j).SetSiblingIndex(i);
        }
    }

    private void OnLeftClicked(Button left)
    {
        selectedLeft = left;
    }

    private void OnRightClicked(Button right)
    {
        if (selectedLeft == null) return;

        Connect(selectedLeft, right);
        selectedLeft = null;

        if (CheckSolved())
            puzzlePanel?.MarkCompleted();
    }

    private void Connect(Button left, Button right)
    {
        if (connections.TryGetValue(left, out var oldRight))
        {
            connections.Remove(left);

            if (wires.TryGetValue(left, out var oldWire) && oldWire != null)
                Destroy(oldWire.gameObject);

            wires.Remove(left);
        }

        connections[left] = right;

        RectTransform wire = Instantiate(wirePrefab, wiresParent);
        wire.gameObject.SetActive(true);

        var img = wire.GetComponent<Image>();
        if (img != null)
            img.color = GetColorForId(leftId[left]);

        wires[left] = wire;

        UpdateWireTransform(wire, left.GetComponent<RectTransform>(), right.GetComponent<RectTransform>());
    }

    private void ClearConnections()
    {
        connections.Clear();

        foreach (var kv in wires)
        {
            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
        }
        wires.Clear();
    }

    private bool CheckSolved()
    {
        int connected = 0;

        foreach (var left in leftNodes)
        {
            if (!leftId.TryGetValue(left, out int id)) continue;
            if (id < 0 || id >= pairCount) continue;

            connected++;

            if (!connections.TryGetValue(left, out var right)) return false;
            if (!rightId.TryGetValue(right, out int rid)) return false;

            if (id != rid) return false;
        }

        return connected == pairCount;
    }

    private Color GetColorForId(int id)
    {
        if (id < 0) return Color.gray;
        if (idColors != null && id < idColors.Length) return idColors[id];
        return Color.white;
    }

    private void SetNodeColor(Button btn, Color c)
    {
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private void UpdateWireTransform(RectTransform wire, RectTransform a, RectTransform b)
    {
        Vector2 aLocal = WorldCenterToLocal(a, wiresParent);
        Vector2 bLocal = WorldCenterToLocal(b, wiresParent);

        Vector2 dir = bLocal - aLocal;
        float length = dir.magnitude;

        wire.anchoredPosition = aLocal;
        wire.sizeDelta = new Vector2(length, wire.sizeDelta.y);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        wire.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private Vector2 WorldCenterToLocal(RectTransform rt, RectTransform targetSpace)
    {
        Vector3 world = rt.TransformPoint(rt.rect.center);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetSpace, screen, null, out var local);
        return local;
    }
}
