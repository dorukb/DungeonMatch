using System;
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
        private int shield = 0;
        private float currentCrossMultiplier = 0f;
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
        public int GetCurrentHealth()
        {
            return health;
        }
        [Server]
        public void TakeDamage(int damage)
        {
            if (health - damage < 0)
            {
                health = 0;
            }
            else
            {
                health -= damage;
            }
        }
        [Server]
        public void Heal(int heal)
        {
            if (health + heal > PLAYER_STARTING_HEALTH)
            {
                health = PLAYER_STARTING_HEALTH;
            }
            else
            {
                health += heal;
            }
        }
        [Server]
        public void LoseShield(int amount)
        {
            shield -= amount;
        }

        [Server]
        public void GainShield(int amount)
        {
            shield += amount;
        }

        [Server]
        public int GetShield()
        {
            return shield;
        }

        [Server]
        public void GainMultiplier(float multiplier)
        {
            currentCrossMultiplier += multiplier;
        }

        [Server]
        public float GetMultiplier()
        {
            return currentCrossMultiplier;
        }
        [Server]
        public void ResetMultiplier()
        {
            currentCrossMultiplier = 0f;
        }
        [Client]
        public void EnableControls()
        {
            _playerInput.EnableControls();
        }

        [Client]
        public void DisableControls()
        {
            _playerInput.DisableControls();
        }

        // This is called by the local PlayerInput script.
        [Client]
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

        [Client]
        public void AttemptSkillUse(int rewardId)
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting Skill Use: {rewardId}");
            DisableControls();
            CmdAttemptChestSkillEffect(rewardId);
        } 
        
        [Command]
        private void CmdAttemptChestSkillEffect(int effectId)
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameMaster not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received Skill Use request: {effectId}");
            GameMaster.Instance.ProcessPlayerSkillUse(connectionToClient, effectId);
        }
    }
}
