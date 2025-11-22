using UnityEngine;

namespace DorkyProductions
{
    
// This is NOT a NetworkBehaviour. It's a simple, local input handler.
public class PlayerInput : MonoBehaviour
{
    public NetworkPlayer LocalPlayerController { get; private set; }
    private TileView _startTile;
    private bool _isDragging = false;
    private bool isEnabled = false;

    private void Awake()
    {
        isEnabled = false;
    }

    public void DisableControls()
    {
        Debug.Log("Disabling controls");
        isEnabled = false;
    }

    public void EnableControls()
    {
        Debug.Log("Enabling controls");
        isEnabled = true;
    }

    public void SetPlayer(NetworkPlayer localPlayer)
    {
        LocalPlayerController = localPlayer;
    }
    
    public void OnTilePointerDown(TileView tile)
    {
        if (!isEnabled) return;
        
        _startTile = tile;
        _isDragging = true;
    }

    public void OnTilePointerUp(TileView tile)
    {
        if (!isEnabled || !_isDragging || _startTile == null) return;

        _isDragging = false;
        if (tile == null)
        {
            _startTile = null;
            return;
        }
        
        // Check if we released on the same tile
        if (tile == _startTile)
        {
            _startTile = null;
            return; // It was just a click, not a swipe
        }

        // Check for adjacency (we can ask the TileView for its grid pos)
        Vector2Int posA = _startTile.GridPosition;
        Vector2Int posB = tile.GridPosition;

        int dist = Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
        
        if (dist == 1)
        {
            // Valid adjacent swipe!
            // Tell our Player script to send the command
            LocalPlayerController.RequestSwap(posA, posB);
        }
        
        _startTile = null;
    }
}
}