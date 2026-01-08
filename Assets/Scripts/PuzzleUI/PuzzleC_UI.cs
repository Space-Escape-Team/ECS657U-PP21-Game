using UnityEngine;
using UnityEngine.UI;

public class PuzzleC_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("Sliders (size 3)")]
    [SerializeField] private Slider[] sliders = new Slider[3];

    [Header("Indicators (size 3) - placed to RIGHT of each slider row")]
    [SerializeField] private Image[] indicators = new Image[3];

    [Header("Tolerance")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float tolerance = 0.05f;

    private float[] targets = new float[3];

    private static readonly Color IndicatorOff = new Color(0.70f, 0.70f, 0.70f, 1f);

    private bool solved = false;

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        HookSliders();
    }

    private void OnEnable()
    {

        if (!solved)
            UpdateAllIndicators();
    }

    private void HookSliders()
    {
        if (sliders == null) return;

        for (int i = 0; i < sliders.Length; i++)
        {
            int idx = i;
            if (sliders[idx] == null) continue;

            sliders[idx].onValueChanged.RemoveAllListeners();
            sliders[idx].onValueChanged.AddListener(_ => OnSliderChanged(idx));
        }
    }

    public void ResetPuzzle()
    {
        solved = false;

        for (int i = 0; i < 3; i++)
            targets[i] = Random.value;

        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] == null) continue;
            sliders[i].interactable = true;
            sliders[i].SetValueWithoutNotify(0.5f);
        }

        UpdateAllIndicators();
        CheckSolved();
    }

    private void OnSliderChanged(int idx)
    {
        if (solved) return;

        UpdateIndicator(idx);
        CheckSolved();
    }

    private void UpdateAllIndicators()
    {
        for (int i = 0; i < 3; i++)
            UpdateIndicator(i);
    }

    private void UpdateIndicator(int i)
    {
        if (sliders == null || indicators == null) return;
        if (i < 0 || i >= sliders.Length || i >= indicators.Length) return;
        if (sliders[i] == null || indicators[i] == null) return;

        bool ok = Mathf.Abs(sliders[i].value - targets[i]) <= tolerance;
        indicators[i].color = ok ? Color.green : IndicatorOff;
    }

    private void CheckSolved()
    {
        if (solved) return;

        for (int i = 0; i < 3; i++)
        {
            if (sliders[i] == null) return;
            if (Mathf.Abs(sliders[i].value - targets[i]) > tolerance) return;
        }

        solved = true;

        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] == null) continue;
            sliders[i].interactable = false;
        }

        UpdateAllIndicators();
        Debug.Log("[PUZZLE SOLVED] PuzzleC solved", this);
        puzzlePanel?.MarkCompleted();
        PuzzleUIDebugLauncher.Instance?.NotifyPuzzleSolved("PuzzleC");
    }
}
