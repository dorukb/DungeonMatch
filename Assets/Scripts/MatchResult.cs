using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions
{

// This struct holds the data for ONE match (e.g., a 4-in-a-row)
    public class MatchResult
    {
        // TODO: Consider enum types for tiles, also update them on the SO setup.
        public Tile tileType;
        public int matchCount;
        public bool isDoubleEffect;
        // We store the actual positions for clearing them
        // TODO: maybe make positions HashSet, to prevent duplicate memory usage during find all matches.
        public List<Vector2Int> positions;

        public MatchResult(List<Vector2Int> positions, Tile tileType, bool isDoubleEffect)
        {
            this.tileType = tileType;
            this.positions = positions;
            this.isDoubleEffect = isDoubleEffect;
            this.matchCount = positions.Count;
        }
        public string ToString()
        {
            return tileType.ToString();
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
}