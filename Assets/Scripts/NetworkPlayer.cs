using UnityEngine;
using Mirror;

namespace DorkyProductions
{

    public class NetworkPlayer : NetworkBehaviour
    {
        private PlayerInput _playerInput;
        private ClientEventHandler _clientEventHandler;

        public readonly int PLAYER_STARTING_HEALTH = 20;
        private int health = 0;
        
        public System.Action<uint, int> OnHealthChange;
        public System.Action<uint, int> OnDealtDamage;
        public System.Action<uint, int> OnReceivedDamage;
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
            health = PLAYER_STARTING_HEALTH;
            
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
        public void TakeDamage(int damage)
        {
            health -= damage;
        }

        [Server]
        public void Heal(int heal)
        {
            health = Mathf.Clamp(health, health, health + heal);
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

        public void DealDamage(int dealtAmount)
        {
            // we attacked the other player.
            // TODO: Fire events for SFX, UI bla bla.
            OnDealtDamage?.Invoke(netIdentity.netId, dealtAmount);
        } 
        public void ReceiveDamage(int dealtAmount)
        {
            // Other played dealt damage to us.
            health -= dealtAmount;
            var uiController = FindAnyObjectByType<PlayerAvatarUIController>();
            if (uiController != null)
            {
                uiController.UpdateLocalPlayerHealth(health, PLAYER_STARTING_HEALTH);
            }
            OnHealthChange?.Invoke(netIdentity.netId, health);
            OnReceivedDamage?.Invoke(netIdentity.netId, dealtAmount);
        }

        public void ReceiveHeal(int healAmount)
        {
            health += healAmount;
            var uiController = FindAnyObjectByType<PlayerAvatarUIController>();
            if (uiController != null)
            {
                uiController.UpdateLocalPlayerHealth(health, PLAYER_STARTING_HEALTH);
            }
            OnHealthChange?.Invoke(netIdentity.netId, health);
        }
    }
}
