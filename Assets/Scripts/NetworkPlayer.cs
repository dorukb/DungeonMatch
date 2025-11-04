using UnityEngine;
using Mirror;

namespace DorkyProductions
{

    public class NetworkPlayer : NetworkBehaviour
    {
        private PlayerInput _playerInput;
        private ClientEventHandler _clientEventHandler;

        public static readonly int PLAYER_STARTING_HEALTH = 20;
        private int health = PLAYER_STARTING_HEALTH; // server only.
        private bool _hasShield = false;
        
        public override void OnStartServer()
        {
            // When the player object is spawned on the server, register it
            GameMaster.Instance.RegisterPlayer(this);
        }

        public override void OnStopServer()
        {
            // When the player disconnects, unregister it
            // Use '?.' for safety in case GameManager is destroyed first
            GameMaster.Instance?.UnregisterPlayer(this);
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
            
            _clientEventHandler = FindAnyObjectByType<ClientEventHandler>();
            if (_clientEventHandler == null)
            {
                Debug.LogError("Could not find ClientGameMaster in the scene, check the Player Prefab.");
                return;
            }

            _clientEventHandler.SetLocalPlayer(this);
            
        }


        [Server]
        public ushort GetCurrentHealth()
        {
            return (ushort)health;
        }
        [Server]
        public void TakeDamage(int damage)
        {
            health =  Mathf.Clamp(health - damage, 0, PLAYER_STARTING_HEALTH);
        }

        [Server]
        public void Heal(int heal)
        {
            health = Mathf.Clamp(health + heal, health, PLAYER_STARTING_HEALTH);
        }
        
        public void EnableControls()
        {
            _playerInput.EnableControls();
        }

        public void DisableControls()
        {
            _playerInput.DisableControls();
        }

        // This is called by the local PlayerInput script.
        public void RequestSwap(Vector2Int posA, Vector2Int posB)
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting swap: {posA} <-> {posB}");
            DisableControls();
            CmdAttemptSwap(posA, posB);
        }

        [Command]
        private void CmdAttemptSwap(Vector2Int posA, Vector2Int posB)
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameBoard not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received swap request: {posA} <-> {posB}");
            GameMaster.Instance.ProcessPlayerSwap(connectionToClient, posA, posB);
        }

        public bool HasShield()
        {
            return _hasShield;
        }

        public void DeactivateShield()
        {
            _hasShield = false;
        }

        public void ActivateShield()
        {
            _hasShield = true;
        }
    }
}
