using UnityEngine;
using UnityEngine.UI;

public class PuzzleC_UI : MonoBehaviour, IPuzzleUI
{
    [Header("World Panel")]
    [SerializeField] private PuzzlePanel_script puzzlePanel;

    [Header("Sliders (size 3)")]
    [SerializeField] private Slider[] sliders = new Slider[3];

    [Header("Indicators (size 3) - place to RIGHT of each slider row")]
    [SerializeField] private Image[] indicators = new Image[3];

    [Header("Tolerance")]
    [Range(0.01f, 0.2f)]
    [SerializeField] private float tolerance = 0.05f;

    private float[] targets = new float[3];

    private static readonly Color IndicatorOff = new Color(0.70f, 0.70f, 0.70f, 1f);

    private void Awake()
    {
        if (puzzlePanel == null)
            puzzlePanel = GetComponentInParent<PuzzlePanel_script>();

        if (sliders != null)
        {
            for (int i = 0; i < sliders.Length; i++)
            {
                int idx = i;
                if (sliders[idx] == null) continue;

                sliders[idx].onValueChanged.RemoveAllListeners();
                sliders[idx].onValueChanged.AddListener(_ => OnSliderChanged(idx));
            }
        }
    }

    public void ResetPuzzle()
    {
        for (int i = 0; i < 3; i++)
            targets[i] = Random.value;

        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] == null) continue;
            sliders[i].value = 0.5f;
        }

        UpdateAllIndicators();
        CheckSolved();
    }

    private void OnSliderChanged(int idx)
    {
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
        for (int i = 0; i < 3; i++)
        {
            if (sliders[i] == null) return;
            if (Mathf.Abs(sliders[i].value - targets[i]) > tolerance) return;
        }

        puzzlePanel?.MarkCompleted();
    }
}
