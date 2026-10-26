using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Prototype Evidence Log HUD per GDD §03 — Facts / Leads / Unresolved.
    /// Prototype lists all evidence; production will separate categories.
    /// Refreshes on both Take pickups and hotspot examinations (EvidenceAddedEvent).
    /// </summary>
    public sealed class EvidenceLogPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text hudText;

        private IEvidenceLog log;
        private IEventBus bus;

        [Inject]
        public void Construct(IEvidenceLog log, IEventBus bus)
        {
            this.log = log;
            this.bus = bus;
        }

        private void Awake()
        {
            if (hudText == null) hudText = GetComponentInChildren<TMP_Text>(true);
        }

        private void OnEnable()
        {
            bus?.Subscribe<ItemPickedUpEvent>(OnEvidenceAdded);
            bus?.Subscribe<EvidenceAddedEvent>(OnEvidenceLogged);
            RefreshHud();
        }

        private void OnDisable()
        {
            bus?.Unsubscribe<ItemPickedUpEvent>(OnEvidenceAdded);
            bus?.Unsubscribe<EvidenceAddedEvent>(OnEvidenceLogged);
        }

        private void OnEvidenceAdded(ItemPickedUpEvent e)
        {
            Debug.Log($"[Yumiwa] EvidenceLog: new fact for '{e.ItemId}' — check Log UI.", this);
            RefreshHud();
        }

        private void OnEvidenceLogged(EvidenceAddedEvent e)
        {
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (hudText == null) return;
            var all = GetAll();
            if (all.Count == 0) { hudText.text = "Evidence Log: (empty)"; return; }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Evidence Log [{all.Count}]");
            foreach (var ev in all) sb.AppendLine($"- {ev.EvidenceId} [{ev.Category}]");
            hudText.text = sb.ToString();
        }

        public IReadOnlyList<Evidence> GetFacts() => log?.GetEvidenceByCategory(EvidenceCategory.Fact) ?? new List<Evidence>();
        public IReadOnlyList<Evidence> GetLeads() => log?.GetEvidenceByCategory(EvidenceCategory.Lead) ?? new List<Evidence>();
        public IReadOnlyList<Evidence> GetUnresolved() => log?.GetEvidenceByCategory(EvidenceCategory.Unresolved) ?? new List<Evidence>();
        public IReadOnlyList<Evidence> GetAll() => log?.GetAllEvidence() ?? new List<Evidence>();
    }
}
