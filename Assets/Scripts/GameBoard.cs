using System.Collections.Generic;
using Mirror;
using UnityEngine;

// It tells clients "A tile of this type with this unique ID exists."
public struct TileState
{
    public ushort uniqueID;
    public int tileType;    // 0=Attack, 1=Attackx2, 2=Shield, etc.
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
        // This script should only run on the server
        if (!isServer)
        {
            Debug.LogError("This shouldnt be called on Clients!");
            return;
        }
        
        // When the server starts, populate the initial board.
        // (Don't just add 25 items, you need to call .Add()
        // for each one to sync properly)
        for (int i = 0; i < 25; i++)
        {
            boardState.Add(GenerateNewTile());
        }
    }

    private TileState GenerateNewTile()
    {
        return new TileState
        {
            uniqueID = _nextTileID++,
            tileType = UnityEngine.Random.Range(0, tileDatabase.allTileDefinitions.Count)
        };
    }

    // This is the main server logic loop
    public void ProcessTurn(Vector2Int posA, Vector2Int posB)
    {
        // 1. Tell all clients to animate the *successful* swap.
        RpcAnimateSwap(posA, posB);

        // 2. Update server's internal state (the SyncList)
        int indexA = posA.y * 5 + posA.x;
        int indexB = posB.y * 5 + posB.x;
        var tileA = boardState[indexA];
        boardState[indexA] = boardState[indexB];
        boardState[indexB] = tileA;

        // 3. Run all match logic
        //    (This is your complex logic: find matches,
        //     clear tiles, calculate falls, add new tiles)
        
        // 3a. Find matches
        var matches = FindAllMatches();
        
        // 3b. Tell clients WHAT to "pop"
        // We send the *unique IDs* so clients know which GameObjects to pop.
        List<ushort> matchedIDs = GetIDsFromMatches(matches);
        RpcAnimateMatch(matchedIDs);

        // 3c. Update the SyncList with the FINAL board state
        // (after clearing matched tiles, falling, and spawning new ones)
        UpdateBoardStateAfterMatches(matches); 
    }

    private void UpdateBoardStateAfterMatches(List<ushort> matches)
    {
        throw new System.NotImplementedException();
    }

    private List<ushort> GetIDsFromMatches(List<ushort> matches)
    {
        throw new System.NotImplementedException();
    }

    private List<ushort> FindAllMatches()
    {
        return null;}
    

    [ClientRpc]
    private void RpcAnimateSwap(Vector2Int posA, Vector2Int posB)
    {
        // Called on ALL clients.
        // (The client who initiated the move can just ignore this)
        if (isLocalPlayer) return; // Example of ignoring if you're the one
        
        // _visualizer.AnimateSwap(posA, posB);
    }

    [ClientRpc]
    private void RpcAnimateMatch(List<ushort> matchedTileIDs)
    {
        // Called on ALL clients.
        // _visualizer.AnimateMatch(matchedTileIDs);
    }
    [Server]
public bool ProcessPlayerSwap(NetworkConnectionToClient sender, Vector2Int posA, Vector2Int posB)
{
    // --- 1. Validation ---
    if (!IsValidSwap(posA, posB))
    {
        Debug.LogWarning($"[Server] Invalid swap: {posA} <-> {posB}. Not adjacent or out of bounds.");
        return false;
    }

    // --- 2. State Change (Stubbed for now) ---
    // This is where we will actually modify the boardState SyncList
    // and trigger match-checking logic.
    ExecuteSwap(posA, posB);
    
    // --- 3. Broadcast (Stubbed for now) ---
    // After executing, we'd check for matches and send RPCs
    // e.g., RpcAnimateSwap(posA, posB);
    // e.g., RpcAnimateMatch(foundMatches);
    
    Debug.Log($"[Server] Executing swap: {posA} <-> {posB}");

    return true;
}

[Server]
private bool IsValidSwap(Vector2Int posA, Vector2Int posB)
{
    // Check bounds
    if (posA.x < 0 || posA.x >= BoardWidth || posA.y < 0 || posA.y >= BoardHeight ||
        posB.x < 0 || posB.x >= BoardWidth || posB.y < 0 || posB.y >= BoardHeight)
    {
        return false;
    }

    // Check for adjacency (Manhattan distance == 1)
    int dist = Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
    return dist == 1;
}

[Server]
private void ExecuteSwap(Vector2Int posA, Vector2Int posB)
{
    // This is the core logic. We swap the items
    // in the SyncList. This change will automatically
    // propagate to all clients.
    int indexA = (posA.y * BoardWidth) + posA.x;
    int indexB = (posB.y * BoardWidth) + posB.x;

    TileState stateA = boardState[indexA];
    TileState stateB = boardState[indexB];

    // This 'set' operation is what clients will receive
    // in their SyncList.Callback
    boardState[indexA] = stateB;
    boardState[indexB] = stateA;
    Debug.LogWarning($"[Server] Invalid swap: {posA} <-> {posB}. Not adjacent or out of bounds.");

    // TODO: After this, we must:
    // 1. Check for matches starting from posA and posB.
    // 2. If NO matches, swap them back! (An invalid move)
    //    - boardState[indexA] = stateA;
    //    - boardState[indexB] = stateB;
    //    - And return false from ProcessPlayerSwap so it sends the TargetRpcRevert.
    // 3. If there ARE matches, proceed to clear tiles, etc.
}
    // ... Server logic for checking matches, etc., goes here ...
}