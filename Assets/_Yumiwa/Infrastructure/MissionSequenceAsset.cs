using System.Collections.Generic;
using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/Mission/Mission Sequence", fileName = "SO_MissionSequence")]
    public sealed class MissionSequenceAsset : ScriptableObject
    {
        [System.Serializable]
        public struct MissionEntry
        {
            public string MissionId;
            public string DisplayName;
            [TextArea(2,2)] public string Description;
            public ChapterState Chapter;
        }

        [SerializeField] private MissionEntry[] missions = new MissionEntry[]
        {
            new MissionEntry { MissionId = "M01_Alley", DisplayName = "Jejak Darah Pertama", Chapter = ChapterState.Chapter1_BloodTrail },
            new MissionEntry { MissionId = "M02_Motel", DisplayName = "Pembunuhan di Motel", Chapter = ChapterState.Chapter2_MotelMurder },
            new MissionEntry { MissionId = "M03_Witness", DisplayName = "Saksi yang Selamat", Chapter = ChapterState.Chapter3_SurvivorWitness },
            new MissionEntry { MissionId = "M04_Fog", DisplayName = "Wajah di Tengah Kabut", Chapter = ChapterState.Chapter4_FoggyFace },
            new MissionEntry { MissionId = "M05_House", DisplayName = "Rumah Tanpa Jalan Keluar", Chapter = ChapterState.Chapter5_NoExitHouse },
        };

        public IReadOnlyList<MissionDefinition> GetDefinitions()
        {
            var list = new List<MissionDefinition>(missions.Length);
            for (int i = 0; i < missions.Length; i++)
            {
                var m = missions[i];
                list.Add(new MissionDefinition(m.MissionId, m.DisplayName, m.Description, m.Chapter, i));
            }
            return list;
        }

        public MissionDefinition? GetById(string missionId)
        {
            for (int i = 0; i < missions.Length; i++)
                if (missions[i].MissionId == missionId)
                    return new MissionDefinition(missions[i].MissionId, missions[i].DisplayName, missions[i].Description, missions[i].Chapter, i);
            return null;
        }

        private void OnValidate()
        {
            // Ensure unique IDs
            var seen = new HashSet<string>();
            foreach (var m in missions)
            {
                if (string.IsNullOrEmpty(m.MissionId))
                    Debug.LogWarning($"[Yumiwa] MissionSequence has empty MissionId.", this);
                else if (!seen.Add(m.MissionId))
                    Debug.LogWarning($"[Yumiwa] Duplicate MissionId '{m.MissionId}'.", this);
            }
        }
    }
}
