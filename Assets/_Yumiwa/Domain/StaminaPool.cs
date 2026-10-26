namespace Yumiwa.Domain
{
    /// <summary>Pure stamina pool entity: spend gating plus delayed regeneration.</summary>
    public sealed class StaminaPool
    {
        public float Max { get; }
        public float RegenPerSecond { get; }
        public float RegenDelaySeconds { get; }
        public float Current { get; private set; }

        private float lastSpendTime = float.NegativeInfinity;

        public StaminaPool(float max, float regenPerSecond, float regenDelaySeconds)
        {
            Max = max;
            RegenPerSecond = regenPerSecond;
            RegenDelaySeconds = regenDelaySeconds;
            Current = max;
        }

        public bool CanAfford(float cost) => Current >= cost;

        public bool TrySpend(float cost, float now)
        {
            if (!CanAfford(cost))
                return false;
            Current -= cost;
            lastSpendTime = now;
            return true;
        }

        public void Tick(float deltaTime, float now)
        {
            if (Current >= Max || now - lastSpendTime < RegenDelaySeconds)
                return;
            Current = System.Math.Min(Max, Current + RegenPerSecond * deltaTime);
        }

        public void Refill() => Current = Max;
    }
}
