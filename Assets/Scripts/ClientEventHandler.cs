using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Serialization;
using Sequence = DG.Tweening.Sequence;

namespace DorkyProductions
{
    public class ClientEventHandler : MonoBehaviour
    {
        public ClientBoardVisualizer Visualizer;

        private List<GameEvent> gameHistory = new List<GameEvent>();
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
                SyncType syncType = ev.syncType;

                if (syncType == SyncType.Blocking)
                {
                    // --- BLOCKING ---
                    // Wait for this one event's animation to fully complete
                    yield return StartCoroutine(HandleBlockingEvent(ev));
                }
                else if (syncType == SyncType.Immediate)
                {
                    // Start the event logic, but do NOT wait for it.
                    HandleImmediate(ev);
                }
                else if (syncType == SyncType.Parallel) // syncType == EventSyncType.Parallel
                {
                    // 1. Create a batch, starting with this event
                    List<GameEvent> parallelBatch = new List<GameEvent>();
                    parallelBatch.Add(ev);

                    // 2. Look ahead and group all other parallel events
                    // Only group events that are of the SAME type!
                    // Note: subsequent calls to peek always return the same object, the front of the queue.
                    // dequeue inside the loop changes the front element.
                    while (eventQueue.Count > 0
                           && eventQueue.Peek().syncType == SyncType.Parallel
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

        private void HandleImmediate(GameEvent ev)
        {
            switch (ev.type)
            {
                case EventType.TurnStarted:
                    if (ev.turnData.playerNetId == _localPlayer.netId)
                    {
                        Debug.Log("My Turn Started");
                        _localPlayer.StartPlayerTurn();
                        // TODO: Add UI text that flies from left to right, saying "Your turn!"
                        // fire UI event for it.
                        // yourTurnStartedEvent?.Invoke()
                        // UI CLass listens to that event.
                    }
                    break;
                case EventType.TurnEnded:
                    if (ev.turnData.playerNetId == _localPlayer.netId)
                    {
                        Debug.Log("My Turn Ended");
                        _localPlayer.EndPlayerTurn();
                    }
                    break;
                case EventType.GameEnded:
                    Debug.LogWarning("Game Ended NOT IMPLEMENTED");
                    break;
                default:
                    Debug.LogError("This event type is not immediate." + ev.type);
                    break;
            }
            
        }

        /// Handles a single event (Blocking or Immediate).
        /// Returns a coroutine that waits for its animation.
        private IEnumerator HandleBlockingEvent(GameEvent ev)
        {
            Tween animTween = null;

            switch (ev.type)
            {
                case EventType.GameStarted:
                    Debug.Log("Game started, setup the local board");
                    var boardState = ev.gameStartData.boardState;
                    animTween = Visualizer.InitBoard(boardState);
                    break;
                    
                case EventType.MatchedTiles:
                    Debug.Log($"MatchOccurred/RemoveTiles for: {ev.matchData.matchedTileIDs}");
                    animTween = Visualizer.AnimatePop(ev.matchData.matchedTileIDs);
                    break;

                case EventType.SwappedTiles:
                    Debug.Log($"Swap tiles: {ev.swapData.firstId}, {ev.swapData.secondId}");
                    animTween = Visualizer.AnimateSwap(ev.swapData.firstId, ev.swapData.secondId);
                    break;
                case EventType.SwapDenied:
                    Debug.Log($"Swap denied");
                    _localPlayer.EnableMoves();
                    break;
                
                case EventType.Attack:
                    Debug.Log($"Attack event received.");
                    uint attackerId = ev.attackData.attackerNetId;
                    if (attackerId == _localPlayer.netId)
                    {
                        // we play the attack anim
                        _localPlayer.DealDamage(ev.attackData.damageAmount);
                    }
                    else
                    {
                        // we play the "get attacked" anim.
                        _localPlayer.ReceiveDamage(ev.attackData.damageAmount);
                    }

                    break;
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
            Debug.Log($"Starting a parallel batch of {batch.Count} events of Type: {batch[0].type}");

            // 1. Create one "master" sequence
            Sequence parallelSequence = DOTween.Sequence();

            foreach (var ev in batch)
            {
                Tween tileTween = null;
                switch (ev.type)
                {
                    case EventType.TileMoved:
                        tileTween = Visualizer.AnimateFall(ev.tileMoveData.tileId, ev.tileMoveData.toGridPos);
                        break;

                    case EventType.TileSpawned:
                        tileTween = Visualizer.SpawnVisualTile(ev.tileSpawnData.state, ev.tileSpawnData.pos);
                        break;
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