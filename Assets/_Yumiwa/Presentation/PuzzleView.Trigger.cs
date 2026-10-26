using UnityEngine;

namespace Yumiwa.Presentation
{
    public sealed partial class PuzzleView
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerMovementPresenter>(out _))
            {
                if (puzzleService != null && puzzleService.GetState(puzzleId) == Yumiwa.Domain.PuzzleState.Solved)
                {
                    Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' sudah solved — tidak bisa di-interact lagi.", this);
                    return;
                }
                playerInRange = true;
                if (puzzleService != null && !puzzleService.CanAttempt(puzzleId))
                    Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' dekat — butuh evidence/item dulu.", this);
                else
                    Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' dekat — tekan [F] Use untuk membuka.", this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerMovementPresenter>(out _))
            {
                playerInRange = false;
                DestroyPanel();
            }
        }

        private void OnUsePressed()
        {
            if (!playerInRange) return;
            if (puzzleService != null && puzzleService.GetState(puzzleId) == Yumiwa.Domain.PuzzleState.Solved) return;
            if (puzzlePanel != null && puzzlePanel.activeSelf) return;
            TryShowPanel();
        }

        private void OnCancelPressed()
        {
            if (puzzlePanel != null)
                DestroyPanel();
        }

        private void TryShowPanel()
        {
            if (!playerInRange) return;
            if (puzzleService != null && puzzleService.GetState(puzzleId) == Yumiwa.Domain.PuzzleState.Solved) return;
            if (puzzleService == null) return;
            if (puzzlePanel != null && puzzlePanel.activeSelf) return;
            CreatePanel();
            if (!puzzleService.CanAttempt(puzzleId))
            {
                var hint = $"Terkunci — butuh bukti/item dulu. State: {puzzleService.GetState(puzzleId)}";
                Debug.Log($"[Yumiwa] Puzzle '{puzzleId}' requirements not met — panel shown with hint: {hint}", this);
                if (feedbackText != null)
                {
                    feedbackText.text = hint;
                    feedbackText.color = Color.yellow;
                }
            }
        }
    }
}
