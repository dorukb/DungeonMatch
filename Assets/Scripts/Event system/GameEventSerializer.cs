using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions
{
    /// <summary>
    /// Custom serializer for manually writing/reading GameEventBase
    /// to and from a byte stream. Integrates with the EventPool.
    /// </summary>
    public static class GameEventSerializer
    {
        // --- WRITER ---
        public static void WriteGameEvent(this NetworkWriter writer, GameEventBase ev)
        {
            // The writer logic is identical, as it switches on the concrete type
            switch (ev)
            {
                case GameStartedEvent e:
                    writer.Write((byte)EventType.GameStarted);
                    writer.Write(e.boardState);
                    break;
                case GameEndedEvent e:
                    writer.Write((byte)EventType.GameEnded);
                    writer.Write(e.winnerID);
                    break;
                case MatchedTilesEvent e:
                    writer.Write((byte)EventType.MatchedTiles);
                    writer.Write(e.matchedTileIDs);
                    break;
                case SwappedTilesEvent e:
                    writer.Write((byte)EventType.SwappedTiles);
                    writer.Write(e.firstId);
                    writer.Write(e.secondId);
                    break;
                case SwapDeniedEvent e:
                    writer.Write((byte)EventType.SwapDenied);
                    writer.Write(e.playerNetId);
                    break;
                case AttackEvent e:
                    writer.Write((byte)EventType.Attack);
                    writer.Write(e.targetsUpdatedHealth);
                    writer.Write(e.targetsUpdatedShield);
                    writer.Write(e.isPowerful);
                    writer.Write(e.targetPlayerID);
                    writer.Write(e.absorbedByShieldAmount);
                    writer.Write(e.sufferedDamage);
                    break;
                case HealEvent e:
                    writer.Write((byte)EventType.Heal);
                    writer.Write(e.targetsUpdatedHealth);
                    writer.Write(e.isPowerful);
                    writer.Write(e.targetPlayerID);
                    break;
                case ShieldEvent e:
                    writer.Write((byte)EventType.Shield);
                    writer.Write(e.targetPlayerID);
                    writer.Write(e.targetsUpdatedShield);
                    break;
                case CrossMatchedEvent e:
                    writer.Write((byte)EventType.Shield);
                    writer.Write(e.targetPlayerID);
                    writer.Write(e.currentMultiplier);
                    break;
                case CrossConsumedEvent e:
                    writer.Write((byte)EventType.CrossConsumed);
                    writer.Write(e.targetPlayerID);
                    writer.Write(e.appliedMultiplier);
                    writer.Write((byte)e.appliedBlessingType);
                    break;
                case TurnStartedEvent e:
                    writer.Write((byte)EventType.TurnStarted);
                    writer.Write(e.playerNetId);
                    break;
                case TurnEndedEvent e:
                    writer.Write((byte)EventType.TurnEnded);
                    writer.Write(e.playerNetId);
                    break;
                case TileMovedEvent e:
                    writer.Write((byte)EventType.TileMoved);
                    writer.Write(e.tileId);
                    writer.Write(e.toGridPos);
                    break;
                case TileSpawnedEvent e:
                    writer.Write((byte)EventType.TileSpawned);
                    writer.Write(e.pos);
                    writer.Write(e.state);
                    break;
                default:
                    Debug.LogError($"No writer for event type {ev.GetType()}");
                    break;
            }
        }

        // --- READER ---
        public static GameEventBase ReadGameEvent(this NetworkReader reader)
        {
            // 1. Read the "Type ID" byte
            EventType type = (EventType)reader.Read<byte>();

            // 2. Get a recycled object from the pool
            GameEventBase ev = EventPool.Get(type);

            // 3. Populate the pooled object with data
            switch (type)
            {
                case EventType.GameStarted:
                    ((GameStartedEvent)ev).boardState.AddRange(reader.Read<List<TileState>>());
                    break;
                case EventType.GameEnded:
                    ((GameEndedEvent)ev).winnerID = reader.Read<uint>();
                    break;
                case EventType.MatchedTiles:
                    ((MatchedTilesEvent)ev).matchedTileIDs.AddRange(reader.Read<List<ushort>>());
                    break;
                case EventType.SwappedTiles:
                    var swappedEvent = (SwappedTilesEvent)ev;
                    swappedEvent.firstId = reader.Read<ushort>();
                    swappedEvent.secondId = reader.Read<ushort>();
                    break;
                case EventType.SwapDenied:
                    ((SwapDeniedEvent)ev).playerNetId = reader.Read<uint>();
                    break;
                case EventType.Attack:
                    var attackEvent = (AttackEvent)ev;
                    attackEvent.targetsUpdatedHealth = reader.Read<int>();
                    attackEvent.targetsUpdatedShield = reader.Read<int>();
                    attackEvent.isPowerful = reader.Read<bool>();
                    attackEvent.targetPlayerID = reader.Read<uint>();
                    attackEvent.absorbedByShieldAmount = reader.Read<int>();
                    attackEvent.sufferedDamage = reader.Read<int>();
                    break;
                case EventType.Heal:
                    var potionEvent = (HealEvent)ev;
                    potionEvent.targetsUpdatedHealth = reader.Read<int>();
                    potionEvent.isPowerful = reader.Read<bool>();
                    potionEvent.targetPlayerID = reader.Read<uint>();
                    break;
                case EventType.Shield:
                    var shieldEvent = (ShieldEvent)ev;
                    shieldEvent.targetPlayerID = reader.Read<uint>();
                    shieldEvent.targetsUpdatedShield = reader.Read<int>();
                    break;
                case EventType.CrossMatched:
                    var crossMatchedEvent = (CrossMatchedEvent)ev;
                    crossMatchedEvent.targetPlayerID = reader.Read<uint>();
                    crossMatchedEvent.currentMultiplier = reader.Read<float>();
                    break;
                case EventType.CrossConsumed:
                    var crossConsumedEvent = (CrossConsumedEvent)ev;
                    crossConsumedEvent.targetPlayerID = reader.Read<uint>();
                    crossConsumedEvent.appliedMultiplier = reader.Read<float>();
                    crossConsumedEvent.appliedBlessingType = (BlessingType)reader.Read<byte>();
                    break;
                case EventType.TurnStarted:
                    ((TurnStartedEvent)ev).playerNetId = reader.Read<uint>();
                    break;
                case EventType.TurnEnded:
                    ((TurnEndedEvent)ev).playerNetId = reader.Read<uint>();
                    break;
                case EventType.TileMoved:
                    var movedEvent = (TileMovedEvent)ev;
                    movedEvent.tileId = reader.Read<ushort>();
                    movedEvent.toGridPos = reader.Read<Vector2Int>();
                    break;
                case EventType.TileSpawned:
                    var spawnedEvent = (TileSpawnedEvent)ev;
                    spawnedEvent.pos = reader.Read<Vector2Int>();
                    spawnedEvent.state = reader.Read<TileState>();
                    break;
                default:
                    Debug.LogError($"No reader for event type {type}");
                    // Release the unused event back to the pool
                    EventPool.Release(ev);
                    return null;
            }
            return ev;
        }
    }
}