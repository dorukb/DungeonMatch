using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

namespace  DorkyProductions
{
    
// The possible states of the game, synced to all clients
public enum GameState
{
    WaitingForPlayers,
    Playing,
    GameEnded
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [Tooltip("The current state of the game")]
    [SyncVar]
    public GameState gameState = GameState.WaitingForPlayers;

    [Tooltip("The player who is currently allowed to make a move")]
    [SyncVar]
    public NetworkPlayer activePlayer;

    [Header("Game Settings")]
    [Tooltip("The number of players required to start a game")]
    public int requiredPlayers = 2;

    // --- Server-Only State ---
    // List of all connected players (server-side only)
    private List<NetworkPlayer> players = new List<NetworkPlayer>();
    
    // The server's authoritative history of all events
    private List<GameEvent> serverGameHistory = new List<GameEvent>();
    
    // Server-side index for tracking turns
    private int activePlayerIndex = 0;
    
    private GameBoard board; 
    private ClientGameMaster clientGameMaster;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
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
            EndGame(players.FirstOrDefault()); // The remaining player is the winner
        }
    }


    [Server]
    private void StartGame()
    {
        if (gameState != GameState.WaitingForPlayers) return;

        Debug.Log("Starting game...");
        gameState = GameState.Playing; // This SyncVar will update all clients

        // 1. Pick the first player (e.g., the first to connect)
        activePlayerIndex = 0;
        activePlayer = players[activePlayerIndex]; // This SyncVar will update all clients

        // 2. Create the first event batch
        List<GameEvent> eventBatch = new List<GameEvent>();
        
        var boardState = board.FillBoardWithNoMatches();
        eventBatch.Add(GameEvent.GameStarted(boardState));
        eventBatch.Add(GameEvent.TurnStarted(activePlayer.netIdentity.netId));

        // 3. Add to history and send to clients
        SendAndLogBatch(eventBatch);
    }

    [Server]
    private void EndGame(NetworkPlayer winner)
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
            // eventBatch.Add(GameEvent.GameEnded(winner.netIdentity));
        }
        
        // SendAndLogBatch(eventBatch);
        
        // You might want to disconnect players or reset the server here
    }

    // This is the main "transaction" method called by a Player [Command].
    // It processes the move and generates all resulting events.
    [Server]
    public void ProcessPlayerSwap(NetworkConnectionToClient sender, Vector2Int posA, Vector2Int posB)
    {
        if (gameState != GameState.Playing || sender.identity != activePlayer.netIdentity)
        {
            Debug.LogWarning($"Player {sender.identity.netId} tried to move out of turn.");
            return; // Not this player's turn, or game isn't running
        }
        // --- 1. Validation ---
        if (!board.IsValidSwap(posA, posB))
        {
            // Debug.LogWarning($"[Server] Invalid swap: {posA} <-> {posB}. Not adjacent.");
            return;
        }

        List<GameEvent> eventBatch = new List<GameEvent>();
        // --- 2. State Change ---
        // This function does all the work AND checks for matches
        bool didMatchOccur = board.ProcessSwapMove(posA, posB, sender.identity, eventBatch);
        if (didMatchOccur)
        {
            // Debug.Log($"[Server] Swap {posA} <-> {posB} successful. Board processed.");
        }
        else
        {
            // Debug.Log($"[Server] Swap {posA} <-> {posB} resulted in no match. This is totally fine.");
        }

        SendAndLogBatch(eventBatch);
    }
    [Server]
    private List<GameEvent> EndTurnAndStartNext()
    {
        List<GameEvent> events = new List<GameEvent>();
        
        // 1. End current player's turn
        events.Add(GameEvent.TurnEnded(activePlayer.netIdentity.netId));
        
        // 2. Find next player
        activePlayerIndex = (activePlayerIndex + 1) % players.Count;
        activePlayer = players[activePlayerIndex]; // SyncVar update
        
        // 3. Start new player's turn
        events.Add(GameEvent.TurnStarted(activePlayer.netIdentity.netId));
        
        return events;
    }

    [Server]
    private void SendAndLogBatch(List<GameEvent> batch)
    {
        // Add to the server's authoritative history
        serverGameHistory.AddRange(batch);
        
        // Send to all clients
        RpcSendEventBatch(batch);
    }

    [ClientRpc]
    void RpcSendEventBatch(List<GameEvent> batch)
    {
        // This is called on ALL clients
        Debug.Log($"Client received a batch of {batch.Count} events.");
        
        // Find the local ClientGameManager and give it the batch
        // (This assumes you have the ClientGameManager.cs from our previous talk)

        if (clientGameMaster == null)
        {
            clientGameMaster = FindAnyObjectByType<ClientGameMaster>();
            if (clientGameMaster == null)
            {
                Debug.LogError("No client game master found.");
            }
        }
        clientGameMaster.EnqueueEventBatch(batch);
    }
    
}
}