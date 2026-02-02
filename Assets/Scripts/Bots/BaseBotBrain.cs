using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions
{
    public interface IBotBrain
    {
        List<Vector2Int> FindSwap(GameBoard board);
        Vector2Int GetRandomTile();
        List<Vector2Int> GetPhantomMatchSkillInput(GameBoard board);
        List<Vector2Int> GetPhaseShiftSkillInput(GameBoard board);
    }
    
    public abstract class BaseBotBrain : IBotBrain
    {
        protected List<Vector2Int> RandomSwap(GameBoard board)
        {
            // 1. Pick a random tile that isn't on the edge 
            // This ensures all 8 neighbors are within board bounds (0-4)
            int randomX = Random.Range(1, GameBoard.BoardWidth - 1);
            int randomY = Random.Range(1, GameBoard.BoardHeight - 1);
            Vector2Int posA = new Vector2Int(randomX, randomY);

            // 2. Define the 8 relative neighbor offsets
            Vector2Int[] neighbors = {
                new Vector2Int(-1,  1), Vector2Int.up, new Vector2Int(1,  1), // Top row
                Vector2Int.left,                               Vector2Int.right, // Middle row
                new Vector2Int(-1, -1), Vector2Int.down, new Vector2Int(1, -1)  // Bottom row
            };

            // 3. Shuffle or loop through neighbors to find a valid swap
            foreach (Vector2Int offset in neighbors)
            {
                Vector2Int posB = posA + offset;

                // Since we picked range (1, width-1), posB is guaranteed to be on the board
                if (board.IsValidSwap(posA, posB))
                {
                    return new List<Vector2Int> { posA, posB };
                }
            }

            // 4. Emergency Fallback (should not be happen)
            Debug.Log("Random swap failed. 3*3 block has the same type.");
            return new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(2, 1) };
        }

        public abstract List<Vector2Int> FindSwap(GameBoard board);
        
        public virtual Vector2Int GetRandomTile()
        {
            int x = Random.Range(0,5);
            int y = Random.Range(0,5);
            return new Vector2Int(x, y);
        }

        public virtual List<Vector2Int> GetPhantomMatchSkillInput(GameBoard board)
        {
            // try to find 3 of the same tile type, based on your priorities.
            bool foundMove = false;
            List<Tile> priorityTiles = new List<Tile> { Tile.Cross, Tile.Chest, Tile.Attack, Tile.Heal, Tile.Shield };
            
            foreach (Tile keyTile in priorityTiles)
            {            
                List<Vector2Int> selectedPositions = new List<Vector2Int>();
                for (int x = 0; x < GameBoard.BoardWidth; x++)
                {
                    for (int y = 0; y < GameBoard.BoardHeight; y++)
                    {
                        Vector2Int currentPos = new Vector2Int(x, y);
                        var tile = board.GetTileAt(currentPos);
                        if (tile.type == keyTile)
                        {
                            selectedPositions.Add(currentPos);
                            if (selectedPositions.Count == 3)
                            {
                                return selectedPositions;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public List<Vector2Int> GetPhaseShiftSkillInput(GameBoard board)
        {
            Vector2Int bestA = Vector2Int.zero;
            Vector2Int bestB = Vector2Int.zero;
            int bestRank = 99; 
            bool found = false;
            int boardSize = GameBoard.BoardHeight * GameBoard.BoardWidth;
            int height = GameBoard.BoardHeight;

            for (int i = 0; i < boardSize; i++)
            {
                Vector2Int posA = new Vector2Int(i / height, i % height); 
                int rankA = MatchAlgorithm.GetPriorityWeight(board.GetTileAt(posA).type);

                // Optimization: If this tile is worse than what we already found, don't check it
                if (rankA >= bestRank) continue;

                for (int j = 0; j < boardSize; j++)
                {
                    if (i == j) continue; 
                    Vector2Int posB = new Vector2Int(j / height, j % height);

                    if (board.IsValidSwap(posA, posB))
                    {
                        if (MatchAlgorithm.FindMatchesAfterSwap(board, posA, posB).Count > 0)
                        {
                            bestRank = rankA;
                            bestA = posA;
                            bestB = posB;
                            found = true;

                            // Shortcut: We found the best possible move
                            if (bestRank == 0) return new List<Vector2Int> {bestA, bestB};
                        }
                    }
                }
            }

            return found ? new List<Vector2Int> {bestA, bestB} : new List<Vector2Int> {GetRandomTile(), GetRandomTile()};
        }

    }
}