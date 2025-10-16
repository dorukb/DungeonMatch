using UnityEngine;
using Mirror;

// Enum to represent the different types of tiles.
// Add as many as you need. Start with at least 3.
public enum TileType
{
    Empty,
    Red,
    Green,
    Blue,
    Yellow,
    Purple
}

public class GameBoard : NetworkBehaviour
{
    [Header("Board Dimensions")]
    public int width = 5;
    public int height = 5;

    [Header("Prefabs")]
    [Tooltip("Assign the prefab for the visual representation of a tile")]
    public GameObject tilePrefab;

    // The SyncList is the core of the networking.
    // It's a list that automatically synchronizes its contents from the server to all clients.
    // We will store the state of the entire board in this single list.
    private readonly SyncList<TileType> boardTiles = new SyncList<TileType>();

    // This will hold the visual GameObjects for the tiles on each client.
    // It is not networked, as each client manages its own visual objects.
    private GameObject[,] tileObjects;

    #region Server-Only Logic

    // This runs on the server when the object is created.
    public override void OnStartServer()
    {
        base.OnStartServer();
        tileObjects = new GameObject[width, height];
        InitializeBoard();
    }

    /// <summary>
    /// [Server] Fills the board with initial random tiles.
    /// </summary>
    [Server]
    private void InitializeBoard()
    {
        boardTiles.Clear();
        for (int i = 0; i < width * height; i++)
        {
            // Add a random tile type to the SyncList.
            // Exclude 'Empty' (starting from 1).
            boardTiles.Add((TileType)Random.Range(1, System.Enum.GetValues(typeof(TileType)).Length));
        }
    }

    /// <summary>
    /// [Server] The main logic for handling a player's move request.
    /// </summary>
    [Server]
    private void HandleSwap(Vector2Int pos1, Vector2Int pos2)
    {
        // 1. Validate the swap (e.g., are they adjacent?)
        if (!IsSwapValid(pos1, pos2))
        {
            // Invalid move, do nothing.
            // You might want to send an error message back to the specific client.
            return;
        }

        // 2. Perform the swap in the SyncList
        int index1 = To1D(pos1.x, pos1.y);
        int index2 = To1D(pos2.x, pos2.y);

        TileType temp = boardTiles[index1];
        boardTiles[index1] = boardTiles[index2];
        boardTiles[index2] = temp;

        // 3. Check for matches
        // Note: You need to implement the match-finding logic.
        // This is a complex part of a match-3 game.
        if (CheckForMatches())
        {
            // If matches are found:
            // - Clear matched tiles (set to Empty)
            // - Add score
            // - Shift tiles down
            // - Refill the board
            // The SyncList changes will automatically propagate to clients.
        }
        else
        {
            // No match found, swap back immediately.
            temp = boardTiles[index1];
            boardTiles[index1] = boardTiles[index2];
            boardTiles[index2] = temp;
        }
    }

    [Server]
    private bool IsSwapValid(Vector2Int pos1, Vector2Int pos2)
    {
        // Basic adjacency check
        return (Mathf.Abs(pos1.x - pos2.x) == 1 && pos1.y == pos2.y) ||
               (Mathf.Abs(pos1.y - pos2.y) == 1 && pos1.x == pos2.x);
    }

    [Server]
    private bool CheckForMatches()
    {
        // Placeholder for your match-finding logic.
        // This should iterate through the `boardTiles` list and find chains of 3 or more.
        Debug.Log("Server is checking for matches...");
        return true; // Assume a match is found for demonstration
    }

    #endregion

    #region Client-Side Logic

    // This is called on clients when the object is created.
    public override void OnStartClient()
    {
        base.OnStartClient();

        tileObjects = new GameObject[width, height];

        // The callback is crucial. It tells the client what to do whenever the SyncList changes.
        boardTiles.Callback += OnBoardUpdated;

        // When a client joins, the board might already exist.
        // This initial loop renders the board in its current state.
        for (int i = 0; i < boardTiles.Count; i++)
        {
            Vector2Int pos = To2D(i);
            CreateTileObject(boardTiles[i], pos.x, pos.y);
        }
    }

    /// <summary>
    /// This is the heart of the client-side visuals. It fires on every client
    /// whenever the 'boardTiles' SyncList is modified on the SERVER.
    /// </summary>
    private void OnBoardUpdated(SyncList<TileType>.Operation op, int index, TileType oldItem, TileType newItem)
    {
        Vector2Int pos = To2D(index);

        switch (op)
        {
            case SyncList<TileType>.Operation.OP_ADD:
                // A new item was added (used during initial setup)
                CreateTileObject(newItem, pos.x, pos.y);
                break;
            case SyncList<TileType>.Operation.OP_SET:
                // An item was changed (most common operation: swaps, clearing, refilling)
                UpdateTileObject(newItem, pos.x, pos.y);
                break;
            case SyncList<TileType>.Operation.OP_REMOVEAT:
                // An item was removed (not typically used in this board setup)
                Destroy(tileObjects[pos.x, pos.y]);
                break;
            case SyncList<TileType>.Operation.OP_CLEAR:
                // The whole list was cleared
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (tileObjects[x, y] != null)
                        {
                           Destroy(tileObjects[x, y]);
                        }
                    }
                }
                break;
        }
    }

    /// <summary>
    /// [Client] Instantiates a new tile prefab.
    /// </summary>
    private void CreateTileObject(TileType type, int x, int y)
    {
        if (tileObjects[x, y] != null) return; // Already exists

        GameObject newTile = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
        newTile.name = $"Tile ({x}, {y})";
        tileObjects[x, y] = newTile;
        
        // Get the Tile component to set its visual properties
        Tile tileComponent = newTile.GetComponent<Tile>();
        if (tileComponent != null)
        {
            tileComponent.SetTile(type, x, y);
        }
    }

    /// <summary>
    /// [Client] Updates an existing tile's visuals.
    /// </summary>
    private void UpdateTileObject(TileType type, int x, int y)
    {
        if (tileObjects[x, y] == null)
        {
             CreateTileObject(type, x, y);
             return;
        }
        
        Tile tileComponent = tileObjects[x, y].GetComponent<Tile>();
        if (tileComponent != null)
        {
            tileComponent.SetTile(type, x, y);
        }
    }


    #endregion

    #region Player Commands

    // A Command is a message sent from a Client to the Server.
    [Command(requiresAuthority = false)] // `requiresAuthority=false` lets any player call this
    public void CmdSwapTiles(Vector2Int pos1, Vector2Int pos2)
    {
        // This code runs ONLY on the server.
        HandleSwap(pos1, pos2);
    }

    #endregion

    #region Helper Functions

    // Converts a 2D board position to a 1D list index.
    private int To1D(int x, int y)
    {
        return y * width + x;
    }

    // Converts a 1D list index back to a 2D board position.
    private Vector2Int To2D(int index)
    {
        return new Vector2Int(index % width, index / width);
    }

    #endregion
}

