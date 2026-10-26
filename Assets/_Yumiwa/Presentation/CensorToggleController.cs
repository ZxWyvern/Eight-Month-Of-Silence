using UnityEngine;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Prototype censor toggle (placeholder for Settings integration).
    /// Press K to toggle ICensorService.Enabled. Style/replace manually later.
    /// </summary>
    public sealed class CensorToggleController : MonoBehaviour
    {
        private ICensorService censor;
        private IPlayerInput playerInput;

        [Inject]
        public void Construct(ICensorService censor, IPlayerInput playerInput)
        {
            this.censor = censor;
            this.playerInput = playerInput;
        }

        private void OnEnable()
        {
            if (playerInput != null)
                playerInput.CensorPressed += OnToggle;
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.CensorPressed -= OnToggle;
        }

        private void OnToggle()
        {
            censor.SetEnabled(!censor.Enabled);
            Debug.Log($"[Yumiwa] Censor toggled: {censor.Enabled}", this);
        }
    }
}