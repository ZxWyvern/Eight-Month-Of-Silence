using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Linear mission progression per GDD §5/6 + ADR-0003 — order is data (MissionSequence), not enum switch.
    /// Vertical slice: drives Missions 1..N-1 investigation loop; final house loop is separate FinalEncounterService.
    /// </summary>
    public sealed class MissionFlowService
    {
        private readonly List<MissionDefinition> sequence;
        private readonly IEventBus bus;
        private int currentIndex = -1;

        public MissionFlowService(IReadOnlyList<MissionDefinition> sequence, IEventBus bus)
        {
            this.sequence = new List<MissionDefinition>(sequence);
            this.bus = bus;
            this.sequence.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
        }

        public MissionDefinition? Current => currentIndex >= 0 && currentIndex < sequence.Count ? sequence[currentIndex] : (MissionDefinition?)null;
        public int CurrentIndex => currentIndex;
        public int Count => sequence.Count;
        public IReadOnlyList<MissionDefinition> Sequence => sequence;

        public bool CanAdvance => currentIndex + 1 < sequence.Count;
        public bool IsFinalMission => currentIndex == sequence.Count - 1;

        public bool Start()
        {
            if (sequence.Count == 0)
                return false;
            currentIndex = 0;
            bus?.Publish(new MissionChangedEvent(sequence[currentIndex].MissionId, sequence[currentIndex].Chapter));
            return true;
        }

        public bool Advance()
        {
            if (!CanAdvance)
                return false;
            currentIndex++;
            bus?.Publish(new MissionChangedEvent(sequence[currentIndex].MissionId, sequence[currentIndex].Chapter));
            return true;
        }

        public MissionDefinition? GetNext() => CanAdvance ? sequence[currentIndex + 1] : (MissionDefinition?)null;

        public int ExportMissionIndex() => currentIndex;
        public void ImportMissionIndex(int index) { if (index >= -1 && index < sequence.Count) { currentIndex = index; if (currentIndex >=0) bus?.Publish(new MissionChangedEvent(sequence[currentIndex].MissionId, sequence[currentIndex].Chapter)); } }
        public void Clear() { currentIndex = -1; }

        public void SetIndex(int index)
        {
            if (index < 0 || index >= sequence.Count)
                return;
            currentIndex = index;
            bus?.Publish(new MissionChangedEvent(sequence[currentIndex].MissionId, sequence[currentIndex].Chapter));
        }
    }

    public readonly struct MissionChangedEvent
    {
        public readonly string MissionId;
        public readonly ChapterState Chapter;
        public MissionChangedEvent(string missionId, ChapterState chapter)
        {
            MissionId = missionId;
            Chapter = chapter;
        }
    }
}
