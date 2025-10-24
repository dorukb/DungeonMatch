using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
namespace DorkyProductions
{
    
public class ClientBoardVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameBoard gameBoard; // Assign the networked GameBoard object

    [SerializeField]
    private TileDatabase tileDatabase; 

    [SerializeField]
    private GameObject tileViewPrefab; // A prefab with a SpriteRenderer and a TileView.cs script

    [SerializeField]
    private Transform boardContainer; // The parent to spawn tiles under
    
    [SerializeField]
    public PlayerInput playerInput;
    // This is our client-side lookup to connect a logical tile (by ID)
    // to its visual GameObject.
    private Dictionary<ushort, TileView> _visualTiles = new Dictionary<ushort, TileView>();
    
    [Header("Settings")]
    public const float SWAP_DURATION = 0.3f;
    public const float FALL_DURATION = 0.5f;
    
    void Start()
    {
        tileDatabase.Initialize();
    }
    
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

        // 2. Set its starting scale to 0 (so it's invisible)
        tileGO.transform.localScale = Vector3.zero;
        float spawnDuration = 1.0f;
        tileGO.transform.DOScale(1f, spawnDuration).SetEase(Ease.InQuint);
         
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
        
//         Debug.Log($"[VIZ] Added tile {state.uniqueID} type: {state.ToString()} at ");
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

    // public void RemoveTilesOnMatch(List<ushort> ids)
    // {
    //     foreach (ushort id in ids)
    //     {
    //         if (_visualTiles.TryGetValue(id, out TileView tileToPop))
    //         {
    //             // Play a pop animation and destroy it
    //             tileToPop.AnimatePop(); 
    //             _visualTiles.Remove(id);
    //         }
    //     }
    // }
    public Tween AnimateSwap(ushort firstTileID, ushort secondTileID)
    {
        TileView firstTileView = _visualTiles[firstTileID];
        TileView secondTileView = _visualTiles[secondTileID];
        
        Vector2Int firstGridPos = firstTileView.GridPosition;
        var tween1 = MoveTile(firstTileView, secondTileView.GridPosition, SWAP_DURATION);
        var tween2 = MoveTile(secondTileView, firstGridPos, SWAP_DURATION);
        
        
        // Create the tweens and add them to a Sequence
        Sequence s = DOTween.Sequence();
        s.Join(tween1);
        s.Join(tween2);
        
        return s; 
    }

    private Tween MoveTile(TileView tileToMove, Vector2Int newGridPos, float duration)
    {
        Vector2 newAnchoredPos = GetAnchoredPosition(newGridPos);
        tileToMove.GridPosition = newGridPos;
        
        return tileToMove.RectTransform.DOAnchorPos(newAnchoredPos, duration)
            .SetEase(Ease.OutCubic);
    }

    public Tween AnimateFall(ushort tileId, Vector2Int toPos)
    {
        if (_visualTiles.TryGetValue(tileId, out TileView tileView))
        {
            return MoveTile(tileView, toPos, FALL_DURATION);
        }
        else
        {
            Debug.LogError($"AnimateFall failed, tile ${tileId} not found");
            return null;
        }
    }
    // We'll use this for matches
    // public void AnimatePop(float duration = 0.2f)
    // {
    //     RectTransform.DOPunchScale(Vector3.one * 0.2f, duration, 10, 1)
    //         .OnComplete(() => Destroy(gameObject)); // Simple pop and destroy
    // }
    public float swapDuration = 0.4f;
    public float tileMoveDuration = 0.3f;
    
}
}