using UnityEngine;
using Mirror;

// This is NOT a NetworkBehaviour. It's a simple, local input handler.
public class PlayerInput : MonoBehaviour
{
    private Camera _mainCamera;
    private Vector2Int _dragStartPos;
    private bool _isDragging = false;

    private NetworkPlayer _localPlayerController;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        if (_localPlayerController != null)
        {
            HandleInput();
        }
    }

    public void SetPlayer(NetworkPlayer localPlayer)
    {
        _localPlayerController = localPlayer;
    }
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int gridPos = GetGridPosFromWorld(Input.mousePosition);
            if (IsValidGridPos(gridPos))
            {
                _dragStartPos = gridPos;
                _isDragging = true;
            }
        }
        else if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            _isDragging = false;
            Vector2Int gridPos = GetGridPosFromWorld(Input.mousePosition);
            if (!IsValidGridPos(gridPos) || gridPos == _dragStartPos)
            {
                // Invalid drag or just a click
                return; 
            }

            // We have a start and end position.
            // Figure out the direction.
            Vector2Int dragEndPos = GetAdjacentPosFromDrag(_dragStartPos, gridPos);

            if (IsValidGridPos(dragEndPos))
            {
                // We have a valid swap request!
                // Tell our networked Player script to send the command.
                _localPlayerController.RequestSwap(_dragStartPos, dragEndPos);
            }
        }
    }

    // --- Helper Methods ---

    // This converts screen space to your grid coordinates.
    // This is HIGHLY dependent on your camera and board setup.
    private Vector2Int GetGridPosFromWorld(Vector2 screenPos)
    {
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
        // Assuming your board is at (0,0) and each tile is 1 unit
        // You MUST adjust this logic.
        int x = Mathf.RoundToInt(worldPos.x); 
        int y = Mathf.RoundToInt(worldPos.y);
        return new Vector2Int(x, y);
    }

    private bool IsValidGridPos(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < GameBoard.BoardWidth &&
               pos.y >= 0 && pos.y < GameBoard.BoardHeight;
    }

    // A simple way to get the intended swap direction
    private Vector2Int GetAdjacentPosFromDrag(Vector2Int start, Vector2Int end)
    {
        Vector2Int diff = end - start;
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            // Horizontal swipe
            return start + new Vector2Int(diff.x > 0 ? 1 : -1, 0);
        }
        else
        {
            // Vertical swipe
            return start + new Vector2Int(0, diff.y > 0 ? 1 : -1);
        }
    }
}