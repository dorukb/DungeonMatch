using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions
{
    public class MediumBotBrain : BaseBotBrain
    {
        public override List<Vector2Int> FindSwap(GameBoard board)
        {
            Vector2Int posA = Vector2Int.zero;
            Vector2Int posB = Vector2Int.zero;
            List<Vector2Int> foundPos = new List<Vector2Int>();

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
            
            foundPos.Add(posA);
            foundPos.Add(posB);

            if (foundMove == false)
            {
                //if no possible match then select a random swap.
                Debug.Log("no possible match so swap randomly");
                foundPos = RandomSwap(board);
                if (foundPos.Count == 2)
                {
                    posA = foundPos[0];
                    posB = foundPos[1];
                }
                else
                {
                    Debug.LogError("No position found to be swapped. You really messed up");
                }
            }
        
            return foundPos;
        }
    }
}