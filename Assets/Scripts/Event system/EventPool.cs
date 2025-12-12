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

        /// Gets a recycled event object by its EventType enum.
        /// Used by the deserializer.
        public static GameEventBase Get(EventType type)
        {
            // This is now an expression, so we return its result directly.
            return type switch
            {
                // Blocking
                EventType.GameStarted => Get<GameStartedEvent>(),
                EventType.GameEnded => Get<GameEndedEvent>(),
                EventType.MatchedTiles => Get<MatchedTilesEvent>(),
                EventType.SwappedTiles => Get<SwappedTilesEvent>(),
                EventType.SwapDenied => Get<SwapDeniedEvent>(),
                EventType.Attack => Get<AttackEvent>(),
                EventType.Heal => Get<HealEvent>(),
                EventType.Shield => Get<ShieldEvent>(),
                EventType.CrossMatched => Get<CrossMatchedEvent>(),
                EventType.CrossConsumed => Get<CrossConsumedEvent>(),
                EventType.ChestMatched => Get<ChestMatchedEvent>(),
                EventType.TurnStarted => Get<TurnStartedEvent>(),
                EventType.TurnEnded => Get<TurnEndedEvent>(),
                EventType.TileRemoved => Get<TileRemovedEvent>(),
                EventType.AIDelay => Get<AIDelayEvent>(),

                // Parallel
                EventType.TileMoved => Get<TileMovedEvent>(),
                EventType.TileSpawned => Get<TileSpawnedEvent>(),
            };
        }
        
        /// Resets an event object and returns it to the pool for re-use.
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