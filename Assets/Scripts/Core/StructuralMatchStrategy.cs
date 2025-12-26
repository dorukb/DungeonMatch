namespace DorkyProductions
{
    using System.Collections.Generic;
    using UnityEngine;
  // --------------------------------------------------------------------------
    // STRATEGY B: STRUCTURAL / CANDY CRUSH (Strict Line + Intersections)
    // --------------------------------------------------------------------------
    // Matches strictly rows/cols >= 3.
    // Shape: T, L, Cross are formed by merging a Vert match into a Horz match.
    // --------------------------------------------------------------------------
   public class StructuralMatchStrategy : IMatchStrategy
    {
        private static readonly MatchResult[,] _matchMap = new MatchResult[GameBoard.BoardWidth, GameBoard.BoardHeight];

        public List<MatchResult> FindMatchesAt(GameBoard board, Vector2Int pos)
        {
            List<MatchResult> results = new List<MatchResult>();
            MatchResult match = FindMatchIntersectionAt(board, pos);
            if (match != null) results.Add(match);
            return results;
        }

        public List<MatchResult> FindMatchesAfterSwap(GameBoard board, Vector2Int swapPos1, Vector2Int swapPos2)
        {
            List<MatchResult> foundMatches = new List<MatchResult>();
            HashSet<Vector2Int> claimedCenters = new HashSet<Vector2Int>();
            void CheckAndAdd(Vector2Int pos)
            {
                if (claimedCenters.Contains(pos)) return;
                var result = FindMatchIntersectionAt(board, pos);
                if (result != null)
                {
                    foundMatches.Add(result);
                    claimedCenters.Add(pos);
                }
            }
            CheckAndAdd(swapPos1);
            CheckAndAdd(swapPos2);
            return foundMatches;
        }

        public List<MatchResult> FindAllMatchesOnBoard(GameBoard board)
        {
            List<MatchResult> allMatches = new List<MatchResult>();
            System.Array.Clear(_matchMap, 0, _matchMap.Length);

            // 1. Horizontal Pass
            for (int y = 0; y < GameBoard.BoardHeight; y++)
            {
                for (int x = 0; x < GameBoard.BoardWidth; x++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);
                    TileState currentTile = board.GetTileAt(currentPos);
                    if (currentTile.IsEmpty()) continue;

                    int matchLen = 1;
                    while (x + matchLen < GameBoard.BoardWidth)
                    {
                        TileState nextTile = board.GetTileAt(new Vector2Int(x + matchLen, y));
                        if (nextTile.type == currentTile.type && !nextTile.IsEmpty()) matchLen++;
                        else break;
                    }

                    if (matchLen >= 3)
                    {
                        List<Vector2Int> points = new List<Vector2Int>(matchLen);
                        bool isDouble = false;
                        for (int i = 0; i < matchLen; i++)
                        {
                            Vector2Int p = new Vector2Int(x + i, y);
                            points.Add(p);
                            if (board.GetTileAt(p).isDoubleEffect) isDouble = true;
                        }
                        // Check Length >= 5
                        bool isSpecial = matchLen >= 5;
                        var hMatch = new MatchResult(points, currentTile.type, isDouble, isSpecial);
                        allMatches.Add(hMatch);

                        foreach (var p in points) _matchMap[p.x, p.y] = hMatch;
                        x += matchLen - 1;
                    }
                }
            }

            // 2. Vertical Pass (Merge Intersections)
            for (int x = 0; x < GameBoard.BoardWidth; x++)
            {
                for (int y = 0; y < GameBoard.BoardHeight; y++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);
                    TileState currentTile = board.GetTileAt(currentPos);
                    if (currentTile.IsEmpty()) continue;

                    int matchLen = 1;
                    while (y + matchLen < GameBoard.BoardHeight)
                    {
                        TileState nextTile = board.GetTileAt(new Vector2Int(x, y + matchLen));
                        if (nextTile.type == currentTile.type && !nextTile.IsEmpty()) matchLen++;
                        else break;
                    }

                    if (matchLen >= 3)
                    {
                        MatchResult intersectingMatch = null;
                        List<Vector2Int> vertPoints = new List<Vector2Int>(matchLen);
                        bool isDouble = false;

                        for (int i = 0; i < matchLen; i++)
                        {
                            Vector2Int p = new Vector2Int(x, y + i);
                            vertPoints.Add(p);
                            if (board.GetTileAt(p).isDoubleEffect) isDouble = true;
                            if (_matchMap[p.x, p.y] != null) intersectingMatch = _matchMap[p.x, p.y];
                        }

                        if (intersectingMatch != null)
                        {
                            // MERGE: Intersections are ALWAYS Special
                            intersectingMatch.isSpecialShape = true;
                            if (isDouble) intersectingMatch.isDoubleEffect = true;
                            foreach (var vp in vertPoints)
                            {
                                if (!intersectingMatch.positions.Contains(vp)) intersectingMatch.positions.Add(vp);
                            }
                        }
                        else
                        {
                            // Check Length >= 5
                            bool isSpecial = matchLen >= 5;
                            allMatches.Add(new MatchResult(vertPoints, currentTile.type, isDouble, isSpecial));
                        }
                        y += matchLen - 1;
                    }
                }
            }
            return allMatches;
        }

        private MatchResult FindMatchIntersectionAt(GameBoard board, Vector2Int pos)
        {
            TileState centerTile = board.GetTileAt(pos);
            if (centerTile.IsEmpty()) return null;

            List<Vector2Int> horz = GetLine(board, pos, Vector2Int.right);
            List<Vector2Int> vert = GetLine(board, pos, Vector2Int.down);
            bool hValid = horz.Count >= 3;
            bool vValid = vert.Count >= 3;

            if (!hValid && !vValid) return null;

            List<Vector2Int> finalPos = new List<Vector2Int>();
            bool isSpecial = false;

            if (hValid && vValid)
            {
                isSpecial = true; // Intersections = T/L/Cross = Special
                finalPos.AddRange(horz);
                foreach (var p in vert) if (p != pos) finalPos.Add(p);
            }
            else
            {
                finalPos = hValid ? horz : vert;
                // Check Length >= 5
                if (finalPos.Count >= 5) isSpecial = true;
            }

            bool isDouble = centerTile.isDoubleEffect;
            foreach (var p in finalPos) if (board.GetTileAt(p).isDoubleEffect) isDouble = true;

            return new MatchResult(finalPos, centerTile.type, isDouble, isSpecial);
        }

        private List<Vector2Int> GetLine(GameBoard board, Vector2Int start, Vector2Int dir)
        {
            List<Vector2Int> line = new List<Vector2Int> { start };
            Tile type = board.GetTileAt(start).type;
            for (int i = 1; i < 5; i++)
            {
                Vector2Int p = start + (dir * i);
                if (!MatchAlgorithm.IsPosInBounds(p) || board.GetTileAt(p).type != type) break;
                line.Add(p);
            }
            for (int i = 1; i < 5; i++)
            {
                Vector2Int p = start - (dir * i);
                if (!MatchAlgorithm.IsPosInBounds(p) || board.GetTileAt(p).type != type) break;
                line.Add(p);
            }
            return line;
        }

        public Tile GetBestMatchedType(GameBoard board, Vector2Int posA, Vector2Int posB) => MatchAlgorithm.SharedPredictionLogic.Predict(this, board, posA, posB);
    }
}