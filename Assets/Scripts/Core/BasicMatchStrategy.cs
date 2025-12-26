namespace DorkyProductions
{
    using System.Collections.Generic;
    using UnityEngine;

    public class BasicMatchStrategy : IMatchStrategy
    {
        // Helper
        private bool IsPosInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < GameBoard.BoardWidth &&
                   pos.y >= 0 && pos.y < GameBoard.BoardHeight;
        }

        // --- Interface Implementation ---

        public List<MatchResult> FindMatchesAfterSwap(GameBoard board, Vector2Int swapPos1, Vector2Int swapPos2)
        {
            List<MatchResult> foundMatches = new List<MatchResult>();
            // Use the internal helper that populates the list
            FindMatchesAtInternal(board, swapPos1, foundMatches);
            FindMatchesAtInternal(board, swapPos2, foundMatches);
            return foundMatches;
        }

        public List<MatchResult> FindMatchesAt(GameBoard board, Vector2Int pos)
        {
            List<MatchResult> foundMatches = new List<MatchResult>();
            FindMatchesAtInternal(board, pos, foundMatches);
            return foundMatches;
        }

        // Renamed from FindAllMatchesOnBoardAlternative to satisfy Interface
        public List<MatchResult> FindAllMatchesOnBoard(GameBoard board)
        {
            List<MatchResult> allMatches = new List<MatchResult>();
            HashSet<Vector2Int> claimedTiles = new HashSet<Vector2Int>();

            for (int y = 0; y < GameBoard.BoardHeight; y++)
            {
                for (int x = 0; x < GameBoard.BoardWidth; x++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);
                    TileState currentTile = board.GetTileAt(currentPos);

                    if (claimedTiles.Contains(currentPos) || currentTile.IsEmpty())
                    {
                        continue;
                    }

                    // Check Horizontal
                    bool isHorzDoubleEffect = currentTile.isDoubleEffect;
                    List<Vector2Int> horizontalMatch = new List<Vector2Int> { currentPos };
                    for (int i = x + 1; i < GameBoard.BoardWidth; i++)
                    {
                        var searchPos = new Vector2Int(i, y);
                        TileState nextTile = board.GetTileAt(searchPos);
                        if (nextTile.IsEmpty() || nextTile.type != currentTile.type || claimedTiles.Contains(searchPos))
                        {
                            break;
                        }
                        if (nextTile.isDoubleEffect) isHorzDoubleEffect = true;
                        horizontalMatch.Add(searchPos);
                    }

                    // Check Vertical
                    bool isVertDoubleEffect = currentTile.isDoubleEffect;
                    List<Vector2Int> verticalMatch = new List<Vector2Int> { currentPos };
                    for (int j = y + 1; j < GameBoard.BoardHeight; j++)
                    {
                        var searchPos = new Vector2Int(x, j);
                        TileState nextTile = board.GetTileAt(searchPos);
                        if (nextTile.IsEmpty() || nextTile.type != currentTile.type || claimedTiles.Contains(searchPos))
                        {
                            break;
                        }
                        if (nextTile.isDoubleEffect) isVertDoubleEffect = true;
                        verticalMatch.Add(searchPos);
                    }

                    // Select the Best Match (Favor Horizontal if equal, or Length)
                    if (horizontalMatch.Count >= 3)
                    {
                        allMatches.Add(new MatchResult(horizontalMatch, currentTile.type, isHorzDoubleEffect, false));
                        foreach (Vector2Int pos in horizontalMatch) claimedTiles.Add(pos);
                    }
                    else if (verticalMatch.Count >= 3)
                    {
                        allMatches.Add(new MatchResult(verticalMatch, currentTile.type, isVertDoubleEffect, false));
                        foreach (Vector2Int pos in verticalMatch) claimedTiles.Add(pos);
                    }
                }
            }
            return allMatches;
        }

        public Tile GetBestMatchedType(GameBoard board, Vector2Int posA, Vector2Int posB)
        {
            // Use the shared logic to avoid duplication
            return MatchAlgorithm.SharedPredictionLogic.Predict(this, board, posA, posB);
        }

        // --- Internal Logic ---

        private void FindMatchesAtInternal(GameBoard board, Vector2Int pos, List<MatchResult> foundMatches)
        {
            MatchResult horzMatch = FindMatchesInLine(pos, Vector2Int.right, board);
            MatchResult vertMatch = FindMatchesInLine(pos, Vector2Int.down, board);

            if (horzMatch != null && vertMatch != null)
            {
                // If both exist, take the longer one (Basic strategy doesn't merge T/L shapes)
                foundMatches.Add(horzMatch.matchCount > vertMatch.matchCount ? horzMatch : vertMatch);
            }
            else if (horzMatch == null && vertMatch != null)
            {
                foundMatches.Add(vertMatch);
            }
            else if (vertMatch == null && horzMatch != null)
            {
                foundMatches.Add(horzMatch);
            }
        }

        private MatchResult FindMatchesInLine(Vector2Int startPos, Vector2Int direction, GameBoard board)
        {
            List<Vector2Int> candidateTiles = new List<Vector2Int>();
            Tile currentType = board.GetTileAt(startPos).type;

            candidateTiles.Add(startPos);

            bool includesDoubleEffectTile = board.GetTileAt(startPos).isDoubleEffect;

            // Positive direction
            for (int i = 1; i <= 2; i++)
            {
                Vector2Int pos = startPos + direction * i;
                if (!IsPosInBounds(pos)) break;

                var currTile = board.GetTileAt(pos);
                if (currTile.type != currentType) break;

                candidateTiles.Add(pos);
                if (currTile.isDoubleEffect) includesDoubleEffectTile = true;
            }
            // Negative direction
            for (int i = 1; i <= 2; i++)
            {
                Vector2Int pos = startPos - direction * i;
                if (!IsPosInBounds(pos)) break;

                var currTile = board.GetTileAt(pos);
                if (currTile.type != currentType) break;

                candidateTiles.Add(pos);
                if (currTile.isDoubleEffect) includesDoubleEffectTile = true;
            }

            if (candidateTiles.Count >= 3)
            {
                return new MatchResult(candidateTiles, currentType, includesDoubleEffectTile, false);
            }
            else return null;
        }
    }
}