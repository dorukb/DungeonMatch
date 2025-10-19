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
        gameBoard.boardState.Callback += OnBoardStateChanged;
        PopulateInitialBoardVisuals();
    }

    private void OnDestroy()
    {
        // Always unsubscribe
        if (gameBoard != null)
        {
            gameBoard.boardState.Callback -= OnBoardStateChanged;
        }
    }

    // Spawn visuals for tiles that are *already* in the SyncList when we join
    private void PopulateInitialBoardVisuals()
    {
        for (int i = 0; i < gameBoard.boardState.Count; i++)
        {
            SpawnVisualTile(gameBoard.boardState[i], i);
        }
    }

    // This is the "magic" that reacts to server state changes
    private void OnBoardStateChanged(SyncList<TileState>.Operation op, int index, TileState oldState, TileState newState)
    {
        switch (op)
        {
            // OP_ADD is called for the initial population
            // and for any *new* tiles added later (e.g., from falling)
            case SyncList<TileState>.Operation.OP_ADD:
                // We might have already spawned this from PopulateInitialBoardVisuals
                if (_visualTiles.ContainsKey(newState.uniqueID)) break;
                
                SpawnVisualTile(newState, index);
                break;

            // We'll handle these in the next steps
            case SyncList<TileState>.Operation.OP_SET:
                // A tile's data changed at this index
                // This is how we handle tile *movement* (falling, swapping)
                HandleTileMove(newState, index);
                break;
                
            case SyncList<TileState>.Operation.OP_REMOVEAT:
                // A tile was removed (e.g., matched)
                Debug.LogError("Remove tile not implemented yet.");
                // HandleTileRemove(oldState);
                break;
        }
    }
// REBUILT: Spawns the UI prefab
    private void SpawnVisualTile(TileState state, int index)
    {
        Vector2Int gridPos = GetGridPosFromIndex(index);
        TileDefinitionSO def = tileDatabase.GetTileByType(state.tileType);
        if (def == null) return;

        // 1. Instantiate as a child of the board container
        GameObject tileGO = Instantiate(tileViewPrefab, gameBoard.boardContainer);
        tileGO.name = $"Tile (ID: {state.uniqueID}, Type: {state.tileType})";

        // 2. Setup its RectTransform
        RectTransform rt = tileGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = gameBoard.tileViewSize;
        
        // 3. Set its initial position
        Vector2 anchoredPos = GetAnchoredPosition(gridPos);
        rt.anchoredPosition = anchoredPos;
        
        // 4. Initialize the TileView
        TileView tileView = tileGO.GetComponent<TileView>();
        tileView.Initialize(def, gridPos, playerInput);
        
        // 5. Add to our dictionary for tracking
        _visualTiles[state.uniqueID] = tileView;
    }

    // REBUILT: This now handles tile *movement*
    private void HandleTileMove(TileState newState, int newIndex)
    {
        if (_visualTiles.TryGetValue(newState.uniqueID, out TileView tileView))
        {
            Vector2Int newGridPos = GetGridPosFromIndex(newIndex);
            
            // Update the tile's internal logical position
            tileView.GridPosition = newGridPos; 
            
            // Get the new UI position
            Vector2 newAnchoredPos = GetAnchoredPosition(newGridPos);
            
            // Tell the tile to animate to its new spot
            tileView.MoveToPosition(newAnchoredPos);
        }
        else
        {
            _visualTiles[newState.uniqueID] = tileView;
            // This is a new tile falling in.
            // For now, just spawn it. We can animate it "falling" later.
            SpawnVisualTile(newState, newIndex);
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
}