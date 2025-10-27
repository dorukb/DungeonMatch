namespace DorkyProductions
{
using System.Collections.Generic;
using UnityEngine;
public static class MatchAlgorithm
{
    public static bool IsPosInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < GameBoard.BoardWidth &&
               pos.y >= 0 && pos.y < GameBoard.BoardHeight;
    }
    public static MatchResult FindMatchesInLine(Vector2Int startPos, Vector2Int direction,  GameBoard board)
    {
        List<Vector2Int> candidateTiles = new List<Vector2Int>();
        Tile currentType = board.GetTileAt(startPos).type;
    
        candidateTiles.Add(startPos);
        // 2 units to the pos dir, 2 units to the neg.
        // no need to check 3 units away, as that would lead to a match BEFORE This.
        for (int i = 1; i <= 2; i++)
        {
            Vector2Int pos = startPos + direction * i;
            if (!IsPosInBounds(pos) || board.GetTileAt(pos).type != currentType)
            {
                break; // End of line or type mismatch
            }
            candidateTiles.Add(pos);
        }
        // check the negative direction (left or down)
        for (int i = 1; i <= 2; i++)
        {
            Vector2Int pos = startPos - direction * i;
            if (!IsPosInBounds(pos) || board.GetTileAt(pos).type != currentType)
            {
                break; // End of line or type mismatch
            }
            candidateTiles.Add(pos);
        }

        if (candidateTiles.Count >= 3)
        {
            MatchResult result = new MatchResult
            {
                tileType = currentType,
                positions = candidateTiles,
                matchCount = candidateTiles.Count
            };
            return result;
        }
        else return null;
    }
    
    public static List<MatchResult> FindMatchesAfterSwap(GameBoard board, Vector2Int swapPos1, Vector2Int swapPos2)
    {
        List<MatchResult> foundMatches = new List<MatchResult>();
        FindMatchesAt(board, swapPos1, foundMatches);
        FindMatchesAt(board, swapPos2, foundMatches);
        return foundMatches;
    }
    public static List<MatchResult> FindMatchesAt(GameBoard board, Vector2Int pos)
    {
        List<MatchResult> foundMatches = new List<MatchResult>();
        FindMatchesAt(board, pos, foundMatches);
        return foundMatches;
    }
    private static void FindMatchesAt(GameBoard board, Vector2Int pos, in List<MatchResult> foundMatches)
    {
        MatchResult horzMatch = FindMatchesInLine(pos, Vector2Int.right, board);
        MatchResult vertMatch = FindMatchesInLine(pos, Vector2Int.down, board);

        if (horzMatch != null && vertMatch != null)
        {
            foundMatches.Add(horzMatch.matchCount > vertMatch.matchCount ? horzMatch : vertMatch);
        }
        if (horzMatch == null && vertMatch != null)
        {
            foundMatches.Add(vertMatch);
        }
        if (vertMatch == null && horzMatch != null)
        {
            foundMatches.Add(horzMatch);
        }
    }


    
    public static List<MatchResult> FindAllMatchesOnBoard(GameBoard board)
    {
        List<MatchResult> allMatches = new List<MatchResult>();
        HashSet<Vector2Int> matchedPositions = new HashSet<Vector2Int>();
         
        for (int y = 0; y < GameBoard.BoardHeight; y++)
        {
            for (int x = 0; x < GameBoard.BoardWidth; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);

                // OPTIMIZATION: Only check for a match if this tile hasn't already been 
                // claimed by a previous match.
                if (matchedPositions.Contains(currentPos))
                {
                    continue; // Skip this tile
                }
                List<MatchResult> matches = MatchAlgorithm.FindMatchesAt(board, currentPos);

                allMatches.AddRange(matches);
                foreach (MatchResult match in matches)
                {
                    foreach (var pos in match.positions)
                    {
                        matchedPositions.Add(pos);
                    }
                }
            }
        }

        return allMatches;
    }
}
    
}