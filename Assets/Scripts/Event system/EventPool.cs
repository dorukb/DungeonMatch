using System;
using System.Collections.Generic;

namespace DorkyProductions
{
    public static class EventPool
    {
        // A dictionary of queues, one for each event type
        private static readonly Dictionary<Type, Queue<GameEventBase>> _pool = 
            new Dictionary<Type, Queue<GameEventBase>>();

        /// <summary>
        /// Gets a recycled event object of type T from the pool,
        /// or creates a new one if the pool is empty.
        /// </summary>
        public static T Get<T>() where T : GameEventBase, new()
        {
            Queue<GameEventBase> queue;
            if (!_pool.TryGetValue(typeof(T), out queue))
            {
                // No pool for this type yet, create one
                queue = new Queue<GameEventBase>();
                _pool[typeof(T)] = queue;
            }

            if (queue.Count > 0)
            {
                // Re-use an existing object
                return (T)queue.Dequeue();
            }

            // Pool is empty, create a new object
            return new T();
        }

        /// <summary>
        /// Gets a recycled event object by its EventType enum.
        /// Used by the deserializer.
        /// </summary>
        public static GameEventBase Get(EventType type)
        {
            switch (type)
            {
                // Blocking
                case EventType.GameStarted: return Get<GameStartedEvent>();
                case EventType.GameEnded: return Get<GameEndedEvent>();
                case EventType.MatchedTiles: return Get<MatchedTilesEvent>();
                case EventType.SwappedTiles: return Get<SwappedTilesEvent>();
                case EventType.SwapDenied: return Get<SwapDeniedEvent>();
                case EventType.Attack: return Get<AttackEvent>();
                case EventType.Heal: return Get<HealEvent>();
                case EventType.Shield: return Get<ShieldEvent>();
                
                // Immediate
                case EventType.TurnStarted: return Get<TurnStartedEvent>();
                case EventType.TurnEnded: return Get<TurnEndedEvent>();
                
                // Parallel
                case EventType.TileMoved: return Get<TileMovedEvent>();
                case EventType.TileSpawned: return Get<TileSpawnedEvent>();
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), $"No event class registered for type: {type}");
            }
        }

        /// <summary>
        /// Resets an event object and returns it to the pool for re-use.
        /// </summary>
        public static void Release(GameEventBase ev)
        {
            if (ev == null) return;

            Type type = ev.GetType();
            if (!_pool.ContainsKey(type))
            {
                _pool[type] = new Queue<GameEventBase>();
            }

            ev.Reset(); // Clear its data
            _pool[type].Enqueue(ev);
        }
    }
}