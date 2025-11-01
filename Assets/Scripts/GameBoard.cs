using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DorkyProductions
{
    
public class GameBoard
{
    public static readonly int BoardWidth = 5;
    public static readonly int BoardHeight = 5;
 
    // The "Single Source of Truth"
    // This list represents a 5x5 grid, flattened to 1D.
    // Index = (y * BoardWidth) + x
    public readonly List<TileState> boardState = new List<TileState>(BoardHeight * BoardWidth);
    private ushort _nextTileID = 0;
           
    public List<TileState> FillBoardWithNoMatches()
    {
        for (int i = 0; i < BoardHeight * BoardWidth; i++)
        {
            int x = i % BoardWidth;
            int y = i / BoardWidth;

            // 1. Get a list of all possible tile types
            List<Tile> availableTypes = new List<Tile> { Tile.Attack, Tile.Shield, Tile.Cross, Tile.Potion, Tile.Chest};
            
            // 2. Check for potential horizontal matches (check 2 tiles to the left)
            if (x > 1)
            {
                Tile left1ID = GetTileAt(x-1,y).type;
                Tile left2ID = GetTileAt(x-2,y).type;
                if (left1ID == left2ID)
                {
                    // Both tiles to the left match, so we cannot use their type.
                    availableTypes.Remove(left1ID);
                }
            }

            // 3. Check for potential vertical matches (check 2 tiles below)
            if (y > 1)
            {
                Tile down1ID = GetTileAt(x,y-1).type;
                Tile down2ID = GetTileAt(x,y-2).type;
                if (down1ID == down2ID)
                {
                    // Both tiles below match, so we cannot use their type.
                    availableTypes.Remove(down1ID);
                }
            }
            boardState.Add(GenerateNewTile(availableTypes));
        }

        return boardState;
    }
    // Overload: Generates a random tile from a specific list of allowed types.
    private TileState GenerateNewTile(List<Tile> availableTypes)
    {
        if (availableTypes.Count == 0)
        {
            // This is a safety net. It's very rare but could happen
            // if both horizontal and vertical checks removed the same, last-available type.
            // In this case, just pick any random one.
            Debug.LogError("Ran out of available types. Picking a random one.");
            return GenerateNewTile();
        }
    
        // Pick a random ID from the *allowed* list
        int randomIndex = Random.Range(0, availableTypes.Count);
        Tile randomType = availableTypes[randomIndex];

        return new TileState
        {
            uniqueID = _nextTileID++,
            type = randomType
        };
    }
    
    private TileState GenerateNewTile()
    {
        return new TileState
        {
            uniqueID = _nextTileID++,
            type = (Tile) UnityEngine.Random.Range(1, GameMaster.Instance.TileDatabase.allTileDefinitions.Count)
        };
    }
    
    public bool ProcessSwapMove(Vector2Int posA, Vector2Int posB, NetworkIdentity performingPlayer, List<GameEvent> eventBatch)
    {
        // --- 1. Perform the swap ---
        int indexA = GetIndex(posA);
        int indexB = GetIndex(posB);
        TileState stateA = boardState[indexA];
        TileState stateB = boardState[indexB];

        boardState[indexA] = stateB;
        boardState[indexB] = stateA;
        // RpcAnimateSwap(stateA.uniqueID, stateB.uniqueID);
        eventBatch.Add(GameEvent.SwapOccurred(stateA.uniqueID, stateB.uniqueID));
        
        // Check if this swap caused a match
        // We only need to check the rows/cols of the two tiles we moved
        var matchResults = MatchAlgorithm.FindMatchesAfterSwap(this, posA, posB);
        if (matchResults.Count == 0)
        {
            // TODO: Move didnt yield a Match, maybe do not allow and rollback the move.
            return false;
        }

        bool hasMoreMatches = true;
        while (hasMoreMatches)
        {
            bool shouldOpenChest = ApplyMatchEffects(matchResults, eventBatch);
            // TODO: Handle chest open case that breaks/pauses the chain events.
            RemoveMatchedTiles(matchResults, eventBatch);
            SimulateTileFall(eventBatch);
            RefillBoard(eventBatch);

            hasMoreMatches = false;
            matchResults = MatchAlgorithm.FindAllMatchesOnBoard(this);
            if (matchResults.Count > 0)
            {
                Debug.Log("[Server] Matches on board:");
                foreach (var res in matchResults)
                {
                    Debug.Log(res.Debug());
                }
                hasMoreMatches = true;
            }
        }
        return true; // A match occurred
    }

    private void RemoveMatchedTiles(List<MatchResult> matchResults, List<GameEvent> eventBatch)
    {
        foreach (var match in matchResults)
        {
            foreach (var pos in match.positions)
            {
                boardState[GetIndex(pos)] = TileState.Empty;
            }
        }
    }
    // --- BOARD PROCESSING HELPERS ---

    // returns: Whether this match should stop the Chain events immediately: i.e, shouldOpenChest
    private bool ApplyMatchEffects(List<MatchResult> matchResults, List<GameEvent> eventBatch)
    {
        // This is where Card specific match effect will take place.
        foreach (var match in matchResults)
        {
            Debug.Log($"Matched: {match.matchCount} of {match.ToString()}");
            var ids = new List<ushort>();
            foreach (var pos in match.positions)
            {
                ids.Add(GetTileAt(pos).uniqueID);
            }
            // RpcApplyMatchEffect(ids);
            eventBatch.Add(GameEvent.MatchOccurred(ids));

            var activePlayer = GameMaster.Instance.activePlayer;
            var opponent = GameMaster.Instance.GetInactivePlayer();
            // Then apply the effect
            switch (match.tileType)
            {
                case Tile.Unknown:
                    Debug.LogError($"Unknown tiles matched. Shouldnt happen : {match.tileType}");
                    break;
                case Tile.Attack:
                    // TODO: set isPowerful to true for x2 effect.
                    int dmgAmount = AttackEffect.GetDamage(match.matchCount);
                    opponent.TakeDamage(dmgAmount, opponent.shielded);
                    eventBatch.Add(GameEvent.Attack(opponent.GetCurrentHealth(), opponent.netId, false));
                    opponent.shielded = false;
                    break;
                case Tile.Shield:
                    activePlayer.shielded = true;
                    break;
                case Tile.Cross:
                    break;
                case Tile.Potion:
                    int healAmount = PotionEffect.GetHeal(match.matchCount);
                    activePlayer.Heal(healAmount);
                    eventBatch.Add(GameEvent.Potion(activePlayer.GetCurrentHealth(), activePlayer.netId, false));
                    break;
                case Tile.Chest:
                    // TODO: OpenChest();
                    // This could trigger another skill, which might
                    // modify the board again. Be careful of recursive loops!
                    // For now, let's keep it simple.
                    break;
                default:
                    Debug.LogError($"Sth is wrong. what is this tile type?? : {match.tileType}");
                    break;
            }
        }

        return false;
    }

    private void SimulateTileFall( List<GameEvent> eventBatch)
    {
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                if (GetTileAt(new Vector2Int(x, y)).IsEmpty())
                {
                    // ...look for the first non-empty tile *above* it
                    for (int yAbove = y + 1; yAbove < BoardHeight; yAbove++)
                    {
                        TileState tileToMove = GetTileAt(new Vector2Int(x, yAbove));
                        if (!tileToMove.IsEmpty())
                        {
                            // We found one! Move it down.
                            // Debug.Log($"Move tile ({x},{yAbove}) to ({x},{y})");
                            int moveIndex = GetIndex(x, y);
                            boardState[moveIndex] = tileToMove;
                            boardState[GetIndex(x, yAbove)] = TileState.Empty;

                            // RpcMoveTile(new Vector2Int(x, y), tileToMove);
                            eventBatch.Add(GameEvent.TileMoved(tileToMove.uniqueID, GetGridPos(moveIndex)));
                            // Break the inner 'yAbove' loop to continue
                            // checking the *current* 'y' position again.
                            break; 
                        }
                    }
                }
            }
        }
    }
    
    private void RefillBoard(List<GameEvent> eventBatch)
    {
        Debug.Log("[SERVER] Refilling board");
        // rules: "bottom to top, then left to right"
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                if (GetTileAt(new Vector2Int(x, y)).IsEmpty())
                {
                    // TODO: Spawn considering if we restrict combo-matches.
                    TileState fillingTile = GenerateNewTile();
                    int spawnIdx = GetIndex(x, y);
                    boardState[spawnIdx] = fillingTile;

                    // RpcRefillBoard(new Vector2Int(x, y),fillingTile);
                    eventBatch.Add(GameEvent.TileSpawned(fillingTile, GetGridPos(spawnIdx)));
                    Debug.Log($"Draw new tile to pos: ({x},{y})");
                }
            }
        }
    }

    public bool CheckForValidSwaps()
    {
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                // 1. Check a swap with the right neighbor
                if (y < BoardHeight - 1)
                {
                    if (TestSwap(new Vector2Int(x, y), new Vector2Int(x, y + 1))) 
                    {
                        return true; // Found a valid move!
                    }
                }

                // 2. Check a swap with the bottom neighbor
                if (x < BoardWidth - 1)
                {
                    if (TestSwap(new Vector2Int(x, y), new Vector2Int(x + 1, y)))
                    {
                        return true; // Found a valid move!
                    }
                }
            }
        }

        return false; // No valid moves found after checking all possibilities
    }
    
    public bool TestSwap(Vector2Int pos1, Vector2Int pos2)
    {
        TileState tmp = boardState[GetIndex(pos1)];
        boardState[GetIndex(pos1)] = boardState[GetIndex(pos2)];
        boardState[GetIndex(pos2)] = tmp;

        List<MatchResult> matchResults = MatchAlgorithm.FindMatchesAfterSwap(this, pos1, pos2);

        if (matchResults.Count > 0)
        {
            return true;
        }

        return false;
    }
    //To-Do & Q: include test swap here? 
    public bool IsValidSwap(Vector2Int posA, Vector2Int posB)
    {
        // Check bounds
        if (posA.x < 0 || posA.x >= BoardWidth || posA.y < 0 || posA.y >= BoardHeight ||
            posB.x < 0 || posB.x >= BoardWidth || posB.y < 0 || posB.y >= BoardHeight)
        {
            return false;
        }

        // Tiles must be different type, otherwise no effect.
        // Prevents accidental no-effect swaps. 
        if (GetTileAt(posA).type == GetTileAt(posB).type)
        {
            Debug.LogWarning($"[Client] Invalid swap: {posA} <-> {posB}. Tiles are same type.");
            return false;
        }

        if (GetTileAt(posA).type == Tile.Unknown|| GetTileAt(posB).type == Tile.Unknown)
        {
            Debug.LogWarning($"[Client] Invalid swap: {posA} <-> {posB}. at least one of the tiles is Empty.");
            // dont allow swapping with Empty tiles.
            return false;
        }
        
        // Check for adjacency (Manhattan distance == 1)
        int dist = Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
        return dist == 1;
    }
    public static Vector2Int GetGridPos(int idx)
    {
        // idx = 8 , 9th tile
        // (x,y)
        // (1,3)
        int y = idx % BoardWidth;
        int x = idx / BoardHeight;
        return new Vector2Int(x, y);
    }
    public int GetIndex(Vector2Int pos) =>  GetIndex(pos.x, pos.y);
    private int GetIndex(int x, int y) => (x * BoardHeight) + y;
    public TileState GetTileAt(Vector2Int pos) => boardState[GetIndex(pos)];

    private TileState GetTileAt(int x, int y)
    {
        // Check bounds
        if (x < 0 || x >= BoardWidth || y < 0 || y >= BoardHeight)
        {
            // Return an "empty" or "invalid" state, not a real tile
            return TileState.Empty; 
        }
    
        int index = (y * BoardWidth) + x;
    
        // Check if the tile has been added yet
        if (index >= boardState.Count)
        {
            return TileState.Empty;
        }
    
        return boardState[index];
    }

    public TileState GetTile(ushort id)
    {
        // Use FindIndex, which is safe
        int index = boardState.FindIndex(t => t.uniqueID == id);

        if (index == -1)
        {
            // Not found, so return your safe "Empty" struct
            Debug.LogError($"Tile with id: {id} not found. wtf?");
            return TileState.Empty;
        }
        return boardState[index];
    }
    // ... Server logic for checking matches, etc., goes here ...
}

}