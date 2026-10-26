using UnityEngine;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Prototype hotspot presenter — angle-gated examine per GDD §03.
    /// Hotspot only valid at yaw/pitch/distance; progress survives Cancel.
    /// </summary>
    public sealed class InspectionHotspotPresenter : MonoBehaviour
    {
        private InspectionSessionService session;
        private InspectionRotationModel rotation;

        [Inject]
        public void Construct(InspectionSessionService session, InspectionRotationModel rotation = null)
        {
            this.session = session;
            this.rotation = rotation;
        }

        public void SetRotationModel(InspectionRotationModel model) => rotation = model;

        public bool TryExamine(string hotspotId)
        {
            if (session == null || rotation == null)
            {
                Debug.LogWarning("[Yumiwa] Hotspot examine: no session/rotation.", this);
                return false;
            }
            bool ok = session.TryExamineHotspot(hotspotId, rotation);
            Debug.Log(ok ? $"[Yumiwa] Hotspot '{hotspotId}' discovered." : $"[Yumiwa] Hotspot '{hotspotId}' not valid at current angle.", this);
            return ok;
        }

        /// <summary>Examines the first hotspot that is valid at the current rotation.</summary>
        public bool ExamineAnyValid()
        {
            if (session == null || rotation == null || !session.IsActive)
            {
                Debug.LogWarning("[Yumiwa] ExamineAnyValid: no session/rotation or not active.", this);
                return false;
            }
            var id = session.TryExamineFirstValid(rotation);
            if (id != null)
            {
                Debug.Log($"[Yumiwa] Hotspot '{id}' discovered via ExamineAnyValid.", this);
                return true;
            }
            Debug.Log("[Yumiwa] No valid hotspot at current angle.", this);
            return false;
        }
    }
}
