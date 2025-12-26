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
        public bool isSpecialShape;
        // We store the actual positions for clearing them
        // TODO: maybe make positions HashSet, to prevent duplicate memory usage during find all matches.
        public List<Vector2Int> positions;

        public MatchResult(List<Vector2Int> positions, Tile tileType, bool isDoubleEffect, bool isSpecialShape)
        {
            this.tileType = tileType;
            this.positions = positions;
            this.isDoubleEffect = isDoubleEffect;
            this.isSpecialShape = isSpecialShape;
            this.matchCount = positions.Count;
        }
        public string DisplayMatchType()
        {
            return tileType.ToString();
        }
        public string Debug()
        {
            string positionsStr = positions == null ? "[null list]" : $"[{string.Join(", ", positions)}]";
            return $"MatchData(Type: {tileType}, Count: {matchCount}, Special: {isSpecialShape}, Pos: {positionsStr})";
        }
    }
}