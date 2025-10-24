using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions
{

// This struct holds the data for ONE match (e.g., a 4-in-a-row)
    public class MatchResult
    {
        // TODO: Consider enum types for tiles, also update them on the SO setup.
        public int tileTypeID;
        public int matchCount;
        // We store the actual positions for clearing them
        // TODO: maybe make positions HashSet, to prevent duplicate memory usage during find all matches.
        public List<Vector2Int> positions;

        public string ToString()
        {
            return TileDefinitionSO.ToString(tileTypeID);
        }
        public string Debug()
        {
            string positionsStr;
            if (positions == null)
            {
                positionsStr = "[null list]";
            }
            else
            {
                // string.Join automatically calls .ToString() on each Vector2Int,
                // formatting them as (x, y).
                positionsStr = $"[{string.Join(", ", positions)}]";
            }

            // Uses this.ToString() to get the friendly type name and adds the count/positions
            return $"MatchData(Type: {this.ToString()}, Count: {matchCount}, Positions: {positionsStr})";
        }
    }
        
    public struct TileState
    {
        public static ushort INVALID_TILE_ID = 9999;
        public ushort uniqueID;
        public int tileType;     // 0=Attack, 1=Attackx2, 2=Shield, etc.

        // A static "empty" tile for logic
        public static TileState Empty => new TileState 
        { 
            uniqueID = INVALID_TILE_ID, // An invalid, recognizable ID
            tileType = -1           // An invalid type
        };

        public bool IsEmpty() => tileType == -1;
    }
}