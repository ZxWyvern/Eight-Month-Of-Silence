namespace Yumiwa.Domain
{
    public struct PlayerWoundedEvent
    {
        public readonly float HealthAtWound;

        public PlayerWoundedEvent(float healthAtWound)
        {
            HealthAtWound = healthAtWound;
        }
    }
}