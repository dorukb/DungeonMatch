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
        private ClientChestSkillHelper _clientChestSkillHelper;

        private int health = 0;
        private int shield = 0;
        private float currentCrossMultiplier = 0f;
        private int coin = 0;
    
        [Serializable]
        public class PlayerData { public int coins; }

        public string guestID;
        public bool IsBot { get; private set; }

        private void Awake()
        {
            var botBrain = GetComponent<BotNetworkPlayer>();
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
            
            int savedCoins = PlayerLocalSave.GetSavedCoins();
            
            CmdSyncCoinsToServer(savedCoins);
            
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

            _clientChestSkillHelper = new ClientChestSkillHelper();
        }
        
        
        [Command]
        void CmdSyncCoinsToServer(int amount)
        {
            this.coin = amount;
        }

        // 2. When coins are added, save them ONLY if this is the local player
        [Server]
        public void AddCoins(int amount)
        {
            coin += amount;
            // Tell the client to save this new total to their Registry/File
            RpcSaveCoinsToDisk(coin);
        }
        
        [Server]
        public void LoseCoins(int amount)
        {
            if (coin - amount < 0)
            {
                coin = 0;
            }
            else
            {
                coin -= amount;
            }    
            RpcSaveCoinsToDisk(coin);
        }
        
        [ClientRpc]
        void RpcSaveCoinsToDisk(int total)
        {
            if (isLocalPlayer) 
            {
                PlayerLocalSave.SaveCoins(total);
                Debug.Log("Saved " + total + " coins to local device.");
            }
        }
        [Server]
        public int GetCoins()
        {
            return coin;
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

        [Client]
        public void EnableTutorialControls()
        {
            Debug.Log("activating tutorial controls for human player.");
            _humanPlayerInput.EnableTutorialControls();
        }
        // This is called by the local PlayerInput script.
        [Client]
        public void RequestSwap(Vector2Int posA, Vector2Int posB)
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            // Debug.Log($"[Local Client] Requesting swap: {posA} <-> {posB}");
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

            // Debug.Log($"[Server] Received swap request: {posA} <-> {posB}");
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
        
        [Client]
        private void AttemptPhaseShiftSkillUse(List<Vector2Int> targetTilePos)
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting Phantom Skill Use");
            DisableSwapControls();
            CmdAttemptPhaseShiftSkill(targetTilePos);
        }
        
        [Client]
        private void AttemptStoneGuardSkillUse()
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting Phantom Skill Use");
            DisableSwapControls();
            
            CmdAttemptStoneGuardSkill();
        }
        
        [Client]
        private void AttemptSoulReaverSkillUse()
        {
            if (!isLocalPlayer) return; // Should never happen, but good check
            Debug.Log($"[Local Client] Requesting Phantom Skill Use");
            DisableSwapControls();
            
            CmdAttemptSoulReaverSkill();
        }

        [Client]
        private void AttemptSweepSkillUse(Vector2Int targetTilePos)
        {
            if (!isLocalPlayer) return;
            Debug.Log($"[Local Client] Requesting Sweep Skill Use");
            DisableSwapControls();
            
            CmdAttemptSweepSkill(targetTilePos);
        }
        
        [Client]
        private void AttemptCleaveSkillUse(Vector2Int targetTilePos)
        {
            if (!isLocalPlayer) return;
            Debug.Log($"[Local Client] Requesting Cleave Skill Use");
            DisableSwapControls();
            
            CmdAttemptCleaveSkill(targetTilePos);
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
        
        [Command]
        private void CmdAttemptPhaseShiftSkill(List<Vector2Int> targetTiles)
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameMaster not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received Phase Shift Skill Use request");
            GameMaster.Instance.ProcessPlayerPhaseShiftSkill(connectionToClient.identity, targetTiles);
        }
        
        [Command]
        private void CmdAttemptStoneGuardSkill()
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameMaster not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received Stone Guard Skill Use request");
            int amount = RemoteConfigManager.Instance.GetRewardShield();
            GameMaster.Instance.ProcessPlayerStoneGuardSkill(amount, connectionToClient.identity);
        }
        
        [Command]
        private void CmdAttemptSoulReaverSkill()
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameMaster not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received Soul Reaver Skill Use request");
            int healAmount = RemoteConfigManager.Instance.GetStolenHealth();
            GameMaster.Instance.ProcessPlayerSoulReaverSkill(healAmount, connectionToClient.identity);
        }

        [Command]
        private void CmdAttemptSweepSkill(Vector2Int targetTilePos)
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameMaster not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received Arcane Sweep Skill Use request");
       
            GameMaster.Instance.ProcessPlayerSweepSkill(targetTilePos, connectionToClient.identity);
        }
        
        [Command]
        private void CmdAttemptCleaveSkill(Vector2Int targetTilePos)
        {
            if (GameMaster.Instance == null)
            {
                Debug.LogError("Command failed: GameMaster not found on server.");
                return;
            }
            
            Debug.Log($"[Server] Received Arcane Cleave Skill Use request");
            
            GameMaster.Instance.ProcessPlayerCleaveSkill(targetTilePos, connectionToClient.identity);
        }

        public void ActivateLightningInput()
        {
            _humanPlayerInput.ActivateLightningInput();
        }

        public void ActivatePhantomMatchInput()
        {
            _humanPlayerInput.ChangePhantomInputState(true);
        }

        public void ActivatePhaseShiftInput()
        {
            _humanPlayerInput.ChangePhaseShiftInputState(true);
        }

        public void ActivateArcaneSweepInput()
        {
            _humanPlayerInput.ChangeSweepInputState(true);
        }
        
        public void ActivateArcaneCleaveInput()
        {
            _humanPlayerInput.ChangeCleaveInputState(true);
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
            bool shouldTriggerSkill = _clientChestSkillHelper.OnNewTileSelected(selectedTile, SkillType.PhantomMatch);
            if (shouldTriggerSkill)
            {
                _humanPlayerInput.ChangePhantomInputState(false);
                UIMediator.OnPlayerChestEnded.Invoke();
                AttemptPhantomSkillUse(_clientChestSkillHelper.GetSelectedTilePositions());
                _clientChestSkillHelper.ClearSelectedTiles();
            }

        }

        public void OnTileSelectedForPhaseShift(TileView selectedTile)
        {
            bool shouldTriggerSkill = _clientChestSkillHelper.OnNewTileSelected(selectedTile, SkillType.PhaseShift);
            if (shouldTriggerSkill)
            {
                _humanPlayerInput.ChangePhaseShiftInputState(false);
                UIMediator.OnPlayerChestEnded.Invoke();
                AttemptPhaseShiftSkillUse(_clientChestSkillHelper.GetSelectedTilePositions());
                _clientChestSkillHelper.UnselectTiles();
                _clientChestSkillHelper.ClearSelectedTiles();
            }
            
        }
        public void OnUseStoneGuard()
        {
            AudioManager.Instance.PlaySFX(SFXType.Lightning);
            UIMediator.OnPlayerChestEnded.Invoke(); 
            AttemptStoneGuardSkillUse();
        }

        public void OnUseSoulReaver()
        {
            AudioManager.Instance.PlaySFX(SFXType.Lightning);
            UIMediator.OnPlayerChestEnded.Invoke(); 
            AttemptSoulReaverSkillUse();
        }
        
        public void OnTileSelectedForSweep(Vector2Int targetTilePos)
        {
            // if this tile was already selected, unselect it.
            /*bool shouldTriggerSkill = _chestSkillHelper.OnNewTileSelected(selectedTile, SkillType.ArcaneSweep);
            if (shouldTriggerSkill)
            {
                _humanPlayerInput.ChangeSweepInputState(false);
                UIMediator.OnPlayerChestEnded.Invoke();
                AttemptSweepSkillUse(_chestSkillHelper.GetSelectedTilePositions()[0]);
                _chestSkillHelper.ClearSelectedTiles();
            }*/
            
            UIMediator.OnPlayerChestEnded.Invoke();
            AttemptSweepSkillUse(targetTilePos);
            _humanPlayerInput.ChangeSweepInputState(false);
            
        }

        public void OnTileSelectedForCleave(Vector2Int targetTilePos)
        {
            UIMediator.OnPlayerChestEnded.Invoke();
            AttemptCleaveSkillUse(targetTilePos);
            _humanPlayerInput.ChangeCleaveInputState(false);
        }
    }
}
