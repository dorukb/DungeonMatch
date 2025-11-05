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
}