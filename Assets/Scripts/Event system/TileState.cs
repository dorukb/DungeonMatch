namespace DorkyProductions
{
    public struct TileState
    {
        public static ushort INVALID_TILE_ID = 9999;
        public ushort uniqueID;
        public Tile type;     // 0=Attack, 1=Attack x2, 2=Shield, etc.
        public bool isDoubleEffect;
        // A static "empty" tile for logic
        public TileState(ushort uniqueID, Tile type, bool isDoubleEffect)
        {
            this.uniqueID = uniqueID;
            this.type = type;
            this.isDoubleEffect = isDoubleEffect;
        }
        public static TileState Empty => new TileState 
        { 
            uniqueID = INVALID_TILE_ID, // An invalid, recognizable ID
            type = Tile.Unknown,        // An invalid type
            isDoubleEffect = false,
        };

        public bool IsEmpty() => type == Tile.Unknown;
    }
}