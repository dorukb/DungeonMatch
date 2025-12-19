using System.Collections.Generic;
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
                //Change Swap method as Easy-Medium
                MediumSwap(board);
            }
        }

        
        private void EasySwap(GameBoard board)
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

        private void MediumSwap(GameBoard board)
        {
            Vector2Int posA = Vector2Int.zero;
            Vector2Int posB = Vector2Int.zero;
            bool foundMove = false;
            int bestPriorityFound = 100;

            for (int x = 0; x < GameBoard.BoardWidth; x++)
            {
                for (int y = 0; y < GameBoard.BoardHeight; y++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);
                    // Only need to check Right and Up to cover all unique adjacent pairs
                    Vector2Int[] neighbors = { currentPos + Vector2Int.right, currentPos + Vector2Int.up };

                    foreach (Vector2Int neighbor in neighbors)
                    {
                        // 1. Use your existing IsValidSwap for bounds and type-mismatch checks
                        if (board.IsValidSwap(currentPos, neighbor))
                        {
                            // 2. Check what tile type this move would actually match
                            Tile resultType = MatchAlgorithm.GetBestMatchedType(board, currentPos, neighbor);

                            if (resultType != Tile.Unknown)
                            {
                                int priority = MatchAlgorithm.GetPriorityWeight(resultType); // Use the weight function from step 1
                    
                                // 3. Prioritize the move (Lower weight = Higher priority)
                                if (priority < bestPriorityFound)
                                {
                                    bestPriorityFound = priority;
                                    posA = currentPos;
                                    posB = neighbor;
                                    foundMove = true;
                                }
                            }
                        }
                    }
                }
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