using UnityEngine;

namespace Yumiwa.Presentation
{
    public sealed partial class PuzzleView
    {
        private void OnInputSubmit(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            OnSubmit(text);
        }

        private void OnSubmit(string code)
        {
            if (puzzleService == null) return;
            bool ok = puzzleService.TrySolve(puzzleId, code, out var feedback);
            if (feedbackText != null)
            {
                feedbackText.text = feedback ?? "";
            }
            Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' submit '{code}': {feedback}", this);
        }

        public bool TryInteract()
        {
            if (puzzleService != null && puzzleService.GetState(puzzleId) == Yumiwa.Domain.PuzzleState.Solved)
            {
                Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' sudah solved — TryInteract ditolak.", this);
                return false;
            }
            if (puzzleService == null)
                return false;
            if (!puzzleService.CanAttempt(puzzleId))
            {
                Debug.LogWarning($"[Yumiwa] Puzzle '{puzzleId}' requirements not met — check Evidence Log & inventory.", this);
                return false;
            }
            Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' ready — call Submit(code).", this);
            return true;
        }

        public bool Submit(string code)
        {
            if (puzzleService == null)
                return false;
            bool ok = puzzleService.TrySolve(puzzleId, code, out var feedback);
            Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' submit '{code}': {feedback}", this);
            return ok;
        }

        private void OnSolved(Yumiwa.Application.PuzzleSolvedEvent e)
        {
            if (e.PuzzleId == puzzleId)
            {
                Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' solved — deduction unlocked. Puzzle locked, tidak bisa di-interact lagi.", this);
                ApplyColor(solvedColor);
                failedFlashTimer = 0f;
                if (feedbackText != null)
                    feedbackText.text = "Benar! Deduction unlocked.";
                if (missionFlow != null && missionFlow.CanAdvance)
                {
                    missionFlow.Advance();
                    Debug.Log($"[Yumiwa] Mission advanced via puzzle solve → {missionFlow.Current?.DisplayName} [{missionFlow.CurrentIndex + 1}/{missionFlow.Count}]", this);
                }
                if (puzzlePanel != null && puzzlePanel.activeSelf)
                {
                    StartCoroutine(DestroyPanelAfter(1.5f));
                }
                var col = GetComponent<BoxCollider>();
                if (col != null) col.enabled = false;
                playerInRange = false;
            }
        }

        private void OnFailed(Yumiwa.Application.PuzzleFailedEvent e)
        {
            if (e.PuzzleId == puzzleId)
            {
                Debug.LogWarning($"[Yumiwa] Puzzle '{puzzleId}' failed — retry, no progress lost.", this);
                ApplyColor(failedColor);
                failedFlashTimer = 0.6f;
                if (feedbackText != null)
                    feedbackText.text = $"Salah: {e.Attempt} — coba lagi.";
            }
        }
    }
}
