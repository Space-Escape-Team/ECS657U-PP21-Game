using UnityEngine;
using UnityEngine.UI;

/// Slider alignment puzzle UI.
/// Each slider has a hidden target value in [0,1]; indicators turn green when within tolerance.
/// Puzzle is solved when all sliders are within tolerance, at which point input is locked and completion is reported.
public class PuzzleC_UI : MonoBehaviour, IPuzzleUI,IPuzzlePanelBindable
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

    public void BindPanel(PuzzlePanel_script panel)
    {
        puzzlePanel = panel;
    }

    private void Awake()
    {
        /// Cache panel reference and bind slider change callbacks.
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        HookSliders();
    }

    private void OnEnable()
    {
        /// Refresh indicator state when reopening an unsolved puzzle.
        if (!solved)
            UpdateAllIndicators();
    }

    private void HookSliders()
    {
        if (sliders == null) return;

        /// Bind each slider to OnSliderChanged(index) and clear existing listeners.
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
        /// Generate new random targets, reset sliders to midpoint, and re-enable interaction.
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
        /// Update indicator for this slider and re-check solve state.
        if (solved) return;

        UpdateIndicator(idx);
        CheckSolved();
    }

    private void UpdateAllIndicators()
    {
        /// Refresh all indicator colors based on current slider values.
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
        /// Solved when all sliders are within tolerance of their targets.
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
