using UnityEngine;
using UnityEngine.UI;

public class PuzzleC_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("3 Sliders")]
    [SerializeField] private Slider sliderA;
    [SerializeField] private Slider sliderB;
    [SerializeField] private Slider sliderC;

    [Range(0.01f, 0.2f)]
    [SerializeField] private float tolerance = 0.05f;

    private float targetA, targetB, targetC;

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        if (sliderA) sliderA.onValueChanged.AddListener(_ => CheckSolved());
        if (sliderB) sliderB.onValueChanged.AddListener(_ => CheckSolved());
        if (sliderC) sliderC.onValueChanged.AddListener(_ => CheckSolved());
    }

    public void ResetPuzzle()
    {
        targetA = Random.value;
        targetB = Random.value;
        targetC = Random.value;

        if (sliderA) sliderA.value = 0.5f;
        if (sliderB) sliderB.value = 0.5f;
        if (sliderC) sliderC.value = 0.5f;

        CheckSolved();
    }

    private void CheckSolved()
    {
        if (!sliderA || !sliderB || !sliderC) return;

        bool ok =
            Mathf.Abs(sliderA.value - targetA) <= tolerance &&
            Mathf.Abs(sliderB.value - targetB) <= tolerance &&
            Mathf.Abs(sliderC.value - targetC) <= tolerance;

        if (ok)
            puzzlePanel?.MarkCompleted();
    }
}
