using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sequence = DG.Tweening.Sequence;

namespace DorkyProductions
{
    public class ClientGameMaster : MonoBehaviour
    {
        public ClientBoardVisualizer _visualizer;

        public List<GameEvent> gameHistory = new List<GameEvent>();
        private Queue<GameEvent> eventQueue = new Queue<GameEvent>();
        private bool isProcessingEvents = false;
        private NetworkPlayer _localPlayer;

        private void OnDisable()
        {
            if (eventQueue.Count > 0)
            {
                Debug.LogError($"OnDisable time, there are {eventQueue.Count} events in the queue." +
                               $"Removing all and stopping all Coroutines as well. This is highly likely to cause issues.");
            }
            else
            {
                Debug.Log($"ClientGameMaster becomes disabled in a safe time. There were no events in the queue.");
            }

            eventQueue.Clear();
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            Debug.Log("ClientGameMaster is destroyed.");
        }

        public void SetLocalPlayer(NetworkPlayer networkPlayer)
        {
            _localPlayer = networkPlayer;
        }

        // Called by the CleintRPC to add a batch of events from the server.
        public void EnqueueEventBatch(List<GameEvent> batch)
        {
            foreach (GameEvent ev in batch)
            {
                eventQueue.Enqueue(ev);
                gameHistory.Add(ev); // Log to history
            }

            // If the processor isn't already running, start it.
            if (!isProcessingEvents)
            {
                StartCoroutine(ProcessEventQueue());
            }
        }

        /// The main processor coroutine. Reads from the queue.
        private IEnumerator ProcessEventQueue()
        {
            isProcessingEvents = true;

            while (eventQueue.Count > 0)
            {
                GameEvent ev = eventQueue.Dequeue();
                EventSyncType syncType = ev.SyncType;

                if (syncType == EventSyncType.Blocking)
                {
                    // --- BLOCKING ---
                    // Wait for this one event's animation to fully complete
                    yield return StartCoroutine(HandleEvent(ev));
                }
                else if (syncType == EventSyncType.Immediate)
                {
                    // Start the event logic, but do NOT wait for it.
                    StartCoroutine(HandleEvent(ev));
                }
                else if (syncType == EventSyncType.Parallel) // syncType == EventSyncType.Parallel
                {
                    // 1. Create a batch, starting with this event
                    List<GameEvent> parallelBatch = new List<GameEvent>();
                    parallelBatch.Add(ev);

                    // 2. Look ahead and group all other parallel events
                    // Only group events that are of the SAME type!
                    // Note: subsequent calls to peek always return the same object, the front of the queue.
                    // dequeue inside the loop changes the front element.
                    while (eventQueue.Count > 0
                           && eventQueue.Peek().SyncType == EventSyncType.Parallel
                           && eventQueue.Peek().type == ev.type)
                    {
                        parallelBatch.Add(eventQueue.Dequeue());
                    }

                    // 3. Play all of them and wait for ALL to finish.
                    yield return StartCoroutine(HandleParallelBatch(parallelBatch));
                }
                else
                {
                    Debug.LogError("Unhandled/Unknown SyncType: " + syncType);
                }
            }

            isProcessingEvents = false;
        }

        /// Handles a single event (Blocking or Immediate).
        /// Returns a coroutine that waits for its animation.
        private IEnumerator HandleEvent(GameEvent ev)
        {
            Tween animTween = null;

            switch (ev.type)
            {
                case EventType.TurnStarted:
                    //NetworkIdentity player = NetworkClient.spawned[ev.playerData.playerNetId];
                    //UIManager.ShowTurnBanner(player.name);
                    Debug.Log("Turn Started");
                    _localPlayer.StartPlayerTurn();
                    break;

                case EventType.SwapOccurred:
                    animTween = _visualizer.AnimateSwap(ev.swapData.firstId, ev.swapData.secondId);
                    break;

                case EventType.SwapDenied:
                    // animTween = boardVisuals.AnimateFailedSwap(ev.swapData.pos1, ev.swapData.pos2);
                    Debug.Log("Swap Denied");
                    break;

                // ... other non-parallel cases
            }

            // If an animation was created, wait for it to complete.
            if (animTween != null)
            {
                yield return animTween.WaitForCompletion();
            }

            yield break;
        }

        // Handles a batch of parallel events, playing them all at once.
        private IEnumerator HandleParallelBatch(List<GameEvent> batch)
        {
            Debug.Log($"Starting a parallel batch of {batch.Count} events.");

            // 1. Create one "master" sequence
            Sequence parallelSequence = DOTween.Sequence();

            foreach (var ev in batch)
            {
                Tween tileTween = null;
                switch (ev.type)
                {
                    case EventType.TileMoved:
                        tileTween = _visualizer.AnimateFall(ev.tileMoveData.tileId, ev.tileMoveData.toPos);
                        break;

                    // case EventType.TileSpawned:
                    //     tileTween = boardVisuals.AnimateTileSpawn(ev.spawnData.pos);
                    //     break;
                }

                if (tileTween != null)
                {
                    // 2. Add the tween to the master sequence to play concurrently
                    parallelSequence.Join(tileTween);
                }
            }

            // 3. Wait for the *entire sequence* of parallel tweens to finish.
            yield return parallelSequence.WaitForCompletion();

            Debug.Log("Parallel batch finished.");
        }
    }
}