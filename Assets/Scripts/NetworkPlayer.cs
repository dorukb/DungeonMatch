using System;
using DorkyProductions.Core;
using UnityEngine;
using Mirror;

namespace  DorkyProductions
{
    
public class NetworkPlayer : NetworkBehaviour
{
    // We need a reference to the one-and-only GameBoard.
    // We can find it when we start.
    private GameBoard _gameBoard;
    private PlayerInput _playerInput;
    public override void OnStartClient()
    {
        // Find the board on the client
        _gameBoard = FindAnyObjectByType<GameBoard>();
        if (_gameBoard == null)
        {
            Debug.LogError("Could not find GameBoard!");
        }
    }

    private void Update()
    {
        if (_playerInput == null) return;
        
        // Check if we are allowed to make a move
        if (GameMaster.Instance.activePlayer == this.netIdentity &&
            GameMaster.Instance.currentGameState == GameState.WaitingForInput)
        {
            // Allow swapping
            // Debug.Log("Enabling input");
            _playerInput.EnableControls(); 
        }
        else
        {
            // Debug.Log("Disabling input");
            // Not our turn, or game is processing
            _playerInput.DisableControls();
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _playerInput = FindAnyObjectByType<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("Could not find PlayerInput in the scene, check the Player Prefab.");
            return;
        }
        
        _playerInput.SetPlayer(this);
    }

    // This function is called ONLY on the Host/Server 
    // when this player object is spawned.
    public override void OnStartServer()
    {
        base.OnStartServer();

        // Find the board on the server
        _gameBoard = FindAnyObjectByType<GameBoard>();
        // Find the singleton GameManager
        GameMaster gm = GameMaster.Instance;
        if (gm == null)
        {
            Debug.LogError("GameManager not found!");
            return;
        }

        // Assign this player to an empty slot
        if (gm.player1 == null)
        {
            // this.netIdentity is the NetworkIdentity of this specific Player object
            Debug.Log("First player joined: " + netIdentity.netId);
            gm.player1 = this.netIdentity; 
        }
        else if (gm.player2 == null)
        {
            gm.player2 = this.netIdentity;
            
            // --- IMPORTANT ---
            // The second player has joined! This is the perfect place
            // to tell the GameManager to start the game.
            gm.StartGame();
        }
        else
        {
            // This is a 1v1 game, so a 3rd player is a spectator
            // or should be disconnected.
            Debug.LogWarning("A third player tried to join. Ignoring.");
        }
    }
    // This is called by the local PlayerInput script.
    public void RequestSwap(Vector2Int posA, Vector2Int posB)
    {
        if (!isLocalPlayer) return; // Should never happen, but good check
        
        // Let Client validate first to avoid useless requests.
        if (_gameBoard.IsValidSwap(posA, posB))
        {
            // Not needed for now. But if considerable delay:
            // implement: LOCAL PREDICTION 
            // This is where you would tell your ClientBoardVisualizer
            // to *immediately* start animating the swap.
            // e.g., FindObjectOfType<ClientBoardVisualizer>().AnimatePredictedSwap(posA, posB);
            // If not valid, Server will send back a Revert RPC that will undo the animation.
            
            Debug.Log($"[Local Client] Requesting swap: {posA} <-> {posB}");
            // Now, send the actual request to the server for final validation.
            CmdAttemptSwap(posA, posB);
        }
    }

    [Command]
    private void CmdAttemptSwap(Vector2Int posA, Vector2Int posB)
    {
        if (_gameBoard == null)
        {
            Debug.LogError("Command failed: GameBoard not found on server.");
            return;
        }

        Debug.Log($"[Server] Received swap request: {posA} <-> {posB}");
        
        // Ask the GameBoard (the authority) to process this.
        // We pass 'connectionToClient' so the server knows
        // *who* made the request, in case it needs to reply.
        bool isValid = _gameBoard.ProcessPlayerSwap(connectionToClient, posA, posB);
        
        if (!isValid)
        {
            // The move was invalid (e.g., not adjacent).
            // Tell the *specific client* that made the request to revert.
            TargetRpcRevertSwap(posA, posB);
        }
        // If the move *was* valid, the GameBoard will handle
        // updating the SyncList and sending RPCs itself.
        // We don't need to do anything else here.
    }

    [TargetRpc]
    private void TargetRpcRevertSwap(Vector2Int posA, Vector2Int posB)
    {
        // This runs ONLY on the client that sent the bad command.
        Debug.LogWarning($"[Local Client] Swap REJECTED: {posA} <-> {posB}. Reverting.");

        // --- REVERT PREDICTION ---
        // This is where you tell your ClientBoardVisualizer
        // to animate the tiles swapping *back* to their
        // original positions.
        // e.g., FindObjectOfType<ClientBoardVisualizer>().AnimateSwapBack(posA, posB);
    }
}
}