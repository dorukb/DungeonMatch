using System.Collections.Generic;
using Mirror;

namespace DorkyProductions
{
    public struct TileState
    {
        public static ushort INVALID_TILE_ID = 9999;
        public ushort uniqueID;
        public Tile type;     // 0=Attack, 1=Attack x2, 2=Shield, etc.
        public bool isDoubleEffect;
        // A static "empty" tile for logic
        public static TileState Empty => new TileState 
        { 
            uniqueID = INVALID_TILE_ID, // An invalid, recognizable ID
            type = Tile.Unknown,        // An invalid type
            isDoubleEffect = false,
        };

        public bool IsEmpty() => type == Tile.Unknown;
    }
    /// <summary>
    /// This static class provides the explicit serialization methods
    /// for the TileState struct, which Mirror's weaver will
    /// automatically find and use.
    /// </summary>
    public static class TileStateSerializer
    {
        public static void WriteListTileState(this NetworkWriter writer, List<TileState> states)
        {
            // Write null check (as int count)
            if (states == null)
            {
                writer.Write(-1);
                return;
            }
            // Write count
            writer.Write(states.Count);

            // Write each element using the (assumed registered) TileState writer
            for (int i = 0; i < states.Count; i++)
            {
                // This relies on 'WriteTileState' being registered or found
                writer.Write(states[i]); 
            }
        }

        public static List<TileState> ReadListTileState(this NetworkReader reader)
        {
            // Read count
            int count = reader.Read<int>();

            // Handle null
            if (count == -1)
            {
                return null;
            }

            List<TileState> states = new List<TileState>(count);
            for (int i = 0; i < count; i++)
            {
                // This relies on 'ReadTileState' being registered or found
                states.Add(reader.Read<TileState>());
            }
            return states;
        }
        public static void WriteTileState(this NetworkWriter writer, TileState state)
        {
            writer.Write(state.uniqueID);
            // Serialize the enum as its underlying byte value for efficiency
            writer.Write((byte)state.type); 
            writer.Write(state.isDoubleEffect);
        }

        public static TileState ReadTileState(this NetworkReader reader)
        {
            return new TileState
            {
                uniqueID = reader.Read<ushort>(),
                type = (Tile)reader.Read<byte>(),
                isDoubleEffect = reader.Read<bool>()
            };
        }
    }
}