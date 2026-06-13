using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using CodeWriter.UIExtensions;
using Unity.VisualScripting;

namespace DorkyProductions
{
public class HumanPlayerInput : MonoBehaviour
{
    public NetworkPlayer LocalPlayerController { get; private set; }
    
    // Physical distance (inches) the finger must move to register a swipe.
    private float _swipeThresholdInches = 0.25f;

    // State Tracking
    private TileView _selectedTile;
    private TileView _pressedTile;
    private Vector2 _pressPosition;
    
    // Flags
    private bool _canSwap = false;
    private bool _isLightningInputActive = false;
    private bool _isPhantomInputActive = false;
    private bool _isPhaseShiftInputActive = false;
    private bool _isArcaneSweepInputActive = false;
    private bool _isArcaneCleaveInputActive = false;
    private bool _isTutorialInputActive = true;
    private float _dpi;

    private void Awake()
    {
        _canSwap = false;
        _dpi = Screen.dpi == 0 ? 96 : Screen.dpi;
    }

    private void Update()
    {
        //TODO :is this looking even good?
        
        if (!_canSwap && !_isLightningInputActive && !_isPhantomInputActive && !_isPhaseShiftInputActive 
            && !_isArcaneSweepInputActive && !_isArcaneCleaveInputActive) return;

        // -- 1. Detect Input Source --
        bool isPressed = false;
        bool wasPressed = false;
        bool wasReleased = false;
        Vector2 position = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isPressed = true;
            wasPressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            wasReleased = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            position = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPressed = true;
            wasPressed = Mouse.current.leftButton.wasPressedThisFrame;
            wasReleased = Mouse.current.leftButton.wasReleasedThisFrame;
            position = Mouse.current.position.ReadValue();
        }
        
        // Handle release frame
        if (!isPressed)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                wasReleased = true;
                position = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                wasReleased = true;
                position = Touchscreen.current.primaryTouch.position.ReadValue();
            }
        }

        // -- 2. Process States --
        if (wasPressed) HandleDown(position);
        else if (isPressed) HandleDrag(position); // Only updates state now
        else if (wasReleased) HandleUp(position); // Execution happens here
    }

    private void HandleDown(Vector2 screenPos)
    {
        TileView hit = RaycastForTile(screenPos);
        if (hit == null) return;

        if (_isLightningInputActive)
        {
            LocalPlayerController.OnTileSelectedForLightning(hit.GridPosition);
            return;
        }
        if (_isPhantomInputActive)
        {
            LocalPlayerController.OnTileSelectedForPhantom(hit);
            return;
        }
        if (_isPhaseShiftInputActive)
        {
            LocalPlayerController.OnTileSelectedForPhaseShift(hit);
            return;
        }

        if (_isArcaneSweepInputActive)
        {
            LocalPlayerController.OnTileSelectedForSweep(hit.GridPosition);
        }

        if (_isArcaneCleaveInputActive)
        {
            LocalPlayerController.OnTileSelectedForCleave(hit.GridPosition);
        }

        _pressedTile = hit;
        _pressPosition = screenPos;
    }

    private void HandleDrag(Vector2 screenPos)
    {
    }

    private void HandleUp(Vector2 screenPos)
    {
        if (_pressedTile == null) return;

        // Calculate final distance from the start point
        float dist = Vector2.Distance(_pressPosition, screenPos);
        float pixelThreshold = _swipeThresholdInches * _dpi;

        // Decision: Was this a Swipe or a Tap?
        // We use the distance at the moment of RELEASE. 
        // This allows the user to drag out, change their mind, drag back, and release to cancel.
        if (dist > pixelThreshold)
        {
            // It is a Swipe
            Vector2 dir = (screenPos - _pressPosition).normalized;
            AttemptSwipe(_pressedTile, dir);
        }
        else
        {
            // It is a Tap (even if they wiggled the finger a little bit)
            // We Raycast again to see if they released ON the same tile they started.
            TileView hit = RaycastForTile(screenPos);
            if (hit != null && hit == _pressedTile)
            {
                HandleTap(hit);
            }
            else
            {
                // Released over void or different tile but didn't drag enough to swipe
                // Just deselect to be safe/clean
                DeselectCurrent(); 
            }
        }

        ResetInputState();
    }

    private void AttemptSwipe(TileView startTile, Vector2 direction)
    {
        Vector2Int targetPos = startTile.GridPosition;

        // Direction Locking: Ensure we don't accidentally swap diagonally
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal
            targetPos.x += direction.x > 0 ? 1 : -1;
        }
        else
        {
            // Vertical
            targetPos.y += direction.y > 0 ? 1 : -1;
        }

        if (targetPos.x < 0 || targetPos.x > 4 || targetPos.y < 0 || targetPos.y > 4)
        {
            Debug.Log("Out of bounds swipe, ignored client-side.");
        }
        else
        {
            LocalPlayerController.RequestSwap(startTile.GridPosition, targetPos);
            DeselectCurrent(); 
        }
    }

    private void HandleTap(TileView tile)
    {
        if (_selectedTile == null)
        {
            SelectTile(tile);
        }
        else if (_selectedTile == tile)
        {
            DeselectCurrent();
        }
        else
        {
            if (IsAdjacent(_selectedTile, tile))
            {
                LocalPlayerController.RequestSwap(_selectedTile.GridPosition, tile.GridPosition);
                DeselectCurrent();
            }
            else
            {
                DeselectCurrent();
                SelectTile(tile);
            }
        }
    }

    // --- Helpers ---
    private void SelectTile(TileView tile)
    {
        _selectedTile = tile;
        _selectedTile.SetSelected(true);
    }
    
    private void DeselectCurrent()
    {
        if (_selectedTile != null)
        {
            _selectedTile.SetSelected(false);
            _selectedTile = null;
        }
    }
    
    
    private bool IsAdjacent(TileView a, TileView b)
    {
        int diff = Mathf.Abs(a.GridPosition.x - b.GridPosition.x) + 
                   Mathf.Abs(a.GridPosition.y - b.GridPosition.y);
        return diff == 1;
    }

    private void ResetInputState()
    {
        _pressedTile = null;
    }

    private TileView RaycastForTile(Vector2 screenPos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0) return null;


        foreach (var result in results)
        {
            var tile = result.gameObject.GetComponent<TileView>();

            if (tile != null && _isTutorialInputActive)
            {
                var tutObj = tile.gameObject.GetComponentInChildren<TutorialObject>();
                if (tutObj != null)
                {
                    Debug.Log("Hit tutorial object: " + tile.gameObject.name);
                    TutorialController.CompleteCurrentStep();
                    return tile;
                }
                else tile = null;
            }
            if (tile != null) return tile;
        }
        // var tutorialMask = results[0].gameObject.GetComponent<TutorialMask>();
        // if (tutorialMask != null)
        // {
        //     // allow tutorial mask to "consume" the hit event, blocking everything else.
        //     Debug.Log("Hit tutorial mask.");
        //     return null;
        // }
        //
       
        return null;
    }

    // --- Public API ---
    public void DisableSwapControls()
    {
        _canSwap = false;
        DeselectCurrent();
        ResetInputState();
    }
    public void EnableControls() => _canSwap = true;
    public void EnableTutorialControls() => _isTutorialInputActive = true;
    public void DisableTutorialControls() => _isTutorialInputActive = false;
    public void SetPlayer(NetworkPlayer p) => LocalPlayerController = p;
    public void ActivateLightningInput() => _isLightningInputActive = true;
    public void DisableLightningInput() => _isLightningInputActive = false;
    public void ChangePhantomInputState(bool s) => _isPhantomInputActive = s;
    public void ChangePhaseShiftInputState(bool s) => _isPhaseShiftInputActive = s;
    public void ChangeSweepInputState(bool s) => _isArcaneSweepInputActive = s;
    public void ChangeCleaveInputState(bool s) => _isArcaneCleaveInputActive = s;
}
}