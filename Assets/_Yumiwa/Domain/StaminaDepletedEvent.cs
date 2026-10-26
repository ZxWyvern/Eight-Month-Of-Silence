namespace Yumiwa.Domain
{
    /// <summary>Raised when a spend attempt fails from insufficient stamina.</summary>
    public readonly struct StaminaDepletedEvent
    {
        public readonly float AttemptedCost;

        public StaminaDepletedEvent(float attemptedCost)
        {
            AttemptedCost = attemptedCost;
        }
    }
}
