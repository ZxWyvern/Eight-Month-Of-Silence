using UnityEngine;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/Flashlight Config", fileName = "FlashlightConfig")]
    public sealed class FlashlightConfig : ScriptableObject
    {
        [Header("Battery")]
        public float drainPerSecond = 0.8f;
        public float regenPerSecond = 0.5f;
        public float flickerThreshold = 20f;

        [Header("Light")]
        public float intensityOn = 1.2f;
        public float intensityOff = 0f;
        public Color colorOn = Color.white;

        public FlashlightService CreateService(IEventBus bus)
        {
            return new FlashlightService(bus, drainPerSecond, regenPerSecond, flickerThreshold);
        }
    }
}
