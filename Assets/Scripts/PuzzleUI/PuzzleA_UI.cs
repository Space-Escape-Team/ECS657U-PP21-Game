using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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

    private List<Image> leftNodes = new List<Image>();
    private List<Image> rightNodes = new List<Image>();

    private Image selectedLeftNode;
    private List<(Image left, Image right)> connections = new List<(Image, Image)>();

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
        leftNodes.Clear();
        rightNodes.Clear();
        connections.Clear();

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

        puzzleActive = true;
    }

    private void HandleClick()
    {
        if (!puzzleActive) return;

        // Raycast into UI for clicked node
        Vector2 mousePos = Mouse.current.position.ReadValue();
        var raycastResults = new List<UnityEngine.EventSystems.RaycastResult>();
        var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = mousePos
        };
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (var result in raycastResults)
        {
            Image clickedNode = result.gameObject.GetComponent<Image>();
            if (clickedNode == null) continue;

            if (leftNodes.Contains(clickedNode))
            {
                selectedLeftNode = clickedNode; // select a left node
                return;
            }
            else if (rightNodes.Contains(clickedNode) && selectedLeftNode != null)
            {
                // Connect left → right
                ConnectNodes(selectedLeftNode, clickedNode);
                selectedLeftNode = null;

                if (CheckIfSolved())
                    SolvePuzzle();
                return;
            }
        }
    }
    private Vector2 WorldToLocalPoint(RectTransform parent, Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out Vector2 localPoint
        );
        return localPoint;
    }

    private Vector2 GetLocalPosition(RectTransform parent, RectTransform child)
    {
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(
        // parent, 
        // RectTransformUtility.WorldToScreenPoint(null, child.position), 
        //null, 
        //out Vector2 localPos
        //);
        //return localPos;

        // For Screen Space - Overlay canvas, we need a different approach
        Vector2 localPoint;

        // Convert the child's position directly to local position of the parent
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            child.position, // Already in screen space for UI elements
            null, // null for Screen Space - Overlay
            out localPoint
        );

        return localPoint;
    }

    //private void ConnectNodes(Image left, Image right)
    //{
    //    // New method to use image instead of line renderer
    //  RectTransform wire = Instantiate(wirePrefab, wiresParent);
    //
    //Vector2 start = GetLocalPosition(wiresParent, left.rectTransform);
    //Vector2 end = GetLocalPosition(wiresParent, right.rectTransform);

    //Vector2 diff = end - start;

    //wire.anchoredPosition = start;
    //wire.sizeDelta = new Vector2(diff.magnitude, 5);

    //float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
    // wire.localRotation = Quaternion.Euler(0, 0, angle);

    //  connections.Add((left, right));

    // }

    private void ConnectNodes(Image left, Image right)
    {
        RectTransform wire = Instantiate(wirePrefab, wiresParent);

        // Reset transform completely
        wire.anchoredPosition = Vector2.zero;
        wire.localRotation = Quaternion.identity;
        wire.localScale = Vector3.one;

        // Get the positions in the canvas space
        Vector2 leftPos = GetAnchoredPositionInCanvas(left.rectTransform);
        Vector2 rightPos = GetAnchoredPositionInCanvas(right.rectTransform);

        // Convert to wiresParent local space
        Vector2 leftLocal = ConvertToParentSpace(wiresParent, leftPos);
        Vector2 rightLocal = ConvertToParentSpace(wiresParent, rightPos);

        Vector2 diff = rightLocal - leftLocal;

        // Position and rotate the wire
        wire.anchoredPosition = leftLocal;
        wire.sizeDelta = new Vector2(diff.magnitude, wire.sizeDelta.y);

        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        wire.localRotation = Quaternion.Euler(0, 0, angle);

        connections.Add((left, right));

        Debug.Log($"Left: {leftPos}, Right: {rightPos}, WirePos: {wire.anchoredPosition}");
    }

    private Vector2 GetAnchoredPositionInCanvas(RectTransform rectTransform)
    {
        // This gets the position relative to its anchor in the canvas
        return rectTransform.anchoredPosition;
    }

    private Vector2 ConvertToParentSpace(RectTransform parent, Vector2 anchoredPosition)
    {
        // For simplicity, return the anchored position directly
        // This assumes wiresParent has anchors at (0,0) and (1,1)
        return anchoredPosition;
    }
    private void ShuffleRightNodes()
    {
        rightNodes.Shuffle();

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
