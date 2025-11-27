using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

// Create a puzzle to match up same-coloured pairs of randomly generated left and right nodes
public class PuzzleA_UI : MonoBehaviour
{
    [Header("Puzzle References")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;
    [SerializeField] private RectTransform wiresParent;
    [SerializeField] private RectTransform wirePrefab;

    private Controls controls;
    private bool puzzleActive = false;

    private readonly List<Image> leftNodes = new ();
    private readonly List<Image> rightNodes = new ();

    private Image selectedLeftNode;
    private readonly List<(Image left, Image right)> connections = new ();

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        controls = new Controls();
        controls.Gameplay.Click.performed += ctx => HandleClick();
        controls.Gameplay.Cancel.performed += ctx => ClosePuzzle();
    }

    private void OnEnable()
    {
        controls.Enable();
        InitPuzzle();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void InitPuzzle()
    {
        // Clear potential previous lists
        leftNodes.Clear();
        rightNodes.Clear();
        connections.Clear();

        // Create images (based on coloured nodes in hierarchy) to act as the left and right nodes
        foreach (Transform child in leftPanel)
        {
            Image node = child.GetComponent<Image>();
            if (node != null) leftNodes.Add(node);
        }

        foreach (Transform child in rightPanel)
        {
            Image node = child.GetComponent<Image>();
            if (node != null) rightNodes.Add(node);
        }

        // Shuffle right nodes for random placement
        ShuffleRightNodes();

        // Leads to HandleClick() call
        puzzleActive = true;
    }

    private void HandleClick()
    {
        if (!puzzleActive) return;

        // Upon click, read mouse position and use Raycast to return UI elements under the cursor
        Vector2 mousePos = Mouse.current.position.ReadValue();
        var raycastResults = new List<UnityEngine.EventSystems.RaycastResult>();
        var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = mousePos
        };
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (var result in raycastResults)
        {
            // Checks if an image exists in what was clicked
            Image clickedNode = result.gameObject.GetComponent<Image>();
            if (clickedNode == null) continue; // Skips if no image

            if (leftNodes.Contains(clickedNode))
            {
                selectedLeftNode = clickedNode; // Clicked node is considered selected
                return;
            }
            // If clicked node is a right node and there is currently a selected left node, connect them and check solved status
            else if (rightNodes.Contains(clickedNode) && selectedLeftNode != null)
            {
                ConnectNodes(selectedLeftNode, clickedNode);
                selectedLeftNode = null;

                if (CheckIfSolved())
                    SolvePuzzle();
                return;
            }
        }
    }
   
    private Vector2 ScreenToLocal(RectTransform parent, Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            screenPos,
            null,                 // canvas using Screen Space Overlay
            out Vector2 localPoint);

        return localPoint;
    }

    private void ConnectNodes(Image left, Image right)
    {
        RectTransform wire = Instantiate(wirePrefab, wiresParent);

        // Get screen space positions of the UI nodes
        Vector2 leftScreen = GetNodeScreenCenter(left.rectTransform);
        Vector2 rightScreen = GetNodeScreenCenter(right.rectTransform);

        // Find local versions of those positions
        Vector2 leftLocal = ScreenToLocal(wiresParent, leftScreen);
        Vector2 rightLocal = ScreenToLocal(wiresParent, rightScreen);

        Vector2 diff = rightLocal - leftLocal; // Vector between left and right nodes being connected

        // Set wire's width to the distance between nodes and height to appropriate thickness value
        float thickness = 3f;
        wire.sizeDelta = new Vector2(diff.magnitude, thickness);

        wire.anchoredPosition = leftLocal; // Lines up anchor with left node (wire prefab pivot set to left-centre)

        // Find angle of direction vector relative to x axis ( arctan(diff.x / diff.y) ) 
        // Atan2: arctan, but handles divide by 0 case and covers -180 to 180 degrees
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        wire.localRotation = Quaternion.Euler(0, 0, angle); // Sets wire rotation to this angle to visually connect nodes correctly

        connections.Add((left, right));
    }

    private Vector2 GetNodeScreenCenter(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners); // Returns world space co-ordinates of rect's corners

        // Find centre point: average of bottom-left (index 0) and top-right (index 2)
        return (corners[0] + corners[2]) * 0.5f;
    }

    private void ShuffleRightNodes()
    {
        rightNodes.Shuffle(); // Uses ListExtensions Shuffle() method

        // Re order node indices to ensure the UI visually reflects the shuffle
        for (int i = 0; i < rightNodes.Count; i++)
            rightNodes[i].transform.SetSiblingIndex(i);
    }

    private bool CheckIfSolved()
    {
        // Solved if every left node is connected to the matching-colored right node
        foreach (var pair in connections)
        {
            if (pair.left.name.Replace("LeftNode_", "") != pair.right.name.Replace("RightNode_", ""))
                return false;
        }

        return connections.Count == leftNodes.Count;
    }

    private void SolvePuzzle()
    {
        puzzleActive = false;
        if (puzzlePanel != null)
            puzzlePanel.MarkCompleted();

        Debug.Log("Puzzle A solved!");
    }

    private void ClosePuzzle()
    {
        puzzleActive = false;
        if (puzzlePanel != null)
            puzzlePanel.TryClosePuzzle();
    }
}
