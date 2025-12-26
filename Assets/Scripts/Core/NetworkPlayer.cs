using System;
using System.Collections.Generic;
using DorkyProductions.AI;
using DorkyProductions.Skills;
using DorkyProductions.UI;
using UnityEngine;
using Mirror;

namespace DorkyProductions
{

    public class NetworkPlayer : NetworkBehaviour
    {
        private HumanPlayerInput _humanPlayerInput;
        private ClientEventHandler _clientEventHandler;
        private ChestSkillHelper _chestSkillHelper;

        private int health = 0;
        private int shield = 0;
        private float currentCrossMultiplier = 0f;

        public bool IsBot { get; private set; }
        private void Awake()
        {
            var botBrain = GetComponent<BotBrain>();
            IsBot = botBrain != null;
        }

        public override void OnStartServer()
        {
            // When the player object is spawned on the server, register it
            health = RemoteConfigManager.Instance.GetStartingHealth();
            shield = RemoteConfigManager.Instance.GetStartingShield();
            Debug.Log($"OnStartServer for id :{netId}, health:{health}, shield:{shield}");
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
            // TODO: Verify this is not a problem with Bot/AI Player.
            Debug.Log($"OnStartLocalPlayer for netId: {netId}");
            base.OnStartLocalPlayer();
            _humanPlayerInput = FindAnyObjectByType<HumanPlayerInput>();
            if (_humanPlayerInput == null)
            {
                Debug.LogError("Could not find PlayerInput in the scene, check the Player Prefab.");
                return;
            }

            _humanPlayerInput.SetPlayer(this);
            
            _clientEventHandler = FindAnyObjectByType<ClientEventHandler>();
            if (_clientEventHandler == null)
            {
                Debug.LogError("Could not find ClientGameMaster in the scene, check the Player Prefab.");
                return;
            }

            _clientEventHandler.SetLocalPlayer(this);

            _chestSkillHelper = new ChestSkillHelper();
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
        // returns actual gained amount, capped by starting value.
        public int Heal(int heal)
        {
            var startingHealth = RemoteConfigManager.Instance.GetStartingHealth();
            if (health + heal > startingHealth)
            {
                int actualGainedAmount = startingHealth - health;
                health = startingHealth;
                return actualGainedAmount;
            }
            else
            {
                health += heal;
                return heal;
            }
        }
        [Server]
        public void LoseShield(int amount)
        {
            shield -= amount;
        }

        [Server]
        // returns actual gained amount, capped by starting value.
        public int GainShield(int amount)
        {
            var startingShield = RemoteConfigManager.Instance.GetStartingShield();
            if (shield + amount > startingShield)
            {
                int actualGainedAmount = startingShield - shield;
                shield = startingShield;
                return actualGainedAmount;
            }
            else
            {
                shield += amount;
                return amount;
            }
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
            _humanPlayerInput.EnableControls();
        }

        [Client]
        public void DisableSwapControls()
        {
            _humanPlayerInput.DisableSwapControls();
        }

        // This is called by the local PlayerInput script.
        [Client]
        public void RequestSwap(Vector2Int posA, Vector2Int posB)
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting swap: {posA} <-> {posB}");
            DisableSwapControls();
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
            GameMaster.Instance.ProcessPlayerSwap(connectionToClient.identity, posA, posB);
        }

        [Client]
        private void AttemptLightningSkillUse(Vector2Int targetTilePos)
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting Lightning Skill Use");
            DisableSwapControls();
            CmdAttemptLightningSkill(targetTilePos);
        } 
        [Client]
        private void AttemptPhantomSkillUse(List<Vector2Int> targetTilePos)
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting Phantom Skill Use");
            DisableSwapControls();
            CmdAttemptPhantomMatchSkill(targetTilePos);
        } 
        
        [Command]
        private void CmdAttemptLightningSkill(Vector2Int targetTilePos)
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameMaster not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received Lightning Skill Use request");
            GameMaster.Instance.ProcessPlayerLightningSkillUse(connectionToClient.identity, targetTilePos);
        }
        
        [Command]
        private void CmdAttemptPhantomMatchSkill(List<Vector2Int> targetTiles)
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameMaster not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received Phantom Match Skill Use request");
            GameMaster.Instance.ProcessPlayerPhantomMatchSkill(connectionToClient.identity, targetTiles);
        }

        public void ActivateLightningInput()
        {
            _humanPlayerInput.ActivateLightningInput();
        }

        public void ActivatePhantomMatchInput()
        {
            _humanPlayerInput.ChangePhantomInputState(true);
        }
        public void OnTileSelectedForLightning(Vector2Int targetTilePos)
        {
            AudioManager.Instance.PlaySFX(SFXType.Lightning);
            UIMediator.OnPlayerChestEnded.Invoke();
            AttemptLightningSkillUse(targetTilePos);
            _humanPlayerInput.DisableLightningInput();

        }
        public void OnTileSelectedForPhantom(TileView selectedTile)
        {          
            // if this tile was already selected, unselect it.
           bool shouldTriggerSkill = _chestSkillHelper.OnNewTileSelected(selectedTile);
           if (shouldTriggerSkill)
           {
               _humanPlayerInput.ChangePhantomInputState(false);
               UIMediator.OnPlayerChestEnded.Invoke();
               AttemptPhantomSkillUse(_chestSkillHelper.GetSelectedTilePositions());
               _chestSkillHelper.ClearSelectedTiles();
           }

        }
    }
}
