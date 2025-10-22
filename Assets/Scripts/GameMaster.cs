using DorkyProductions.States;
using Mirror;
using UnityEngine;

namespace DorkyProductions.Core
{
    
    
public enum GameState {
    WaitingForPlayers,
    WaitingForInput,
    ProcessingMove,
    WaitingForSpecialInput, // Chest opening
    GameOver
}
public class GameMaster : NetworkBehaviour
{
    public static GameMaster Instance { get; private set; }
    
    private Context _context;

    public IGameState CurrentState { get; private set; }
    
    // We add hooks to these. When the server sets them,
    // the hook method will run on ALL clients.
    [SyncVar(hook = nameof(OnPlayer1Assigned))] 
    public NetworkIdentity player1;

    [SyncVar(hook = nameof(OnPlayer2Assigned))]
    public NetworkIdentity player2;
    
    public Context context { get; private set; }
    
    [SyncVar(hook = nameof(OnActivePlayerChanged))]
    public NetworkIdentity activePlayer;

    [SyncVar(hook = nameof(OnGameStateChanged))]
    public GameState currentGameState;
    
    private bool extraTurnGranted;
    public float maxTurnTime = 30f;
    public float turnTimer;
    
    // TODO: GameMaster match specific state must be reset manually, when the match ends.
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
    private void Update()
    {
        // CurrentState?.Update(this.context);
    }
    
    [Server]
    public void StartGame()
    {
        Debug.Log("Starting game...");
        // Randomly pick first player
        activePlayer = (Random.Range(0, 2) == 0) ? player1 : player2;
        StartTurn();
    }
    // HOOK METHOD (runs on clients)
    void OnPlayer1Assigned(NetworkIdentity oldId, NetworkIdentity newId)
    {
        // player1 has been set, tell our local UI to refresh!
        // UIManager.Instance?.UpdatePlayerUI();
        Debug.Log($"Player1: {newId} assigned.");
    }

    // HOOK METHOD (runs on clients)
    void OnPlayer2Assigned(NetworkIdentity oldId, NetworkIdentity newId)
    {
        // player2 has been set, tell our local UI to refresh!
        // UIManager.Instance?.UpdatePlayerUI();
        Debug.Log($"Player2: {newId} assigned.");
    }
    [Server]
    public void StartTurn()
    {
        Debug.Log("Starting turn...");
        turnTimer = maxTurnTime;
        currentGameState = GameState.WaitingForInput;
        extraTurnGranted = false;
        
        // You might have a TargetRpc here to tell the active player "Your Turn!"
        // TargetShowTurnStart(activePlayer.connectionToClient);
    }

    [Server]
    public void EndTurn(bool timedOut = false)
    {
        if (currentGameState == GameState.GameOver) return;

        // If an extra turn was granted, just start the *same* player's turn again.
        if (extraTurnGranted && !timedOut)
        {
            StartTurn(); // activePlayer is already correct
            return;
        }

        // Otherwise, pass the turn
        activePlayer = (activePlayer == player1) ? player2 : player1;
        
        // This also changes the state.
        StartTurn();
    }
    void OnActivePlayerChanged(NetworkIdentity oldPlayer, NetworkIdentity newPlayer)
    {
        // Update UI to show whose turn it is.
        // e.g., if (NetworkClient.localPlayer.netIdentity == newPlayer) { ... }
        if (NetworkClient.localPlayer.netId == newPlayer.netId)
        {
            Debug.Log("We are the active player.");
        }
        
    }
    
    void OnGameStateChanged(GameState oldState, GameState newState)
    {
        // Use this to enable/disable the board input
        // if (newState == GameState.WaitingForInput) { EnableBoard(); }
        // else { DisableBoard(); }
    }
    //
    // public void ChangeLocalPlayerName(string playerName)
    // {
    //    _localMiyavPlayer.DisplayName = playerName;
    //    Debug.Log($"Local player is now called: {playerName}");
    // }
    public void TransitionToState(IGameState newState)
    {
        CurrentState?.Exit(this.context);
        CurrentState = newState;
        CurrentState.Enter(this.context);
    }
    public void StartNewGame()
    {
        this.context = new Context(this);
        TransitionToState(new GameStartState());
    }
   
}

}