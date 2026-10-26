namespace Yumiwa.Domain
{
    /// <summary>Pure flashlight state — battery 0-100, IsOn, flicker intensity. No UnityEngine.</summary>
    public readonly struct FlashlightState
    {
        public readonly float Battery; // 0-100
        public readonly bool IsOn;
        public readonly float Flicker; // 0-1 random when battery low or wounded

        public FlashlightState(float battery, bool isOn, float flicker = 0f)
        {
            Battery = UnityClamp(battery, 0f, 100f);
            IsOn = isOn;
            Flicker = UnityClamp(flicker, 0f, 1f);
        }

        public FlashlightState WithToggled() => new FlashlightState(Battery, !IsOn, Flicker);
        public FlashlightState WithBattery(float battery) => new FlashlightState(battery, IsOn, Flicker);
        public FlashlightState WithFlicker(float flicker) => new FlashlightState(Battery, IsOn, flicker);
        public bool HasBattery => Battery > 0.5f;

        private static float UnityClamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
