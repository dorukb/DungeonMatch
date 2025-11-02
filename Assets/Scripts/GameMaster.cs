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
    private List<GameEvent> serverGameHistory = new List<GameEvent>();
    
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
        List<GameEvent> eventBatch = new List<GameEvent>();
        
        _gameBoard = new GameBoard();
        var boardState = _gameBoard.FillBoardWithNoMatches();
        eventBatch.Add(GameEvent.GameStarted(boardState));
        eventBatch.Add(GameEvent.TurnStarted(activePlayer.netIdentity.netId));

        // Add to history and send to clients
        SendAndLogBatch(eventBatch);
    }

    [Server]
    public void EndGame(NetworkPlayer winner)
    {
        //TODO: End game is not implemented yet.
        
        if (gameState == GameState.GameEnded) return;

        Debug.Log($"Ending game. Winner: {winner.netId}");
        gameState = GameState.GameEnded;
        activePlayer = null;

        List<GameEvent> eventBatch = new List<GameEvent>();
        
        // We check for null winner in case both disconnected at once
        if (winner != null)
        {
            eventBatch.Add(GameEvent.GameEnded(winner.netId));
        }
        
        SendAndLogBatch(eventBatch);
        
        // TODO : You might want to disconnect players or reset the server here
    }

    // This is the main "transaction" method called by a Player [Command].
    // It processes the move and generates all resulting events.
    [Server]
    public void ProcessPlayerSwap(NetworkConnectionToClient sender, Vector2Int posA, Vector2Int posB)
    {
        List<GameEvent> eventBatch = new List<GameEvent>();
        if (gameState != GameState.Playing || sender.identity != activePlayer.netIdentity)
        {
            Debug.LogWarning($"Player {sender.identity.netId} tried to move out of turn.");
            eventBatch.Add(GameEvent.SwapFailed(activePlayer.netIdentity.netId));
            SendAndLogBatch(eventBatch);
            return; // Not this player's turn, or game isn't running
        }
        // --- 1. Validation ---
        if (!_gameBoard.IsValidSwap(posA, posB))
        {
            // Debug.LogWarning($"[Server] Invalid swap: {posA} <-> {posB}. Not adjacent.");
            eventBatch.Add(GameEvent.SwapFailed(activePlayer.netIdentity.netId));
            SendAndLogBatch(eventBatch);
            return;
        }

        // --- 2. State Change ---
        // This function does all the work AND checks for matches
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
        SendAndLogBatch(eventBatch);
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
    public void EndTurnAndStartNext(List<GameEvent> eventBatch)
    {
        // 1. End current player's turn
        eventBatch.Add(GameEvent.TurnEnded(activePlayer.netIdentity.netId));
        activePlayerIndex = (activePlayerIndex + 1) % players.Count;
        activePlayer = players[activePlayerIndex]; // SyncVar update
        activePlayer.shielded = false;
        eventBatch.Add(GameEvent.TurnStarted(activePlayer.netIdentity.netId));
    }

    [Server]
    private void SendAndLogBatch(List<GameEvent> batch)
    {
        serverGameHistory.AddRange(batch);
        // Send to all clients
        RpcSendEventBatch(batch);
    }

    [ClientRpc]
    void RpcSendEventBatch(List<GameEvent> batch)
    {
        Debug.Log($"Client received a batch of {batch.Count} events.");
        
        if (_clientEventHandler == null)
        {
            _clientEventHandler = FindAnyObjectByType<ClientEventHandler>();
            if (_clientEventHandler == null)
            {
                Debug.LogError("No client game master found.");
                return;
            }
        }
        _clientEventHandler.EnqueueEventBatch(batch);
    }
    
}
}