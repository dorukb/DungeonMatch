using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace  DorkyProductions
{
    
public enum EventType
{
    // Blocking
    SwapOccurred,
    SwapDenied,
    MatchOccurred, // e.g., show an explosion
    
    // Parallel
    TileMoved,
    TileSpawned,
    
    // Immediate
    TurnStarted,
    TurnEnded,
    GameStarted,
    GameEnded,
}

public enum SyncType
{
    // Waits for this event's animation to finish before starting the next event.
    Blocking,
    
    // Plays this event's animation, but does NOT wait. Starts the next event immediately.
    Immediate,
    
    // Groups with other 'Parallel' events of the same Type that follow it, to be played simultaneously.
    // The queue will wait for ALL of them to finish before continuing.
    Parallel
}

[Serializable]
public struct TurnData
{
    public uint playerNetId;
}

[Serializable]
public struct SwapData
{
    public ushort firstId;
    public ushort secondId;
}

[Serializable]
public struct TileMoveData
{
    // TODO: do we need tile index?
    public ushort tileId;
    public Vector2Int toGridPos;
}

[Serializable]
public struct TileSpawnData
{
    public Vector2Int pos;
    public TileState state;
}

[Serializable]
public struct GameStartData
{
    public List<TileState> boardState;
}
[Serializable]
public struct MatchData
{
     public List<ushort> matchedTileIDs;
    // TODO: We prob dont need the player id, simple ActivePlayer check can resolve all issues.
    // all events belong to activePlayer unless specified (Sword match fires TakeDamageEvent that will include targetID)
    // public uint playerNetId;
    // active player is set after every PlayerTurnStart() event.
}

[Serializable]
public struct GameEvent
{
    public EventType type;

    // --- All possible event data, neatly grouped ---
    public TurnData turnData;
    public SwapData swapData;
    public TileMoveData tileMoveData;
    public TileSpawnData tileSpawnData;
    public MatchData matchData;
    public GameStartData gameStartData;
    public SyncType syncType;
    
    
    // Blocking Event
    public static GameEvent GameStarted (List<TileState> boardState)
    {
        return new GameEvent
        {
            type = EventType.GameStarted,
            syncType = SyncType.Blocking,
            gameStartData = new GameStartData() { boardState = boardState }
        };
    }
    public static GameEvent SwapOccurred(ushort firstId, ushort secondId)
    {
        return new GameEvent
        {
            type = EventType.SwapOccurred,
            syncType = SyncType.Blocking,
            swapData = new SwapData { firstId = firstId, secondId = secondId }
        };
    }

    public static GameEvent MatchOccurred(List<ushort> matchedTileIndices)
    {
        return new GameEvent()
        {
            type = EventType.MatchOccurred,
            syncType = SyncType.Blocking,
            matchData = new MatchData { matchedTileIDs = matchedTileIndices }
        };
    }
    
    // ImmediateEvents
    public static GameEvent TurnStarted(uint playerNetId)
    {
        return new GameEvent
        {
            type = EventType.TurnStarted,
            syncType = SyncType.Immediate,
            turnData = new TurnData { playerNetId = playerNetId }
        };
    }
    public static GameEvent TurnEnded(uint playerNetId)
    {
        return new GameEvent
        {
            type = EventType.TurnEnded,
            syncType = SyncType.Immediate,
            turnData = new TurnData { playerNetId = playerNetId }
        };
    }

    // Parallel Events.
    public static GameEvent TileMoved(ushort tileId, Vector2Int to)
    {
        return new GameEvent
        {
            type = EventType.TileMoved,
            syncType = SyncType.Parallel,
            tileMoveData = new TileMoveData { tileId = tileId, toGridPos = to }
        };
    } 
    public static GameEvent TileSpawned (TileState state, Vector2Int spawnPos)
    {
        return new GameEvent
        {
            type = EventType.TileMoved,
            syncType = SyncType.Parallel,
            tileSpawnData = new TileSpawnData() { pos = spawnPos, state = state }
        };
    }
    public static GameEvent SwapFailed(uint playerId)
    {
        return new GameEvent
        {
            type = EventType.SwapDenied,
            syncType = SyncType.Blocking,
            turnData = new TurnData { playerNetId = playerId }
        };
    }
}
}