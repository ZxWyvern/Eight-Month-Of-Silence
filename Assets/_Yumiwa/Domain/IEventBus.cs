namespace Yumiwa.Domain
{
    /// <summary>Contract for zero-allocation struct event broadcast.</summary>
    public interface IEventBus
    {
        void Subscribe<T>(System.Action<T> handler) where T : struct;
        void Unsubscribe<T>(System.Action<T> handler) where T : struct;
        void Publish<T>(T evt) where T : struct;
    }
}
