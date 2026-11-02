using UnityEngine;
using VContainer;
using Yumiwa.Application;

namespace Yumiwa.Presentation
{
    public sealed class ChapterEntryPresenter : MonoBehaviour
    {
        private MissionFlowService mission;

        [Inject]
        public void Construct(MissionFlowService mission)
        {
            this.mission = mission;
        }

        private void Start()
        {
            if (mission == null || mission.Current != null || mission.Count == 0)
                return;
            mission.Start();
        }
    }
}
