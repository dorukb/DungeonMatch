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
    public static MatchData FindMatchesInLine(Vector2Int startPos, Vector2Int direction,  GameBoard board)
    {
        List<Vector2Int> candidateTiles = new List<Vector2Int>();
        int currentType = board.GetTileAt(startPos).tileType;
    
        candidateTiles.Add(startPos);
        // 2 units to the pos dir, 2 units to the neg.
        // no need to check 3 units away, as that would lead to a match BEFORE This.
        for (int i = 1; i <= 2; i++)
        {
            Vector2Int pos = startPos + direction * i;
            if (!IsPosInBounds(pos) || board.GetTileAt(pos).tileType != currentType)
            {
                break; // End of line or type mismatch
            }
            candidateTiles.Add(pos);
        }
        // check the negative direction (left or down)
        for (int i = 1; i <= 2; i++)
        {
            Vector2Int pos = startPos - direction * i;
            if (!IsPosInBounds(pos) || board.GetTileAt(pos).tileType != currentType)
            {
                break; // End of line or type mismatch
            }
            candidateTiles.Add(pos);
        }

        if (candidateTiles.Count >= 3)
        {
            MatchData result = new MatchData
            {
                tileTypeID = currentType,
                positions = candidateTiles,
                matchCount = candidateTiles.Count
            };
            return result;
        }
        else return null;
    }
    
    public static List<MatchData> FindMatchesAfterSwap(GameBoard board, Vector2Int swapPos1, Vector2Int swapPos2)
    {
        List<MatchData> foundMatches = new List<MatchData>();
        FindMatchesAt(board, swapPos1, foundMatches);
        FindMatchesAt(board, swapPos2, foundMatches);
        return foundMatches;
    }
    public static List<MatchData> FindMatchesAt(GameBoard board, Vector2Int pos)
    {
        List<MatchData> foundMatches = new List<MatchData>();
        FindMatchesAt(board, pos, foundMatches);
        return foundMatches;
    }
    private static void FindMatchesAt(GameBoard board, Vector2Int pos, in List<MatchData> foundMatches)
    {
        MatchData horzMatch = FindMatchesInLine(pos, Vector2Int.right, board);
        MatchData vertMatch = FindMatchesInLine(pos, Vector2Int.down, board);

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
}
    
}