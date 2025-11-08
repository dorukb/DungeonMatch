using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Mathematics;
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
    private readonly List<TileState> boardState = new List<TileState>(BoardHeight * BoardWidth);
    private ushort _nextTileID = 0;
    
    private readonly List<Tile> _allTileTypes = new List<Tile> 
    { 
        Tile.Attack, Tile.Shield, Tile.Cross, 
        Tile.Heal, Tile.Chest 
    };
    // 2. A single, reusable list to hold available types for each tile.
    private readonly List<Tile> _availableTypes = new List<Tile>();
    
    public List<TileState> FillBoardWithNoMatches()
    {
        for (int i = 0; i < BoardHeight * BoardWidth; i++)
        {
            int x = i % BoardWidth;
            int y = i / BoardWidth;

            // 1. Reset the reusable list to the master list
            _availableTypes.Clear();
            _availableTypes.AddRange(_allTileTypes);
            // 2. Check for potential horizontal matches (check 2 tiles to the left)
            if (x > 1)
            {
                Tile left1ID = GetTileSafe(x-1,y).type;
                Tile left2ID = GetTileSafe(x-2,y).type;
                if (left1ID == left2ID)
                {
                    // Both tiles to the left match, so we cannot use their type.
                    _availableTypes.Remove(left1ID);
                }
            }

            // 3. Check for potential vertical matches (check 2 tiles below)
            if (y > 1)
            {
                Tile down1ID = GetTileSafe(x,y-1).type;
                Tile down2ID = GetTileSafe(x,y-2).type;
                if (down1ID == down2ID)
                {
                    // Both tiles below match, so we cannot use their type.
                    _availableTypes.Remove(down1ID);
                }
            }
            boardState.Add(GenerateNewTile(_availableTypes));
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

        bool isDoubleEffect = false;
        if (randomType == Tile.Attack || randomType == Tile.Heal)
        {
            // %20 double effect
            isDoubleEffect = Random.value > 0.8f;
        }
        return new TileState(_nextTileID++, randomType, isDoubleEffect);
    }
    
    private TileState GenerateNewTile()
    {
        // 1st Tile type is UNKNOWN, exclude it.
        // TODO: WTF is this code?
        // fix this random type based on allDefnCount, looks stupid.
        Tile randomType = (Tile)Random.Range(1, GameMaster.Instance.TileDatabase.allTileDefinitions.Count);
        
        bool isDoubleEffect = false;
        if (randomType == Tile.Attack || randomType == Tile.Heal)
        {
            // %20 double effect
            isDoubleEffect = Random.value > 0.8f;
        }
        return new TileState(_nextTileID++, randomType, isDoubleEffect);
    }
    
    public bool ProcessSwapMove(Vector2Int posA, Vector2Int posB, NetworkIdentity performingPlayer, List<GameEventBase> eventBatch)
    {
        // --- 1. Perform the swap ---
        int indexA = GetIndex(posA);
        int indexB = GetIndex(posB);
        TileState stateA = boardState[indexA];
        TileState stateB = boardState[indexB];

        boardState[indexA] = stateB;
        boardState[indexB] = stateA;
        // RpcAnimateSwap(stateA.uniqueID, stateB.uniqueID);
        eventBatch.Add(EventPool.Get<SwappedTilesEvent>().Setup(stateA.uniqueID, stateB.uniqueID));
        
        // Check if this swap caused a match
        // We only need to check the rows/cols of the two tiles we moved
        var matchesToProcess = MatchAlgorithm.FindMatchesAfterSwap(this, posA, posB);
        if (matchesToProcess.Count == 0)
        {
            // TODO: Move didnt yield a Match, maybe do not allow and rollback the move.
            return false;
        }

        // This 'master' loop handles all chain reactions (cascades AND refills).
        // StabilizeBoard procedure
        int refillCnt = 0;
        while (matchesToProcess.Count > 0)
        { 
            Debug.Log("[Server] Matches on board:");
            foreach (var res in matchesToProcess)
            {
                Debug.Log(res.Debug());
            }
            // TODO: Handle chest open case that breaks/pauses the chain events.
            // Idea chest:
            // finish this loop, stabilize the board, then DO NOT END the turn, send open chest to clients, wait for the result, client should send "chest open"
            // we execute that effect, then again run StabilizeBoard.
            bool shouldOpenChest = ApplyMatchEffects(matchesToProcess, eventBatch);
            RemoveMatchedTiles(matchesToProcess);
            SimulateTileFall(eventBatch);
            matchesToProcess = MatchAlgorithm.FindAllMatchesOnBoardAlternative(this);
            
            // Refill, when no more falling matches
            if (matchesToProcess.Count == 0)
            {
                bool avoidFurtherMatches = refillCnt > 0;
                RefillBoard(eventBatch, avoidFurtherMatches);
                refillCnt++;
                matchesToProcess = MatchAlgorithm.FindAllMatchesOnBoardAlternative(this);
            }
        }
        return true; // A match occurred
    }

    private void RemoveMatchedTiles(List<MatchResult> matchResults)
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
    private bool ApplyMatchEffects(List<MatchResult> matchResults, List<GameEventBase> eventBatch)
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
            eventBatch.Add(EventPool.Get<MatchedTilesEvent>().Setup(ids));
            

            var activePlayer = GameMaster.Instance.activePlayer;
            var opponent = GameMaster.Instance.GetInactivePlayer();
            // Then apply the effect
            switch (match.tileType)
            {
                case Tile.Unknown:
                    Debug.LogError($"Unknown tiles matched. Shouldn't happen : {match.tileType}");
                    break;
                case Tile.Attack:
                    ApplyAttackEffect(eventBatch, activePlayer, match, opponent);
                    break;
                case Tile.Shield:
                    ApplyShieldEffect(eventBatch, activePlayer, match);
                    break;
                case Tile.Cross:
                    ApplyCrossEffect(eventBatch, activePlayer, match, GameMaster.Instance);
                    break;
                case Tile.Heal:
                    ApplyHealEffect(eventBatch, activePlayer, match);
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

    private void ApplyCrossEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match, GameMaster gm)
    {
        // Cross grants extra turn and multiplies the effect of next attack/heal.
        // can stack.
        // What's the diff between matching 3-4-5 cross?
        // => 1.75, 2.0, 2.5x multiplier.
        float gainedMultiplier = BasicCardEffects.GetCrossMultiplier(match.matchCount);
        activePlayer.GainMultiplier(gainedMultiplier);
        eventBatch.Add(EventPool.Get<CrossMatchedEvent>().Setup(activePlayer.netId, activePlayer.GetMultiplier()));
        gm.GrantExtraTurnToCurrentPlayer();
    }

    private static void ApplyHealEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match)
    {
        int healAmount = BasicCardEffects.GetHeal(match.matchCount);
        if (match.isDoubleEffect)
        {
            healAmount *= 2;
        }
        
        // Consume Cross Multiplier, if any.
        if (activePlayer.GetMultiplier() > 1 + Mathf.Epsilon)
        {
            healAmount = (int)(healAmount * activePlayer.GetMultiplier());
            eventBatch.Add(EventPool.Get<CrossConsumedEvent>().Setup(activePlayer.netId, activePlayer.GetMultiplier(), BlessingType.Healing));
            activePlayer.ResetMultiplier();
        }
        
        activePlayer.Heal(healAmount);
        eventBatch.Add(EventPool.Get<HealEvent>().Setup(activePlayer.GetCurrentHealth(), activePlayer.netId, powerful: match.isDoubleEffect));
    }

    private static void ApplyShieldEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match)
    {
        int shieldAmount = BasicCardEffects.GetShield(match.matchCount);
        
        // Consume Cross Multiplier, if any.
        if (activePlayer.GetMultiplier() > 1 + Mathf.Epsilon)
        {
            shieldAmount = (int)(shieldAmount * activePlayer.GetMultiplier());
            eventBatch.Add(EventPool.Get<CrossConsumedEvent>().Setup(activePlayer.netId, activePlayer.GetMultiplier(), BlessingType.Shield));
            activePlayer.ResetMultiplier();
        }
        
        activePlayer.GainShield(shieldAmount);
        ShieldEvent shieldEvent = EventPool.Get<ShieldEvent>();
        eventBatch.Add(shieldEvent.Setup(activePlayer.netId, activePlayer.GetShield()));
    }

    private static void ApplyAttackEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match, NetworkPlayer opponent)
    {
        int dmgAmount = BasicCardEffects.GetDamage(match.matchCount);
        if (match.isDoubleEffect)
        {
            dmgAmount *= 2;
        }
        // Consume Cross Multiplier, if any.
        if (activePlayer.GetMultiplier() > 1 + Mathf.Epsilon)
        {
            dmgAmount = (int)(dmgAmount * activePlayer.GetMultiplier());
            eventBatch.Add(EventPool.Get<CrossConsumedEvent>().Setup(activePlayer.netId, activePlayer.GetMultiplier(), BlessingType.Sword));
            activePlayer.ResetMultiplier();
        }
        
        int opponentShieldAmount = opponent.GetShield();

        int absorbedAmount = 0;
        if (opponentShieldAmount < dmgAmount)
        {
            absorbedAmount = opponentShieldAmount;
        }
        else
        {
            absorbedAmount = dmgAmount;
        }

        int remainingDmg = dmgAmount - absorbedAmount;
        opponent.LoseShield(absorbedAmount);
        opponent.TakeDamage(remainingDmg);

        var attackEvent = EventPool.Get<AttackEvent>();
        bool isPowerfulAttack = match.isDoubleEffect;
        attackEvent.Setup( opponent.GetCurrentHealth(), opponent.GetShield(), opponent.netId, isPowerfulAttack, absorbedAmount, remainingDmg);
        eventBatch.Add(attackEvent);
        
        if (opponent.GetCurrentHealth() == 0)
        {
            GameMaster.Instance.EndGame(activePlayer);
        }
    }

    private void SimulateTileFall(List<GameEventBase> eventBatch)
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
                            eventBatch.Add(EventPool.Get<TileMovedEvent>().Setup(tileToMove.uniqueID, GetGridPos(moveIndex)));
                            // Break the inner 'yAbove' loop to continue
                            // checking the *current* 'y' position again.
                            break; 
                        }
                    }
                }
            }
        }
    }
    
    private void RefillBoard(List<GameEventBase> eventBatch, bool avoidMatches)
    {
        // TODO: impl. Avoid/prevent Matches while refilling.
        // we cant directly use the logic from  setupWithoutMatches, as it assumes empty tiles in many places.
        // current check for match functions are also unusable as-is, as they query the board and check the tile type from there.
        // sure, we could set the tile and then check for matches, then revert, that would be fine.
        // but we also dont need all those matchresult allocations, we just want to know whether this would cause ANY MATCH
        // and we would try it for all types of cards, hopefully finding one that doesn't cause match, and spawn that one.
        
        Debug.Log("[Server] Refilling board");
        // rules: "bottom to top, then left to right"
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                var gridPros = new Vector2Int(x, y);
                if (GetTileAt(gridPros).IsEmpty())
                {
                    // TODO: Spawn considering if we restrict combo-matches.
                    
                    TileState fillingTile = GenerateNewTile();
                    int spawnIdx = GetIndex(x, y);
                    boardState[spawnIdx] = fillingTile;
                    
                    eventBatch.Add(EventPool.Get<TileSpawnedEvent>().Setup(fillingTile, gridPros));
                    Debug.Log($"[Server] Draw new tile to pos: ({x},{y})");
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

    private TileState GetTileSafe(int x, int y)
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