using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class ClientBoardVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameBoard gameBoard; // Assign the networked GameBoard object

    [SerializeField]
    private TileDatabase tileDatabase; // Assign the same TileDatabase SO

    [SerializeField]
    private GameObject tileViewPrefab; // A prefab with a SpriteRenderer and a TileView.cs script

    [SerializeField]
    private Transform boardContainer; // The parent to spawn tiles under
    
    [SerializeField]
    public PlayerInput playerInput;
    // This is our client-side lookup to connect a logical tile (by ID)
    // to its visual GameObject.
    private Dictionary<ushort, TileView> _visualTiles = new Dictionary<ushort, TileView>();
    
    void Start()
    {
        tileDatabase.Initialize();
    }
// REBUILT: Spawns the UI prefab
    private void SpawnVisualTile(TileState state, int index)
    {
        Vector2Int gridPos = GetGridPosFromIndex(index);
        TileDefinitionSO def = tileDatabase.GetTileByType(state.tileType);
        if (def == null)
        {
            Debug.LogError($"[VIZ] Tile definition not found for type: {state.tileType}");
            return;
        }

        // 1. Locally instantiate as a child of the board container
        GameObject tileGO = Instantiate(tileViewPrefab, gameBoard.boardContainer);
        tileGO.name = $"Tile (ID: {state.uniqueID}, Type: {state.tileType})";

        // 2. Setup the RectTransform Spawn Position
        RectTransform rt = tileGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = gameBoard.tileViewSize;
        
        Vector2 anchoredPos = GetAnchoredPosition(gridPos);
        rt.anchoredPosition = anchoredPos;
        
        TileView tileView = tileGO.GetComponent<TileView>();
        tileView.Initialize(def, gridPos, playerInput);
        
        // 5. Add to our dictionary for tracking
        _visualTiles[state.uniqueID] = tileView;
    }

    private void HandleTileMove(TileState newState, int newIndex)
    {
        if (_visualTiles.TryGetValue(newState.uniqueID, out TileView tileView))
        {
            Vector2Int newGridPos = GetGridPosFromIndex(newIndex);
            
            // Update the tile's internal logical position
            tileView.GridPosition = newGridPos; 
            Vector2 newAnchoredPos = GetAnchoredPosition(newGridPos);
            tileView.MoveToPosition(newAnchoredPos);
        }
        else
        {
            Debug.LogError("Moved tile not found in visuals.");
        }
    }

    // --- Public methods for Prediction / Reverting ---
    
    public void AnimatePredictedSwap(Vector2Int posA, Vector2Int posB)
    {
        // Find the TileViews at these grid positions
        TileView tileA = GetTileViewAt(posA);
        TileView tileB = GetTileViewAt(posB);

        if (tileA != null && tileB != null)
        {
            // Get their target UI positions
            Vector2 posA_UI = GetAnchoredPosition(posA);
            Vector2 posB_UI = GetAnchoredPosition(posB);

            // Tell them to swap
            tileA.MoveToPosition(posB_UI);
            tileB.MoveToPosition(posA_UI);
            
            // Update their internal grid positions
            tileA.GridPosition = posB;
            tileB.GridPosition = posA;
        }
    }

    public void AnimateSwapBack(Vector2Int posA, Vector2Int posB)
    {
        // The tiles are *visually* at posB and posA now.
        // We need to tell them to move *back* to posA and posB.
        TileView tileA = GetTileViewAt(posB); // Tile A is now at B
        TileView tileB = GetTileViewAt(posA); // Tile B is now at A

        if (tileA != null && tileB != null)
        {
            Vector2 posA_UI = GetAnchoredPosition(posA);
            Vector2 posB_UI = GetAnchoredPosition(posB);

            // Use the "swap back" animation
            tileA.AnimateSwapBack(posA_UI);
            tileB.AnimateSwapBack(posB_UI);
            
            // Reset their internal grid positions
            tileA.GridPosition = posA;
            tileB.GridPosition = posB;
        }
    }


    // --- Helper Methods ---

    // Converts a 1D list index to a 2D grid position
    private Vector2Int GetGridPosFromIndex(int index)
    {
        return new Vector2Int(index % GameBoard.BoardWidth, index / GameBoard.BoardHeight);
    }
    
    // This is the core layout logic
    public Vector2 GetAnchoredPosition(Vector2Int gridPos)
    {
        Vector2 fullTileSize = gameBoard.tileViewSize + gameBoard.tileSpacing;

        // This calculation centers the grid (0,0) at the container's pivot
        float x = (gridPos.x - (GameBoard.BoardWidth - 1) / 2.0f) * fullTileSize.x;
        float y = (gridPos.y - (GameBoard.BoardHeight - 1) / 2.0f) * fullTileSize.y;

        return new Vector2(x, y);
    }

    public TileView GetTileViewAt(Vector2Int gridPos)
    {
        // This is inefficient, but simple.
        // A 2D array of TileViews would be faster.
        foreach (var tileView in _visualTiles.Values)
        {
            if (tileView.GridPosition == gridPos)
            {
                return tileView;
            }
        }
        return null;
    }

    public void OnBoardCleared()
    {
        _visualTiles.Clear();
    }

    public void OnTileChanged(int idx, TileState newTile)
    {
        Debug.Log($"[VIZ] {idx} now has {newTile.uniqueID} type: {newTile.tileType} at idx: {idx}");
        HandleTileMove(newTile, idx);
    }

    public void OnTileRemoved(int idx, TileState removedTile)
    {
        Debug.Log($"[VIZ] Removed (due to match or special effects) tile: {removedTile.uniqueID}");
        Debug.LogError("[VIZ] Remove tile not implemented yet.");
    }

    public void OnTileInserted(int idx, TileState insertedTile)
    {
        // We might have already spawned this from PopulateInitialBoardVisuals
        if (_visualTiles.ContainsKey(insertedTile.uniqueID))
        {
            Debug.Log($"[VIZ] The tile {insertedTile.uniqueID} already exists, due to init? ignoring INSERT request");
            return;
        }
        
        // This is a new tile falling in.
        // For now, just spawn it. We can animate it "falling" later.
        Debug.Log($"[VIZ]Inserted tile {insertedTile.uniqueID} type: {insertedTile.tileType} at idx: {idx}");
        SpawnVisualTile(insertedTile, idx);
    }

    public void OnTileAdded(int idx, TileState addedTile)
    {    
        // TODO: Decide whether we consider Insertion & Add different events.
        // so far, they lead to exactly the same code path.
        // but insertion seems tobe related to "Refill board"
        // whereas Add is about "Fill the board with initial tiles"
        
        // We might have already spawned this from PopulateInitialBoardVisuals
        if (_visualTiles.ContainsKey(addedTile.uniqueID))
        {
            Debug.Log($"[VIZ] The tile {addedTile.uniqueID} already exists, due to init? ignoring ADD request");
            return;
        }
        
        // This is a new tile falling in.
        // For now, just spawn it. We can animate it "falling" later.
        Debug.Log($"[VIZ] Added tile {addedTile.uniqueID} type: {addedTile.tileType} at idx: {idx}");
        SpawnVisualTile(addedTile, idx);
    }
}