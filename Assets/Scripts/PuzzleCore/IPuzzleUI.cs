public interface IPuzzleUI
{
    /// Reset all puzzle progress/state.
    /// Called on open and on close to guarantee "resets when leaving".
    void ResetPuzzle();
}
