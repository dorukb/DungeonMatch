using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

namespace DorkyProductions.AI
{
    // Attach this to your Player Prefab (or a variant of it).
    // This script only runs on the Server.
    [RequireComponent(typeof(NetworkPlayer))]
    public class BotBrainManager : NetworkBehaviour
    {
        [Header("Bot Settings")]
        [Tooltip("How long the client will see the 'Thinking' state before the move resolves.")]
        public float VisualThinkingDuration = 1.5f;
        public int skillId = -1;
        private Dictionary<SkillType, Action> _skillMap;
        public override void OnStartServer()
        {
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.OnServerTurnStarted += MakeMove;
                GameMaster.Instance.OnServerChestMatched += SaveChestRewardForOpening;
                
                InitializeSkillMap();
            }
        }

        public override void OnStopServer()
        {
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.OnServerTurnStarted -= MakeMove;
                GameMaster.Instance.OnServerChestMatched -= SaveChestRewardForOpening;
            }
        }
        
        [Server]
        private void MakeMove(Context context, GameBoard board)
        {
            // If it's not my turn, ignore
            if (netId != context.ActivePlayerNetId) return;

            
            Debug.Log("Bot is Making a Move.");
            if (context.ChestsLeft > 0)
            {
                skillId = Random.Range(0, _skillMap.Count);
                SkillType type = (SkillType)skillId;
                // Check if the key exists to prevent crashes
                if (_skillMap.TryGetValue(type, out Action skill))
                {
                    skill.Invoke();
                }
            }
            else
            {
                //Change Swap method as Easy-Medium
                List<Vector2Int> foundPos = new List<Vector2Int>(BotBrain.FindSwap(board));
                GameMaster.Instance.ProcessPlayerSwap(netIdentity, foundPos[0], foundPos[1], VisualThinkingDuration);

                
            }
        }
        
        [Server]
        private void PerformLightningSkill()
        {
            int x = Random.Range(0,5);
            int y = Random.Range(0,5);
            // Execute with delay
            GameMaster.Instance.ProcessPlayerLightningSkillUse(netIdentity, new Vector2Int(x, y), VisualThinkingDuration);
        }

        //TODO: implement phantom match
        [Server]
        private void PerformPhantomMatchSkill()
        {
            int x = Random.Range(0,5);
            int y = Random.Range(0,5);
            // Execute with delay
            GameMaster.Instance.ProcessPlayerLightningSkillUse(netIdentity, new Vector2Int(x, y),
                VisualThinkingDuration);
        }
        
        //TODO: implement phaseshift
        [Server]
        private void PerformPhaseShiftSkill()
        {
            int x = Random.Range(0,5);
            int y = Random.Range(0,5);
            // Execute with delay
            GameMaster.Instance.ProcessPlayerLightningSkillUse(netIdentity, new Vector2Int(x, y),
                VisualThinkingDuration);
        }
        
        [Server]
        private void PerformStoneGuardSkill()
        {
            int amount = RemoteConfigManager.Instance.GetRewardShield();
            GameMaster.Instance.ProcessPlayerStoneGuardSkill(amount, netIdentity, VisualThinkingDuration);
        }
        
        [Server]
        private void PerformSoulReaverSkill()
        {
            int amount = RemoteConfigManager.Instance.GetStolenHealth();
            GameMaster.Instance.ProcessPlayerSoulReaverSkill(amount, netIdentity, VisualThinkingDuration);
        }
        
        [Server]
        private void PerformArcaneSweepSkill()
        {
            int x = Random.Range(0,5);
            int y = Random.Range(0,5);
            Vector2Int tilePos = new Vector2Int(x, y);
            GameMaster.Instance.ProcessPlayerSweepSkill(tilePos, netIdentity, VisualThinkingDuration);
        }
        
        [Server]
        private void PerformArcaneCleaveSkill()
        {
            int x = Random.Range(0,5);
            int y = Random.Range(0,5);
            Vector2Int tilePos = new Vector2Int(x, y);
            GameMaster.Instance.ProcessPlayerCleaveSkill(tilePos, netIdentity, VisualThinkingDuration);
        }
        
        [Server]
        private void SaveChestRewardForOpening(Context ctx, int rewardSkillId)
        {
            // If it's not my turn, ignore
            if (netId != ctx.ActivePlayerNetId) return;
            
            skillId = rewardSkillId;
        }
        
        private void InitializeSkillMap()
        {
            // Map the Enum directly to the Method
            _skillMap = new Dictionary<SkillType, Action>
            {
                { SkillType.Lightning, PerformLightningSkill },
                { SkillType.PhantomMatch, PerformPhantomMatchSkill },
                { SkillType.PhaseShift, PerformPhaseShiftSkill },
                { SkillType.StoneGuard, PerformStoneGuardSkill },
                { SkillType.SoulReaver, PerformSoulReaverSkill },
                { SkillType.ArcaneSweep, PerformArcaneSweepSkill },
                { SkillType.ArcaneCleave, PerformArcaneCleaveSkill }
            };
        }
        
        
    }
}