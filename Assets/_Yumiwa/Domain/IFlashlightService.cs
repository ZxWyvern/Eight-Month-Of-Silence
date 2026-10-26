namespace Yumiwa.Domain
{
    public interface IFlashlightService
    {
        FlashlightState State { get; }
        bool IsOn { get; }
        float Battery { get; }
        void Toggle();
        void ForceOff();
        void SetBattery(float battery);
        void Tick(float deltaTime, bool wounded);
        event System.Action<FlashlightToggledEvent> Toggled;
        event System.Action<FlashlightBatteryEvent> BatteryChanged;
    }

    public readonly struct FlashlightToggledEvent
    {
        public readonly bool IsOn;
        public FlashlightToggledEvent(bool isOn) { IsOn = isOn; }
    }

    public readonly struct FlashlightBatteryEvent
    {
        public readonly float Battery;
        public FlashlightBatteryEvent(float battery) { Battery = battery; }
    }
}
