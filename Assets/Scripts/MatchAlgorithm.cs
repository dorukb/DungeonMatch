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
        
        bool includesDoubleEffectTile = board.GetTileAt(startPos).isDoubleEffect;
        // 2 units to the pos dir, 2 units to the neg.
        // no need to check 3 units away, as that would lead to a match BEFORE This.
        for (int i = 1; i <= 2; i++)
        {
            Vector2Int pos = startPos + direction * i;
            if (!IsPosInBounds(pos))
            {
                break; // invalid tile
            }
            
            var currTile = board.GetTileAt(pos);
            if (currTile.type != currentType)
            {
                break; //type mismatch
            }
            candidateTiles.Add(pos);
            if (currTile.isDoubleEffect)
            {
                includesDoubleEffectTile = true;
            }
        }
        // check the negative direction (left or down)
        for (int i = 1; i <= 2; i++)
        {
            Vector2Int pos = startPos - direction * i;
            if (!IsPosInBounds(pos))
            {
                break; // invalid tile
            }
            var currTile = board.GetTileAt(pos);
            if (currTile.type != currentType)
            {
                break; // End of line or type mismatch
            }
            candidateTiles.Add(pos);
            if (currTile.isDoubleEffect)
            {
                includesDoubleEffectTile = true;
            }
        }

        if (candidateTiles.Count >= 3)
        {
            return new MatchResult(candidateTiles, currentType, includesDoubleEffectTile);
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
                List<MatchResult> matches = FindMatchesAt(board, currentPos);

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
    
    public static List<MatchResult> FindAllMatchesOnBoardAlternative(GameBoard board)
    {
        List<MatchResult> allMatches = new List<MatchResult>();
        
        // This set tracks tiles that are already part of a confirmed match.
        // This ensures a tile is only processed once and prevents duplicate matches.
        
        // TODO: we have to send remove tile message based on these claimedTiles.
        // but then the order is not visible? otherwise we send multiple "remove messages" for intersections...
        HashSet<Vector2Int> claimedTiles = new HashSet<Vector2Int>();

        // Iterate from bottom-left (0,0) to top-right
        for (int y = 0; y < GameBoard.BoardHeight; y++)
        {
            for (int x = 0; x < GameBoard.BoardWidth; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                TileState currentTile = board.GetTileAt(currentPos);
                
                // 1. Skip this tile if it's already part of a match we've found
                if (claimedTiles.Contains(currentPos) || currentTile.IsEmpty())
                {
                    continue;
                }
                
                bool isHorzDoubleEffect = currentTile.isDoubleEffect;
                List<Vector2Int> horizontalMatch = new List<Vector2Int> { currentPos };
                for (int i = x + 1; i < GameBoard.BoardWidth; i++)
                {
                    var searchPos = new Vector2Int(i, y);
                    TileState nextTile = board.GetTileAt(searchPos);
                    if (nextTile.IsEmpty() 
                        || nextTile.type != currentTile.type
                        || claimedTiles.Contains(searchPos))
                    {
                        break;
                    }
                    
                    // Add the valid matching 
                    if (nextTile.isDoubleEffect)
                    {
                        isHorzDoubleEffect = true;
                    }
                    horizontalMatch.Add(new Vector2Int(i, y));
                }

                // --- 4. Check for VERTICAL match starting from currentPos (moving up) ---
                bool isVertDoubleEffect = currentTile.isDoubleEffect;
                List<Vector2Int> verticalMatch = new List<Vector2Int> { currentPos };
                for (int j = y + 1; j < GameBoard.BoardHeight; j++)
                {
                    var searchPos = new Vector2Int(x,j);
                    TileState nextTile = board.GetTileAt(searchPos);
                    if (nextTile.IsEmpty() 
                        || nextTile.type != currentTile.type
                        || claimedTiles.Contains(searchPos))
                    {
                        break;
                    }

                    // Add the valid matching tile
                    verticalMatch.Add(new Vector2Int(x, j));
                    // Add the valid matching 
                    if (nextTile.isDoubleEffect)
                    {
                        isVertDoubleEffect = true;
                    }
                }

                
                // TODO: This shares the current Tile with both Horz and Vert match groups.
                // choose largest one.
                if (horizontalMatch.Count >= 3)
                {
                    allMatches.Add(new MatchResult(horizontalMatch, currentTile.type, isHorzDoubleEffect));
                    // "Claim" all tiles in this match so they can't start a new (sub) match
                    foreach (Vector2Int pos in horizontalMatch)
                    {
                        claimedTiles.Add(pos);
                    }
                }
                else if (verticalMatch.Count >= 3)
                {
                    // Add it to our main list (this handles T/L shapes)
                    allMatches.Add(new MatchResult(verticalMatch, currentTile.type, isVertDoubleEffect));
                    
                    // "Claim" all tiles. The HashSet gracefully handles
                    // duplicates (the intersection tile in a T/L shape).
                    foreach (Vector2Int pos in verticalMatch)
                    {
                        claimedTiles.Add(pos);
                    }
                }
            }
        }

        return allMatches;
    }
}
    
}