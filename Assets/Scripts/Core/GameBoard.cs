using System.Collections.Generic;
using System.Linq;
using Mirror;
using DorkyProductions.UI;
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
            
            Dictionary<Tile, double> dynamicWeights = TileDistribution.GetDynamicWeightsForAllowedTypes(_availableTypes);
            Tile newType = TileDistribution.SelectTileFromWeights(dynamicWeights);
            TileState newTile = GenerateNewTile(newType);
            boardState.Add(newTile);
            TileDistribution.TileAdded(newType);
        }

        return boardState;
    }

    public void ProcessSwapMove(Vector2Int posA, Vector2Int posB, NetworkIdentity performingPlayer, List<GameEventBase> eventBatch)
    {
        // Perform the swap ---
        int indexA = GetIndex(posA);
        int indexB = GetIndex(posB);
        TileState stateA = boardState[indexA];
        TileState stateB = boardState[indexB];
        boardState[indexA] = stateB;
        boardState[indexB] = stateA;
        
        eventBatch.Add(EventPool.Get<SwappedTilesEvent>().Setup(stateA.uniqueID, stateB.uniqueID));
        
        // Check the rows/cols of the two tiles we moved for a Match
        var matchesToProcess = MatchAlgorithm.FindMatchesAfterSwap(this, posA, posB);
        if (matchesToProcess.Count == 0)
        {
            // Non-match yielding moves are accepted, player turn ends here.
            return;
        }

        StabilizeBoard(eventBatch, matchesToProcess);
    }


    // Removes the tile at TilePos. runs the usual procedure.
    public void ProcessLightningEffect(Vector2Int tilePos, NetworkIdentity senderIdentity, List<GameEventBase> eventBatch)
    {
        // Remove tile without any Match effects.
        TileState tileToRemove = boardState[GetIndex(tilePos)];
        TileDistribution.TileRemoved(tileToRemove.type);
        eventBatch.Add(EventPool.Get<TileRemovedEvent>().Setup(tileToRemove.uniqueID));
        boardState[GetIndex(tilePos)] = TileState.Empty;
        
        SimulateTileFall(eventBatch);
        var matchesToProcess = MatchAlgorithm.FindAllMatchesOnBoardAlternative(this);

        if (matchesToProcess.Count > 0)
        {
            StabilizeBoard(eventBatch, matchesToProcess);
        }
        else
        {
            RefillBoard(eventBatch, true);
            matchesToProcess = MatchAlgorithm.FindAllMatchesOnBoardAlternative(this);
            StabilizeBoard(eventBatch, matchesToProcess);
        }
    }
    
    public void ProcessPhantomMatchEffect(List<Vector2Int> targetTiles, NetworkIdentity senderIdentity, List<GameEventBase> eventBatch)
    {
        TileState matchedTile = boardState[GetIndex(targetTiles[0])];
        
        bool isDoubleEffect = false;
        foreach (Vector2Int tilePos in targetTiles)
        {
            var state = boardState[GetIndex(tilePos)];
            if (state.isDoubleEffect)
            {
                isDoubleEffect = true;
                break;
            }
        }
        var matchesToProcess = new List<MatchResult>
        {
            new MatchResult(targetTiles, matchedTile.type, isDoubleEffect)
        };
        
        StabilizeBoard(eventBatch, matchesToProcess);
    }
    private void StabilizeBoard(List<GameEventBase> eventBatch, List<MatchResult> matchesToProcess)
    {
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
        
            ApplyMatchEffects(matchesToProcess, eventBatch);
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
    }
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
    
    public TileState GetTileAt(Vector2Int pos) => boardState[GetIndex(pos)];
    public static Vector2Int GetGridPos(int idx)
    {
        int y = idx % BoardWidth;
        int x = idx / BoardHeight;
        return new Vector2Int(x, y);
    }

    private void RemoveMatchedTiles(List<MatchResult> matchResults)
    {
        foreach (var match in matchResults)
        {
            foreach (var pos in match.positions)
            {
                TileDistribution.TileRemoved(boardState[GetIndex(pos)].type);
                boardState[GetIndex(pos)] = TileState.Empty;
            }
        }
    }
    // --- BOARD PROCESSING HELPERS ---

    // returns: Whether this match should stop the Chain events immediately: i.e, shouldOpenChest
    private void ApplyMatchEffects(List<MatchResult> matchResults, List<GameEventBase> eventBatch)
    {
        // This is where Card specific match effect will take place.
        foreach (var match in matchResults)
        {
            Debug.Log($"Matched: {match.matchCount} of {match.DisplayMatchType()}");
            var ids = new List<ushort>();
            foreach (var pos in match.positions)
            {
                ids.Add(GetTileAt(pos).uniqueID);
            }
            eventBatch.Add(EventPool.Get<MatchedTilesEvent>().Setup(ids));
            

            var activePlayerNetID = GameMaster.Instance.Context.ActivePlayerNetId;
            var activePlayer = GameMaster.Instance.GetPlayer(activePlayerNetID);
            
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
                    ApplyChestEffect(eventBatch, activePlayer, match, GameMaster.Instance);
                    break;
                default:
                    Debug.LogError($"Sth is wrong. what is this tile type?? : {match.tileType}");
                    break;
            }
        }

        return;
    }

    private void ApplyChestEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match, GameMaster gm)
    {
        // TODO: Use a distribution controlled via ScriptableObject here for the various chest effects & their drop rates.
        int chestSkillIdx = Random.Range(0, 2);
        
        eventBatch.Add(EventPool.Get<ChestMatchedEvent>().Setup(activePlayer.netId, chestSkillIdx));
        gm.GrantExtraTurnToCurrentPlayer(true);
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
        gm.GrantExtraTurnToCurrentPlayer(false);
    }

    private static void ApplyHealEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match)
    {
        int healAmount = BasicCardEffects.GetHeal(match.matchCount);
        if (match.isDoubleEffect)
        {
            healAmount *= 2;
        }
        
        // Consume Cross Multiplier, if any.
        if (activePlayer.GetMultiplier() > Mathf.Epsilon)
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
        if (activePlayer.GetMultiplier() > Mathf.Epsilon)
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
        if (activePlayer.GetMultiplier() > Mathf.Epsilon)
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
            GameMaster.Instance.TriggerEndGame(activePlayer, eventBatch);
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
        Debug.Log("[Server] Refilling board");
        // rules: "bottom to top, then left to right"
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                var gridPos = new Vector2Int(x, y);
                if (GetTileAt(gridPos).IsEmpty())
                {
                    TileState fillingTile;

                    if (avoidMatches)
                    {
                        // If avoiding matches, actively search for a safe tile type.
                        var possibleTypes = TileDistribution.GetAllPossibleTileTypes();
                        fillingTile = TryFindNonMatchingTile(gridPos, possibleTypes);
                    }
                    else
                    {
                        // Normal refill: generate tile based on weights, no match check.
                        var dynamicWeights = TileDistribution.CalculateDynamicWeights();
                        fillingTile = GenerateNewTile(dynamicWeights);
                    }
                
                    // Place the found/generated tile.
                    int spawnIdx = GetIndex(x, y);
                    boardState[spawnIdx] = fillingTile;
                    TileDistribution.TileAdded(fillingTile.type);
                
                    eventBatch.Add(EventPool.Get<TileSpawnedEvent>().Setup(fillingTile, gridPos));
                    Debug.Log($"[Server] Draw new tile to pos: ({x},{y})");
                }
            }
        }
    }

    private TileState TryFindNonMatchingTile(Vector2Int gridPos, List<Tile> possibleTypes)
    {
        // Shuffle the types to ensure a good random distribution of tile types when multiple are safe.
        // Assuming 'Shuffle' is an extension method for List<T>.
        possibleTypes.Shuffle();

        // We only need a temporary TileState instance for checking the type.
        TileState tempTile = new TileState();
    
        foreach (var type in possibleTypes)
        {
            // Temporarily assign the type for the check.
            tempTile.type = type; 
   
            if (!WouldCauseMatchAt(gridPos, tempTile))
            {
                // Found a safe type! Generate the final TileState instance (e.g., with unique ID, etc.)
                return GenerateNewTile(type); 
            }
        }

        // Fallback: If ALL types cause a match (extremely rare in non-full boards),
        // we must pick one to avoid an infinite loop or null reference. We accept the match here.
        Debug.LogWarning($"[Server] WARNING: All possible tile types cause a match at {gridPos}. Falling back to random type.");
        return GenerateNewTile(possibleTypes[0]);
    }

    private bool WouldCauseMatchAt(Vector2Int gridPos, TileState tempTile)
    {
        boardState[GetIndex(gridPos)] = tempTile;
        var foundMatches = MatchAlgorithm.FindMatchesAt(this, gridPos);
        
        //restore the boardstate
        boardState[GetIndex(gridPos)] = TileState.Empty;
        
        if (foundMatches.Count > 0) 
        {
            //There are matches if tile were there.
            return true;
        }

        return false;
    }
    private TileState GenerateNewTile(Tile type)
    {
        bool isDoubleEffect = false;
        if (type == Tile.Attack || type == Tile.Heal)
        {
            // %20 double effect
            isDoubleEffect = Random.value > 0.8f;
        }
        return new TileState(_nextTileID++, type, isDoubleEffect);
    }

    private TileState GenerateNewTile(Dictionary<Tile, double> dynamicWeights)
    {
        double totalWeight = dynamicWeights.Values.Sum();

        if (totalWeight <= 0)
        {
            //TODO: decide what to do here
            //Debug.Log(); // no tile can be dropped
        }

        double randomTarget = Random.value * totalWeight;
            
        double currentCumulativeWeight = 0;

        foreach (var kvp in dynamicWeights)
        {
            Tile tileType = kvp.Key;
            double weight = kvp.Value;
            
            currentCumulativeWeight += weight;

            // Check if the random target falls within this tile's weight segment
            if (randomTarget < currentCumulativeWeight)
            {
                // This tile is selected!
                bool isDoubleEffect = false;
                if (tileType == Tile.Attack || tileType == Tile.Heal)
                {
                    // %20 double effect
                    isDoubleEffect = Random.value > 0.8f;
                }
                return new TileState(_nextTileID++, tileType, isDoubleEffect);
            }
        }

        return default;
    }
    private int GetIndex(Vector2Int pos) =>  GetIndex(pos.x, pos.y);
    private int GetIndex(int x, int y) => (x * BoardHeight) + y;
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

}

}