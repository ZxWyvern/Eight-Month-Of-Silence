namespace Yumiwa.Domain
{
    public struct PlayerHealedEvent
    {
        public readonly float PreviousHealth;
        public readonly float NewHealth;

        public PlayerHealedEvent(float previous, float next)
        {
            PreviousHealth = previous;
            NewHealth = next;
        }
    }
}