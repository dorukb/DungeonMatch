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

    // This is our client-side lookup to connect a logical tile (by ID)
    // to its visual GameObject.
    private Dictionary<ushort, GameObject> _visualTiles = new Dictionary<ushort, GameObject>();

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
                HandleTileRemove(oldState);
                break;
        }
    }

    private void SpawnVisualTile(TileState state, int index)
    {
        // Get the static data (like the sprite)
        TileDefinitionSO def = tileDatabase.GetTileByType(state.tileType);
        if (def == null) return;

        // Calculate its grid position
        Vector2 pos = GetWorldPosition(index);

        // Spawn the prefab
        GameObject tileGO = Instantiate(tileViewPrefab, pos, Quaternion.identity, boardContainer);
        tileGO.name = $"Tile (ID: {state.uniqueID}, Type: {state.tileType})";

        // Set its sprite
        tileGO.GetComponent<TileView>().Initialize(def);
        
        // Add to our dictionary for tracking
        _visualTiles[state.uniqueID] = tileGO;
    }

    private Vector2 GetWorldPosition(int index)
    {
        // Convert 1D index to 2D grid position
        int x = index % GameBoard.BoardWidth;
        int y = index / GameBoard.BoardWidth;
        
        // You'll want to adjust this based on your board's
        // position, cell size, and centering.
        return new Vector2(x, y); 
    }

    // --- Placeholders for next steps ---
    private void HandleTileMove(TileState newState, int newIndex)
    {
        // TODO: Find tile by newState.uniqueID, tell it to animate
        // from its old position to its new one.
        Debug.Log($"Tile {newState.uniqueID} moved to index {newIndex}");
    }

    private void HandleTileRemove(TileState oldState)
    {
        // TODO: Find tile by oldState.uniqueID, play a "pop" animation,
        // and then Destroy() it.
        Debug.Log($"Tile {oldState.uniqueID} was removed");
    }
}