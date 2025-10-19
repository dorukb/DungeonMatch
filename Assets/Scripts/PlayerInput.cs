using UnityEngine;

// This is NOT a NetworkBehaviour. It's a simple, local input handler.
public class PlayerInput : MonoBehaviour
{
    public NetworkPlayer LocalPlayerController { get; private set; }
    private TileView _startTile;
    private bool _isDragging = false;

    public bool IsEnabled { get; private set; }

    private void Start()
    {
        IsEnabled = false;
    }

    void Update()
    {
        // TODO: Add isMyTurn check.
        if (LocalPlayerController != null)
        {
            IsEnabled = true;
        }
    }

    public void SetPlayer(NetworkPlayer localPlayer)
    {
        LocalPlayerController = localPlayer;
    }
    
    // Called by TileView.OnPointerDown()
    public void OnTilePointerDown(TileView tile)
    {
        if (!enabled || _isDragging) return; // Ignore if not our turn or already dragging
        
        _startTile = tile;
        _isDragging = true;
    }

    // Called by TileView.OnPointerUp()
    public void OnTilePointerUp(TileView tile)
    {
        if (!_isDragging || _startTile == null) return;
        
        _isDragging = false;
        
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

    // Optional: If you want to detect swaps by dragging *over*
    // a tile instead of releasing on it.
    // public void OnTilePointerEnter(TileView tile)
    // {
    //     if (!_isDragging || _startTile == null || tile == _startTile)
    //     {
    //         // Not dragging, or no start tile, or it's the same tile
    //         return;
    //     }
    //
    //     // This is a drag-swap
    //     OnTilePointerUp(tile); 
    // }
}