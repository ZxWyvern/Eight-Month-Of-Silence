using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>Application stamina service: pool orchestration plus change events.</summary>
    public sealed class StaminaService : IStaminaService
    {
        private readonly StaminaPool pool;
        private readonly ITimeSource clock;
        private readonly IEventBus bus;

        public StaminaService(StaminaPool pool, ITimeSource clock, IEventBus bus)
        {
            this.pool = pool;
            this.clock = clock;
            this.bus = bus;
        }

        public float Current => pool.Current;
        public float Max => pool.Max;

        public bool CanAfford(float cost) => pool.CanAfford(cost);

        public bool TrySpend(float cost)
        {
            if (!pool.TrySpend(cost, clock.Now))
            {
                bus.Publish(new StaminaDepletedEvent(cost));
                return false;
            }
            bus.Publish(new StaminaChangedEvent(pool.Current, pool.Max));
            return true;
        }

        public void Tick(float deltaTime)
        {
            float before = pool.Current;
            pool.Tick(deltaTime, clock.Now);
            if (pool.Current != before)
                bus.Publish(new StaminaChangedEvent(pool.Current, pool.Max));
        }
    }
}
