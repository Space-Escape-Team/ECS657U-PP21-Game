using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PuzzleA_UI : MonoBehaviour
{
    private PuzzlePanel_script parentPanel;

    [Header("Node Containers")]
    public Transform leftPanel;
    public Transform rightPanel;
    public Transform wireParent;

    [Header("Wire Settings")]
    public Material wireMaterial;
    public float lineWidth = 5f;

    private Dictionary<Image, Image> correctPairs = new Dictionary<Image, Image>();
    private Dictionary<Image, Image> playerConnections = new Dictionary<Image, Image>();

    private Image draggingFrom;
    private LineRenderer currentLine;

    void Awake()
    {
        parentPanel = FindObjectOfType<PuzzlePanel_script>();
    }

    void Start()
    {
        SetupPuzzle();
    }

    // ----------------------------------------------------------------

    public void SetupPuzzle()
    {
        // Clear any previous wires
        foreach (Transform child in wireParent)
            Destroy(child.gameObject);

        playerConnections.Clear();

        // Collect nodes
        List<Image> leftNodes = leftPanel.GetComponentsInChildren<Image>().ToList();
        List<Image> rightNodes = rightPanel.GetComponentsInChildren<Image>().ToList();

        // Randomize right
        rightNodes = rightNodes.OrderBy(x => Random.value).ToList();

        // Assign pairs by colour 
        correctPairs.Clear();
        foreach (var left in leftNodes)
        {
            var match = rightNodes.FirstOrDefault(r => r.name.Contains(left.name.Split('_')[1]));
            correctPairs[left] = match;
        }

        // layout right nodes vertically
        for (int i = 0; i < rightNodes.Count; i++)
        {
            RectTransform rt = rightNodes[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -100 * i);
        }
    }

    void Update()
    {
        if (draggingFrom != null && currentLine != null)
        {
            Vector3 worldPos = Input.mousePosition;
            currentLine.SetPosition(1, worldPos);
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDrag();
        }
        else if (Input.GetMouseButtonUp(0) && draggingFrom != null)
        {
            TryEndDrag();
        }
    }

    void TryBeginDrag()
    {
        // Check clicked a left node
        foreach (Image node in leftPanel.GetComponentsInChildren<Image>())
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                node.rectTransform, Input.mousePosition))
            {
                draggingFrom = node;
                StartNewWire(node);
                break;
            }
        }
    }

    void TryEndDrag()
    {
        foreach (Image node in rightPanel.GetComponentsInChildren<Image>())
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                node.rectTransform, Input.mousePosition))
            {
                ConnectNodes(draggingFrom, node);
                draggingFrom = null;
                currentLine = null;
                CheckCompletion();
                return;
            }
        }

        // If not dropped on valid node, delete line
        Destroy(currentLine.gameObject);
        draggingFrom = null;
        currentLine = null;
    }

    void StartNewWire(Image startNode)
    {
        GameObject lineObj = new GameObject("Wire");
        lineObj.transform.SetParent(wireParent, false);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = wireMaterial;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.useWorldSpace = true;

        Vector3 startPos = startNode.rectTransform.position;
        lr.SetPosition(0, startPos);
        lr.SetPosition(1, startPos);

        currentLine = lr;
    }

    void ConnectNodes(Image from, Image to)
    {
        playerConnections[from] = to;

        currentLine.SetPosition(1, to.rectTransform.position);
    }

    void CheckCompletion()
    {
        if (playerConnections.Count < correctPairs.Count) return;

        bool allCorrect = correctPairs.All(pair =>
            playerConnections.ContainsKey(pair.Key) &&
            playerConnections[pair.Key] == pair.Value);

        if (allCorrect)
        {
            Debug.Log("Puzzle A completed!");
            parentPanel.PuzzleCompleted();
        }
    }

    public void ResetPuzzle()
    {
        foreach (Transform child in wireParent)
            Destroy(child.gameObject);

        playerConnections.Clear();
        SetupPuzzle();
    }

    public void ExitPuzzle()
    {
        parentPanel.ClosePuzzle();
    }
}
