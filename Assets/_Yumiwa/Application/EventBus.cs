using System;
using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>Zero-allocation struct event bus (one handler list per event type).</summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> handlersByType = new Dictionary<Type, object>();

        public void Subscribe<T>(Action<T> handler) where T : struct =>
            Handlers<T>().Add(handler);

        public void Unsubscribe<T>(Action<T> handler) where T : struct =>
            Handlers<T>().Remove(handler);

        public void Publish<T>(T evt) where T : struct
        {
            var handlers = Handlers<T>();
            for (int i = 0; i < handlers.Count; i++)
                handlers[i](evt);
        }

        private List<Action<T>> Handlers<T>() where T : struct
        {
            var type = typeof(T);
            if (!handlersByType.TryGetValue(type, out var boxed))
            {
                var created = new List<Action<T>>();
                handlersByType[type] = created;
                return created;
            }
            return (List<Action<T>>)boxed;
        }
    }
}
