namespace DorkyProductions
{
    using System;
    using System.Collections.Generic;
    public abstract class GameEvent
    {
        public int PlayerID { get; set; }
    }

    public class PlayCardEvent : GameEvent
    {
        public int CardID { get; set; }
        public string EventDescription { get; set; }
    }
    // TODO: complete this template.
    
    public static class MessageQueue
    {
        private static Queue<GameEvent> eventQueue = new Queue<GameEvent>();
        private static Dictionary<Type, List<Action<GameEvent>>> subscribers = new Dictionary<Type, List<Action<GameEvent>>>();

        public static void Publish(GameEvent gameEvent)
        {
            eventQueue.Enqueue(gameEvent);
        }

        public static void Subscribe<T>(Action<T> callback) where T : GameEvent
        {
            var eventType = typeof(T);

            if (!subscribers.ContainsKey(eventType))
            {
                subscribers[eventType] = new List<Action<GameEvent>>();
            }

            // Add callback with type safety
            subscribers[eventType].Add(e => callback((T)e));
        }

        public static void ProcessQueue()
        {
            while (eventQueue.Count > 0)
            {
                var gameEvent = eventQueue.Dequeue();
                var eventType = gameEvent.GetType();

                if (subscribers.ContainsKey(eventType))
                {
                    foreach (var callback in subscribers[eventType])
                    {
                        callback.Invoke(gameEvent);
                    }
                }
            }
        }
    }

}


// Example usage?
//
// public class PlayerHandUI : MonoBehaviour
// {
//     void Start()
//     {
//         // Subscribe to PlayCardEvent
//         MessageQueue.Subscribe<PlayCardEvent>(HandlePlayCardEvent);
//     }
//
//     private void HandlePlayCardEvent(PlayCardEvent playCardEvent)
//     {
//         if (playCardEvent.PlayerID == 0) // Human player
//         {
//             Debug.Log($"Updating Player Hand UI: Card {playCardEvent.CardID} played.");
//             // Remove card from hand UI
//             RemoveCardFromHand(playCardEvent.CardID);
//         }
//     }
//
//     private void RemoveCardFromHand(int cardId)
//     {
//         // Logic to update hand UI
//     }
// }