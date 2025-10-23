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
    [SerializeField] private Transform wiresParent;
    [SerializeField] private LineRenderer wirePrefab;

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
        rightNodes.Shuffle();

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

    private void ConnectNodes(Image left, Image right)
    {
        connections.Add((left, right));

        LineRenderer wire = Instantiate(wirePrefab, wiresParent);
        wire.positionCount = 2;
        wire.SetPosition(0, left.transform.position);
        wire.SetPosition(1, right.transform.position);
        wire.startWidth = 0.05f;
        wire.endWidth = 0.05f;
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
