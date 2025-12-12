using UnityEngine;
using Mirror;

namespace DorkyProductions.AI
{
    // Attach this to your Player Prefab (or a variant of it).
    // This script only runs on the Server.
    [RequireComponent(typeof(NetworkPlayer))]
    public class BotBrain : NetworkBehaviour
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
                PerformLightningSkill();
            }
            else
            {
                Swap(board);
            }
        }

        private void Swap(GameBoard board)
        {
            // TODO: Implement actual AI Swap Logic.
            Vector2Int posA = Vector2Int.zero;
            Vector2Int posB = Vector2Int.zero;
            bool foundMove = false;
            
            // Dumb random search for valid swap (Replace with AIHelper.GetBestMove)
            int attempts = 0;
            while(!foundMove && attempts < 50)
            {
                posA = new Vector2Int(Random.Range(0, 5), Random.Range(0, 5)); // Assuming 5x5
                // Try neighbors
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right };
                Vector2Int offset = dirs[Random.Range(0, 2)];
                posB = posA + offset;

                if (board.IsValidSwap(posA, posB))
                {
                    foundMove = true;
                }
                attempts++;
            }

            // --- EXECUTION ---
            // We call GameMaster directly. 
            // We pass 'VisualThinkingDuration' to inject the 'OpponentThinkingEvent'
            GameMaster.Instance.ProcessPlayerSwap(netIdentity, posA, posB, VisualThinkingDuration);
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