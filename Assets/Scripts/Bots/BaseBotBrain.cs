using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions
{
    public interface IBotBrain
    {
        List<Vector2Int> FindSwap(GameBoard board);
        Vector2Int GetLightningSkillInput(GameBoard board);
        List<Vector2Int> GetPhantomMatchSkillInput(GameBoard board);
    }
    
    public abstract class BaseBotBrain : IBotBrain
    {
        protected List<Vector2Int> RandomSwap(Vector2Int posA, Vector2Int posB, GameBoard board)
        {
            // Try up to 250 random combinations
            for (int i = 0; i < 250; i++)
            {
                // 1. Pick random tile
                posA = new Vector2Int(Random.Range(0, 5), Random.Range(0, 5));
                
                // 2. Pick a random neighbor (Up, Right, Down, Left)
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                posB = posA + dirs[Random.Range(0, 4)];

                if (board.IsValidSwap(posA, posB))
                {
                    // This acts like a 'break' - it stops the loop and the method immediately
                    return new List<Vector2Int> { posA, posB };
                }
            }

            // 4. Emergency Fallback
            // If we tried 250 times and found nothing, return an empty list 
            // to prevent the game from freezing.
            Debug.LogWarning("Bot could not find any valid random moves!");
            return new List<Vector2Int> { Vector2Int.one , Vector2Int.right};
        }

        public abstract List<Vector2Int> FindSwap(GameBoard board);

        public virtual Vector2Int GetLightningSkillInput(GameBoard board)
        {
            // random tile.
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
    }
}