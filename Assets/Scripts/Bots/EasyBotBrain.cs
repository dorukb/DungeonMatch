using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions
{
    public class EasyBotBrain : BaseBotBrain
    {
        public override List<Vector2Int> FindSwap(GameBoard board)
        {
            Vector2Int posA = Vector2Int.zero;
            Vector2Int posB = Vector2Int.zero;
            List<Vector2Int> foundPos;
            
            // Dumb random search for valid swap (Replace with AIHelper.GetBestMove)
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

            return foundPos;
        }
    }
}