using Mirror;
using UnityEngine;

public class PlayerInput : NetworkBehaviour
{
    // We need a reference to the one and only GameBoard in the scene.
    private GameBoard gameBoard;
    private Camera mainCamera;

    private Vector2Int? firstSelectedTile = null;

    void Start()
    {
        // Find the board and camera on start.
        gameBoard = FindObjectOfType<GameBoard>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Only the local player should be able to control the board.
        if (!isLocalPlayer) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection();
        }
    }

    void HandleSelection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
            {
                Vector2Int selectedPos = new Vector2Int(tile.x, tile.y);

                if (firstSelectedTile == null)
                {
                    // This is the first tile selected.
                    firstSelectedTile = selectedPos;
                    // Optional: Add a visual indicator (like highlighting)
                    Debug.Log($"Selected first tile at: {selectedPos}");
                }
                else
                {
                    // This is the second tile. Send the command to the server.
                    Debug.Log($"Selected second tile at: {selectedPos}. Sending swap command.");
                    
                    // We have both tiles, so we call the Command on the GameBoard.
                    // The server will then validate and process this move.
                    if (gameBoard != null)
                    {
                        gameBoard.CmdSwapTiles(firstSelectedTile.Value, selectedPos);
                    }

                    // Reset selection
                    firstSelectedTile = null;
                }
            }
        }
        else
        {
             // Clicked on empty space, reset selection
             firstSelectedTile = null;
        }
    }
}
