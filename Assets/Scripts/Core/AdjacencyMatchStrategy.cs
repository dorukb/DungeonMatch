namespace DorkyProductions
{
    using System.Collections.Generic;
    using UnityEngine;
    
    // --------------------------------------------------------------------------
    // STRATEGY A: ADJACENCY (Flood Fill / Blob)
    // --------------------------------------------------------------------------
    // Matches ANY connected group of 3+.
    // Shape: T, L, or arbitrary blobs are single groups.
    // --------------------------------------------------------------------------
    public class AdjacencyMatchStrategy : IMatchStrategy
    {
        private static readonly HashSet<Vector2Int> _visitedBuffer = new HashSet<Vector2Int>();
        private static readonly Queue<Vector2Int> _searchQueue = new Queue<Vector2Int>();
        private static readonly List<Vector2Int> _currentGroup = new List<Vector2Int>(16);
        private static readonly Vector2Int[] NeighborDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public List<MatchResult> FindMatchesAfterSwap(GameBoard board, Vector2Int swapPos1, Vector2Int swapPos2)
        {
            List<MatchResult> foundMatches = new List<MatchResult>();
            HashSet<Vector2Int> handledPositions = new HashSet<Vector2Int>();

            void CheckPos(Vector2Int pos)
            {
                if (handledPositions.Contains(pos)) return;
                var matches = FindMatchesAt(board, pos); // Local helper
                foreach (var m in matches)
                {
                    foreach (var p in m.positions) handledPositions.Add(p);
                    foundMatches.Add(m);
                }
            }

            CheckPos(swapPos1);
            CheckPos(swapPos2);
            return foundMatches;
        }

        public List<MatchResult> FindAllMatchesOnBoard(GameBoard board)
        {
            List<MatchResult> allMatches = new List<MatchResult>();
            HashSet<Vector2Int> globalVisited = new HashSet<Vector2Int>();

            for (int y = 0; y < GameBoard.BoardHeight; y++)
            {
                for (int x = 0; x < GameBoard.BoardWidth; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (globalVisited.Contains(pos)) continue;

                    TileState t = board.GetTileAt(pos);
                    if (t.IsEmpty()) continue;

                    // Perform Flood Fill
                    GetConnectedGroup(board, pos, t.type);

                    // Mark global visited
                    foreach (var p in _currentGroup) globalVisited.Add(p);

                    if (_currentGroup.Count >= 3)
                    {
                        allMatches.Add(CreateResultFromBuffer(board, t.type));
                    }
                }
            }
            return allMatches;
        }
      
        // Local helper for Adjacency (Flood Fill)
        public List<MatchResult> FindMatchesAt(GameBoard board, Vector2Int startPos)
        {
            List<MatchResult> results = new List<MatchResult>();
            TileState startTile = board.GetTileAt(startPos);
            if (startTile.IsEmpty()) return results;

            GetConnectedGroup(board, startPos, startTile.type);

            if (_currentGroup.Count >= 3)
            {
                results.Add(CreateResultFromBuffer(board, startTile.type));
            }
            return results;
        }

        private void GetConnectedGroup(GameBoard board, Vector2Int startPos, Tile targetType)
        {
            _visitedBuffer.Clear();
            _searchQueue.Clear();
            _currentGroup.Clear();

            _searchQueue.Enqueue(startPos);
            _visitedBuffer.Add(startPos);
            _currentGroup.Add(startPos);

            while (_searchQueue.Count > 0)
            {
                Vector2Int current = _searchQueue.Dequeue();
                foreach (var dir in NeighborDirs)
                {
                    Vector2Int neighbor = current + dir;
                    if (!MatchAlgorithm.IsPosInBounds(neighbor)) continue;
                    if (_visitedBuffer.Contains(neighbor)) continue;

                    var nTile = board.GetTileAt(neighbor);
                    if (!nTile.IsEmpty() && nTile.type == targetType)
                    {
                        _visitedBuffer.Add(neighbor);
                        _searchQueue.Enqueue(neighbor);
                        _currentGroup.Add(neighbor);
                    }
                }
            }
        }

        private MatchResult CreateResultFromBuffer(GameBoard board, Tile type)
        {
            // Determine "Special Shape" (Does it span both X and Y?)
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            bool isDouble = false;

            foreach (var pos in _currentGroup)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
                if (board.GetTileAt(pos).isDoubleEffect) isDouble = true;
            }

            bool spansX = (maxX - minX) > 0;
            bool spansY = (maxY - minY) > 0;
            bool isSpecial = (spansX && spansY) || _currentGroup.Count >= 5; // It is 2D, not a line

            return new MatchResult(new List<Vector2Int>(_currentGroup), type, isDouble, isSpecial);
        }

        public Tile GetBestMatchedType(GameBoard board, Vector2Int posA, Vector2Int posB)
        {
            return MatchAlgorithm.SharedPredictionLogic.Predict(this, board, posA, posB);
        }
    }
}