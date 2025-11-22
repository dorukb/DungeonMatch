using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Mirror;

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
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(boardState);
        }

        public override void Deserialize(NetworkReader reader)
        {
            boardState.AddRange(reader.Read<List<TileState>>());
        }
        public override void Reset() => boardState.Clear();
    }

    public class GameEndedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.GameEnded;
        public uint winnerID;

        public GameEndedEvent Setup(uint id) { winnerID = id; return this; }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(winnerID);
        }

        public override void Deserialize(NetworkReader reader)
        {
            winnerID = reader.Read<uint>();
        }

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
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(matchedTileIDs);
        }

        public override void Deserialize(NetworkReader reader)
        {                    
            matchedTileIDs.AddRange(reader.Read<List<ushort>>());
        }

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
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(firstId);
            writer.Write(secondId);
        }

        public override void Deserialize(NetworkReader reader)
        {
            firstId = reader.Read<ushort>();
            secondId = reader.Read<ushort>();
        }

        public override void Reset() { firstId = 0; secondId = 0; }
    }

    public class SwapDeniedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.SwapDenied;
        public uint playerNetId;
        
        public SwapDeniedEvent Setup(uint id) { playerNetId = id; return this; }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(playerNetId);
        }

        public override void Deserialize(NetworkReader reader)
        {                    
            playerNetId = reader.Read<uint>();
        }

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
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(targetsUpdatedHealth);
            writer.Write(targetsUpdatedShield);
            writer.Write(isPowerful);
            writer.Write(targetPlayerID);
            writer.Write(absorbedByShieldAmount);
            writer.Write(sufferedDamage);
        }

        public override void Deserialize(NetworkReader reader)
        {    
            targetsUpdatedHealth = reader.Read<int>();
            targetsUpdatedShield = reader.Read<int>();
            isPowerful = reader.Read<bool>();
            targetPlayerID = reader.Read<uint>();
            absorbedByShieldAmount = reader.Read<int>();
            sufferedDamage = reader.Read<int>();
        }

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
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(targetsUpdatedHealth);
            writer.Write(isPowerful);
            writer.Write(targetPlayerID);
        }

        public override void Deserialize(NetworkReader reader)
        {   
            targetsUpdatedHealth = reader.Read<int>();
            isPowerful = reader.Read<bool>();
            targetPlayerID = reader.Read<uint>();
        }

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
            targetPlayerID = target; 
            targetsUpdatedShield = updatedShieldAmount;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(targetPlayerID);
            writer.Write(targetsUpdatedShield);
        }

        public override void Deserialize(NetworkReader reader)
        {
            targetPlayerID = reader.Read<uint>();
            targetsUpdatedShield = reader.Read<int>();
        }

        public override void Reset() => targetPlayerID = 0;
    }
    
    public class ChestMatchedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.ChestMatched;
        public uint targetPlayerID;
        public int receivedSkillIndex;
        public GameEventBase Setup(uint activePlayerNetId, int receivedSkillIdx)
        {
            this.targetPlayerID = activePlayerNetId;
            this.receivedSkillIndex = receivedSkillIdx;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(targetPlayerID);
            writer.Write(receivedSkillIndex);
        }

        public override void Deserialize(NetworkReader reader)
        {
            targetPlayerID = reader.Read<uint>();
            receivedSkillIndex = reader.Read<int>();
        }

        public override void Reset()
        {
            targetPlayerID = 0;
            receivedSkillIndex = 0;
        }

    }

    public class CrossMatchedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.CrossMatched;
        public uint targetPlayerID;
        public float currentMultiplier;
        public CrossMatchedEvent Setup(uint targetPlayerId, float currentMultiplier)
        {
            this.targetPlayerID = targetPlayerId; 
            this.currentMultiplier = currentMultiplier;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(targetPlayerID);
            writer.Write(currentMultiplier);
        }

        public override void Deserialize(NetworkReader reader)
        {
            targetPlayerID = reader.Read<uint>();
            currentMultiplier = reader.Read<float>();
        }

        public override void Reset()
        {
            targetPlayerID = 0;
            currentMultiplier = 1;
        }

    }
    public class CrossConsumedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.CrossConsumed;
        public uint targetPlayerID;
        public float appliedMultiplier;
        public BlessingType appliedBlessingType;
        public CrossConsumedEvent Setup(uint targetPlayerId, float multiplier, BlessingType blessingType)
        {
            this.targetPlayerID = targetPlayerId; 
            this.appliedMultiplier = multiplier;
            this.appliedBlessingType = blessingType;
            return this;
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(targetPlayerID);
            writer.Write(appliedMultiplier);
            writer.Write((byte)appliedBlessingType);
        }

        public override void Deserialize(NetworkReader reader)
        {
            targetPlayerID = reader.Read<uint>();
            appliedMultiplier = reader.Read<float>();
            appliedBlessingType = (BlessingType)reader.Read<byte>();
        }

        public override void Reset()
        {
            targetPlayerID = 0;
            appliedMultiplier = 1;
        }

    }
    // --- Immediate Events ---

    public enum TurnStartReason
    {
        TurnOrder,  // Default, we were the next player.
        Cross,      // Extra turn due to a Cross match
        Chest       // Limited extra turn due to a Chest match, only to open the Chest, not to make another swap!
    }
    public class TurnStartedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.TurnStarted;
        public uint playerNetId;
        public TurnStartReason reason;
        
        public TurnStartedEvent Setup(uint nextPlayerId, TurnStartReason reason) 
        {
            this.playerNetId = nextPlayerId;
            this.reason = reason;
            return this; 
        }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(playerNetId);
            writer.Write((byte)reason);
        }

        public override void Deserialize(NetworkReader reader)
        {
            playerNetId = reader.Read<uint>();
            reason = (TurnStartReason)reader.Read<byte>();
        }

        public override void Reset() => playerNetId = 0;
    }

    public class TurnEndedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Blocking;
        public override EventType EventType => EventType.TurnEnded;
        public uint playerNetId;
        
        public TurnEndedEvent Setup(uint id) { playerNetId = id; return this; }
        public override Tween Accept(IGameEventHandler handler) => handler.Handle(this);
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(playerNetId);
        }

        public override void Deserialize(NetworkReader reader)
        {
            playerNetId = reader.Read<uint>();
        }

        public override void Reset() => playerNetId = 0;
    }

    // --- Parallel Events ---

    public class TileMovedEvent : GameEventBase
    {
        public override SyncType SyncType => SyncType.Parallel;
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
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(tileId);
            writer.Write(toGridPos);
        }

        public override void Deserialize(NetworkReader reader)
        { 
            tileId = reader.Read<ushort>();
            toGridPos = reader.Read<Vector2Int>();
        }

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
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(pos);
            writer.Write(state);
        }

        public override void Deserialize(NetworkReader reader)
        {
            pos = reader.Read<Vector2Int>();
            state = reader.Read<TileState>();
        }

        public override void Reset() { pos = Vector2Int.zero; state = default; }
    }
}