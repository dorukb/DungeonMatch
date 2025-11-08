using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DorkyProductions.UI;
using Mirror;
using Sequence = DG.Tweening.Sequence;

namespace DorkyProductions
{
    public class ClientEventHandler : MonoBehaviour, IGameEventHandler
    {
        public ClientBoardVisualizer Visualizer;

        // private List<GameEvent> gameHistory = new List<GameEvent>();
        private Queue<GameEventBase> eventQueue = new Queue<GameEventBase>();
        private bool isProcessingEvents = false;
        private NetworkPlayer _localPlayer;
    
        [SerializeField]
        private UIMediator _mediator;
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

        // Called by the ClientRPC to add a batch of events from the server.
        public void EnqueueEventBatch(ArraySegment<byte> eventBatch)
        {
            // Use a PooledReader to deserialize the data
            using (NetworkReaderPooled reader = NetworkReaderPool.Get(eventBatch))
            {
                // Read the number of events
                ushort eventCount = reader.Read<ushort>();
                Debug.Log($"[Client] received {eventCount} events.");
                
                for (int i = 0; i < eventCount; i++)
                {
                    // The ReadGameEvent() method now returns a
                    // pooled object, so no 'new' is called here.
                    GameEventBase ev = reader.ReadGameEvent();
                    if (ev != null)
                    {
                        eventQueue.Enqueue(ev);
                    }
                }
            }
            // If the processor isn't already running, start it.
            if (!isProcessingEvents)
            {
                StartCoroutine(ProcessEventQueue());
            }
        }
 // --- Event Loop (Now with Pooling) ---
        private IEnumerator ProcessEventQueue()
        {
            isProcessingEvents = true;

            while (eventQueue.Count > 0)
            {
                GameEventBase ev = eventQueue.Dequeue();
                SyncType syncType = ev.SyncType;

                if (syncType == SyncType.Blocking)
                {
                    // --- BLOCKING ---
                    Tween animTween = ev.Accept(this);
                    if (animTween != null)
                    {
                        yield return animTween.WaitForCompletion();
                    }
                    EventPool.Release(ev); // Release after processing
                }
                else if (syncType == SyncType.Immediate)
                {
                    // --- IMMEDIATE ---
                    ev.Accept(this);
                    EventPool.Release(ev); // Release immediately
                }
                else if (syncType == SyncType.Parallel)
                {
                    // --- PARALLEL ---
                    List<GameEventBase> parallelBatch = new List<GameEventBase>();
                    parallelBatch.Add(ev);

                    while (eventQueue.Count > 0
                           && eventQueue.Peek().SyncType == SyncType.Parallel
                           && eventQueue.Peek().EventType == ev.EventType)
                    {
                        parallelBatch.Add(eventQueue.Dequeue());
                    }

                    yield return StartCoroutine(HandleParallelBatch(parallelBatch));
                    
                    // Release all events in the batch
                    foreach (var e in parallelBatch)
                    {
                        EventPool.Release(e);
                    }
                }
            }

            isProcessingEvents = false;
        }

        private IEnumerator HandleParallelBatch(List<GameEventBase> batch)
        {
            Debug.Log($"Starting a parallel batch of {batch.Count} events of Type: {batch[0].EventType}");

            Sequence parallelSequence = DOTween.Sequence();
            foreach (var ev in batch)
            {
                Tween tileTween = ev.Accept(this);
                if (tileTween != null)
                {
                    parallelSequence.Join(tileTween);
                }
            }

            yield return parallelSequence.WaitForCompletion();
            Debug.Log("Parallel batch finished.");
        }

        // =======================================================
        // --- IGameEventHandler (VISITOR) IMPLEMENTATION ---
        // =======================================================
        // This logic is identical to your original, just
        // separated into type-safe methods.
        // =======================================================

        #region Blocking Handlers
        public Tween Handle(GameStartedEvent e)
        {
            Debug.Log("Game started, setup the local board");
            return Visualizer.InitBoard(e.boardState);
        }

        public Tween Handle(GameEndedEvent e)
        {
            Debug.Log($"GAME END and WINNER is {e.winnerID}");
            // return UIMediator.ShowGameOverScreen(e.winnerID);
            return null;
        }

        public Tween Handle(MatchedTilesEvent e)
        {
            Debug.Log($"MatchOccurred/RemoveTiles for: {e.matchedTileIDs}");
            return Visualizer.AnimatePop(e.matchedTileIDs);
        }

        public Tween Handle(SwappedTilesEvent e)
        {
            Debug.Log($"Swap tiles: {e.firstId}, {e.secondId}");
            return Visualizer.AnimateSwap(e.firstId, e.secondId);
        }

        public Tween Handle(SwapDeniedEvent e)
        {
            Debug.Log($"Swap denied");
            if (_localPlayer != null && e.playerNetId == _localPlayer.netId)
            {
                _localPlayer.EnableControls();
            }
            return null;
        }

        public Tween Handle(AttackEvent e)
        {
            Debug.Log($"Attack event received.");
            // TODO: Create & Return the Attack anim tween.

            var targetPlayer = GetPlayerType(e.targetPlayerID);
            UIMediator.OnPlayerHealthUpdated?.Invoke(targetPlayer, e.targetsUpdatedHealth);
            UIMediator.OnPlayerShieldUpdated?.Invoke(targetPlayer, e.targetsUpdatedShield);
            return null;
        }

        public Tween Handle(HealEvent e)
        {
            // TODO: Create & Return the Heal anim tween.
            Debug.Log($"Heal player {e.targetPlayerID}");
            
            var targetPlayer = GetPlayerType(e.targetPlayerID);
            UIMediator.OnPlayerHealthUpdated?.Invoke(targetPlayer, e.targetsUpdatedHealth);
            return null;
        }

        public Tween Handle(ShieldEvent e)
        {
            Debug.Log($"Player {e.targetPlayerID} gained some shield.");
            var targetPlayer = GetPlayerType(e.targetPlayerID);
            UIMediator.OnPlayerShieldUpdated?.Invoke(targetPlayer, e.targetsUpdatedShield);
            return null;
        }

        public Tween Handle(CrossMatchedEvent e)
        {
            Debug.Log($"Player {e.targetPlayerID} matched Crosses. Got multiplier: {e.currentMultiplier}");
            var targetPlayer = GetPlayerType(e.targetPlayerID);
            UIMediator.OnPlayersCrossMultiplierUpdated?.Invoke(targetPlayer, e.currentMultiplier);
            
            // TODO: Implement Cross multiplayer View. anim sfc etc.
            return null;
        } 
        public Tween Handle(CrossConsumedEvent e)
        {
            Debug.Log($"Player {e.targetPlayerID} CONSUMED cross multiplier. Used multiplier: {e.appliedMultiplier}");
            
            var targetPlayer = GetPlayerType(e.targetPlayerID);
            UIMediator.OnPlayersCrossMultiplierUpdated?.Invoke(targetPlayer, e.appliedMultiplier);
            // TODO: Implement Cross using, Bless like animation on the Sword, Shield??
            return null;
        }
        #endregion

        #region Immediate Handlers
        public Tween Handle(TurnStartedEvent e)
        {
            if (_localPlayer == null)
            {
                Debug.LogError($"LocalPlayer is null.");
                return null;
            }
            
            if (e.playerNetId == _localPlayer.netId)
            {
                Debug.Log("My Turn Started");
                _localPlayer.EnableControls();
            }
            else
            {
                _localPlayer.DisableControls();
            }
            return null;
        }

        public Tween Handle(TurnEndedEvent e)
        {
            if (_localPlayer != null && e.playerNetId == _localPlayer.netId)
            {
                Debug.Log("My Turn Ended");
                _localPlayer.DisableControls();
            }
            return null;
        }
        #endregion

        #region Parallel Handlers
        public Tween Handle(TileMovedEvent e)
        {
            return Visualizer.AnimateFall(e.tileId, e.toGridPos);
        }

        public Tween Handle(TileSpawnedEvent e)
        {
            return Visualizer.SpawnVisualTile(e.state, e.pos);
        }

        public PlayerType GetPlayerType(uint playerNetId)
        {
            if (_localPlayer != null && playerNetId == _localPlayer.netId)
            {
                return PlayerType.Local;
            }
            return PlayerType.Opponent;
        }
        #endregion
    }
}