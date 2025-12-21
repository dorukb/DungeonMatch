using System;
using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace  DorkyProductions
{
    
// The possible Overview states of the game, synced to all clients.
// granular states are handles by GameEvent's
public enum GameState
{
    WaitingForPlayers,
    Playing,
    GameEnded
}

public class GameMaster : NetworkBehaviour
{
    public static GameMaster Instance { get; private set; }
    private GameState gameState = GameState.WaitingForPlayers;

    [Header("Game Settings")]
    [Tooltip("The number of players required to start a game")]
    public int requiredPlayers = 2;

    private List<NetworkPlayer> players = new List<NetworkPlayer>();
    private int activePlayerIndex = 0;
    
    private GameBoard _gameBoard; 
    private ClientEventHandler _clientEventHandler;
    public Context Context { get; private set; }
    
    // --- Server-Side Events (For Bots/AI) ---
    // Bots subscribe to these to know when to act, since they don't receive ClientRPCs
    public event Action<Context, GameBoard> OnServerTurnStarted;
    
    // param: <rewardId>
    public event Action<Context, int> OnServerChestMatched;
    
    void Awake()
    {
        Context = new Context();
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Debug.LogWarning("GameMaster already exists.Trying to create another one signals something is wrong.");
            Destroy(gameObject);
        }
        
        // TODO: Remove.
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    [Server]
    public void RegisterPlayer(NetworkPlayer player)
    {
        if (gameState != GameState.WaitingForPlayers)
        {
            // TODO: Handle players joining mid-game (spectating?)
            Debug.LogWarning("A player tried to join a game already in progress.");
            return;
        }

        players.Add(player);
        Debug.Log($"Player {player.netId} registered. Total players: {players.Count}");
        
        //TODO: is this place correct?
        AudioManager.Instance.PlaySFX(SFXType.GameFound);

        // Check if we have enough players to start
        if (players.Count == requiredPlayers)
        {
            StartGame();
        }
    }

    [Server]
    public void UnregisterPlayer(NetworkPlayer player)
    {
        players.Remove(player);
        Debug.Log($"Player {player.netId} unregistered.");

        // If the game is in progress and a player leaves, end the game
        if (gameState == GameState.Playing && players.Count < requiredPlayers)
        {
            // TODO: Handle opponent leaving. the remaining player should win.
            // EndGame(players.FirstOrDefault());
        }
    }
    
    [Server]
    private void StartGame()
    {
        if (gameState != GameState.WaitingForPlayers) return;

        Debug.Log("Starting game...");
        gameState = GameState.Playing;

        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].IsBot)
            {
                activePlayerIndex = i;
            }
        }
        // activePlayerIndex = 0;
        Context.Setup(players[activePlayerIndex].netId, 0, 0, false);
        
        List<GameEventBase> eventBatch = new List<GameEventBase>();
        
        _gameBoard = new GameBoard();
        var boardState = _gameBoard.FillBoardWithNoMatches();
        
        eventBatch.Add(EventPool.Get<GameStartedEvent>().Setup(boardState));
        eventBatch.Add(EventPool.Get<TurnStartedEvent>().Setup(Context));
        // TileDistribution.LogBoardDensity();
        // Send to clients
        SendEventBatch(eventBatch);
        
        // Notify Server-side entities (Bots) that the turn has started
        OnServerTurnStarted?.Invoke(Context, _gameBoard);
    }

    [Server]
    public void TriggerEndGame(NetworkPlayer winner, List<GameEventBase> eventBatch)
    {
        if (gameState == GameState.GameEnded) return;

        gameState = GameState.GameEnded;
        eventBatch.Add(EventPool.Get<GameEndedEvent>().Setup(winner.netId));
        Debug.Log($"[Server] Game over. Winner: {winner.netId}");
    }

    [Server]
    public void ProcessPlayerLightningSkillUse(NetworkIdentity sender, Vector2Int tilePos, float artificialDelay = 0f)
    {
        List<GameEventBase> eventBatch = new List<GameEventBase>();
        bool canMakeMove = ValidateUserTurn(sender);
        if (gameState == GameState.GameEnded)
        {
            Debug.Log("[Server] Game has ended already, Skill Use request has no effect at this point.");
            return;
        }
        Context.ChestsLeft--;
        // TODO: Validate the player actually has this skill/received the chest?
        // maybe dont even accept skillId as param, server should already know.
        if (canMakeMove)
        {
            if (artificialDelay > 0.1f)
            {
                eventBatch.Add(EventPool.Get<AIDelayEvent>().Setup(artificialDelay));
            }
            _gameBoard.ProcessLightningEffect(tilePos, sender, eventBatch);
        }
        else
        {
            // TODO: User currently loses the extra turn, if skill validation fails!
            Debug.LogError("[Server] Couldnt use Lightning skill. ending turn.");
        }
        EndTurnAndStartNext(eventBatch);
        SendEventBatch(eventBatch);
        // Notify Bot (Same player goes again)
        OnServerTurnStarted?.Invoke(Context, _gameBoard);
    }
    
    public void ProcessPlayerPhantomMatchSkill(NetworkIdentity sender, List<Vector2Int> targetTiles, float artificialDelay = 0f)
    { 
        // TODO: refactor using Template Method pattern.
        List<GameEventBase> eventBatch = new List<GameEventBase>();
        bool canMakeMove = ValidateUserTurn(sender);   
        
        Context.ChestsLeft--;
        // TODO: Validate the player actually has this skill/received the chest?
        // maybe dont even accept skillId as param, server should already know.
        // make sure all tiles are of same type.
        if (canMakeMove && targetTiles.Count >= 3) 
        {
            if (artificialDelay > 0.1f)
            {
                eventBatch.Add(EventPool.Get<AIDelayEvent>().Setup(artificialDelay));
            }
            _gameBoard.ProcessPhantomMatchEffect(targetTiles, sender, eventBatch);
        }
        else
        {
            Debug.LogError("[Server] Couldnt use Phantom Match skill. ending turn.");
        }
        EndTurnAndStartNext(eventBatch);
        SendEventBatch(eventBatch);
        // Notify Bot (Same player goes again)
        OnServerTurnStarted?.Invoke(Context, _gameBoard);
    }
    // This is the main "transaction" method called by a Player via [Command].
    // It processes the move and generates all resulting events.
    [Server]
    public void ProcessPlayerSwap(NetworkIdentity sender, Vector2Int posA, Vector2Int posB, float artificialDelay = 0f)
    {
        List<GameEventBase> eventBatch = new List<GameEventBase>();
        bool canMakeMove = ValidateUserTurn(sender);
        bool isValidMove = canMakeMove && _gameBoard.IsValidSwap(posA, posB);
        if (isValidMove)
        {
            if (artificialDelay > 0.1f)
            {
                eventBatch.Add(EventPool.Get<AIDelayEvent>().Setup(artificialDelay));
            }
            // Core algorithm.
            _gameBoard.ProcessSwapMove(posA, posB, sender, eventBatch);

            EndTurnAndStartNext(eventBatch);
            SendEventBatch(eventBatch);
            
            // Notify Bot (Same player goes again)
            OnServerTurnStarted?.Invoke(Context, _gameBoard);
        }
        else
        {
            Debug.LogWarning($"Player {sender.netId} tried to move out of turn or the swap was not valid.");
            eventBatch.Add(EventPool.Get<SwapDeniedEvent>().Setup(Context.ActivePlayerNetId));
            
            // Note: we do NOT end the turn here, just let the player make another move.
            SendEventBatch(eventBatch);
        }
    }

    
    // This assumes a 2-player game.
    [Server]
    public NetworkPlayer GetInactivePlayer()
    {
        if (players.Count != 2)
        {
            Debug.LogError($"GetInactivePlayer assumes there are 2 players. but we have: {players.Count}");
        }
        return players.Find(t => t.netId != Context.ActivePlayerNetId);
    }

    [Server]
    public NetworkPlayer GetPlayer(uint netID)
    {
        return players.Find(t => t.netId == netID);
    }
    
    [Server]
    public void GrantExtraTurnToCurrentPlayer(bool isChest)
    {
        Debug.Log($"[Server] Active player: {Context.ActivePlayerNetId} has been granted an extra turn.");
        Context.ExtraTurnsLeft += 1;
        if (isChest)
        {
            Context.ChestsLeft += 1;
        }
    }

    [Server]
    private bool ValidateUserTurn(NetworkIdentity playerIdentity)
    {
        if (gameState == GameState.GameEnded)
        {
            Debug.Log("[Server] Game has ended already, Swap request has no effect at this point.");
            return false;
        }
        bool canMakeMove = (gameState == GameState.Playing) && (playerIdentity.netId == Context.ActivePlayerNetId);
        return canMakeMove;
    }
    
    [Server]
    private void EndTurnAndStartNext(List<GameEventBase> eventBatch)
    {
        if (gameState == GameState.GameEnded)
        {
            Debug.Log("[Server] Game has already, End Turn will have no effect at this point.");
            return;
        }
        
        if (Context.ExtraTurnsLeft > 0)
        {
            Debug.Log("[Server] Not changing the active player at the end of the turn due to Extra Turn.");
            Context.ExtraTurnsLeft -= 1;
            Context.IsCurrentTurnExtra = true;
        }
        else //regular behavior, go to Next player.
        {
            // 1. End current player's turn
            eventBatch.Add(EventPool.Get<TurnEndedEvent>().Setup(Context.ActivePlayerNetId));
            
            activePlayerIndex = (activePlayerIndex + 1) % players.Count;
            var newActivePlayer = players[activePlayerIndex];
            Context.Setup(newActivePlayer.netId, 0, 0, false);
        }
        eventBatch.Add(EventPool.Get<TurnStartedEvent>().Setup(Context));
    }

    [Server]
    private void SendEventBatch(List<GameEventBase> batch)
    {
        if (batch.Count == 0) return;

        // Use a PooledWriter for efficiency
        using (NetworkWriterPooled writer = NetworkWriterPool.Get())
        {
            writer.Write((ushort)batch.Count);
            foreach (GameEventBase ev in batch)
            {
                writer.WriteGameEvent(ev);
            }

            // Get the raw byte data and send it in the RPC
            RpcReceiveEventBatch(writer.ToArraySegment());
        }

        // Release all events back to the pool after sending
        foreach (GameEventBase ev in batch)
        {
            EventPool.Release(ev);
        }
        batch.Clear();
    }
    
    [ClientRpc]
    private void RpcReceiveEventBatch(ArraySegment<byte> eventBatch)
    {
        if (_clientEventHandler == null)
        {
            _clientEventHandler = FindAnyObjectByType<ClientEventHandler>();
            if (_clientEventHandler == null)
            {
                Debug.LogError("No client game master found.");
                return;
            }
        }
        _clientEventHandler.EnqueueEventBatch(eventBatch);
    }

    public void NotifyBotChestMatched(int chestSkillIdx)
    {
        OnServerChestMatched?.Invoke(Context, chestSkillIdx);
    }
}
}