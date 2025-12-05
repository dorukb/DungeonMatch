using UnityEngine;

namespace DorkyProductions
{
    
// This is NOT a NetworkBehaviour. It's a simple, local input handler.
public class PlayerInput : MonoBehaviour
{
    public NetworkPlayer LocalPlayerController { get; private set; }
    private TileView _startTile;
    private bool _isDragging = false;
    private bool canSwap = false;
    private bool isLightningInputActive = false;
    private bool isPhantomInputActive = false;
    private void Awake()
    {
        canSwap = false;
        isLightningInputActive = false;
        isPhantomInputActive = false;
    }

    public void DisableSwapControls()
    {
        Debug.Log("Disabling controls");
        canSwap = false;
    }

    public void EnableControls()
    {
        Debug.Log("Enabling controls");
        canSwap = true;
    }

    public void SetPlayer(NetworkPlayer localPlayer)
    {
        LocalPlayerController = localPlayer;
    }
    
    public void OnTilePointerDown(TileView tile)
    {
        if (!canSwap) return;
        
        _startTile = tile;
        _isDragging = true;
    }

    public void OnTilePointerUp(TileView tile)
    {
        // This doesn't scale. Needs refactor.
        if (isLightningInputActive)
        {
            LocalPlayerController.OnTileSelectedForLightning(tile.GridPosition);
        }

        if (isPhantomInputActive)
        {
            LocalPlayerController.OnTileSelectedForPhantom(tile);
        }
        
        if (!canSwap || !_isDragging || _startTile == null) return;

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

    public void ActivateLightningInput()
    {
        isLightningInputActive = true;
    }
    public void DisableLightningInput()
    {
        isLightningInputActive = false;
    }
    public void ChangePhantomInputState(bool isActive)
    {
        isPhantomInputActive = isActive;
    }
}
}