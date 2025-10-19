using System.Collections.Generic;
using Mirror;
using UnityEngine;

// It tells clients "A tile of this type with this unique ID exists."
public struct TileState
{
    public static ushort INVALID_TILE_ID = 9999;
    public ushort uniqueID;
    public int tileType;     // 0=Attack, 1=Attackx2, 2=Shield, etc.

    // A static "empty" tile for logic
    public static TileState Empty => new TileState 
    { 
        uniqueID = INVALID_TILE_ID, // An invalid, recognizable ID
        tileType = -1           // An invalid type
    };

    public bool IsEmpty() => tileType == -1;
}

// This struct holds the data for ONE match (e.g., a 4-in-a-row)
public class MatchData
{
    public int tileTypeID;
    public int matchCount;
    // We store the actual positions for clearing them
    public List<Vector2Int> positions;

    public string ToString()
    {
        return TileDefinitionSO.ToString(tileTypeID);
    }
}

public class GameBoard : NetworkBehaviour
{
    [Header("Board Dimensions")]
    public const int BoardWidth = 5;
    public const int BoardHeight = 5;
    
    [Header("UI Layout (Read by Client)")]
    [Tooltip("The RectTransform that holds the 5x5 grid UI.")]
    public RectTransform boardContainer;

    [Tooltip("The size (width/height) of a single tile's RectTransform.")]
    public Vector2 tileViewSize = new Vector2(100, 100);

    [Tooltip("The space between adjacent tiles.")]
    public Vector2 tileSpacing = new Vector2(10, 10);
    
    [Header("Game Data")]
    [SerializeField]
    private TileDatabase tileDatabase; // Assign your TileDatabase SO here

    [SerializeField]
    private ClientBoardVisualizer _visualizer;

    
    // The "Single Source of Truth" for all clients.
    // This list represents a 5x5 grid, flattened to 1D.
    // Index = (y * BoardWidth) + x
    public readonly SyncList<TileState> boardState = new SyncList<TileState>();

    private ushort _nextTileID = 0;

    public override void OnStartServer()
    {
        if (!isServer)
        {
            Debug.LogError("This shouldn't be called on Clients!");
            return;
        }
        
        // When the server starts, populate the initial board.
        // (Don't just add 25 items, you need to call .Add()
        // for each one to sync properly)
        for (int i = 0; i < BoardHeight * BoardWidth; i++)
        {
            boardState.Add(GenerateNewTile());
        }
    }
    public override void OnStartClient()
    {
        // Add handlers for SyncList Actions
        boardState.OnAdd += OnItemAdded;
        boardState.OnInsert += OnItemInserted;
        boardState.OnSet += OnItemSet;
        boardState.OnRemove += OnItemRemoved;
        boardState.OnClear += OnListCleared;

        // For Clients, List is populated before handlers are wired up so we
        // need to manually invoke OnAdd for each element.
        for (int i = 0; i < boardState.Count; i++)
            boardState.OnAdd.Invoke(i);
    }

    public override void OnStopClient()
    {
        // Remove handlers when client stops
        boardState.OnAdd -= OnItemAdded;
        boardState.OnInsert -= OnItemInserted;
        boardState.OnSet -= OnItemSet;
        boardState.OnRemove -= OnItemRemoved;
        boardState.OnClear -= OnListCleared;
        // namesList.OnChange -= OnListChanged;
    }

    private void OnItemAdded(int idx) => _visualizer.OnTileAdded(idx, boardState[idx]);
    private void OnItemSet(int idx, TileState oldState) => _visualizer.OnTileSet(idx, oldState, boardState[idx]);
    private void OnItemRemoved(int idx, TileState removedTile) => _visualizer.OnTileRemoved(idx, removedTile);
    private void OnItemInserted(int idx) => _visualizer.OnTileInserted(idx, boardState[idx]);
    private void OnListCleared() => _visualizer.OnBoardCleared();

    private TileState GenerateNewTile()
    {
        return new TileState
        {
            uniqueID = _nextTileID++,
            tileType = UnityEngine.Random.Range(0, tileDatabase.allTileDefinitions.Count)
        };
    }
    //
    // [ClientRpc]
    // private void RpcAnimateSwap(Vector2Int posA, Vector2Int posB)
    // {
    //     // Called on ALL clients.
    //     // (The client who initiated the move can just ignore this)
    //     if (isLocalPlayer) return; // Example of ignoring if you're the one
    //     
    //     // _visualizer.AnimateSwap(posA, posB);
    // }
    //
    // [ClientRpc]
    // private void RpcAnimateMatch(List<ushort> matchedTileIDs)
    // {
    //     // Called on ALL clients.
    //     // _visualizer.AnimateMatch(matchedTileIDs);
    // }
    //
    
    [Server]
    public bool ProcessPlayerSwap(NetworkConnectionToClient sender, Vector2Int posA, Vector2Int posB)
    {
        // --- 1. Validation ---
        if (!IsValidSwap(posA, posB))
        {
            Debug.LogWarning($"[Server] Invalid swap: {posA} <-> {posB}. Not adjacent.");
            return false;
        }

        // --- 2. State Change ---
        // This function does all the work AND checks for matches
        bool didMatchOccur = AttemptSwapAndProcessBoard(posA, posB);
        if (didMatchOccur)
        {
            Debug.Log($"[Server] Swap {posA} <-> {posB} successful. Board processed.");
        }
        else
        {
            Debug.Log($"[Server] Swap {posA} <-> {posB} resulted in no match. This is totally fine.");
        }

        return true;
    }
    
    public bool IsValidSwap(Vector2Int posA, Vector2Int posB)
    {
        // Check bounds
        if (posA.x < 0 || posA.x >= BoardWidth || posA.y < 0 || posA.y >= BoardHeight ||
            posB.x < 0 || posB.x >= BoardWidth || posB.y < 0 || posB.y >= BoardHeight)
        {
            return false;
        }

        // Tiles must be different type, otherwise no effect.
        // Prevents accidental no-effect swaps. 
        if (GetTileAt(posA).tileType == GetTileAt(posB).tileType)
        {
            Debug.LogWarning($"[Client] Invalid swap: {posA} <-> {posB}. Tiles are same type.");
            return false;
        }

        if (GetTileAt(posA).tileType == -1 || GetTileAt(posB).tileType == -1)
        {
            Debug.LogWarning($"[Client] Invalid swap: {posA} <-> {posB}. at least one of the tiles is Empty.");
            // dont allow swapping with Empty tiles.
            return false;
        }
        
        // Check for adjacency (Manhattan distance == 1)
        int dist = Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
        return dist == 1;
    }
/// <summary>
    /// This is the main server-side game loop for a turn.
    /// returns bool: didMatchHappen
    /// </summary>
    [Server]
    private bool AttemptSwapAndProcessBoard(Vector2Int posA, Vector2Int posB)
    {
        // --- 1. Perform the initial swap ---
        int indexA = GetIndex(posA);
        int indexB = GetIndex(posB);
        TileState stateA = boardState[indexA];
        TileState stateB = boardState[indexB];

        boardState[indexA] = stateB;
        boardState[indexB] = stateA;

        
        // Check if this swap caused a match
        // We only need to check the rows/cols of the two tiles we moved
        var matchResults = FindMatchesAt(posA, posB);
        if (matchResults.Count == 0)
        {
            // TODO: Move didnt yield a Match, maybe do not allow and rollback the move.
            return false;
        }

        bool hasMoreMatches = true;
        while (hasMoreMatches)
        {
            ApplyMatchEffects(matchResults);

            // --- C. MATCH PHASE (Remove matched cards) ---
            // Set all matched positions to "Empty"
            foreach (var match in matchResults)
            {
                foreach (var pos in match.positions)
                {
                    boardState[GetIndex(pos)] = TileState.Empty;
                }
            }

            // --- D. ARRANGE PHASE (Let tiles fall) ---
            // SimulateTileFall();

            // --- E. REFILL PHASE (Refill the board) ---
            // RefillBoard();

            hasMoreMatches = false;
            // TODO: Check for new matches ---
            // allMatchedPositions = FindAllMatchesOnBoard();
            // if (allMatchedPositions.Count == 0)
            // {
            //     hasMoreMatches = false;
            // }
        }

        return true; // A match occurred
    }

// --- MATCH-FINDING HELPERS ---

    [Server]
    private List<MatchData> FindMatchesAt(params Vector2Int[] positions)
    {
        List<MatchData> foundMatches = new List<MatchData>();
        foreach (var pos in positions)
        {        
            MatchData horzMatch = FindMatchesInLine(pos, Vector2Int.right);
            MatchData vertMatch = FindMatchesInLine(pos, Vector2Int.down);

            if (horzMatch != null && vertMatch != null)
            {
                foundMatches.Add(horzMatch.matchCount > vertMatch.matchCount ? horzMatch : vertMatch);
            }
            if (horzMatch == null && vertMatch != null)
            {
                foundMatches.Add(vertMatch);
            }
            if (vertMatch == null && horzMatch != null)
            {
                foundMatches.Add(horzMatch);
            }
            // else both are null.
        }
        return foundMatches;
    }

    // [Server]
    // private HashSet<Vector2Int> FindAllMatchesOnBoard()
    // {
    //     // HashSet<Vector2Int> allMatches = new HashSet<Vector2Int>();
    //     // for (int y = 0; y < BoardHeight; y++)
    //     // {
    //     //     for (int x = 0; x < BoardWidth; x++)
    //     //     {
    //     //         FindMatchesInLine(new Vector2Int(x, y), new Vector2Int(1, 0), allMatches);
    //     //         FindMatchesInLine(new Vector2Int(x, y), new Vector2Int(0, 1), allMatches);
    //     //     }
    //     // }
    //     // return allMatches;
    // }

    
    [Server]
    private MatchData FindMatchesInLine(Vector2Int startPos, Vector2Int direction)
    {
        List<Vector2Int> candidateTiles = new List<Vector2Int>();
        int currentType = GetTileAt(startPos).tileType;
        
        candidateTiles.Add(startPos);
        // 2 units to the pos dir, 2 units to the neg.
        // no need to check 3 units away, as that would lead to a match BEFORE This.
        for (int i = 1; i <= 2; i++)
        {
            Vector2Int pos = startPos + direction * i;
            if (!IsPosInBounds(pos) || GetTileAt(pos).tileType != currentType)
            {
                break; // End of line or type mismatch
            }
            candidateTiles.Add(pos);
        }
        // check the negative direction (left or down)
        for (int i = 1; i <= 2; i++)
        {
            Vector2Int pos = startPos - direction * i;
            if (!IsPosInBounds(pos) || GetTileAt(pos).tileType != currentType)
            {
                break; // End of line or type mismatch
            }
            candidateTiles.Add(pos);
        }

        if (candidateTiles.Count >= 3)
        {
            MatchData result = new MatchData
            {
                tileTypeID = currentType,
                positions = candidateTiles,
                matchCount = candidateTiles.Count
            };
            return result;
        }
        else return null;
    }
    
    // --- BOARD PROCESSING HELPERS ---

    [Server]
    private void ApplyMatchEffects(List<MatchData> matchResults)
    {
        // This is where Card specific match effect will take place.
        foreach (var match in matchResults)
        {
            if (match.tileTypeID == 4) // e.g., '5' is your CHEST_TILE_ID
            {
                // TODO: OpenChest();
                // This could trigger another skill, which might
                // modify the board again. Be careful of recursive loops!
                // For now, let's keep it simple.
            }
            RpcApplyMatchEffect(match);
        }
    }

    [ClientRpc]
    private void RpcApplyMatchEffect(MatchData matchData)
    {
        Debug.Log($"Matched: {matchData.matchCount} of {matchData.ToString()}");
        
        // TODO: Notify visualizer of the match.
        // _visualizer.
    }

    [Server]
    private void SimulateTileFall()
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
                        if (!GetTileAt(new Vector2Int(x, yAbove)).IsEmpty())
                        {
                            // We found one! Move it down.
                            Debug.Log($"Move tile ({x},{y}) to ({x},{yAbove})");
                            boardState[GetIndex(x, y)] = GetTileAt(new Vector2Int(x, yAbove));
                            boardState[GetIndex(x, yAbove)] = TileState.Empty;
                            
                            // Break the inner 'yAbove' loop to continue
                            // checking the *current* 'y' position again.
                            break; 
                        }
                    }
                }
            }
        }
    }

    [Server]
    private void RefillBoard()
    {
        Debug.Log("Refilling board");
        // Per your rules: "bottom to top, then left to right"
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                if (GetTileAt(new Vector2Int(x, y)).IsEmpty())
                {
                    // This slot is empty, so fill it with a new tile
                    Debug.Log($"Draw new tile to pos: ({x},{y})");
                    boardState[GetIndex(x, y)] = GenerateNewTile();
                }
            }
        }
    }


    // --- COORDINATE & STATE HELPERS ---

    private int GetIndex(Vector2Int pos) => (pos.y * BoardWidth) + pos.x;
    private int GetIndex(int x, int y) => (y * BoardWidth) + x;
    private TileState GetTileAt(Vector2Int pos) => boardState[GetIndex(pos)];
    
    private bool IsPosInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < BoardWidth &&
               pos.y >= 0 && pos.y < BoardHeight;
    }
    // ... Server logic for checking matches, etc., goes here ...
}