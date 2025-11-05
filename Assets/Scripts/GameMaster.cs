using System;
using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace  DorkyProductions
{
    
// The possible states of the game, synced to all clients
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

    public TileDatabase TileDatabase;
    
    [Tooltip("The player who is currently allowed to make a move")]
    public NetworkPlayer activePlayer;

    [Header("Game Settings")]
    [Tooltip("The number of players required to start a game")]
    public int requiredPlayers = 2;

    // --- Server-Only State ---
    // List of all connected players (server-side only)
    private List<NetworkPlayer> players = new List<NetworkPlayer>();
    private List<GameEventBase> serverGameHistory = new List<GameEventBase>();
    
    // Server-side index for tracking turns
    private int activePlayerIndex = 0;
    
    private GameBoard _gameBoard; 
    private ClientEventHandler _clientEventHandler;
    void Awake()
    {
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
        Application.targetFrameRate = 144;
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
            // EndGame(players.FirstOrDefault()); // The remaining player is the winner
        }
    }
    
    [Server]
    private void StartGame()
    {
        if (gameState != GameState.WaitingForPlayers) return;

        Debug.Log("Starting game...");
        gameState = GameState.Playing;

        activePlayerIndex = 0;
        activePlayer = players[activePlayerIndex];

        // 2. Create the first event batch
        List<GameEventBase> eventBatch = new List<GameEventBase>();
        
        _gameBoard = new GameBoard();
        var boardState = _gameBoard.FillBoardWithNoMatches();
        
        eventBatch.Add(EventPool.Get<GameStartedEvent>().Setup(boardState));
        eventBatch.Add(EventPool.Get<TurnStartedEvent>().Setup(activePlayer.netId));

        // Add to history and send to clients
        SendEventBatch(eventBatch);
    }

    [Server]
    public void EndGame(NetworkPlayer winner)
    {
        //TODO: End game is not implemented yet.
        
        if (gameState == GameState.GameEnded) return;

        Debug.Log($"[SERVER] Ending game. Winner: {winner.netId}");
        gameState = GameState.GameEnded;
        activePlayer = null;

        List<GameEventBase> eventBatch = new List<GameEventBase>();
        
        // We check for null winner in case both disconnected at once
        if (winner != null)
        {
            eventBatch.Add(EventPool.Get<GameEndedEvent>().Setup(winner.netId));
        }
        
        SendEventBatch(eventBatch);
        
        // TODO : You might want to disconnect players or reset the server here
    }

    // This is the main "transaction" method called by a Player [Command].
    // It processes the move and generates all resulting events.
    [Server]
    public void ProcessPlayerSwap(NetworkConnectionToClient sender, Vector2Int posA, Vector2Int posB)
    {
        List<GameEventBase> eventBatch = new List<GameEventBase>();
        bool canMakeMove = (gameState == GameState.Playing) && (sender.identity == activePlayer.netIdentity);
        bool isValidMove = canMakeMove && _gameBoard.IsValidSwap(posA, posB);
        if (!isValidMove)
        {
            Debug.LogWarning($"Player {sender.identity.netId} tried to move out of turn or the swap was not valid.");
            eventBatch.Add(EventPool.Get<SwapDeniedEvent>().Setup(activePlayer.netId));
            // we send this to both players, is that a problem?
            SendEventBatch(eventBatch);
            return;
        }

        // Core algorithm.
        bool didMatchOccur = _gameBoard.ProcessSwapMove(posA, posB, sender.identity, eventBatch);
        if (didMatchOccur)
        {
            // Debug.Log($"[Server] Swap {posA} <-> {posB} successful. Board processed.");
        }
        else
        {
            // Debug.Log($"[Server] Swap {posA} <-> {posB} resulted in no match. This is totally fine.");
        }

        EndTurnAndStartNext(eventBatch);
        SendEventBatch(eventBatch);
    }

    
    // This assumes a 2-player game.
    [Server]
    public NetworkPlayer GetInactivePlayer()
    {
        if (players.Count != 2)
        {
            Debug.LogError($"GetInactivePlayer assumes there are 2 players. but we have: {players.Count}");
        }
        return players.Find(t => t.netId != activePlayer.netId);
    }
    
    [Server]
    public void EndTurnAndStartNext(List<GameEventBase> eventBatch)
    {
        // 1. End current player's turn
        eventBatch.Add(EventPool.Get<TurnEndedEvent>().Setup(activePlayer.netId));
        
        activePlayerIndex = (activePlayerIndex + 1) % players.Count;
        activePlayer = players[activePlayerIndex]; // SyncVar update
        eventBatch.Add(EventPool.Get<TurnStartedEvent>().Setup(activePlayer.netId));
    }

    [Server]
    private void SendEventBatch(List<GameEventBase> batch)
    {
        if (batch.Count == 0) return;

        // Use a PooledWriter for efficiency
        using (NetworkWriterPooled writer = NetworkWriterPool.Get())
        {
            // First, write the number of events
            writer.Write((ushort)batch.Count);

            // Loop and write each event using our custom serializer
            foreach (GameEventBase ev in batch)
            {
                writer.WriteGameEvent(ev);
            }

            // Get the raw byte data and send it in the RPC
            RpcReceiveEventBatch(writer.ToArraySegment());
        }

        // --- CRITICAL ---
        // Release all events back to the pool after sending
        foreach (GameEventBase ev in batch)
        {
            EventPool.Release(ev);
        }
        batch.Clear();
    }
    
    [ClientRpc]
    void RpcReceiveEventBatch(ArraySegment<byte> eventBatch)
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
    
}
}