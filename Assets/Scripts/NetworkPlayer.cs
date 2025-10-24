using UnityEngine;
using Mirror;

namespace DorkyProductions
{

    public class NetworkPlayer : NetworkBehaviour
    {
        private PlayerInput _playerInput;
        private ClientGameMaster _clientGameMaster;
        private bool isMyTurn = false;

        public override void OnStartServer()
        {
            // When the player object is spawned on the server, register it
            GameManager.Instance.RegisterPlayer(this);
        }

        public override void OnStopServer()
        {
            // When the player disconnects, unregister it
            // Use '?.' for safety in case GameManager is destroyed first
            GameManager.Instance?.UnregisterPlayer(this);
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
            
            _clientGameMaster = FindAnyObjectByType<ClientGameMaster>();
            if (_clientGameMaster == null)
            {
                Debug.LogError("Could not find ClientGameMaster in the scene, check the Player Prefab.");
                return;
            }

            _clientGameMaster.SetLocalPlayer(this);
            
        }
        private void Update()
        {
            if (_playerInput == null) return;

            // Check if we are allowed to make a move
            if (isMyTurn)
            {
                _playerInput.EnableControls();
            }
            else
            {
                _playerInput.DisableControls();
            }
        }

        public void StartPlayerTurn()
        {
            isMyTurn = true;
        }

        public void EndPlayerTurn()
        {
            isMyTurn = false;
        }

        // This is called by the local PlayerInput script.
        public void RequestSwap(Vector2Int posA, Vector2Int posB)
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting swap: {posA} <-> {posB}");
            CmdAttemptSwap(posA, posB);
        }

        [Command]
        private void CmdAttemptSwap(Vector2Int posA, Vector2Int posB)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("Command failed: GameBoard not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received swap request: {posA} <-> {posB}");
            GameManager.Instance.ProcessPlayerSwap(connectionToClient, posA, posB);
        }
    }
}
