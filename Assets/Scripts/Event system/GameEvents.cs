using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace DorkyProductions
{
    // --- Blocking Events ---

    public class GameStartedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.GameStarted;
        public List<TileState> boardState = new List<TileState>();

        public GameStartedEvent Setup(List<TileState> state)
        {
            boardState.Clear();
            boardState.AddRange(state);
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() => boardState.Clear();
    }

    public class GameEndedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.GameEnded;
        public uint winnerID;

        public GameEndedEvent Setup(uint id) { winnerID = id; return this; }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() => winnerID = 0;
    }

    public class MatchedTilesEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.MatchedTiles;
        public List<ushort> matchedTileIDs = new List<ushort>();
        
        public MatchedTilesEvent Setup(List<ushort> ids) 
        {
            matchedTileIDs.Clear();
            matchedTileIDs.AddRange(ids);
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() => matchedTileIDs.Clear();
    }

    public class SwappedTilesEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.SwappedTiles;
        public ushort firstId;
        public ushort secondId;
        
        public SwappedTilesEvent Setup(ushort first, ushort second)
        {
            firstId = first;
            secondId = second;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() { firstId = 0; secondId = 0; }
    }

    public class SwapDeniedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.SwapDenied;
        public uint playerNetId;
        
        public SwapDeniedEvent Setup(uint id) { playerNetId = id; return this; }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() => playerNetId = 0;
    }

    public class AttackEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.Attack;
        public uint targetPlayerID;
        public int targetsUpdatedHealth;
        public int absorbedByShieldAmount;
        public int sufferedDamage;
        public int targetsUpdatedShield;
        public bool isPowerful;
        
        public AttackEvent Setup(int health, int remainingShield, uint target, bool powerful, int absorbedAmount, int sufferedDamageAmount)
        {
            targetsUpdatedHealth = health;
            targetsUpdatedShield = remainingShield;
            isPowerful = powerful;
            targetPlayerID = target;
            absorbedByShieldAmount = absorbedAmount;
            sufferedDamage = sufferedDamageAmount;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() { targetsUpdatedHealth = 0; isPowerful = false; targetPlayerID = 0; }
    }

    public class HealEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.Heal;
        public int targetsUpdatedHealth;
        public bool isPowerful;
        public uint targetPlayerID;
        
        public HealEvent Setup(int health, uint targetId, bool powerful)
        {
            targetsUpdatedHealth = health;
            isPowerful = powerful;
            targetPlayerID = targetId;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() { targetsUpdatedHealth = 0; isPowerful = false; targetPlayerID = 0; }
    }

    public class ShieldEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.Shield;
        public uint targetPlayerID;
        public int targetsUpdatedShield;

        public ShieldEvent Setup(uint target, int updatedShieldAmount)
        {
            targetPlayerID = target; return this;
            targetsUpdatedShield = updatedShieldAmount;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() => targetPlayerID = 0;
    }

    // --- Immediate Events ---

    public class TurnStartedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Immediate;
        public override EventType EventType => EventType.TurnStarted;
        public uint playerNetId;
        
        public TurnStartedEvent Setup(uint id) { playerNetId = id; return this; }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() => playerNetId = 0;
    }

    public class TurnEndedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Immediate;
        public override EventType EventType => EventType.TurnEnded;
        public uint playerNetId;
        
        public TurnEndedEvent Setup(uint id) { playerNetId = id; return this; }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() => playerNetId = 0;
    }

    // --- Parallel Events ---

    public class TileMovedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.TileMoved;
        public ushort tileId;
        public Vector2Int toGridPos;
        
        public TileMovedEvent Setup(ushort id, Vector2Int pos)
        {
            tileId = id;
            toGridPos = pos;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() { tileId = 0; toGridPos = Vector2Int.zero; }
    }

    public class TileSpawnedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.TileSpawned;
        public Vector2Int pos;
        public TileState state;
        
        public TileSpawnedEvent Setup(TileState state, Vector2Int pos)
        {
            this.pos = pos;
            this.state = state;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Reset() { pos = Vector2Int.zero; state = default; }
    }
}