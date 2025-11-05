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