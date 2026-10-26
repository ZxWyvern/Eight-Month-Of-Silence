using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>Application flashlight: battery drain/regen + flicker when low/wounded. Pure, testable.</summary>
    public sealed class FlashlightService : IFlashlightService
    {
        private FlashlightState state;
        private readonly IEventBus bus;
        private readonly float drainPerSecond;
        private readonly float regenPerSecond;
        private readonly float flickerThreshold;
        private System.Random rng = new System.Random();

        public FlashlightService(IEventBus bus, float drainPerSecond = 0.8f, float regenPerSecond = 0.5f, float flickerThreshold = 20f)
        {
            this.bus = bus;
            this.drainPerSecond = drainPerSecond;
            this.regenPerSecond = regenPerSecond;
            this.flickerThreshold = flickerThreshold;
            state = new FlashlightState(100f, false);
        }

        public FlashlightState State => state;
        public bool IsOn => state.IsOn;
        public float Battery => state.Battery;
        public event System.Action<FlashlightToggledEvent> Toggled;
        public event System.Action<FlashlightBatteryEvent> BatteryChanged;

        public void Toggle()
        {
            if (state.IsOn)
            {
                state = state.WithToggled();
                bus?.Publish(new FlashlightToggledEvent(false));
                Toggled?.Invoke(new FlashlightToggledEvent(false));
                return;
            }
            if (!state.HasBattery) return; // no battery → cannot turn on
            state = state.WithToggled();
            bus?.Publish(new FlashlightToggledEvent(true));
            Toggled?.Invoke(new FlashlightToggledEvent(true));
        }

        public void ForceOff()
        {
            if (!state.IsOn) return;
            state = new FlashlightState(state.Battery, false, 0f);
            bus?.Publish(new FlashlightToggledEvent(false));
            Toggled?.Invoke(new FlashlightToggledEvent(false));
        }

        public void SetBattery(float battery)
        {
            state = state.WithBattery(battery);
            bus?.Publish(new FlashlightBatteryEvent(state.Battery));
            BatteryChanged?.Invoke(new FlashlightBatteryEvent(state.Battery));
        }

        public void Tick(float deltaTime, bool wounded)
        {
            float beforeBattery = state.Battery;
            bool beforeOn = state.IsOn;
            float nextBattery = beforeBattery;
            if (state.IsOn)
            {
                nextBattery -= drainPerSecond * deltaTime;
                if (nextBattery <= 0f)
                {
                    nextBattery = 0f;
                    state = new FlashlightState(nextBattery, false, 0f);
                    bus?.Publish(new FlashlightToggledEvent(false));
                    Toggled?.Invoke(new FlashlightToggledEvent(false));
                }
                else
                {
                    state = state.WithBattery(nextBattery);
                }
            }
            else if (nextBattery < 100f)
            {
                nextBattery += regenPerSecond * deltaTime;
                if (nextBattery > 100f) nextBattery = 100f;
                state = state.WithBattery(nextBattery);
            }
            // Publish battery only on meaningful change (>0.05) to avoid per-frame bus spam (ARCH §8) — no UnityEngine in Application
            if (System.Math.Abs(state.Battery - beforeBattery) > 0.05)
            {
                bus?.Publish(new FlashlightBatteryEvent(state.Battery));
                BatteryChanged?.Invoke(new FlashlightBatteryEvent(state.Battery));
            }

            // Flicker when low battery or wounded
            float flicker = 0f;
            if (state.IsOn && (state.Battery < flickerThreshold || wounded))
            {
                flicker = (float)rng.NextDouble() * 0.3f;
                if (wounded) flicker += 0.1f;
            }
            state = state.WithFlicker(flicker);
        }
    }
}
