using System.Collections.Generic;
using Mirror;
using Firebase.RemoteConfig;
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

    // OPTIMIZATION: Reusable list of ScriptableObjects (References only, very cheap)
    private List<TileDefinitionSO> _validCandidates = new List<TileDefinitionSO>();
    
    private TileDatabase _tileDatabase;
    public GameBoard(TileDatabase tileDatabase)
    {
        _tileDatabase = tileDatabase;
    }
    
   public List<TileState> SetupInitialBoardWithNoMatches()
    {
        boardState.Clear();
        if (boardState.Capacity < BoardWidth * BoardHeight)
            boardState.Capacity = BoardWidth * BoardHeight;

        var masterList = _tileDatabase.allTileDefinitions;

        for (int i = 0; i < BoardWidth * BoardHeight; i++)
        {
            int x = i % BoardWidth;
            int y = i / BoardWidth;

            // ---------------------------------------------------------
            // 1. DETERMINE CONSTRAINTS
            // ---------------------------------------------------------
            Tile forbiddenType1 = Tile.Unknown;
            Tile forbiddenType2 = Tile.Unknown;

            // Check Left (Needs 2 tiles to the left)
            if (x >= 2)
            {
                Tile left1 = GetTileSafe(x - 1, y).type;
                Tile left2 = GetTileSafe(x - 2, y).type;

                // Only forbid if they are valid types and they match
                if (left1 != Tile.Unknown && left1 == left2)
                {
                    forbiddenType1 = left1;
                }
            }

            // Check Down (Needs 2 tiles below)
            if (y >= 2)
            {
                Tile down1 = GetTileSafe(x, y - 1).type;
                Tile down2 = GetTileSafe(x, y - 2).type;

                if (down1 != Tile.Unknown && down1 == down2)
                {
                    forbiddenType2 = down1;
                }
            }

            _validCandidates.Clear();

            for (int k = 0; k < masterList.Count; k++)
            {
                var def = masterList[k];
                
                if (def.type == Tile.Unknown) continue;
                if (def.type == forbiddenType1 || def.type == forbiddenType2) continue;

                _validCandidates.Add(def);
            }

            TileDefinitionSO selectedDef;

            if (_validCandidates.Count > 0)
            {
                selectedDef = _tileDatabase.GetWeightedRandomDefinition(_validCandidates);
            }
            else
            {
                // FALLBACK: Rules were too strict
                selectedDef = _tileDatabase.GetRandomDefinition(); // Uses master list internally
            }
            boardState.Add(GenerateNewTile(selectedDef));
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
        while (matchesToProcess.Count > 0)
        { 
            // Debug.Log("[Server] Matches on board:");
            // foreach (var res in matchesToProcess)
            // {
            //     Debug.Log(res.Debug());
            // }
        
            ApplyMatchEffects(matchesToProcess, eventBatch);
            RemoveMatchedTiles(matchesToProcess);
            SimulateTileFall(eventBatch);
            matchesToProcess = MatchAlgorithm.FindAllMatchesOnBoardAlternative(this);
            
            // Refill, when no more falling matches
            if (matchesToProcess.Count == 0)
            {
                // TODO: when chest breaks this flow, avoid further matches is useless, this is "after a swap" maybe we need a turn based check.
                // 85% chance to avoid matches (true),15% chance to allow them (false)
                bool avoidMatches = Random.value < RemoteConfigManager.Instance.GetAvoidMatchChance();
                RefillBoard(eventBatch, avoidMatches);
                matchesToProcess = MatchAlgorithm.FindAllMatchesOnBoardAlternative(this);
            }
        }
    }
    public bool IsValidSwap(Vector2Int posA, Vector2Int posB, bool isVerbose = false)
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
            if (isVerbose)
            {
                Debug.LogWarning($"[Client] Invalid swap: {posA} <-> {posB}. Tiles are same type.");
            }
            return false;
        }

        if (GetTileAt(posA).type == Tile.Unknown|| GetTileAt(posB).type == Tile.Unknown)
        {
            if (isVerbose)
            {
                Debug.LogWarning($"[Client] Invalid swap: {posA} <-> {posB}. at least one of the tiles is Empty.");
            }
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
            // Debug.Log($"Matched: {match.matchCount} of {match.DisplayMatchType()}");
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
        gm.NotifyBotChestMatched(chestSkillIdx);
        gm.GrantExtraTurnToCurrentPlayer(true);
    }

    private void ApplyCrossEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match, GameMaster gm)
    {
        // Cross grants extra turn and multiplies the effect of next attack/heal.
        // can stack.
        // What's the diff between matching 3-4-5 cross?
        // => 1.75, 2.0, 2.5x multiplier.
        float gainedMultiplier = RemoteConfigManager.Instance.GetCrossVal(match.matchCount);
        activePlayer.GainMultiplier(gainedMultiplier);
        eventBatch.Add(EventPool.Get<CrossMatchedEvent>().Setup(activePlayer.netId, activePlayer.GetMultiplier()));
        gm.GrantExtraTurnToCurrentPlayer(false);
    }

    private static void ApplyHealEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match)
    {
        int healAmount = RemoteConfigManager.Instance.GetHealVal(match.matchCount);
        
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
        
        int actualHealedAmount = activePlayer.Heal(healAmount);
        eventBatch.Add(EventPool.Get<HealEvent>().Setup(activePlayer.GetCurrentHealth(), activePlayer.netId, powerful: match.isDoubleEffect, actualHealedAmount));
    }

    private static void ApplyShieldEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match)
    {
        int shieldAmount = RemoteConfigManager.Instance.GetShieldVal(match.matchCount);
        
        // Consume Cross Multiplier, if any.
        if (activePlayer.GetMultiplier() > Mathf.Epsilon)
        {
            shieldAmount = (int)(shieldAmount * activePlayer.GetMultiplier());
            eventBatch.Add(EventPool.Get<CrossConsumedEvent>().Setup(activePlayer.netId, activePlayer.GetMultiplier(), BlessingType.Shield));
            activePlayer.ResetMultiplier();
        }
        
        int actualGainedShieldAmount = activePlayer.GainShield(shieldAmount);
        ShieldEvent shieldEvent = EventPool.Get<ShieldEvent>();
        eventBatch.Add(shieldEvent.Setup(activePlayer.netId, activePlayer.GetShield(), actualGainedShieldAmount));
    }

    private static void ApplyAttackEffect(List<GameEventBase> eventBatch, NetworkPlayer activePlayer, MatchResult match, NetworkPlayer opponent)
    {
        int dmgAmount = RemoteConfigManager.Instance.GetAttackVal(match.matchCount);

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
        bool isFinalHit = opponent.GetCurrentHealth() == 0;
        attackEvent.Setup( opponent.GetCurrentHealth(), opponent.GetShield(), opponent.netId, isPowerfulAttack, absorbedAmount, remainingDmg, isFinalHit );
        eventBatch.Add(attackEvent);
        
        if (isFinalHit)
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
        // Debug.Log("[Server] Refilling board");
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
                        fillingTile = TryFindNonMatchingTile(gridPos);
                    }
                    else
                    {
                        var def = _tileDatabase.GetRandomDefinition();
                        fillingTile = GenerateNewTile(def);
                    }
                
                    // Place the found/generated tile.
                    int spawnIdx = GetIndex(x, y);
                    boardState[spawnIdx] = fillingTile;
                
                    eventBatch.Add(EventPool.Get<TileSpawnedEvent>().Setup(fillingTile, gridPos));
                    // Debug.Log($"[Server] Draw new tile to pos: ({x},{y})");
                }
            }
        }
    }
    private TileState TryFindNonMatchingTile(Vector2Int gridPos)
    {
        // 1. CLEAR buffer
        _validCandidates.Clear();
        
        // We need a temp state just to check the 'WouldCauseMatch' logic
        TileState tempTile = new TileState();

        // 2. ITERATE the master list directly
        // We don't filter types; we check every definition in the database.
        var masterList = _tileDatabase.allTileDefinitions;
        for (int i = 0; i < masterList.Count; i++)
        {
            TileDefinitionSO def = masterList[i];
            
            // Assign type to temp state for the check
            tempTile.type = def.type;

            if (!WouldCauseMatchAt(gridPos, tempTile))
            {
                _validCandidates.Add(def);
            }
        }

        // 3. PICK directly from buffer
        if (_validCandidates.Count > 0)
        {
            TileDefinitionSO selectedDef = _tileDatabase.GetWeightedRandomDefinition(_validCandidates);
            return GenerateNewTile(selectedDef);
        }

        // Fallback
        Debug.LogWarning($"[Server] No safe tile found at {gridPos}.");
        return GenerateNewTile(_tileDatabase.GetRandomDefinition());
    }

    // UPDATED: Now takes the SO directly! No more lookups.
    private TileState GenerateNewTile(TileDefinitionSO def)
    {
        bool isDouble = false;
        if (def.supportsDoubleEffect)
        {
            isDouble = Random.value < def.doubleEffectChance;
        }
        return new TileState(_nextTileID++, def.type, isDouble);
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
    
    internal void SetTileAtInternal(int index, TileState state)
    {
        boardState[index] = state;
    }

}

}