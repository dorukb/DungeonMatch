using UnityEngine;
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
    public void SpawnVisualTile(TileState state, Vector2Int gridPos)
    {
        TileDefinitionSO def = tileDatabase.GetTileByType(state.tileType);
        if (def == null)
        {
            Debug.LogError($"[VIZ] Tile definition not found for type: {state.tileType}");
            return;
        }

        // 1. Locally instantiate as a child of the board container
        GameObject tileGO = Instantiate(tileViewPrefab, gameBoard.boardContainer);

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
        
        Debug.Log($"[VIZ] Added tile {state.uniqueID} type: {state.ToString()} at ");
    }
    
    public Vector2 GetAnchoredPosition(Vector2Int gridPos)
    {
        Vector2 fullTileSize = gameBoard.tileViewSize + gameBoard.tileSpacing;

        // This calculation centers the grid (0,0) at the container's pivot
        float x = (gridPos.x - (GameBoard.BoardWidth - 1) / 2.0f) * fullTileSize.x;
        float y = (gridPos.y - (GameBoard.BoardHeight - 1) / 2.0f) * fullTileSize.y;

        return new Vector2(x, y);
    }
    public void OnBoardCleared()
    {
        _visualTiles.Clear();
    }

    public void RemoveTilesOnMatch(List<ushort> ids)
    {
        foreach (ushort id in ids)
        {
            if (_visualTiles.TryGetValue(id, out TileView tileToPop))
            {
                // Play a pop animation and destroy it
                tileToPop.AnimatePop(); 
                _visualTiles.Remove(id);
            }
        }
    }

    public void AnimateSwap(ushort firstTileID, ushort secondTileID)
    {
        TileView firstTileView = _visualTiles[firstTileID];
        TileView secondTileView = _visualTiles[secondTileID];
        
        Vector2Int firstGridPos = firstTileView.GridPosition;
        MoveTile(firstTileView, secondTileView.GridPosition);
        MoveTile(secondTileView, firstGridPos);
    }

    private void MoveTile(TileView tileToMove, Vector2Int newGridPos)
    {
        Vector2 newAnchoredPos = GetAnchoredPosition(newGridPos);
        tileToMove.GridPosition = newGridPos;
        tileToMove.MoveToPosition(newAnchoredPos, 0.2f);
    }

    public void AnimateFall(TileState movedTile, Vector2Int toPos)
    {
        if (_visualTiles.TryGetValue(movedTile.uniqueID, out TileView tileView))
        {
            Vector2 newAnchoredPos = GetAnchoredPosition(toPos);
            tileView.GridPosition = toPos;
            tileView.MoveToPosition(newAnchoredPos, 0.2f); // Faster fall
        }
    }
}