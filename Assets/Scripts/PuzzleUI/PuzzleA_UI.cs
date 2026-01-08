using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleA_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel (optional in debug)")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("Parents containing LEFT and RIGHT node buttons")]
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;

    [Header("Wires")]
    [SerializeField] private RectTransform wiresParent;
    [SerializeField] private RectTransform wirePrefab;

    [Header("Pairs")]
    [SerializeField] private int pairCount = 4;

    [Header("ID -> Color (index = ID)")]
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
    private readonly Dictionary<Button, RectTransform> connectionWires = new();

    private Button selectedLeft;

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();
    }

    private void OnEnable()
    {
        ResetPuzzle();
    }

    private void OnDisable()
    {
        selectedLeft = null;
        ClearConnections();
    }

    public void ResetPuzzle()
    {
        if (!ValidateReferences())
            return;

        CacheNodes();
        AssignIdsAndColors();
        ShuffleRightSidePositions();
        ClearConnections();
        selectedLeft = null;
    }

    private bool ValidateReferences()
    {
        if (leftPanel == null || rightPanel == null)
        {
            Debug.LogError("PuzzleA_UI: LeftPanel or RightPanel is not assigned.", this);
            return false;
        }
        if (wiresParent == null || wirePrefab == null)
        {
            Debug.LogError("PuzzleA_UI: WiresParent or WirePrefab is not assigned.", this);
            return false;
        }

        var img = wirePrefab.GetComponent<Image>();
        if (img == null)
        {
            Debug.LogError("PuzzleA_UI: WirePrefab must have an Image component.", this);
            return false;
        }

        if (wirePrefab.pivot.x > 0.01f || Mathf.Abs(wirePrefab.pivot.y - 0.5f) > 0.01f)
            Debug.LogWarning("PuzzleA_UI: WirePrefab pivot should be (0, 0.5) for correct wire placement.", this);

        return true;
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
            leftNodes[idx].onClick.RemoveAllListeners();
            leftNodes[idx].onClick.AddListener(() => OnLeftClicked(leftNodes[idx]));
        }

        for (int i = 0; i < rightNodes.Count; i++)
        {
            int idx = i;
            rightNodes[idx].onClick.RemoveAllListeners();
            rightNodes[idx].onClick.AddListener(() => OnRightClicked(rightNodes[idx]));
        }
    }

    private void AssignIdsAndColors()
    {
        for (int i = 0; i < leftNodes.Count; i++)
        {
            int id = (i < pairCount) ? i : -1;
            leftId[leftNodes[i]] = id;
            SetNodeColor(leftNodes[i], GetColorForId(id));
        }

        for (int i = 0; i < rightNodes.Count; i++)
        {
            int id = (i < pairCount) ? i : -1;
            rightId[rightNodes[i]] = id;
            SetNodeColor(rightNodes[i], GetColorForId(id));
        }
    }

    private void ShuffleRightSidePositions()
    {
        if (rightNodes.Count <= 1) return;

        List<int> original = new List<int>(rightNodes.Count);
        for (int i = 0; i < rightNodes.Count; i++)
            original.Add(rightId.TryGetValue(rightNodes[i], out int id) ? id : -999);

        const int maxAttempts = 20;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            for (int i = 0; i < rightPanel.childCount; i++)
            {
                int j = Random.Range(i, rightPanel.childCount);
                rightPanel.GetChild(j).SetSiblingIndex(i);
            }

            List<int> current = new List<int>(rightNodes.Count);
            for (int i = 0; i < rightPanel.childCount; i++)
            {
                var btn = rightPanel.GetChild(i).GetComponent<Button>();
                if (btn == null) continue;
                current.Add(rightId.TryGetValue(btn, out int id) ? id : -999);
            }

            if (!IsSameOrder(original, current))
                return;
        }

        if (rightPanel.childCount >= 2)
        {
            Transform a = rightPanel.GetChild(0);
            Transform b = rightPanel.GetChild(1);
            int aIndex = a.GetSiblingIndex();
            int bIndex = b.GetSiblingIndex();
            a.SetSiblingIndex(bIndex);
            b.SetSiblingIndex(aIndex);
        }
    }

    private bool IsSameOrder(List<int> a, List<int> b)
    {
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }


    private void OnLeftClicked(Button left)
    {
        selectedLeft = (selectedLeft == left) ? null : left;

        for (int i = 0; i < leftNodes.Count; i++)
        {
            var o = leftNodes[i].GetComponent<Outline>();
            if (o) o.enabled = (leftNodes[i] == selectedLeft);
        }
    }

    private void OnRightClicked(Button right)
    {
        if (selectedLeft == null) return;

        Connect(selectedLeft, right);
        selectedLeft = null;

        if (IsSolved())
            PuzzleUIDebugLauncher.Instance?.NotifyPuzzleSolved("PuzzleA");
            puzzlePanel?.MarkCompleted();
    }

    private void Connect(Button left, Button right)
    {
        if (connections.TryGetValue(left, out var oldRight))
        {
            connections.Remove(left);

            if (connectionWires.TryGetValue(left, out var oldWire) && oldWire != null)
                Destroy(oldWire.gameObject);

            connectionWires.Remove(left);
        }

        connections[left] = right;

        RectTransform wire = Instantiate(wirePrefab, wiresParent);
        wire.gameObject.SetActive(true);

        var img = wire.GetComponent<Image>();
        if (img != null && leftId.TryGetValue(left, out int lid))
            img.color = GetColorForId(lid);

        connectionWires[left] = wire;

        UpdateWireTransform(wire, left.GetComponent<RectTransform>(), right.GetComponent<RectTransform>());
    }

    private void ClearConnections()
    {
        connections.Clear();

        foreach (var kv in connectionWires)
            if (kv.Value != null) Destroy(kv.Value.gameObject);

        connectionWires.Clear();
    }

    private void Update()
    {
        foreach (var kv in connections)
        {
            var left = kv.Key;
            var right = kv.Value;
            if (left == null || right == null) continue;

            if (!connectionWires.TryGetValue(left, out var wire) || wire == null) continue;

            UpdateWireTransform(wire, left.GetComponent<RectTransform>(), right.GetComponent<RectTransform>());
        }
    }

    private bool IsSolved()
    {
        int needed = 0;

        foreach (var left in leftNodes)
        {
            if (!leftId.TryGetValue(left, out int lid)) continue;
            if (lid < 0 || lid >= pairCount) continue;

            needed++;

            if (!connections.TryGetValue(left, out var right)) return false;
            if (!rightId.TryGetValue(right, out int rid)) return false;

            if (lid != rid) return false;
        }

        return needed == pairCount;
    }

    private void SetNodeColor(Button btn, Color c)
    {
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = c;
            return;
        }

        var childImg = btn.GetComponentInChildren<Image>();
        if (childImg != null) childImg.color = c;
    }

    private Color GetColorForId(int id)
    {
        if (id < 0) return new Color(0.5f, 0.5f, 0.5f, 1f);
        if (idColors != null && id < idColors.Length) return idColors[id];
        return Color.white;
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
