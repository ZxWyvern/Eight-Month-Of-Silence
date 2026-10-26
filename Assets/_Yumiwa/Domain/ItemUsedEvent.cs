namespace Yumiwa.Domain
{
    /// <summary>Published when player uses an item from hand (left/right). Only hand items can be used.</summary>
    public readonly struct ItemUsedEvent
    {
        public readonly string ItemId;
        public readonly HandType Hand;
        public ItemUsedEvent(string itemId, HandType hand) { ItemId = itemId; Hand = hand; }
    }
}
