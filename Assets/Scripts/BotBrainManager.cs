using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
        public override void OnStartServer()
        {
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.OnServerTurnStarted += MakeMove;
                GameMaster.Instance.OnServerChestMatched += SaveChestRewardForOpening;
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
                // TODO: Bot should be able to use Phantom Match.
                PerformLightningSkill();
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
            // Execute with delay
            GameMaster.Instance.ProcessPlayerLightningSkillUse(netIdentity, new Vector2Int(2,2), VisualThinkingDuration);
        }
        
        [Server]
        private void SaveChestRewardForOpening(Context ctx, int rewardSkillId)
        {
            // If it's not my turn, ignore
            if (netId != ctx.ActivePlayerNetId) return;
            
            skillId = rewardSkillId;
        }
        
        
    }
}