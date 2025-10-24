using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

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

public enum EventSyncType
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
public struct PlayerData
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
    public Vector2Int toPos;
}
[Serializable]
public struct GameStartData
{
    public List<TileState> boardState;
}
[Serializable]
public struct MatchData
{
    public int[] matchedTileIndices;
    public int matchedTileCount;
    
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
    public PlayerData playerData;
    public SwapData swapData;
    public TileMoveData tileMoveData;
    public MatchData matchData;
    public GameStartData gameStartData;
    
    // --- Client-side "Tagger" ---
    // This property tells the client how to process this event
    public EventSyncType SyncType
    {
        get
        {
            switch (type)
            {
                // BLOCKING events
                case EventType.SwapOccurred:
                case EventType.SwapDenied:
                case EventType.MatchOccurred:
                case EventType.GameStarted:
                    return EventSyncType.Blocking;
                
                // PARALLEL events
                case EventType.TileMoved:
                case EventType.TileSpawned:
                    return EventSyncType.Parallel;

                // IMMEDIATE events
                case EventType.TurnStarted:
                case EventType.TurnEnded:
                case EventType.GameEnded:
                    return EventSyncType.Immediate;

                // Default to blocking to be safe
                default:
                    return EventSyncType.Blocking;
            }
        }
    }
    
    // --- Static Factory Methods (Server-side) ---
    public static GameEvent GameStarted (List<TileState> boardState)
    {
        return new GameEvent
        {
            type = EventType.GameStarted,
            gameStartData = new GameStartData() { boardState = boardState}
        };
    }
    public static GameEvent TurnStarted(uint playerNetId)
    {
        return new GameEvent
        {
            type = EventType.TurnStarted,
            playerData = new PlayerData { playerNetId = playerNetId }
        };
    }
    public static GameEvent TurnEnded(uint playerNetId)
    {
        return new GameEvent
        {
            type = EventType.TurnEnded,
            playerData = new PlayerData { playerNetId = playerNetId }
        };
    }
    public static GameEvent SwapOccurred(ushort firstId, ushort secondId)
    {
        return new GameEvent
        {
            type = EventType.SwapOccurred,
            swapData = new SwapData { firstId = firstId, secondId = secondId }
        };
    }

    public static GameEvent TileMoved(ushort tileId, Vector2Int to)
    {
        return new GameEvent
        {
            type = EventType.TileMoved,
            tileMoveData = new TileMoveData { tileId = tileId, toPos = to }
        };
    }
    
    // ... Add factory methods for all your other events
}
}